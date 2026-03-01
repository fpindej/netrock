var builder = DistributedApplication.CreateBuilder(args);

var frontendPort = int.TryParse(builder.Configuration["Ports:Frontend"], out var fp) ? fp : 13000;
var apiPort = int.TryParse(builder.Configuration["Ports:Api"], out var ap) ? ap : 13002;

// Derive infrastructure ports from the base (frontend) port so the entire
// stack lives in one predictable range — no collisions between projects.
var pgAdminPort = frontendPort + 3;
var postgresPort = frontendPort + 4;
var minioPort = frontendPort + 5;
var minioConsolePort = frontendPort + 6;
var mailpitSmtpPort = frontendPort + 7;
var mailpitHttpPort = frontendPort + 8;

// ── Infrastructure ──────────────────────────────────────────────────────────
// Container resources use session lifetime (default) — containers stop on
// Ctrl+C and restart on next run. Named data volumes persist across restarts,
// so database and file data survive. Explicit passwords ensure new containers
// can mount existing volumes without credential mismatch.

var pgPassword = builder.AddParameter("postgres-password", secret: true);
var storagePassword = builder.AddParameter("storage-password", secret: true);

var db = builder.AddPostgres("postgres", password: pgPassword)
    .WithEndpoint("tcp", e => e.Port = postgresPort)
    .WithDataVolume("{INIT_PROJECT_SLUG}-postgres-data")
    .WithPgAdmin(pgAdmin => pgAdmin.WithEndpoint("http", e => e.Port = pgAdminPort))
    .AddDatabase("Database");

var storage = builder.AddMinioContainer("storage", rootPassword: storagePassword)
    .WithEndpoint("http", e => e.Port = minioPort)
    .WithEndpoint("console", e => e.Port = minioConsolePort)
    .WithDataVolume("{INIT_PROJECT_SLUG}-storage-data");

var mailpit = builder.AddMailPit("mailpit", httpPort: mailpitHttpPort, smtpPort: mailpitSmtpPort);

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
    .WaitFor(mailpit)
    .WithEnvironment("Email__Smtp__Host", mailpit.Resource.Host)
    .WithEnvironment("Email__Smtp__Port", () => mailpitSmtpPort.ToString())
    .WithEnvironment("Email__Smtp__UseSsl", "false")
    .WithEnvironment("FileStorage__Endpoint", storage.GetEndpoint("http"))
    .WithEnvironment("FileStorage__AccessKey", storage.Resource.RootUser)
    .WithEnvironment("FileStorage__SecretKey", storage.Resource.PasswordParameter)
    .WithEnvironment("FileStorage__BucketName", "{INIT_PROJECT_SLUG}-files")
    .WithEnvironment("FileStorage__UseSSL", "false");

// ── Frontend (SvelteKit) ────────────────────────────────────────────────────

builder.AddViteApp("frontend", "../../../src/frontend")
    .WithPnpm()
    .WithEndpoint("http", e =>
    {
        e.Port = frontendPort;
        e.IsProxied = false;
    })
    .WithEnvironment("API_URL", "http://127.0.0.1:" + apiPort)
    .WaitFor(api);

builder.Build().Run();
