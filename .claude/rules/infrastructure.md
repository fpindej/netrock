# Infrastructure Rules

## Aspire (Local Dev)
- Run: `dotnet run --project src/backend/MyProject.AppHost`
- Launches PostgreSQL, MinIO, MailPit, API, and Frontend
- Logging: Serilog bridges to OTEL via `writeToProviders: true` - do NOT add `Serilog.Sinks.OpenTelemetry` (causes duplicate logs)

## NuGet
- All versions in `Directory.Packages.props` only - never in `.csproj`
- To add: `<PackageVersion Include="Pkg" Version="X.Y.Z" />` in props, `<PackageReference Include="Pkg" />` in csproj

## Docker Security
- Read-only root filesystem, no-new-privileges, drop all capabilities
- Secrets in env vars or `.env` - never in code or committed config
- Database credentials never in connection strings in logs
- Health check endpoints must not leak sensitive info

## Options Pattern
- `public sealed class {Name}Options` with `const string SectionName`
- `[Required]` on mandatory fields, `string.Empty` default for required strings
- Register with `BindConfiguration`, `ValidateDataAnnotations`, `ValidateOnStart`

## Verification
- Backend: `dotnet build src/backend/MyProject.slnx && dotnet test src/backend/MyProject.slnx -c Release`
- Frontend: `cd src/frontend && pnpm run test && pnpm run format && pnpm run lint && pnpm run check`
- Fix all errors before committing - loop until green
