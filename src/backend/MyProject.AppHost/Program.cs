using Aspire.Hosting.Docker.Resources.ComposeNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes;
using Aspire.Hosting.Docker.Resources.ServiceNodes.Swarm;

var builder = DistributedApplication.CreateBuilder(args);

var frontendPort = int.TryParse(builder.Configuration["Ports:Frontend"], out var fp) ? fp : 13000;
var apiPort = int.TryParse(builder.Configuration["Ports:Api"], out var ap) ? ap : 13002;

// Derive infrastructure ports from the base (frontend) port so the entire
// stack lives in one predictable range - no collisions between projects.
var pgAdminPort = frontendPort + 3;
var postgresPort = frontendPort + 4;
var minioPort = frontendPort + 5;
var minioConsolePort = frontendPort + 6;
var mailpitSmtpPort = frontendPort + 7;
var mailpitHttpPort = frontendPort + 8;

// ── Docker Compose Publisher ──────────────────────────────────────────────────
// Generates production-ready docker-compose.yaml from this app model.
// Run: aspire publish --project src/backend/MyProject.AppHost -o deploy/compose
// The generated file replaces hand-maintained Docker Compose files entirely.

builder.AddDockerComposeEnvironment("{INIT_PROJECT_SLUG}")
    .WithDashboard(false)
    .ConfigureComposeFile(compose =>
    {
        // Network isolation: frontend can only reach API, not DB/storage directly
        compose.AddNetwork(new Network { Name = "frontend", Driver = "bridge" });
        compose.AddNetwork(new Network { Name = "backend", Driver = "bridge" });
    })
    .ConfigureEnvFile(env =>
    {
        // Docker images - operator sets these to their registry/tag
        env["API_IMAGE"] = new() { Name = "API_IMAGE", Description = "API Docker image (e.g. registry.example.com/api:latest)" };
        env["FRONTEND_IMAGE"] = new() { Name = "FRONTEND_IMAGE", Description = "Frontend Docker image (e.g. registry.example.com/frontend:latest)" };
        // Host ports
        env["API_PORT"] = new() { Name = "API_PORT", Description = "Host port for API", DefaultValue = "8080" };
        env["FRONTEND_PORT"] = new() { Name = "FRONTEND_PORT", Description = "Host port for frontend", DefaultValue = "3000" };
        // Frontend config - operator fills these in
        env["ORIGIN"] = new() { Name = "ORIGIN", Description = "Public domain (e.g. https://your-domain.com)" };
        env["TURNSTILE_SITE_KEY"] = new() { Name = "TURNSTILE_SITE_KEY", Description = "Cloudflare Turnstile site key" };
        env["XFF_DEPTH"] = new() { Name = "XFF_DEPTH", Description = "Reverse proxy hops (1 behind nginx, 2 behind LB + nginx)", DefaultValue = "0" };
        env["ALLOWED_ORIGINS"] = new() { Name = "ALLOWED_ORIGINS", Description = "Additional CSRF origins (comma-separated)" };
    });

// ── Infrastructure ──────────────────────────────────────────────────────────
// Container resources use session lifetime (default) - containers stop on
// Ctrl+C and restart on next run. Named data volumes persist across restarts,
// so database and file data survive. Explicit passwords ensure new containers
// can mount existing volumes without credential mismatch.

var pgPassword = builder.AddParameter("postgres-password", secret: true);
var storageUser = builder.AddParameter("storage-user");
var storagePassword = builder.AddParameter("storage-password", secret: true);
var jwtSecret = builder.AddParameter("jwt-secret", secret: true);

var postgres = builder.AddPostgres("db", password: pgPassword)
    .WithEndpoint("tcp", e => e.Port = postgresPort)
    .WithDataVolume("{INIT_PROJECT_SLUG}-db-data")
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Image = "postgres:15-alpine";
        service.Expose.Clear();
        service.Networks = ["backend"];
        ApplyHardened(service, readOnly: false);
        service.Deploy!.Resources!.Limits = new ResourceSpec { Cpus = "1.0", Memory = "1G" };
        service.Healthcheck = new Healthcheck
        {
            Test = ["CMD-SHELL", "pg_isready -U $$POSTGRES_USER"],
            Interval = "5s",
            Timeout = "3s",
            Retries = 5,
            StartPeriod = "15s"
        };
    });

// pgAdmin only for local development
if (builder.ExecutionContext.IsRunMode)
{
    postgres.WithPgAdmin(pgAdmin => pgAdmin.WithEndpoint("http", e => e.Port = pgAdminPort));
}

var db = postgres.AddDatabase("Database");

var storage = builder.AddMinioContainer("storage", rootUser: storageUser, rootPassword: storagePassword)
    .WithEndpoint("http", e => e.Port = minioPort)
    .WithEndpoint("console", e => e.Port = minioConsolePort)
    .WithDataVolume("{INIT_PROJECT_SLUG}-storage-data")
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Expose.Clear();
        service.Networks = ["backend"];
        ApplyHardened(service, readOnly: false);
        service.Deploy!.Resources!.Limits = new ResourceSpec { Cpus = "0.5", Memory = "512M" };
        service.Healthcheck = new Healthcheck
        {
            Test = ["CMD", "curl", "-f", "http://localhost:9000/minio/health/live"],
            Interval = "10s",
            Timeout = "5s",
            Retries = 5,
            StartPeriod = "15s"
        };
    });

// ── API ─────────────────────────────────────────────────────────────────────
// Migrations and seeding are handled by the API on startup (development only).
// See: ApplicationBuilderExtensions.InitializeDatabaseAsync

var api = builder.AddProject<Projects.MyProject_WebApi>("api")
    .WithEndpoint("http", e =>
    {
        e.Port = apiPort;
        e.IsProxied = false;
    })
    .WithReference(db)
    .WaitFor(db)
    .WaitFor(storage)
    .WithEnvironment("Authentication__Jwt__Key", jwtSecret)
    .WithEnvironment("FileStorage__Endpoint", storage.GetEndpoint("http"))
    .WithEnvironment("FileStorage__AccessKey", storage.Resource.RootUser)
    .WithEnvironment("FileStorage__SecretKey", storage.Resource.PasswordParameter)
    .WithEnvironment("FileStorage__BucketName", "{INIT_PROJECT_SLUG}-files")
    .WithEnvironment("FileStorage__UseSSL", "false")
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Image = "${API_IMAGE}";
        service.Build = null;
        service.Environment["ASPNETCORE_ENVIRONMENT"] = "Production";
        service.Environment["HTTP_PORTS"] = "8080";
        // Remove Aspire-generated convenience aliases (we use ConnectionStrings__Database only)
        service.Environment.Remove("DATABASE_HOST");
        service.Environment.Remove("DATABASE_PORT");
        service.Environment.Remove("DATABASE_USERNAME");
        service.Environment.Remove("DATABASE_PASSWORD");
        service.Environment.Remove("DATABASE_URI");
        service.Environment.Remove("DATABASE_JDBCCONNECTIONSTRING");
        service.Environment.Remove("DATABASE_DATABASENAME");
        service.Expose.Clear();
        service.Ports.Clear();
        service.Ports.Add("${API_PORT:-8080}:8080");
        service.EnvFile ??= [];
        service.EnvFile.Add("envs/api.env");
        service.EnvFile.Add("envs/seed.env");
        service.Networks = ["frontend", "backend"];
        foreach (var dep in service.DependsOn)
            dep.Value.Condition = "service_healthy";
        ApplyHardened(service);
        service.Tmpfs = ["/tmp", "/home/app"];
        service.Deploy!.Resources!.Limits = new ResourceSpec { Cpus = "2.0", Memory = "1G" };
        service.Healthcheck = new Healthcheck
        {
            Test = ["CMD", "dotnet", "/app/healthprobe/HealthProbe.dll", "http://localhost:8080/health/live"],
            Interval = "10s",
            Timeout = "5s",
            Retries = 5,
            StartPeriod = "60s"
        };
    });

// Mailpit only for local development - production uses real SMTP (configured in api.env)
if (builder.ExecutionContext.IsRunMode)
{
    var mailpit = builder.AddMailPit("mailpit", httpPort: mailpitHttpPort, smtpPort: mailpitSmtpPort);
    api.WaitFor(mailpit)
        .WithEnvironment("Email__Smtp__Host", mailpit.Resource.Host)
        .WithEnvironment("Email__Smtp__Port", () => mailpitSmtpPort.ToString())
        .WithEnvironment("Email__Smtp__UseSsl", "false");
}

// ── Frontend (SvelteKit) ────────────────────────────────────────────────────

builder.AddViteApp("frontend", "../../../src/frontend")
    .WithPnpm()
    .WithEndpoint("http", e =>
    {
        e.Port = frontendPort;
        e.IsProxied = false;
    })
    .WithEnvironment("API_URL", "http://127.0.0.1:" + apiPort)
    .WaitFor(api)
    .PublishAsDockerComposeService((_, service) =>
    {
        service.Image = "${FRONTEND_IMAGE}";
        service.Build = null;
        service.Environment["PORT"] = "3000";
        service.Environment["API_URL"] = "http://api:8080";
        service.Environment["ORIGIN"] = "${ORIGIN}";
        service.Environment["TURNSTILE_SITE_KEY"] = "${TURNSTILE_SITE_KEY:-}";
        service.Environment["XFF_DEPTH"] = "${XFF_DEPTH:-0}";
        service.Environment["ALLOWED_ORIGINS"] = "${ALLOWED_ORIGINS:-}";
        service.Expose.Clear();
        service.Ports.Clear();
        service.Ports.Add("${FRONTEND_PORT:-3000}:3000");
        service.Networks = ["frontend"];
        foreach (var dep in service.DependsOn)
            dep.Value.Condition = "service_healthy";
        ApplyHardened(service);
        service.Deploy!.Resources!.Limits = new ResourceSpec { Cpus = "1.0", Memory = "512M" };
        service.Healthcheck = new Healthcheck
        {
            Test = ["CMD", "node", "-e", "fetch('http://localhost:3000').then(r=>{process.exit(r.ok?0:1)}).catch(()=>process.exit(1))"],
            Interval = "10s",
            Timeout = "5s",
            Retries = 3,
            StartPeriod = "15s"
        };
    });

builder.Build().Run();

// ── Helpers ─────────────────────────────────────────────────────────────────

static void ApplyHardened(Service service, bool readOnly = true)
{
    service.Restart = "always";
    service.ReadOnly = readOnly;
    service.SecurityOpt ??= [];
    service.SecurityOpt.Add("no-new-privileges:true");
    service.CapDrop ??= [];
    service.CapDrop.Add("ALL");
    service.Tmpfs ??= ["/tmp"];
    service.Logging = new Logging
    {
        Driver = "json-file",
        Options = new Dictionary<string, string>
        {
            ["max-size"] = "10m",
            ["max-file"] = "3"
        }
    };
    service.Deploy ??= new Deploy();
    service.Deploy.Resources ??= new Resources();
    service.Deploy.Resources.Limits ??= new ResourceSpec();
}
