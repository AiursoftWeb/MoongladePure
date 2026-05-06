# Handoff: database code-first refactor

日期：2026-05-06

分支：`users/aimer/refactor_db_service`

## 当前状态

这个分支已经完成数据库 code-first 重构的第一轮可用版本。核心结果是：

- 新的 code-first 数据模型已经建立，包含默认 `Tenant`、默认 `Site`、站点级内容表、文章路由表、文章内容表、媒体/AI 表，以及兼容现有博客功能所需的表。
- 旧版 SQLite 到 MoongladePure2 SQLite 的迁移工具已经实现，并覆盖了 preflight、migrate、validate、dry-run 和 JSON report。
- 迁移工具已经用真实旧库验证过，真实迁移后的数据库也经过 Web 应用手动冒烟测试。
- Web 当前仍保持单站点兼容模式，但已经引入 request-scoped `ISiteContext`，主要查询和写入路径通过当前站点上下文获取 `SiteId`，默认实现仍回落到默认站点。
- AI 后台任务现在会持久化 `AiJob` 和 `AiArtifact`，同时保留原有 UI 依赖的旧字段写入。

当前工作区包含尚未提交的 `ISiteContext` 重构和本文档更新，需要 review 后再决定是否提交。

## 重要提交

近期完成的主要提交：

- `ef6d635c feat: persist ai jobs and artifacts`
- `1f6fda92 refactor: scope web queries to default site`
- `e94abdd5 feat: add migration dry-run validation`
- `fe3334f5 refactor: split legacy sqlite analyzer`
- `54ada764 refactor: split migration target validator`
- `f4504fe0 feat: validate migrated setting json`
- `e504b395 feat: validate legacy post route compatibility`
- `b25f11dd feat: report migration source target comparisons`
- `e2edeeb1 feat: compare migration source and target counts`
- `97815ce7 feat: write migration JSON reports`
- `35ed79fd test: cover legacy SQLite migration tool`
- `7d64c010 feat: add migration target validation command`
- `2033e6d1 docs: document migration validation results`
- `080bfd9e Implement legacy SQLite migration command`
- `10250f60 Add legacy SQLite migration preflight tool`

## 迁移工具

项目路径：

```text
src/Moonglade.Migration/MoongladePure.Migration.csproj
```

主要文件：

- `src/Moonglade.Migration/Program.cs`
- `src/Moonglade.Migration/LegacySqliteAnalyzer.cs`
- `src/Moonglade.Migration/LegacySqliteMigrator.cs`
- `src/Moonglade.Migration/LegacySqliteDryRun.cs`
- `src/Moonglade.Migration/TargetSqliteValidator.cs`
- `src/Moonglade.Migration/README.md`

常用命令：

```bash
dotnet build src/Moonglade.Migration/MoongladePure.Migration.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source old.db
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --target new.db
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --dry-run
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source old.db --target new.db
```

所有 report 类型命令都支持 `--json <path>`。

`migrate --dry-run` 会创建临时目标库，执行完整迁移和验证，然后删除临时库；它不会写入正式 `--target` 数据库。

## 迁移覆盖范围

当前 legacy SQLite 迁移支持：

- 默认租户、默认站点、用户、站点成员关系。
- 设置、主题、二进制站点资产。
- 分类、标签、文章、文章内容、文章路由、文章统计。
- 文章-分类和文章-标签关系。
- 评论和评论回复。
- 页面、菜单、子菜单、友链。
- 旧文章摘要和翻译字段迁移为 `AiArtifact`。

迁移报告里的 `Migrated rows` 使用旧库表名，例如 `LocalAccount`、`CustomPage`、`BlogConfiguration`。

目标库统计里的 `Target rows` 使用新库表名，例如 `User`、`Page`、`SiteSetting`、`Theme`、`SiteBinaryAsset`、`AiArtifact`。

## 验证覆盖范围

`validate --source old.db --target new.db` 当前会检查：

- 目标库必要表是否存在。
- `Tenant`、`Site`、`User`、`SiteMembership` 等基础数据是否存在。
- SQLite foreign key integrity。
- 文章内容、文章路由、文章统计、分类、标签、评论、回复、菜单、成员关系等是否存在孤儿关系。
- 已发布文章是否都有 `PostRoute`。
- 旧文章 URL 兼容性：旧库 `PubDateUtc + Slug` 必须能在目标库 `PostRoute` 中找到。
- 源表和目标表行数对比。
- 旧 `BlogConfiguration` 和新 `SiteSetting` 的 JSON 有效性。
- 文章内容、页面、评论、回复的内容 hash 对比。
- 文章 published/draft/deleted 状态数量对比。
- 每篇文章 `Hits` 和 `Likes` 对比。

## 真实旧库验证

开发期间使用过的真实 legacy SQLite 数据库：

```text
/tmp/moonglade-legacy.db
./app.db
```

该旧库最新 EF migration：

```text
20260115212706_AddUserProfileTable
```

已知干净结果：

- `preflight`: warnings 0, errors 0。
- `migrate`: errors 0。
- `validate`: warnings 0, errors 0。
- `dry-run`: errors 0。

2026-05-06 使用项目根目录下的 `./app.db` 重新验证，正式迁移目标库为：

```text
/tmp/moonglade-app-migrated-20260506.db
```

该库验证结果：

- `preflight`: warnings 0, errors 0。
- `migrate --dry-run`: dry-run errors 0。
- `migrate`: errors 0。
- `validate`: warnings 0, errors 0。

最近一次真实 dry-run / validate 目标统计中包含 `AiArtifact: 8`、`Post: 4`、`PostRoute: 4`、`User: 2`、`SiteMembership: 2`。

迁移后的 Web 手动验证覆盖：

- 首页加载。
- 文章列表加载。
- 旧文章 slug 路由。
- 分类和标签页面。
- 后台登录。
- 现有设置读取。
- 菜单、页面、友链。
- 评论展示。
- 新建和编辑文章。

## Web 默认站点边界

当前还没有真正的 host/domain 动态解析，兼容模型仍然是单站点。但主要业务路径已经通过 request-scoped `ISiteContext` 获取站点边界：

```csharp
ISiteContext.SiteId
```

默认实现：

```text
src/Moonglade.Data/Infrastructure/ISiteContext.cs
```

`DefaultSiteContext` 仍返回 `SystemIds.DefaultSiteId`，用于保留现有单站点行为和后续动态解析的 fallback。

已处理范围：

- Post、Category、Tag、Comment、CommentReply、Page、FriendLink、FeaturedPost、PostTag 等 Data specs。
- Core 中的文章、分类、标签、页面、统计、资产相关命令和查询。
- Comments 模块命令和查询。
- Configuration 查询和更新。
- FriendLink、Menus、Theme、Syndication 相关查询和命令。
- Data exporter 默认只导出默认站点数据。
- Data exporter 通过当前 `ISiteContext` 导出当前站点数据。
- Web startup seed check 仍使用默认站点判断是否需要 seed。
- AI background job 的读取和写入路径。

相关测试：

```text
tests/Moonglade.Tests/SiteScopedSpecTests.cs
```

该测试现在包含两个 EF InMemory 级别的双站点隔离用例：

- `ListPostsQueryUsesCurrentSiteBoundary`
- `GetPageBySlugQueryUsesCurrentSiteBoundary`

## AI 数据模型

现有 UI 兼容行为保留：

- 摘要仍会写回 `Post.ContentAbstractZh` 和 `Post.ContentAbstractEn`。
- AI 评论仍会写入 `Comment`。
- AI 标签仍会写入 `Tag` 和 `PostTag`。

新增持久化记录：

- `AiJob` 记录后台 AI 任务状态。
- `AiArtifact` 记录生成的摘要、评论、标签，以及从旧库迁移来的 AI-like 内容。

旧库迁移映射：

- `ContentAbstractZh` -> `AiArtifactType.Summary`, `zh-CN`。
- `ContentAbstractEn` -> `AiArtifactType.Summary`, `en-US`。
- `LocalizedChineseContent` -> `AiArtifactType.Translation`, `zh-CN`。
- `LocalizedEnglishContent` -> `AiArtifactType.Translation`, `en-US`。

## 已运行测试

反复运行并通过的命令：

```bash
dotnet build tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-build --no-restore --filter MigrationToolTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-build --no-restore --filter SiteScopedSpecTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet test Aiursoft.MoongladePure.sln --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source /tmp/moonglade-legacy.db --dry-run
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source ./app.db
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source ./app.db --dry-run
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source ./app.db --target /tmp/moonglade-app-migrated-20260506.db
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source ./app.db --target /tmp/moonglade-app-migrated-20260506.db
./lint.sh
git diff --check
```

最近已知完整测试结果：

```text
Passed: 32
```

最近已知 `SiteScopedSpecTests` 结果：

```text
Passed: 5
```

## 尚未完成

这个分支还没有完成完整 SaaS 产品形态。剩余工作包括：

- 基于 request host/domain 的动态 `SiteContext`。
- 租户注册、站点创建、站点管理 UI。
- 成员、角色和权限管理；当前只是默认 owner/admin 兼容路径。
- 用真实旧 MySQL 数据库验证或实现 MySQL legacy migration 路径。
- 更深入的媒体文件和外部 blob 引用验证。
- 前端读取路径完全切到 `PostContent` / `AiArtifact` 投影。
- 多实例部署下的 AI job claiming / worker queue。
- 内容和设置编辑的强并发控制。

## 风险点

- 当前 `DefaultSiteContext` 仍返回 `SystemIds.DefaultSiteId`，它是过渡 fallback，不是最终多站点解析方案。
- 使用 `/tmp/moonglade-app-migrated-20260506.db` 做过手动 Web 冒烟，用户反馈看起来没问题；后续引入 host/domain 解析后仍建议再跑一轮。
- 迁移工具目前以 legacy SQLite 为主，不能假设旧 MySQL 路径已经同等成熟。
- `AiJob` / `AiArtifact` 已经落库，但后台任务调度和管理 UI 还没有 SaaS 级能力。
- 媒体迁移目前主要覆盖数据库内资产和元数据，文件系统/对象存储里的真实文件仍需部署侧审计。

## 推荐下一步

1. Review 并提交当前 `ISiteContext` 重构和文档更新，建议提交信息：`refactor: introduce site context boundary`。
2. 实现基于 request host/domain 的动态 `SiteContext`，优先读取 `SiteDomain.Host`，找不到时 fallback 到默认站点。
3. 增加 host/domain 解析测试，覆盖已绑定域名、未知域名 fallback、大小写 host 归一化。
4. 明确 MySQL legacy migration 是否进入当前里程碑；如果不做，需要在 release note 中写清楚。
5. 继续把前端读取路径逐步切到 `PostContent` / `AiArtifact` 投影。
