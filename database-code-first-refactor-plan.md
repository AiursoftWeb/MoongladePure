# MoongladePure 数据库代码优先重构与后续 SaaS / AI 规划

日期：2026-05-08

当前分支已经完成数据库 code-first 重构的阶段性收口。这个阶段的目标不是一次性完成完整 SaaS 产品，而是在保持现有博客体验不变的前提下，把底层数据模型、迁移工具、站点边界和 AI 产物持久化推进到一个可以验收、可以继续演进的基线。

## 1. 阶段目标

本阶段围绕四个目标展开：

1. 建立 code-first 新数据库模型，为后续多租户、多站点和 AI 扩展提供稳定边界。
2. 提供 legacy SQLite 到新库的离线迁移工具，并支持 dry-run、validate 和可审计报告。
3. 让现有 Web UI 在默认租户和默认站点上保持兼容，同时逐步引入 request-scoped 站点上下文。
4. 把已经开始的站点域名管理、文章读取投影和 AI job/artifact 落库补到阶段闭环。

“无感升级”在本规划中仍定义为：迁移完成后，终端用户看到的文章、URL、评论、页面、菜单、友链、设置、账号、主题和资源保持一致。迁移流程允许由开发者或运维通过离线工具、双实例、人工验收和切换流量完成，不要求旧实例原地零停机升级。

## 2. 已完成的阶段性成果

### 2.1 Code-first 新模型

已经建立第一版新 schema，核心包括：

- 默认 `Tenant` 和默认 `Site`。
- 平台用户、站点成员关系和默认 owner/admin 兼容路径。
- 站点级文章、分类、标签、页面、菜单、友链、评论、主题、配置和二进制资产。
- `PostContent` 保存文章正文、摘要和翻译等内容形态。
- `PostRoute` 保存旧文章日期 + slug 访问兼容所需路由。
- `PostMetric` 保存 hits/likes。
- `AiJob` 和 `AiArtifact` 保存 AI 后台任务和 AI 产物。
- `MediaAsset` 和 `MediaVariant` 作为后续媒体资产元数据扩展基础。

当前仍保留必要的旧宽表字段，以降低 UI 和业务逻辑切换风险。

### 2.2 Legacy SQLite 迁移工具

迁移工具位于：

```text
src/Moonglade.Migration/MoongladePure.Migration.csproj
```

已支持命令：

```bash
dotnet run --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source old.db
dotnet run --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --target new.db
dotnet run --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source old.db --dry-run
dotnet run --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source old.db --target new.db
```

所有 report 类型命令支持 `--json <path>`。

当前迁移覆盖：

- 默认租户、默认站点、用户、站点成员关系。
- 设置、主题、二进制站点资产。
- 分类、标签、文章、文章内容、文章路由、文章统计。
- 文章-分类和文章-标签关系。
- 评论和评论回复。
- 页面、菜单、子菜单、友链。
- 旧文章摘要和翻译字段到 `AiArtifact` 的映射。

当前验证覆盖：

- 目标库必要表存在性。
- SQLite foreign key integrity。
- 主要实体孤儿关系。
- 已发布文章旧 URL 兼容性。
- 源表和目标表行数对比。
- 设置 JSON 有效性。
- 文章、页面、评论、回复内容 hash 对比。
- 文章 published/draft/deleted 状态数量对比。
- 每篇文章 hits/likes 对比。

### 2.3 Web 站点上下文

Web 端已经引入 request-scoped `ISiteContext`：

```text
src/Moonglade.Data/Infrastructure/ISiteContext.cs
```

当前行为：

- `RequestSiteContext` 读取 request host。
- 按 `SiteDomain.Host` 查询当前 `SiteId`。
- host 未匹配、无 host 或非 Web 场景 fallback 到 `SystemIds.DefaultSiteId`。
- 已启用 `X-Forwarded-Host`，支持反向代理场景。
- `DefaultSiteContext` 保留为单站点/非 request 场景 fallback。

主要业务路径已经接入站点边界，包括文章、分类、标签、页面、评论、菜单、友链、主题、配置、RSS、导出、资产、统计和 AI 后台任务等。

### 2.4 文章读取投影

文章读取已经引入：

```text
src/Moonglade.Core/PostFeature/PostReadProjection.cs
```

已接入：

- `GetPostByIdQuery`
- `GetPostBySlugQuery`
- `GetDraftQuery`
- `ListPostsQuery`
- `ListArchiveQuery`
- `ListByTagQuery`
- `ListFeaturedQuery`
- `SearchPostQuery`

读取优先级：

- 正文优先读取 `PostContent(ContentKind = RawMarkdown)`，缺少时 fallback 到 `Post.RawContent`。
- 摘要优先读取 `AiArtifact(Summary)`，其次 `PostContent(Summary).Abstract`，最后 fallback 到旧摘要字段。
- 翻译优先读取 `PostContent(Translation)`，其次 `AiArtifact(Translation)`，最后 fallback 到旧本地化字段。

列表类查询会按当前 `SiteId` 批量读取 `PostContent` 和 `AiArtifact`，避免逐篇文章同步查库。

### 2.5 AI 数据落库

当前 AI 后台任务已经新增持久化记录：

- `AiJob` 记录后台 AI 任务状态。
- `AiArtifact` 记录生成的摘要、评论、标签和迁移来的 AI-like 内容。

兼容行为仍保留：

- 摘要继续写回 `Post.ContentAbstractZh` 和 `Post.ContentAbstractEn`。
- AI 评论继续写入 `Comment`。
- AI 标签继续写入 `Tag` 和 `PostTag`。

旧库迁移映射：

- `ContentAbstractZh` -> `AiArtifactType.Summary`, `zh-CN`。
- `ContentAbstractEn` -> `AiArtifactType.Summary`, `en-US`。
- `LocalizedChineseContent` -> `AiArtifactType.Translation`, `zh-CN`。
- `LocalizedEnglishContent` -> `AiArtifactType.Translation`, `en-US`。

### 2.6 站点域名管理

本阶段补完了站点管理的最小闭环。

后端 API：

```text
GET /api/site
POST /api/site/{siteId}/domains
DELETE /api/site/domains/{id}
```

后台 UI：

```text
GET /admin/site
```

当前能力：

- 查看站点列表。
- 查看每个站点的域名绑定。
- 新增站点域名绑定。
- 删除站点域名绑定。
- 新增时对 host 做 trim + lowercase 归一化。
- 拒绝空 host、重复 host 和不存在的站点。

当前仍不包含站点创建、站点编辑、站点删除、租户注册、成员角色权限和完整 SaaS 管理面板。

## 3. 阶段验收结果

### 3.1 自动测试

本阶段已运行并通过：

```bash
dotnet build src/Moonglade.Web/MoongladePure.Web.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet build src/Moonglade.Migration/MoongladePure.Migration.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SiteManagementTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SiteScopedSpecTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter MigrationToolTests -p:UseSharedCompilation=false -maxcpucount:1
```

结果：

- `SiteManagementTests`: 6 passed。
- `SiteScopedSpecTests`: 23 passed。
- `MigrationToolTests`: 12 passed。
- Web 项目构建通过。
- Migration 项目构建通过。

### 3.2 真实 legacy SQLite 迁移验证

使用项目根目录 `./app.db` 验证。

命令：

```bash
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- preflight --source ./app.db
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source ./app.db --dry-run
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- migrate --source ./app.db --target /tmp/moonglade-stage-20260508-codex.db
dotnet run --no-build --project src/Moonglade.Migration/MoongladePure.Migration.csproj -- validate --source ./app.db --target /tmp/moonglade-stage-20260508-codex.db
```

结果：

- `preflight`: warnings 0, errors 0。
- `migrate --dry-run`: dry-run errors 0。
- `migrate`: errors 0。
- `validate`: warnings 0, errors 0。

目标库关键统计：

- `Tenant`: 1
- `Site`: 1
- `User`: 2
- `SiteMembership`: 2
- `Post`: 4
- `PostContent`: 4
- `PostRoute`: 4
- `PostMetric`: 4
- `AiArtifact`: 8

### 3.3 Web 手动冒烟

使用迁移后的目标库：

```text
/tmp/moonglade-stage-20260508-codex.db
```

启动命令：

```bash
ASPNETCORE_URLS='http://127.0.0.1:5088' ConnectionStrings__DefaultConnection='DataSource=/tmp/moonglade-stage-20260508-codex.db;Cache=Shared' BackgroundJobs__Enable='False' dotnet run --no-build --project src/Moonglade.Web/MoongladePure.Web.csproj
```

已验证：

- 首页 `/` 返回 200。
- 旧文章 URL `/post/2026/4/29/welcome-to-moonglade-pure` 返回 200。
- 分类页 `/category/default` 返回 200。
- 标签页 `/tags/ubuntu` 返回 200。
- 页面 `/page/about` 返回 200。
- 归档 `/archive` 返回 200。
- RSS `/rss` 返回 200。
- 未登录访问后台站点管理 `/admin/site` 返回 302，授权保护生效。

受验证码限制，纯终端环境没有完成登录后的后台点击流验证。后台站点域名管理 UI 已通过 Razor 编译，后端 API 已由 `SiteManagementTests` 覆盖。

## 4. 当前阶段边界

当前可以视为完成：

```text
单站点兼容 + code-first 新库 + legacy SQLite 迁移 + request host 站点解析 + 文章内容投影 + AI job/artifact 落库 + 站点域名管理最小 UI
```

当前还不能视为完整 SaaS 产品，原因是：

- 还没有租户注册流程。
- 还没有站点创建、编辑、删除管理。
- 还没有成员、角色和权限管理。
- 还没有资源限制、计费和租户级配额。
- 还没有 MySQL legacy migration 的真实旧库同等验证。
- 还没有多实例 AI job claiming / worker queue。
- 还没有 AI 产物审核、版本管理和成本统计 UI。
- 还没有内容和设置编辑的强并发控制。

## 5. 下一阶段 SaaS 规划

### 5.1 站点生命周期

目标：

- 支持创建站点。
- 支持编辑站点基础信息。
- 支持禁用或归档站点。
- 支持管理站点主域名和备用域名。

建议先做：

1. `CreateSiteCommand` 和 `UpdateSiteCommand`。
2. 站点创建时自动建立默认配置、主题、菜单和 owner membership。
3. 后台 UI 增加站点创建和编辑入口。
4. 明确未绑定域名访问策略：404、跳转还是 fallback 默认站点。

### 5.2 租户与注册

目标：

- 支持平台用户注册。
- 支持用户创建租户或加入租户。
- 支持租户下多个站点。

建议先做：

1. 明确 `Tenant`、`User`、`SiteMembership` 的产品语义。
2. 增加租户 owner 初始化流程。
3. 增加注册、邀请和接受邀请流程。
4. 为后续计费和资源限制保留租户状态字段。

### 5.3 成员、角色和权限

目标：

- 从当前默认 admin 兼容模式，演进为站点级成员权限。

建议角色：

- Owner
- Admin
- Editor
- Author
- Viewer

建议先做：

1. 建立权限常量和授权 policy。
2. 将后台管理 API 按权限拆分。
3. 增加成员列表、邀请、移除、角色变更 UI。
4. 保留单站点部署默认 admin 体验。

### 5.4 多站点数据隔离审计

目标：

- 确认所有站点业务数据都经过 `SiteId` 边界。

建议继续审计：

- 非文章后台编辑路径。
- 配置保存路径。
- 主题保存路径。
- 媒体上传和删除路径。
- 导出和导入路径。
- RSS、Sitemap、Manifest、OpenSearch 等中间件路径。

## 6. 下一阶段 AI 规划

### 6.1 AI Job 状态机

目标：

- 让 AI 后台任务可以在多实例部署中可靠运行。

建议增加：

- `Pending`
- `Claimed`
- `Running`
- `Succeeded`
- `Failed`
- `Canceled`

关键要求：

- claim 必须是数据库原子操作。
- worker 必须有 lease timeout。
- 失败必须记录错误和重试次数。
- 同一目标内容的重复任务需要幂等策略。

### 6.2 AI Artifact 审核和发布

目标：

- 区分 AI 原始输出、人工审核版本和最终展示内容。

建议模型：

- `AiArtifact` 保存原始产物。
- `PostContent` 保存被采用的展示内容。
- 增加 artifact status 或 review metadata。

建议 UI：

- 查看文章相关 AI 摘要、翻译、标签和评论。
- 接受 AI 产物并写入展示内容。
- 拒绝或重新生成 AI 产物。

### 6.3 AI 使用量和成本

目标：

- 为 SaaS 计费和限额做准备。

建议增加：

- token 输入/输出。
- provider、model、duration。
- estimated cost。
- tenant/site/user 维度聚合。

### 6.4 AI 功能扩展

可以按优先级拆分：

1. 摘要和 SEO meta 自动生成。
2. 多语言翻译和人工修订。
3. 标签建议。
4. 评论辅助和审核辅助。
5. 文章问答。
6. 配图 prompt 和图片生成。

## 7. MySQL 和媒体后续验证

### 7.1 MySQL legacy migration

当前迁移工具以 legacy SQLite 为主，不能假设 MySQL 旧库已经同等成熟。

后续需要：

1. 准备真实 legacy MySQL 样例库。
2. 固化旧 MySQL schema 和迁移版本。
3. 实现或验证 MySQL source reader。
4. 跑与 SQLite 同等级别的 preflight、dry-run、migrate、validate。

### 7.2 媒体文件审计

当前数据库内资产和元数据已经迁移，但文件系统或对象存储中的真实文件仍需部署侧审计。

后续需要：

1. 扫描文章、页面、主题和配置里的媒体 URL。
2. 对本地文件或对象存储 key 做存在性检查。
3. 生成缺失文件报告。
4. 决定是否补建 `MediaAsset` metadata。

## 8. 发布前检查清单

进入下一次可发布节点前，至少需要确认：

1. 完整 solution 测试通过。
2. `./lint.sh` 通过。
3. `git diff --check` 通过。
4. 使用真实迁移库完成浏览器登录后的后台手动测试。
5. 站点域名绑定新增、删除和重复 host 错误提示在浏览器中确认。
6. 明确 release note 中 SQLite/MySQL 迁移支持边界。
7. 不提交本地运行配置，例如 `src/Moonglade.Web/appsettings.json` 的私有改动。

## 9. 当前结论

截至 2026-05-08，数据库 code-first 重构已经完成一个可继续演进的阶段性基线。接下来不应继续在旧单站点假设上横向堆字段，而应围绕 SaaS 和 AI 两条主线推进：

- SaaS 主线先补站点生命周期、租户注册、成员权限和未绑定域名策略。
- AI 主线先补 job claiming、artifact 审核发布、使用量统计和多语言内容工作流。

当前最稳妥的下一步，是先完成浏览器环境下的后台站点管理 UI 人工验收，然后再启动站点创建和成员权限设计。
