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

产品边界：

- `Moonglade.Web` 中的 `/admin/site` 目前保留为阶段性管理和验证入口，主要用于当前站点域名绑定验证。
- 不应继续在 `Moonglade.Web` 中扩展租户注册、站点创建、成员管理、计费或 SaaS 平台级管理能力。
- 后续可将该页面收窄或改名为当前站点 `Domains` 管理。
- 完整平台级 `Sites` 管理应放入 `Moonglade.SaaS.Web`。

## 3. 阶段验收结果

### 3.1 自动测试

本阶段已运行并通过：

```bash
dotnet build src/Moonglade.Web/MoongladePure.Web.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet build src/Moonglade.Migration/MoongladePure.Migration.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SiteManagementTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SiteScopedSpecTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter MigrationToolTests -p:UseSharedCompilation=false -maxcpucount:1
dotnet build src/Moonglade.SaaS.Web/MoongladePure.SaaS.Web.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SaaS -p:UseSharedCompilation=false -maxcpucount:1
```

结果：

- `SiteManagementTests`: 6 passed。
- `SiteScopedSpecTests`: 23 passed。
- `MigrationToolTests`: 12 passed。
- `SaaS`: 30 passed。
- Web 项目构建通过。
- SaaS Web 项目构建通过。
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

## 5. SaaS 发布架构决策

SaaS 能力不应直接内置进默认单站点 Web 发布包。默认发布包仍然应该是一个干净的单站点博客系统；部署者不启用 SaaS 时，产物不应包含 SaaS 注册、租户门户、站点创建、成员管理、计费、域名验证等功能入口。

推荐采用同一 solution 下的独立发布入口，并区分普通部署和 SaaS 部署：

```text
src/Moonglade.Web/              # 默认单站点博客发布入口
src/Moonglade.SaaS/             # SaaS 应用层：租户、注册、域名验证、站点生命周期
src/Moonglade.SaaS.Web/         # SaaS 平台入口和 host 网关
```

这个方向的边界：

- 普通单站点部署只启动 `Moonglade.Web`。
- SaaS 部署对外暴露 `Moonglade.SaaS.Web`，同时在内部运行 `Moonglade.Web` 作为博客渲染服务。
- `Moonglade.SaaS.Web` 负责平台首页、注册、租户/站点管理、host 分流、未知 host 404、自定义域名验证和后续转发策略。
- `Moonglade.Web` 负责博客前台和后台渲染，不引用 `Moonglade.SaaS`，默认打包结果不包含 SaaS 功能。
- SaaS 网关将已解析、已允许的用户子域名或 verified custom domain 转发给内部 `Moonglade.Web`，并保留 `Host` / `X-Forwarded-Host`，让 `RequestSiteContext` 继续按 host 解析当前 `SiteId`。
- `Moonglade.SaaS.Web` 不应复制完整博客 Razor UI；博客体验仍由 `Moonglade.Web` 承担。
- 现有 `Tenant`、`Site`、`SiteDomain`、`SiteMembership` 仍作为共享数据模型基础。
- `SystemIds.DefaultSiteId` 继续服务 legacy 迁移、单站点兼容和非 SaaS 部署，不作为 SaaS 平台入口语义。
- SaaS 平台入口不应被建模为普通用户 `Site`，避免平台营销页、注册页和用户博客内容混用同一套站点设置、主题和文章查询逻辑。

暂不建议拆成独立仓库。当前数据库模型、迁移工具和博客核心功能仍高度共享；拆仓会增加 schema、迁移和核心 bugfix 的同步成本。同仓库多发布项目可以同时满足默认包干净、SaaS 独立演进、共享核心能力和网关转发四个目标。

也不建议仅通过 `SaaS:Enabled` 在 `Moonglade.Web` 内隐藏功能。运行时开关无法保证发布产物不包含 SaaS controller、view、API 和依赖，也容易让单站点部署承担 SaaS 的复杂度。

### 5.0 SaaS 数据存储模型

当前规划采用共享数据库、共享表结构、逻辑多租户隔离，不采用每个用户一个数据库或每个站点一套表。

核心层级：

```text
Tenant
  -> Site
      -> Post / Page / Menu / Comment / Config / Theme / Media / AI data
```

主要边界：

- `Tenant` 表示租户或组织。
- `Site` 表示一个博客站点。
- `SiteDomain` 保存用户默认子域名和自定义域名绑定。
- `User` 保存平台用户。
- `SiteMembership` 保存用户在站点内的角色。
- 文章、页面、菜单、评论、配置、主题、媒体和 AI 数据通过 `SiteId` 隔离。

请求路径：

```text
Host -> Moonglade.SaaS.Web 网关校验 -> Moonglade.Web -> RequestSiteContext -> SiteDomain -> SiteId
```

示例：

```text
alice.app.example.com -> SiteId A -> 只读写 Alice 站点数据
bob.app.example.com   -> SiteId B -> 只读写 Bob 站点数据
```

因此 SaaS 的核心要求不是拆库，而是确保所有博客业务查询和写入都经过当前 `SiteId` 边界。

### 5.1 SaaS host 模型

SaaS 发布入口使用配置项定义平台域名和用户站点子域根：

```text
SaaS:PortalHosts               # 例如 example.com,www.example.com
SaaS:SiteSubdomainRoot         # 例如 app.example.com
```

访问策略：

- 平台主域名访问 `PortalHosts`，展示营销注册页和登录/注册入口。
- 用户注册时填写唯一 username，并自动获得 `{username}.{SiteSubdomainRoot}`，例如 `alice.app.example.com`。
- 用户自定义域名必须完成验证后才参与站点 host 路由。
- 未注册、未绑定或未验证的 host 由 `Moonglade.SaaS.Web` 返回友好的 404 页面，并引导回平台官网。
- 已注册用户子域名和 verified custom domain 后续由 `Moonglade.SaaS.Web` 转发到内部 `Moonglade.Web` 渲染博客。
- 非 SaaS 发布入口继续保持当前单站点兼容行为，不启用上述 host 策略。

username 同时承担默认子域名前缀语义，需要建立保留词和格式限制：

- 只允许小写字母、数字和短横线。
- 不能以短横线开头或结尾。
- 长度需要设置上下限。
- 禁止平台保留词，例如 `www`、`admin`、`api`、`auth`、`mail`、`smtp`、`cdn`、`static`、`assets`、`app`、`blog`、`docs`、`support`、`status`、`root`、`localhost`。

### 5.2 自定义域名验证

自定义域名是 SaaS 付费能力，不能只保存绑定记录。建议使用通用 TXT 记录验证：

```text
_moonglade.example.com TXT moonglade-site-verification=<token>
```

`SiteDomain` 或 SaaS 域名扩展模型需要具备：

- 验证状态：`PendingVerification`、`Verified`、`Rejected` 或等价状态。
- 验证 token。
- 最近验证时间。
- 验证通过时间。
- 失败原因或最近错误。

只有 `Verified` 自定义域名参与 host 到 site 的解析。MVP 可以先落库状态、生成 token、暴露验证说明和管理 API；真正的 DNS TXT 查询如果需要引入 DNS client 依赖，需要单独评估并获得明确确认。

### 5.3 SaaS 任务拆分

建议按以下顺序推进：

1. 新增 `Moonglade.SaaS` 和 `Moonglade.SaaS.Web` 项目骨架，保持 `Moonglade.Web` 默认发布包不引用 SaaS。
2. 在 SaaS Web 入口实现 Portal host、用户子域、verified custom domain 和 unknown host 的分流策略。
3. 建立 username/subdomain 校验服务，覆盖格式、保留词和唯一性测试。
4. 实现注册后 Tenant、User、Site、SiteMembership、默认子域名、默认配置、主题和菜单初始化。
5. 增加 SaaS 网关到内部 `Moonglade.Web` 的转发策略，保留原始 host 或 `X-Forwarded-Host`。
6. 实现自定义域名 pending/verified 状态、TXT token 生成、验证说明和管理 API。
7. 增加最小 Portal 营销注册页，后续再补完整文案、定价、示例站点和转化流程。

### 5.4 已启动的 SaaS 基线

已完成第一片 SaaS 代码基线：

- 新增 `src/Moonglade.SaaS/MoongladePure.SaaS.csproj`，保存 SaaS 纯应用规则。
- 新增 `src/Moonglade.SaaS.Web/MoongladePure.SaaS.Web.csproj`，作为独立 SaaS 发布入口。
- `Moonglade.Web` 未引用 SaaS 项目，默认单站点发布包继续保持独立。
- 已实现 Portal host、用户子域、custom domain candidate 和 unknown host 的基础分类。
- 已实现 username/subdomain 格式校验和保留词校验。
- 已实现自定义域名 TXT 记录名和值的生成规则，以及 32-byte hex token 生成。
- 已增加最小 Portal 页面和未知域名 404 响应。
- 已增加 SaaS 规则单元测试，覆盖平台域名、用户子域、嵌套非法子域、保留词、自定义域名候选、username 规则和 TXT 记录格式。

已完成第二片 SaaS host 数据库接入：

- `SiteDomain` 增加验证状态、验证 token、最近验证时间、验证通过时间和最近错误字段。
- 默认单站点后台新增域名仍写入 `Verified`，保持现有兼容体验。
- SaaS custom domain candidate 已接入数据库查询，只有 `Verified` 域名会映射到站点。
- `PendingVerification`、`Rejected` 和不存在的自定义域名都返回 SaaS 404。
- 已为 resolver 层补充 verified、pending、rejected、missing 和 blank host 单元测试。
- 已为 SaaS Web root endpoint 补充 Portal、verified custom domain 和 pending custom domain 行为测试。

已完成第三片 SaaS 注册初始化和用户子域数据库映射：

- 新增 `SaaSSiteProvisioningService`，提供注册后的最小站点初始化流程。
- 初始化时创建 `Tenant`、`User`、`Site`、owner `SiteMembership`、默认用户子域名、默认配置、主题和菜单。
- 默认用户子域名写入 `SiteDomain`，状态为 `Verified`，作为 SaaS 子域访问的数据库来源。
- 新增 `UserSubdomainSiteResolver`，按 username、host、active site 和 owner membership 解析真实站点。
- SaaS root endpoint 已将 `{username}.{SiteSubdomainRoot}` 从占位响应改为真实站点映射。
- 未注册或未初始化的用户子域继续返回 SaaS 404。
- 已增加注册初始化和用户子域 endpoint 行为测试。

已修复 SaaS Web fresh database 启动问题：

- `Moonglade.SaaS.Web` 启动时会执行 `UpdateDbAsync<BlogDbContext>()`，确保 code-first schema 已应用。
- SaaS Web 不执行默认博客 seed；租户、用户和站点数据仍由后续注册流程创建。
- 本地手动测试时，如果使用新的 SQLite 文件，启动后应具备 `SiteDomain` 等新 schema 表，不再因 fresh database 缺表导致 host resolver 抛异常。

已完成第四片 SaaS 最小注册入口：

- 新增 `SaaSRegistrationEndpoint`，把 `SaaSSiteProvisioningService` 接入真实 HTTP 入口。
- `GET /register` 返回最小注册表单。
- `POST /register` 支持表单提交并创建租户、用户、站点、owner membership、默认子域名、默认配置、主题和菜单。
- `POST /api/register` 支持 JSON 注册请求，成功返回 `201 Created` 和站点 host，失败返回 `400 Bad Request`。
- 注册入口会校验密码长度、字母数字组合和允许字符，并使用 PBKDF2 + salt 保存 `PasswordSalt` 与 `PasswordHash`。
- 继续复用 username/subdomain 规则，重复 username 或重复默认子域名仍由 provisioning service 拒绝。
- 已增加注册 endpoint 单元测试，覆盖成功初始化、弱密码拒绝和重复 username 拒绝。

仍未实现：

- SaaS 网关到内部 `Moonglade.Web` 的真实转发。
- 完整登录会话、邮箱验证、找回密码和注册后的控制台跳转。
- SaaS 平台级站点管理、成员管理、权限管理和计费。
- DNS TXT 自动查询验证器。

## 6. 下一阶段 SaaS 规划

### 6.1 站点生命周期

目标：

- 支持创建站点。
- 支持编辑站点基础信息。
- 支持禁用或归档站点。
- 支持管理站点主域名和备用域名。

建议先做：

1. `CreateSiteCommand` 和 `UpdateSiteCommand`。
2. 站点创建时自动建立默认配置、主题、菜单和 owner membership。
3. 后台 UI 增加站点创建和编辑入口。
4. SaaS 发布入口对未绑定域名返回 404；默认单站点发布入口继续保持现有 fallback 兼容行为。

### 6.2 租户与注册

目标：

- 支持平台用户注册。
- 支持用户创建租户或加入租户。
- 支持租户下多个站点。

建议先做：

1. 明确 `Tenant`、`User`、`SiteMembership` 的产品语义。
2. 注册时要求填写全局唯一 username，并用它生成默认子域名。
3. 增加租户 owner 初始化流程。
4. 增加注册、邀请和接受邀请流程。
5. 为后续计费和资源限制保留租户状态字段。

### 6.3 成员、角色和权限

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

### 6.4 多站点数据隔离审计

目标：

- 确认所有站点业务数据都经过 `SiteId` 边界。

建议继续审计：

- 非文章后台编辑路径。
- 配置保存路径。
- 主题保存路径。
- 媒体上传和删除路径。
- 导出和导入路径。
- RSS、Sitemap、Manifest、OpenSearch 等中间件路径。

## 7. 下一阶段 AI 规划

### 7.1 AI Job 状态机

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

### 7.2 AI Artifact 审核和发布

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

### 7.3 AI 使用量和成本

目标：

- 为 SaaS 计费和限额做准备。

建议增加：

- token 输入/输出。
- provider、model、duration。
- estimated cost。
- tenant/site/user 维度聚合。

### 7.4 AI 功能扩展

可以按优先级拆分：

1. 摘要和 SEO meta 自动生成。
2. 多语言翻译和人工修订。
3. 标签建议。
4. 评论辅助和审核辅助。
5. 文章问答。
6. 配图 prompt 和图片生成。

## 8. MySQL 和媒体后续验证

### 8.1 MySQL legacy migration

当前迁移工具以 legacy SQLite 为主，不能假设 MySQL 旧库已经同等成熟。

后续需要：

1. 准备真实 legacy MySQL 样例库。
2. 固化旧 MySQL schema 和迁移版本。
3. 实现或验证 MySQL source reader。
4. 跑与 SQLite 同等级别的 preflight、dry-run、migrate、validate。

### 8.2 媒体文件审计

当前数据库内资产和元数据已经迁移，但文件系统或对象存储中的真实文件仍需部署侧审计。

后续需要：

1. 扫描文章、页面、主题和配置里的媒体 URL。
2. 对本地文件或对象存储 key 做存在性检查。
3. 生成缺失文件报告。
4. 决定是否补建 `MediaAsset` metadata。

## 9. 发布前检查清单

进入下一次可发布节点前，至少需要确认：

1. 完整 solution 测试通过。
2. `./lint.sh` 通过。
3. `git diff --check` 通过。
4. 使用真实迁移库完成浏览器登录后的后台手动测试。
5. 站点域名绑定新增、删除和重复 host 错误提示在浏览器中确认。
6. 明确 release note 中 SQLite/MySQL 迁移支持边界。
7. 确认默认 `Moonglade.Web` 发布包不引用 SaaS 项目，也不包含 SaaS controller、view、API 和依赖。
8. SaaS Web 使用 fresh SQLite 文件启动时能自动应用 schema，并对未知 host 返回 SaaS 404。
9. 不提交本地运行配置，例如 `src/Moonglade.Web/appsettings.json` 的私有改动，也不提交本地生成的 SQLite 数据库文件。

## 10. 当前结论

截至 2026-05-09，数据库 code-first 重构已经完成一个可继续演进的阶段性基线。默认发布入口应继续保持单站点博客定位；SaaS 能力应通过同一 solution 下的独立应用层和独立平台入口/网关推进，避免污染默认开源打包产物。接下来不应继续在旧单站点假设上横向堆字段，而应围绕 SaaS 和 AI 两条主线推进：

- SaaS 主线先补独立平台入口/网关、Portal host、用户子域、verified custom domain、网关转发、站点生命周期、租户注册、成员权限和未知 host 404 策略。
- AI 主线先补 job claiming、artifact 审核发布、使用量统计和多语言内容工作流。

当前最稳妥的下一步，是先补 `Moonglade.SaaS.Web` 到内部 `Moonglade.Web` 的最小转发策略，确认 accepted host 能进入博客渲染服务、unknown host 仍由 SaaS 网关拦截。转发能力建议优先评估 YARP；如果要新增该依赖，需要单独确认依赖变更。与此同时，继续保留浏览器环境下后台站点管理 UI 的人工验收任务。

## 11. 新任务重启入口

如果新的任务从这里重新开始，建议按以下边界接手：

1. 保持 `Moonglade.Web` 不引用 `Moonglade.SaaS` 或 `Moonglade.SaaS.Web`。
2. 测试项目也不要引用 `Moonglade.SaaS.Web`，避免发布入口的 `appsettings.json` 污染 `Moonglade.Web` 集成测试配置；需要测试 endpoint 行为时，测试 `Moonglade.SaaS` 应用层里的 `SaaSRootEndpoint`。
3. 当前已具备 SaaS 注册后的最小站点初始化服务、用户子域数据库映射、`GET /register` 注册表单、`POST /register` 表单提交和 `POST /api/register` JSON 注册入口。
4. 下一步建议先补 SaaS 网关到内部 `Moonglade.Web` 的最小转发；优先评估 YARP，新增依赖前需要明确确认。
5. 转发必须保留原始 host 或设置 `X-Forwarded-Host`，确保内部 `Moonglade.Web` 的 `RequestSiteContext` 仍按 host 解析 `SiteId`。
6. 未知用户子域应继续返回 SaaS 404，默认 `Moonglade.Web` 仍保持单站点 fallback 兼容。
7. `Moonglade.SaaS.Web` 启动时会应用数据库迁移，但不 seed 默认博客数据。
8. 暂不引入 DNS TXT 查询依赖；自定义域名的 DNS 验证执行器另开任务评估。
9. 每次改动至少运行：

```bash
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SaaS -p:UseSharedCompilation=false -maxcpucount:1
dotnet build src/Moonglade.SaaS.Web/MoongladePure.SaaS.Web.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
git diff --check
```

交接或提交前还需要跑一次全量 `dotnet test`，确认 SaaS 发布入口和默认 Web 集成测试没有配置输出污染。

继续提交时不要包含 `src/Moonglade.Web/appsettings.json` 的本地私有配置改动。

## 12. 2026-05-09 继续推进记录

本次新增：

- `src/Moonglade.SaaS/Registration/SaaSRegistrationEndpoint.cs`
- `src/Moonglade.SaaS/Registration/SaaSRegistrationHtml.cs`
- `src/Moonglade.SaaS/Registration/SaaSRegistrationInput.cs`
- `src/Moonglade.SaaS/Registration/SaaSRegistrationResponse.cs`
- `tests/Moonglade.Tests/SaaS/SaaSRegistrationEndpointTests.cs`

本次修改：

- `src/Moonglade.SaaS.Web/Program.cs` 接入 `/register`、`POST /register` 和 `POST /api/register`。
- `database-code-first-refactor-plan.md` 更新当前进展、测试指南和提交建议。

本次已执行测试：

```bash
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore --filter SaaS -p:UseSharedCompilation=false -maxcpucount:1
dotnet build src/Moonglade.SaaS.Web/MoongladePure.SaaS.Web.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
git diff --check
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
./lint.sh
```

结果：

- `SaaS`: 33 passed。
- `Moonglade.SaaS.Web` build succeeded。
- `git diff --check`: passed。
- 全量测试：89 passed。
- `./lint.sh`: passed。此前剩余的 `tests/Moonglade.Tests/MigrationToolTests.cs` 第 80 行和第 117 行 redundant qualifier 已修复。
- `dotnet build`、`dotnet test` 和 `./lint.sh` restore 阶段都出现 `NU1900` warning，原因是当前环境无法读取 `https://nuget.aiursoft.com/v3/index.json` 的 package vulnerability metadata；未影响构建和测试结果。

提交前建议补跑：

```bash
git diff --check
dotnet test tests/Moonglade.Tests/MoongladePure.Tests.csproj --no-restore -p:UseSharedCompilation=false -maxcpucount:1
./lint.sh
```

建议 commit 信息：

```text
feat: add SaaS registration endpoint
```

提交范围建议只包含本次 SaaS 注册入口、对应测试、lint 修复和本文档更新；不要包含 `src/Moonglade.Web/appsettings.json` 的本地私有配置改动。

## 13. 2026-05-09 会话收尾状态

最后收尾任务已完成：

- 修复 `./lint.sh` 剩余错误：`tests/Moonglade.Tests/MigrationToolTests.cs` 中两处 `MoongladePure.Migration.Program.Main` 改为 `Program.Main`，匹配文件顶部已有 `using MoongladePure.Migration;`。
- `./lint.sh` 已通过，输出为 `Linting PASSED! No warnings found.`。
- `MigrationToolTests` 已单独验证通过：12 passed。
- `git diff --check` 已通过。

当前工作区检查结果：

- 需要提交的收尾改动：`tests/Moonglade.Tests/MigrationToolTests.cs` 和 `database-code-first-refactor-plan.md`。
- 仍存在本地私有配置改动：`src/Moonglade.Web/appsettings.json` 中 `OpenAI` token、endpoint 和 model 被改成本地值；不要纳入提交。

下一次新会话可以从这里继续：

1. 先运行 `git status --short`，确认除 `src/Moonglade.Web/appsettings.json` 外没有意外改动。
2. 如需提交本轮成果，先查看 `git diff`，然后只 stage 相关代码和本文档。
3. 建议提交信息：

```text
feat: add SaaS registration endpoint
```

如果只提交最后的 lint 收尾改动，可使用：

```text
chore: fix migration test lint
```
