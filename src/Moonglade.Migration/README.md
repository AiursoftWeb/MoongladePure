# MoongladePure Migration Tool

`MoongladePure.Migration` is an independent console project for legacy database checks and migration.

It is not a runtime dependency of `MoongladePure.Web`, and it is not a NuGet package. The project has `IsPackable=false`, so its intended output is a command-line executable that operators run during an upgrade.

## Build Output

The project target is `net10.0` and the assembly name is `MoongladePure.Migration`.

After a normal debug build, the main output is:

```bash
src/Moonglade.Migration/bin/Debug/net10.0/MoongladePure.Migration.dll
```

Depending on the SDK and platform, .NET may also create a native apphost executable next to the dll.

## Independent Build

The tool can be built independently:

```bash
dotnet build src/Moonglade.Migration/MoongladePure.Migration.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
```

It references the SQLite data provider and shared utility project:

```text
Moonglade.Data.Sqlite
Moonglade.Utils
```

These references let the tool create the new code-first SQLite schema through EF Core migrations and reuse the same post route checksum logic as the web application.

## Commands

Run a read-only preflight check against a legacy SQLite database:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source old.db
```

Write a JSON preflight report:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source old.db --json report.json
```

Migrate a legacy SQLite database into a new MoongladePure2 SQLite database:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --target new.db
```

Write a JSON migration report:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --target new.db --json migration-report.json
```

Overwrite an existing target database:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --target new.db --overwrite
```

Validate a migrated target database:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --target new.db
```

Validate a migrated target database against its legacy source row counts:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source old.db --target new.db
```

When `--source` is provided, the validation report includes source rows, source-to-target comparisons, such as `LocalAccount -> User`, `CustomPage -> Page`, `PostExtension -> PostMetric`, and a legacy post route check that verifies each old `PubDateUtc + Slug` route exists in `PostRoute`.

Write a JSON validation report:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source old.db --target new.db --json validation-report.json
```

## Validation

The current migration path has been tested with a real legacy SQLite database whose latest EF migration was:

```text
20260115212706_AddUserProfileTable
```

That validation produced a clean preflight report:

```text
Warnings: 0
Errors: 0
```

The migration also completed without errors, and the migrated database was manually verified through the web application. The verification covered home page loading, post list loading, old post slug routing, category and tag pages, admin sign-in, existing settings, menus, pages, friend links, comments, and creating and editing posts.

Useful post-migration SQLite checks:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source old.db --target new.db
sqlite3 new.db "select count(*) from Post;"
sqlite3 new.db "select count(*) from PostContent;"
sqlite3 new.db "select count(*) from PostRoute;"
sqlite3 new.db "select count(*) from User;"
sqlite3 new.db "select count(*) from SiteMembership;"
sqlite3 new.db "pragma foreign_key_check;"
```

`Migrated rows` in the report use legacy source names, such as `LocalAccount` and `CustomPage`. `Target rows` use the actual new SQLite table names, such as `User`, `Page`, `SiteSetting`, `Theme`, and `SiteBinaryAsset`.

## Current Scope

The first migration version supports the core blog data path:

- Tenant, site, admin account, and site membership
- Blog settings, themes, and binary assets
- Categories, tags, posts, post content, post routes, post metrics
- Post-category and post-tag relationships
- Comments and replies
- Pages, menus, submenus, and friend links

The migration is designed to be run before switching traffic to the new instance. It does not aim for zero-downtime migration.
