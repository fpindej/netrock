---
disable-model-invocation: true
---

Adds an infrastructure dependency to Aspire AppHost.

## Steps

1. Add hosting package version to `src/backend/Directory.Packages.props`:
   ```xml
   <PackageVersion Include="Aspire.Hosting.{Resource}" Version="13.1.2" />
   ```
2. Add package reference to `src/backend/MyProject.AppHost/MyProject.AppHost.csproj`:
   ```xml
   <PackageReference Include="Aspire.Hosting.{Resource}" />
   ```
3. Add resource in `src/backend/MyProject.AppHost/Program.cs`:
   ```csharp
   var myResource = builder.AddRabbitMQ("rabbitmq");
   ```
4. Wire to the API project:
   ```csharp
   // Native connection string convention:
   .WithReference(myResource)
   // Custom config mapping:
   .WithEnvironment("MySection__ConnectionString", myResource.GetEndpoint("tcp"))
   ```
5. Add startup dependency if the API needs the resource ready:
   ```csharp
   .WaitFor(myResource)
   ```
6. Add standalone defaults to `appsettings.Development.json` so the API can run without Aspire.
7. Verify: `dotnet build src/backend/MyProject.slnx`
