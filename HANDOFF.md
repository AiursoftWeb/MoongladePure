# MoongladePure2 Database Refactor Handoff

This document records the current state of the MoongladePure database refactor and migration work.

## Current Status

The project has moved from the old database-first direction to a code-first database model.

Completed work:

- Added a root planning document: `database-code-first-refactor-plan.md`.
- Reworked the data model around code-first EF Core entities.
- Added SaaS-ready platform concepts:
  - `Tenant`
  - `Site`
  - `SiteDomain`
  - `SiteMembership`
- Added default single-site IDs for the current non-SaaS runtime:
  - `SystemIds.DefaultTenantId`
  - `SystemIds.DefaultSiteId`
  - `SystemIds.DefaultAdminUserId`
- Added AI-ready persistence concepts:
  - `AiJob`
  - `AiArtifact`
  - `MediaAsset`
  - `MediaVariant`
  - `PostContent`
  - `PostRoute`
- Rebuilt SQLite and MySQL EF migrations for the new schema.
- Kept current UI/runtime behavior compatible enough for the app to start and create posts.
- Added an independent migration console project: `src/Moonglade.Migration`.

The migration tool currently supports:

- Read-only legacy SQLite preflight checks.
- Legacy SQLite to new SQLite migration.
- New database creation through the current EF Core SQLite migrations.
- Migration of core blog data:
  - Tenant, site, local account, site membership
  - Blog settings
  - Themes and binary assets
  - Categories and tags
  - Posts, post content, post routes, post metrics
  - Post-category and post-tag relationships
  - Comments and replies
  - Pages, menus, submenus, friend links

## Migration Tool Shape

`src/Moonglade.Migration` is an independent .NET console project.

It is not a NuGet package and is not a runtime dependency of `MoongladePure.Web`.

Project settings:

```xml
<OutputType>Exe</OutputType>
<IsPackable>false</IsPackable>
```

Main build outputs:

```text
src/Moonglade.Migration/bin/Debug/net10.0/MoongladePure.Migration
src/Moonglade.Migration/bin/Debug/net10.0/MoongladePure.Migration.dll
```

It can be built independently:

```bash
dotnet build src/Moonglade.Migration/MoongladePure.Migration.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
```

## Verified So Far

The following checks have passed during the implementation:

```bash
dotnet build Aiursoft.MoongladePure.sln --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet test Aiursoft.MoongladePure.sln --no-build --no-restore -p:UseSharedCompilation=false -maxcpucount:1
```

Test result:

```text
Passed: 15
Failed: 0
Skipped: 0
```

The migration command was smoke-tested with the current local SQLite database:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source src/Moonglade.Web/app.db --target /tmp/moongladepure-migrated-smoke.db --overwrite
```

That smoke test validates the command flow, target database creation, EF migration execution, and basic row writing.

Important limitation:

`src/Moonglade.Web/app.db` is already a new-schema database, not a real old legacy database. A real old database is still needed to validate legacy field mapping.

## How To Test With A Real Old Database

Yes, the next useful step is to prepare an old SQLite database file for testing.

Recommended process:

1. Make a copy of a real old MoongladePure SQLite database.
2. Put it somewhere outside the repo or in `/tmp`, for example:

```bash
/tmp/moonglade-legacy.db
```

3. Run a read-only preflight check:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source /tmp/moonglade-legacy.db --json /tmp/moonglade-legacy-preflight.json
```

4. Review the console output and JSON report.

Pay special attention to:

- Missing expected legacy tables
- Duplicate category route names
- Duplicate tag normalized names
- Duplicate page slugs
- Duplicate published post routes
- Empty required values
- Orphan post/category/tag/comment relationships
- Missing required settings

5. Run migration into a new target database:

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source /tmp/moonglade-legacy.db --target /tmp/moongladepure2-migrated.db --overwrite
```

6. Point a new MoongladePure2 instance at `/tmp/moongladepure2-migrated.db` and manually verify:

- Home page loads
- Post list loads
- Post detail pages load by old slugs
- Categories and tags load
- Admin login works
- Existing settings still apply
- Menus, pages, friend links, comments, and assets are visible where expected
- Creating and editing a new post still works

## Likely Next Engineering Work

After testing with a real old database, expect to fix mismatches in one of these areas:

- Legacy column names that differ from current assumptions.
- Legacy table names that differ across old versions.
- Old nullable values that violate new required fields.
- Duplicate routes that need deterministic conflict resolution.
- Missing account data or password hash format differences.
- Old settings JSON shape changes.
- Legacy themes or assets that need special handling.

The current migrator is intentionally conservative:

- It skips missing optional tables.
- It creates the new schema through EF Core migrations.
- It seeds the default tenant/site shape needed for the current single-site runtime.
- It reports skipped relationship rows instead of silently failing the whole migration.

## Commit State

At the time this handoff was written, the previous migration-tool changes had already been committed and the working tree was clean before adding this file.

Suggested commit message for this handoff document:

```bash
git add HANDOFF.md
git commit -m "Document database migration handoff"
```
