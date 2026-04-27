# MoongladePure 数据库代码优先重构计划书

## 1. 背景与目标

MoongladePure 当前是一个单站点博客系统，功能已经覆盖文章发布、分类标签、评论、页面、菜单、友链、主题、站点配置、图片资产、RSS/Atom、搜索、统计、导出、后台账号和部分 AI 自动处理能力。现有数据模型以历史迁移和 provider-specific 配置为中心，业务上仍然默认“一个应用实例等于一个博客站点”。

本次重构的目标不是改变 UI 或终端用户功能，而是重建底层数据库抽象，使系统从数据库优先逐步转向代码优先，并为后续 SaaS 化、多租户、多站点和 AI 功能扩展打基础。

“无感升级”在本计划中定义为：迁移完成后终端用户看到的站点功能、URL、文章、评论、设置、账号和资源保持一致；开发者或运维可以使用离线迁移工具、双实例、新旧库转换、人工校验等复杂流程，不要求零停机，也不要求旧实例原地自动升级。

## 2. 当前系统功能分析

### 2.1 文章与内容

核心实体是 `PostEntity`，当前字段包括：

- 基础内容：`Title`、`Slug`、`Author`、`RawContent`、`InlineCss`、`HeroImageUrl`。
- 发布状态：`IsPublished`、`IsDeleted`、`PubDateUtc`、`CreateTimeUtc`、`LastModifiedUtc`。
- 展示行为：`CommentEnabled`、`IsFeedIncluded`、`IsFeatured`、`IsOriginal`、`OriginLink`。
- 多语言痕迹：`ContentLanguageCode`、`ContentAbstractZh`、`ContentAbstractEn`、`LocalizedChineseContent`、`LocalizedEnglishContent`、`LocalizeJobRunAt`。
- 访问优化：`HashCheckSum` 用于按日期和 slug 查找文章。

文章 URL 逻辑依赖 `PubDateUtc` 的日期和 `Slug`。创建文章时只检查“当天同 slug”冲突，冲突时追加随机后缀。查询时优先按 `HashCheckSum` 查询，找不到再回退到 `PubDateUtc.Date + Slug`。这说明 permalink 是核心兼容点，迁移后必须保留旧 URL 行为。

### 2.2 分类与标签

分类是 `CategoryEntity`，通过 `PostCategoryEntity` 连接文章。标签是 `TagEntity`，通过 `PostTagEntity` 连接文章。

当前业务规则：

- 分类 `RouteName` 创建时会检查是否重复，但数据库没有唯一约束。
- 标签创建时以 `NormalizedName` 判断重复，但数据库没有唯一约束。
- 文章更新时会删除旧关联再重建新关联。

分类和标签都应在新模型中归属到站点维度，并增加站点内唯一约束。

### 2.3 评论与回复

评论使用 `CommentEntity`，回复使用 `CommentReplyEntity`。评论绑定文章，回复绑定评论。

当前行为：

- 评论可按站点配置决定是否需要审核。
- 评论记录用户名、邮箱、IP、创建时间和审批状态。
- 回复只有内容、创建时间和评论 ID，没有操作者、审批状态、删除状态。
- AI 自动评论目前以特殊 `IPAddress = "127.0.0.1"` 和 `Username = "Qwen3"` 伪装成普通评论。

新模型应保留现有评论展示效果，但需要把人工评论、系统评论、AI 评论的来源表达为结构化字段。

### 2.4 页面、菜单、友链

页面使用 `PageEntity`，菜单使用 `MenuEntity` 和 `SubMenuEntity`，友链使用 `FriendLinkEntity`。

当前特征：

- 页面有 `Slug`、HTML、CSS、发布状态和侧边栏开关。
- 菜单只有两级结构：顶级菜单和子菜单。
- 友链是简单标题和 URL。
- 这些实体都没有站点维度，也缺少站点内唯一约束。

SaaS 化后这些都应该成为 site-scoped 数据。

### 2.5 账号与认证

本地账号使用 `LocalAccountEntity`，包括 `Username`、`PasswordSalt`、`PasswordHash`、最后登录时间和 IP。

当前行为：

- 支持旧密码 hash 在登录成功后迁移为带 salt 的新 hash。
- 本地账号是全局表，没有站点、租户、角色、成员关系。
- 当前 UI 看起来只表达后台管理员账号，不表达多站点成员、权限或平台账号。

新模型需要拆分平台用户、认证凭证、站点成员关系和角色。单站点迁移时可把旧账号迁移为默认租户下默认站点的 owner/admin。

### 2.6 配置、主题与资产

配置使用 `BlogConfigurationEntity`，以 `CfgKey + CfgValue(JSON)` 保存多个设置对象。主题使用 `BlogThemeEntity`，CSS rules 也是 JSON 字符串。站点图标等小资产使用 `BlogAssetEntity` 保存 base64。

当前配置项主要包括：

- `GeneralSettings`
- `ContentSettings`
- `FeedSettings`
- `ImageSettings`
- `AdvancedSettings`
- `CustomStyleSheetSettings`

当前问题是配置是全局的、弱类型落库、缺少唯一约束、缺少版本信息。新模型可以继续允许 JSON 配置，但应变为站点级，并引入 schema name、schema version、唯一键和更新时间。

### 2.7 图片与文件

普通上传图片通过 `IBlogImageStorage` 存储到文件系统或其他 provider，不直接进入关系数据库。`BlogAssetEntity` 主要保存站点图标等 base64 小资产。

SaaS 化后需要把媒体资产元数据纳入数据库：文件名、路径、provider、MIME、大小、hash、宽高、所属站点、创建者、用途。真实二进制仍可留在对象存储或文件系统。

### 2.8 导出与数据搬运

当前 `DataPortingController` 支持导出标签、分类、友链、页面和文章。文章导出包含标题、slug、摘要、原文、时间、评论开关、统计、状态、分类和标签，但没有完整覆盖所有字段，例如外链来源、头图、内联 CSS、本地化内容、AI 生成痕迹、评论、配置、主题、资产和账号。

新的迁移工具不能直接复用当前导出作为完整备份格式，需要单独设计迁移快照格式。

### 2.9 AI 功能现状

当前已有后台 AI 能力：

- 自动生成中英文摘要。
- 自动生成评论。
- 自动生成标签。
- 检测文章语言。
- 翻译文章到中文和英文。

这些结果目前大多直接写入 `Post` 宽表字段，AI 评论写入普通评论表。后续 AI 翻译、AI 问答、AI 配图如果继续直接扩展宽表，会很快变得难以维护。新库应把 AI 作业、AI 产物、AI provider、目标语言、状态、错误、token/成本、关联内容单独建模。

## 3. 当前数据库主要风险

### 3.1 单站点假设过强

所有业务数据都是全局表，没有 `TenantId` 或 `SiteId`。这会阻塞 SaaS 化，也会让未来“平台用户注册后获得独立网站”的模型难以表达。

### 3.2 数据一致性依赖应用代码

数据库缺少大量唯一约束和必要约束：

- 分类 `RouteName` 应站点内唯一。
- 页面 `Slug` 应站点内唯一。
- 标签 `NormalizedName` 应站点内唯一。
- 账号 `Username` 应按平台或租户唯一。
- 配置 `CfgKey` 应站点内唯一。
- 文章 permalink 应站点内唯一。

当前应用代码有部分检查，但并发场景下仍可能写入重复数据。

### 3.3 字段可空性过宽

多数核心字段是 nullable，例如标题、slug、内容、用户名、配置 key。这让数据修复成本转移到了运行时。新模型应明确 required / optional 边界。

### 3.4 AI 数据侵入核心内容表

`Post` 同时保存原文、摘要、翻译和 AI 作业时间。后续增加更多语言、多个模型、多版本翻译、人工修订、失败重试时，宽表会继续膨胀。

### 3.5 配置弱类型且无版本

配置 JSON 没有 schema version。代码升级后如果设置类字段变化，旧 JSON 的兼容行为不直观。新模型应保留灵活性，但要记录配置 schema、版本和迁移状态。

### 3.6 Provider 迁移历史不一致

MySQL 和 SQLite 当前各自维护迁移。SQLite 初始迁移历史中曾出现过隐式 many-to-many 表和 join 表主键变化，说明不同 provider 的迁移历史已经存在差异。代码优先重构时应以统一模型为源头，provider 只负责类型映射和 SQL 生成。

### 3.7 搜索和统计不适合 SaaS 规模

搜索当前会把候选文章拉到内存做匹配和打分。统计 `Hits`/`Likes` 是文章扩展表中的计数器。SaaS 化后需要更明确的索引、计数策略和可选搜索索引。

## 4. 新数据库设计原则

1. 所有站点业务数据必须显式归属 `SiteId`。
2. 平台级数据、租户级数据、站点级数据分层，不再混用全局单例表。
3. 数据一致性优先放到数据库约束，其次才是应用验证。
4. 保留旧 URL、旧展示行为和旧配置语义，迁移后功能不变。
5. AI 能力通过可扩展表建模，不再继续向 `Post` 宽表追加固定语言字段。
6. 多数据库支持由 ORM/provider 承担，模型层只使用通用类型和通用约束。
7. 新模型默认支持离线迁移、可校验、可重复执行、可追踪。
8. 对终端用户无感，对开发者允许复杂。

## 5. 目标数据域设计

### 5.1 平台与租户域

建议表：

- `Tenant`
- `Site`
- `SiteDomain`
- `User`
- `UserCredential`
- `ExternalLogin`
- `SiteMembership`
- `Role`
- `SiteMembershipRole`

关键字段：

- `Tenant.Id`
- `Tenant.Name`
- `Tenant.Status`
- `Site.Id`
- `Site.TenantId`
- `Site.Name`
- `Site.Slug`
- `Site.Status`
- `Site.DefaultCulture`
- `Site.TimeZoneId`
- `Site.CreatedAtUtc`
- `Site.UpdatedAtUtc`
- `SiteDomain.SiteId`
- `SiteDomain.Host`
- `SiteDomain.IsPrimary`
- `User.NormalizedUserName`
- `User.NormalizedEmail`
- `UserCredential.PasswordHash`
- `UserCredential.PasswordSalt`
- `SiteMembership.SiteId`
- `SiteMembership.UserId`
- `SiteMembership.DisplayName`

约束：

- `Site(TenantId, Slug)` 唯一。
- `SiteDomain(Host)` 唯一。
- `User(NormalizedUserName)` 唯一。
- `SiteMembership(SiteId, UserId)` 唯一。

迁移单站点时创建一个默认 `Tenant` 和默认 `Site`，所有旧数据挂载到默认 `SiteId`。

### 5.2 内容域

建议表：

- `Post`
- `PostContent`
- `PostRoute`
- `PostMetric`
- `PostCategory`
- `Category`
- `Tag`
- `PostTag`
- `Page`
- `PageContent`
- `ContentRevision`

`Post` 保存文章身份和状态：

- `Id`
- `SiteId`
- `Title`
- `Slug`
- `AuthorName`
- `Status`
- `IsDeleted`
- `IsFeatured`
- `IsFeedIncluded`
- `CommentEnabled`
- `IsOriginal`
- `OriginUrl`
- `HeroImageUrl`
- `InlineCss`
- `SourceLanguageCode`
- `CreatedAtUtc`
- `PublishedAtUtc`
- `UpdatedAtUtc`
- `DeletedAtUtc`

`PostContent` 保存不同语言和用途的内容：

- `Id`
- `SiteId`
- `PostId`
- `CultureCode`
- `ContentKind`
- `Body`
- `Abstract`
- `IsOriginal`
- `GeneratedBy`
- `GenerationId`
- `CreatedAtUtc`
- `UpdatedAtUtc`

`ContentKind` 初始可包括 `RawMarkdown`、`RenderedHtml`、`Translation`、`Summary`。为了兼容现有行为，第一阶段可以只写 `RawMarkdown` 和目标语言翻译，渲染仍在运行时完成。

`PostRoute` 保存 permalink：

- `Id`
- `SiteId`
- `PostId`
- `RouteDate`
- `Slug`
- `HashCheckSum`
- `IsCanonical`
- `CreatedAtUtc`

这样可以保留旧的“日期 + slug”访问，并允许未来修改 slug 后保留历史路由。

约束：

- `Post(SiteId, Id)` 主体归属。
- `PostRoute(SiteId, RouteDate, Slug)` 唯一。
- `PostRoute(SiteId, HashCheckSum)` 可建索引，但不建议作为唯一性的唯一来源，因为 checksum 理论上可能碰撞。
- `Category(SiteId, RouteName)` 唯一。
- `Tag(SiteId, NormalizedName)` 唯一。
- `PostCategory(PostId, CategoryId)` 唯一。
- `PostTag(PostId, TagId)` 唯一。

### 5.3 评论域

建议表：

- `Comment`
- `CommentModerationEvent`

可以把旧的 `CommentReply` 合并进 `Comment.ParentCommentId`，也可以保留独立回复表。若优先减少业务改造，第一阶段保留 `Comment` + `CommentReply` 更稳；若优先长期一致性，建议统一为树形 `Comment`。

推荐长期模型：

- `Comment.Id`
- `Comment.SiteId`
- `Comment.PostId`
- `Comment.ParentCommentId`
- `Comment.AuthorName`
- `Comment.AuthorEmail`
- `Comment.AuthorIp`
- `Comment.Content`
- `Comment.Status`
- `Comment.Source`
- `Comment.CreatedAtUtc`
- `Comment.UpdatedAtUtc`

`Source` 可表达 `Visitor`、`Admin`、`System`、`AiGenerated`。AI 评论不再依赖特殊 IP 和用户名识别。

约束：

- `Comment.SiteId` 必须与 `Post.SiteId` 一致。
- 删除文章时评论可 cascade 或软删除；SaaS 场景建议文章软删除时评论保留，物理清理由后台任务处理。

### 5.4 站点设置与主题域

建议表：

- `SiteSetting`
- `Theme`
- `SiteTheme`
- `CustomCss`

`SiteSetting`：

- `Id`
- `SiteId`
- `SchemaName`
- `SchemaVersion`
- `JsonValue`
- `UpdatedAtUtc`

约束：

- `SiteSetting(SiteId, SchemaName)` 唯一。

这样可以保留当前 JSON 配置模式，但把全局配置变成站点级配置，并为配置迁移提供版本信息。

主题建议拆分系统主题和用户主题：

- 系统主题可以由代码 seed 或静态配置提供。
- 用户主题保存在 `Theme`，通过 `SiteTheme` 选择使用。
- `Theme(SiteId, ThemeName)` 对用户主题唯一；系统主题 `SiteId` 可为空或挂平台级。

### 5.5 导航、页面与友链域

建议表：

- `Page`
- `PageContent`
- `Menu`
- `MenuItem`
- `FriendLink`

菜单改为统一 `MenuItem`：

- `MenuItem.Id`
- `SiteId`
- `ParentId`
- `Title`
- `Url`
- `Icon`
- `DisplayOrder`
- `OpenInNewTab`

现有两级菜单可直接迁移为 `ParentId` 结构，未来允许多级但 UI 仍只展示两级。

约束：

- `Page(SiteId, Slug)` 唯一。
- `MenuItem(SiteId, ParentId, DisplayOrder)` 建索引。
- `FriendLink(SiteId, DisplayOrder)` 建索引。

### 5.6 媒体资产域

建议表：

- `MediaAsset`
- `MediaVariant`

`MediaAsset`：

- `Id`
- `SiteId`
- `OwnerUserId`
- `Provider`
- `Bucket`
- `ObjectKey`
- `OriginalFileName`
- `PublicUrl`
- `MimeType`
- `FileSize`
- `ContentHash`
- `Width`
- `Height`
- `CreatedAtUtc`

`MediaVariant`：

- `Id`
- `MediaAssetId`
- `VariantName`
- `ObjectKey`
- `Width`
- `Height`
- `FileSize`

当前文件系统图片可以迁移为 metadata-only 记录，真实文件继续留在原目录。当前 `BlogAsset` 中的 base64 站点图标应转成 `MediaAsset` 或保留一个 `SiteBinaryAsset` 兼容表；长期建议统一到媒体资产。

### 5.7 AI 扩展域

建议表：

- `AiProvider`
- `AiPromptTemplate`
- `AiJob`
- `AiArtifact`
- `AiTranslation`
- `AiQuestion`
- `AiAnswer`
- `AiGeneratedImage`
- `AiUsage`

`AiJob`：

- `Id`
- `SiteId`
- `JobType`
- `TargetEntityType`
- `TargetEntityId`
- `Provider`
- `Model`
- `Status`
- `RequestedByUserId`
- `StartedAtUtc`
- `FinishedAtUtc`
- `ErrorMessage`

`AiArtifact`：

- `Id`
- `SiteId`
- `JobId`
- `TargetEntityType`
- `TargetEntityId`
- `ArtifactType`
- `CultureCode`
- `Content`
- `MetadataJson`
- `CreatedAtUtc`

`ArtifactType` 初始可包括：

- `Summary`
- `Translation`
- `Comment`
- `Tags`
- `Question`
- `Answer`
- `ImagePrompt`
- `GeneratedImage`

当前 `ContentAbstractZh`、`ContentAbstractEn`、`LocalizedChineseContent`、`LocalizedEnglishContent` 可迁移为 `PostContent` 或 `AiArtifact`。面向展示的最终内容应在 `PostContent`，AI 过程和原始输出应在 `AiArtifact`。

## 6. 旧库到新库的映射

### 6.1 初始化默认租户和站点

迁移工具先创建：

- 默认 `Tenant`：从旧站点标题或固定名称生成。
- 默认 `Site`：从 `GeneralSettings.SiteTitle`、`TimeZoneId`、语言配置等生成。
- 默认主域名：如果迁移输入提供旧域名则写入 `SiteDomain`，否则留空，部署时配置。

### 6.2 文章迁移

旧 `Post` 到新表：

- `Post.Id` 保留原 Guid。
- `Title`、`Slug`、`Author`、`CommentEnabled`、`IsFeedIncluded`、`IsFeatured`、`IsOriginal`、`OriginLink`、`HeroImageUrl`、`InlineCss` 原样映射。
- `IsPublished`、`IsDeleted` 映射为 `Status` + `IsDeleted` 或统一状态枚举。
- `CreateTimeUtc`、`PubDateUtc`、`LastModifiedUtc` 映射到对应时间字段。
- `ContentLanguageCode` 映射为 `SourceLanguageCode`。
- `RawContent` 写入 `PostContent(ContentKind = RawMarkdown, IsOriginal = true)`。
- `ContentAbstractZh`、`ContentAbstractEn` 写入对应语言的 summary 内容。
- `LocalizedChineseContent`、`LocalizedEnglishContent` 写入对应语言的 translation 内容。
- `HashCheckSum` 写入 `PostRoute`。

`PostRoute.RouteDate` 对已发布文章使用 `PubDateUtc.Date`。草稿没有公开 URL，可不生成 canonical route，或生成内部 route 但标记为非公开。

### 6.3 分类标签迁移

- 旧分类保留 Guid，挂默认 `SiteId`。
- 旧标签原来是 int 自增，建议迁移时改为 Guid 或 long；如果新模型使用 Guid，则建立临时映射表 `OldTagId -> NewTagId`。
- 分类和标签迁移前先按站点内唯一键去重。重复时保留最早记录，并把关联迁移到保留记录；同时输出冲突报告。

### 6.4 评论迁移

若新模型使用统一 `Comment`：

- 旧 `Comment` 迁移为 `ParentCommentId = null`。
- 旧 `CommentReply` 迁移为 `ParentCommentId = old Comment.Id`，`Source = Admin`。
- 旧 AI 评论识别规则可暂时使用 `IPAddress == "127.0.0.1"` 且 `Username` 为已知 AI 名称，迁移为 `Source = AiGenerated`。

若第一阶段保留回复表，则只增加 `SiteId`、`Source`、审计字段即可。

### 6.5 配置迁移

- 旧 `BlogConfiguration.CfgKey` 映射为 `SiteSetting.SchemaName`。
- `CfgValue` 原样写入 `JsonValue`。
- `SchemaVersion` 初始设为 `1`。
- 缺失的配置项由代码默认值补齐。

迁移工具应在写入前反序列化旧 JSON，验证能被当前设置类型读取；失败时输出明确错误，不静默吞掉配置。

### 6.6 账号迁移

- 旧 `LocalAccount` 迁移为平台 `User`。
- `Username` 映射到 `User.NormalizedUserName`。
- `PasswordHash`、`PasswordSalt` 写入 `UserCredential`。
- 每个旧账号加入默认 `SiteMembership`，角色设为 `Admin`。
- 如果旧账号没有 salt，保留旧 hash 格式标记，登录成功后继续懒迁移，或迁移工具要求管理员重置密码。

### 6.7 页面、菜单、友链、主题和资产迁移

- `CustomPage` 迁移到 `Page` + `PageContent`。
- `Menu` 迁移为顶级 `MenuItem`。
- `SubMenu` 迁移为带 `ParentId` 的 `MenuItem`。
- `FriendLink` 挂默认 `SiteId`。
- `BlogTheme` 迁移到 `Theme`，系统主题可由新版本 seed；用户主题必须迁移。
- `BlogAsset` 迁移为站点媒体资产或兼容二进制资产。

## 7. 迁移工具设计

迁移工具建议独立于 Web 应用，可以是控制台程序：

```text
moonglade-migrate
  --source-provider sqlite|mysql
  --source-connection ...
  --target-provider sqlite|mysql
  --target-connection ...
  --site-host example.com
  --dry-run
  --report ./migration-report.json
```

### 7.1 迁移流程

1. 创建目标库并应用新代码优先迁移。
2. 读取源库 schema 和迁移版本，确认支持的旧版本范围。
3. 对源库做只读快照分析，输出数据计数和潜在冲突。
4. 执行 dry-run 转换，生成映射表和校验报告。
5. 写入目标库，顺序为租户站点、用户、设置、主题、分类标签、文章、页面、菜单、友链、评论、媒体、AI 产物。
6. 执行完整校验。
7. 启动新实例连接新库，由维护者验收。
8. 切换流量或域名。

### 7.2 校验规则

必须校验：

- 文章数量、已发布数量、草稿数量、回收站数量一致。
- 每篇已发布文章旧 URL 在新库可解析。
- 分类、标签、文章关联数量一致。
- 评论和回复数量一致。
- 文章 hits/likes 一致。
- 页面 slug 一致。
- 配置 key 完整，JSON 可解析。
- 本地账号数量一致，至少一个 admin 可登录。
- 媒体资产引用不丢失。

建议校验：

- 对文章正文、页面正文、评论正文计算 hash。
- 对旧导出和新导出的核心字段做 diff。
- 对重复 slug、重复 tag、重复 category 输出人工确认清单。

### 7.3 回滚策略

本方案不要求目标库覆盖旧库。迁移过程中旧库保持只读或备份状态，失败时直接继续使用旧实例。切换前不删除旧库、不覆盖旧文件存储。

### 7.4 性能策略

迁移工具应按表批量读取和批量写入。对文章、评论、媒体等大表使用分页批处理。外部对象存储、图片复制、AI 产物生成不应在基础数据迁移中逐条同步执行；基础迁移只迁 metadata 和引用，耗时任务交给后续后台补偿。

## 8. 代码优先实施计划

### 阶段 0：冻结现状和补充测试

- 固化当前旧库 schema 文档。
- 准备 SQLite 和 MySQL 样例旧库。
- 增加迁移前后的数据一致性测试。
- 增加 permalink 兼容测试。

### 阶段 1：建立新数据模型

- 新增代码优先实体和 Fluent 配置。
- 统一 provider-agnostic 配置，避免把核心约束写在单个 provider 项目中。
- 明确所有 required 字段、最大长度、索引、唯一约束和删除行为。
- 引入 `SiteId` 查询边界。

### 阶段 2：实现迁移工具

- 实现旧库读取层。
- 实现目标库写入层。
- 实现 ID 映射和冲突报告。
- 实现 dry-run、校验和报告输出。

### 阶段 3：让现有 UI 运行在新模型上

- 引入默认 `SiteContext`，单站点部署时自动解析默认站点。
- 保持现有 Razor Pages、Controller 和页面行为不变。
- Repository 查询全部增加站点边界。
- 保留旧 URL 解析行为。

### 阶段 4：AI 数据模型迁移

- 把摘要、翻译和 AI 评论从核心宽表迁到 `PostContent` / `AiArtifact`。
- 后台任务改为写 `AiJob` 状态和产物。
- 保留当前前台展示逻辑需要的读取投影。

### 阶段 5：SaaS 能力启用

- 增加租户注册和站点创建流程。
- 增加域名解析和站点上下文。
- 增加成员、角色和权限边界。
- 增加站点级资源限制、AI 使用量统计和计费预留。

## 9. 第一版新 schema 的建议约束清单

强制唯一：

- `SiteDomain.Host`
- `Site(TenantId, Slug)`
- `User.NormalizedUserName`
- `SiteMembership(SiteId, UserId)`
- `SiteSetting(SiteId, SchemaName)`
- `PostRoute(SiteId, RouteDate, Slug)`
- `Category(SiteId, RouteName)`
- `Tag(SiteId, NormalizedName)`
- `Page(SiteId, Slug)`
- `PostCategory(PostId, CategoryId)`
- `PostTag(PostId, TagId)`

关键索引：

- `Post(SiteId, Status, PublishedAtUtc)`
- `Post(SiteId, IsDeleted, PublishedAtUtc)`
- `Post(SiteId, IsFeatured, PublishedAtUtc)`
- `PostRoute(SiteId, HashCheckSum)`
- `Comment(SiteId, PostId, CreatedAtUtc)`
- `Comment(SiteId, Status, CreatedAtUtc)`
- `MediaAsset(SiteId, ContentHash)`
- `AiJob(SiteId, Status, JobType, CreatedAtUtc)`
- `AiArtifact(SiteId, TargetEntityType, TargetEntityId, ArtifactType)`

删除策略：

- 删除站点时才级联删除站点内数据。
- 删除文章默认软删除，后台清理时再物理删除相关内容和评论。
- 删除分类或标签前应先移除关联，避免误删文章。
- 删除用户不应删除其历史文章，可把作者显示名快照留在内容表。

并发策略：

- 文章、页面、配置、菜单、主题应增加并发 token。
- 统计计数器可以使用数据库原子更新或独立事件聚合。
- 后台 AI job 必须有状态机，避免多个实例重复处理同一篇文章。

## 10. 与现有功能的兼容要求

迁移后必须保持：

- 文章列表、详情、分类页、标签页、归档页和搜索功能可用。
- 旧文章 URL 可访问。
- 草稿、发布、回收站语义不变。
- 评论审核、评论回复、评论开关语义不变。
- RSS/Atom 输出不丢文章。
- 站点标题、头像、图标、主题、CSS、菜单、友链、页脚和侧栏设置保持一致。
- 本地管理员账号可以登录。
- 已上传图片和文章内图片引用不失效。
- 已有 AI 摘要和翻译内容继续展示。

## 11. 主要取舍

### 保留 JSON 配置，但加版本和站点边界

完全拆成强类型设置表会带来大量迁移和代码改动。第一阶段保留 JSON 更稳，但必须增加 `SiteId`、`SchemaName` 唯一约束和 `SchemaVersion`。

### 内容翻译不再继续横向加列

当前只有中文和英文，可以勉强用宽表。未来语言数量、AI 模型、多版本、人工修订都会扩展。使用 `PostContent` 和 `AiArtifact` 可以避免每加一个语言就改表。

### 评论长期建议统一为树形模型

当前 `Comment` + `CommentReply` 对 UI 很简单，但后续 AI 问答、作者追问、多轮互动更适合统一 comment tree。若第一阶段风险控制优先，可以先保留旧结构，只增加 `SiteId` 和来源字段。

### 迁移工具优先于原地自动升级

原地迁移更像产品体验，离线迁移更适合这次大结构调整。它能让旧库保持不变，也更容易做 dry-run、diff 和人工验收。

## 12. 后续可拆分任务

1. 输出当前旧 schema 的 machine-readable 描述。
2. 设计新实体类和 EF Core Fluent 配置。
3. 设计 `SiteContext` 和默认单站点解析。
4. 实现迁移工具 dry-run。
5. 实现旧文章到新文章、内容、路由的迁移。
6. 实现配置、主题、页面、菜单、友链迁移。
7. 实现评论和回复迁移。
8. 实现账号和成员迁移。
9. 实现媒体资产 metadata 迁移。
10. 实现迁移校验报告。
11. 将现有查询逐步加上 `SiteId` 边界。
12. 将 AI 后台任务改为 `AiJob` 和 `AiArtifact`。

## 13. 当前阶段结论

MoongladePure 现在不是缺少一个迁移文件，而是缺少一个明确的数据边界：哪些数据属于平台、哪些属于租户、哪些属于站点、哪些属于内容本身、哪些只是 AI 派生产物。新的数据库设计应先建立这些边界，然后再考虑 provider 兼容和具体 EF Core 迁移。

建议第一版重构以“默认租户 + 默认站点 + 旧 UI 完全兼容”为交付目标。这样既能让现有用户无感迁移，也能在不大幅扰动上层交互的前提下，把数据库结构推进到可以承载 SaaS 和 AI 功能的状态。
