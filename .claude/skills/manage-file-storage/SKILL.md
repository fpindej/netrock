---
disable-model-invocation: true
argument-hint: "[swap|remove]"
---

Swap S3 provider or remove file storage entirely.

## Swap S3 Provider (Cloudflare R2, DigitalOcean Spaces, etc.)

No code changes needed - `S3FileStorageService` uses the standard S3 API. Only configuration:

**Update `deploy/envs/production-example/compose.env`:**

```env
# Cloudflare R2
FileStorage__Endpoint=https://<account-id>.r2.cloudflarestorage.com
FileStorage__AccessKey=<R2-access-key>
FileStorage__SecretKey=<R2-secret-key>
FileStorage__BucketName=my-bucket
FileStorage__UseSSL=true
FileStorage__Region=auto

# DigitalOcean Spaces
FileStorage__Endpoint=https://<region>.digitaloceanspaces.com
FileStorage__AccessKey=<spaces-key>
FileStorage__SecretKey=<spaces-secret>
FileStorage__BucketName=my-space
FileStorage__UseSSL=true
FileStorage__Region=<region>
```

Pre-create the bucket in your provider's console. Restart - no rebuild needed.

## Remove File Storage Entirely

1. **Docker:** Remove `storage` service from `docker-compose.yml` and `docker-compose.production.yml`. Remove MinIO resource from `MyProject.AppHost/Program.cs`
2. **Backend:** Remove `Application/Features/FileStorage/`, `Application/Features/Avatar/`, `Infrastructure/Features/FileStorage/`, `Infrastructure/Features/Avatar/`
3. **Entity:** Remove `HasAvatar` from `ApplicationUser`
4. **Endpoints:** Remove avatar endpoints from `UsersController`, `UploadAvatar/` DTOs
5. **DTOs:** Remove `HasAvatar` from `UserOutput`, `AdminUserOutput`, `UserResponse`, `AdminUserResponse`, mappers
6. **DI:** Remove `AddFileStorageServices()` and `AddAvatarServices()` from `Program.cs`
7. **NuGet:** Remove `AWSSDK.S3`, `SkiaSharp`, `SkiaSharp.NativeAssets.Linux.NoDependencies` from `Directory.Packages.props`
8. **Frontend:** Remove `AvatarDialog.svelte`, update `ProfileHeader.svelte` and `UserNav.svelte` to remove avatar image
9. **Config:** Remove `FileStorage` section from all `appsettings*.json` and env files
10. **Health check:** Remove S3 health check from `HealthCheckExtensions.cs`
