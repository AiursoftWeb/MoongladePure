using Microsoft.Data.Sqlite;
using MoongladePure.Migration;

namespace MoongladePure.Tests;

[TestClass]
public class MigrationToolTests
{
    [TestMethod]
    public void AnalyzeLegacyDatabaseReportsCleanFixture()
    {
        using var fixture = LegacyDatabaseFixture.Create();

        var report = LegacySqliteAnalyzer.Analyze(fixture.SourcePath);

        Assert.AreEqual("20260115212706_AddUserProfileTable", report.LatestMigrationId);
        Assert.HasCount(0, report.Warnings);
        Assert.HasCount(0, report.Errors);
        Assert.AreEqual(1, report.KnownTables.Single(t => t.Name == "Post").RowCount);
        Assert.AreEqual(1, report.KnownTables.Single(t => t.Name == "LocalAccount").RowCount);
    }

    [TestMethod]
    public void MigrateLegacyDatabaseProducesValidTargetDatabase()
    {
        using var fixture = LegacyDatabaseFixture.Create();

        var migrateOptions = new MigrationOptions(
            MigrationCommand.Migrate,
            fixture.SourcePath,
            fixture.TargetPath,
            null,
            false,
            false);

        var migrationResult = LegacySqliteMigrator.Migrate(migrateOptions);
        var validationReport = TargetSqliteValidator.Validate(fixture.TargetPath);

        Assert.HasCount(0, migrationResult.Errors);
        Assert.AreEqual(1, migrationResult.MigratedRows["Post"]);
        Assert.AreEqual(1, migrationResult.MigratedRows["LocalAccount"]);
        Assert.HasCount(0, validationReport.Errors);
        Assert.AreEqual(1, validationReport.TableRows["Post"]);
        Assert.AreEqual(1, validationReport.TableRows["PostContent"]);
        Assert.AreEqual(1, validationReport.TableRows["PostRoute"]);
        Assert.AreEqual(1, validationReport.TableRows["User"]);
        Assert.AreEqual(1, validationReport.TableRows["SiteMembership"]);
    }

    [TestMethod]
    public void ValidateTargetDatabaseReportsLegacyDatabaseAsInvalidTarget()
    {
        using var fixture = LegacyDatabaseFixture.Create();

        var validationReport = TargetSqliteValidator.Validate(fixture.SourcePath);

        Assert.IsTrue(validationReport.Errors.Any(e => e.Code == "TargetTableMissing"));
    }

    private sealed class LegacyDatabaseFixture : IDisposable
    {
        private readonly string _directory;

        private LegacyDatabaseFixture(string directory, string sourcePath, string targetPath)
        {
            _directory = directory;
            SourcePath = sourcePath;
            TargetPath = targetPath;
        }

        public string SourcePath { get; }

        public string TargetPath { get; }

        public static LegacyDatabaseFixture Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), "moonglade-migration-tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var sourcePath = Path.Combine(directory, "legacy.db");
            var targetPath = Path.Combine(directory, "target.db");

            using var connection = new SqliteConnection($"Data Source={sourcePath}");
            connection.Open();
            CreateSchema(connection);
            SeedData(connection);

            return new LegacyDatabaseFixture(directory, sourcePath, targetPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }

        private static void CreateSchema(SqliteConnection connection)
        {
            Execute(connection, """
                CREATE TABLE "__EFMigrationsHistory" (
                    "MigrationId" TEXT NOT NULL PRIMARY KEY
                );
                CREATE TABLE "BlogAsset" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Base64Data" TEXT NULL,
                    "LastModifiedTimeUtc" TEXT NOT NULL
                );
                CREATE TABLE "BlogConfiguration" (
                    "Id" INTEGER NOT NULL PRIMARY KEY,
                    "CfgKey" TEXT NULL,
                    "CfgValue" TEXT NULL,
                    "LastModifiedTimeUtc" TEXT NULL
                );
                CREATE TABLE "BlogTheme" (
                    "Id" INTEGER NOT NULL PRIMARY KEY,
                    "ThemeName" TEXT NULL,
                    "CssRules" TEXT NULL,
                    "AdditionalProps" TEXT NULL,
                    "ThemeType" INTEGER NOT NULL
                );
                CREATE TABLE "Category" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "RouteName" TEXT NULL,
                    "DisplayName" TEXT NULL,
                    "Note" TEXT NULL
                );
                CREATE TABLE "Comment" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Username" TEXT NULL,
                    "Email" TEXT NULL,
                    "IPAddress" TEXT NULL,
                    "CreateTimeUtc" TEXT NOT NULL,
                    "CommentContent" TEXT NULL,
                    "PostId" TEXT NOT NULL,
                    "IsApproved" INTEGER NOT NULL
                );
                CREATE TABLE "CommentReply" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "ReplyContent" TEXT NULL,
                    "CreateTimeUtc" TEXT NOT NULL,
                    "CommentId" TEXT NULL
                );
                CREATE TABLE "CustomPage" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Title" TEXT NULL,
                    "Slug" TEXT NULL,
                    "HtmlContent" TEXT NULL,
                    "CssContent" TEXT NULL,
                    "HideSidebar" INTEGER NOT NULL,
                    "IsPublished" INTEGER NOT NULL,
                    "CreateTimeUtc" TEXT NOT NULL,
                    "UpdateTimeUtc" TEXT NULL
                );
                CREATE TABLE "FriendLink" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Title" TEXT NULL,
                    "LinkUrl" TEXT NULL
                );
                CREATE TABLE "LocalAccount" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Username" TEXT NULL,
                    "NormalizedUsername" TEXT NULL,
                    "Email" TEXT NULL,
                    "NormalizedEmail" TEXT NULL,
                    "PasswordSalt" TEXT NULL,
                    "PasswordHash" TEXT NULL,
                    "LastLoginTimeUtc" TEXT NULL,
                    "LastLoginIp" TEXT NULL,
                    "CreateTimeUtc" TEXT NOT NULL
                );
                CREATE TABLE "Menu" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Title" TEXT NULL,
                    "Url" TEXT NULL,
                    "Icon" TEXT NULL,
                    "DisplayOrder" INTEGER NOT NULL,
                    "IsOpenInNewTab" INTEGER NOT NULL
                );
                CREATE TABLE "Post" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "Title" TEXT NULL,
                    "Slug" TEXT NULL,
                    "Author" TEXT NULL,
                    "RawContent" TEXT NULL,
                    "CommentEnabled" INTEGER NOT NULL,
                    "CreateTimeUtc" TEXT NOT NULL,
                    "ContentAbstractZh" TEXT NULL,
                    "ContentLanguageCode" TEXT NULL,
                    "IsFeedIncluded" INTEGER NOT NULL,
                    "PubDateUtc" TEXT NULL,
                    "LastModifiedUtc" TEXT NULL,
                    "IsPublished" INTEGER NOT NULL,
                    "IsDeleted" INTEGER NOT NULL,
                    "IsOriginal" INTEGER NOT NULL,
                    "OriginLink" TEXT NULL,
                    "HeroImageUrl" TEXT NULL,
                    "InlineCss" TEXT NULL,
                    "IsFeatured" INTEGER NOT NULL,
                    "HashCheckSum" INTEGER NOT NULL,
                    "LocalizedChineseContent" TEXT NULL,
                    "LocalizedEnglishContent" TEXT NULL,
                    "LocalizeJobRunAt" TEXT NULL,
                    "ContentAbstractEn" TEXT NULL
                );
                CREATE TABLE "PostCategory" (
                    "PostId" TEXT NOT NULL,
                    "CategoryId" TEXT NOT NULL
                );
                CREATE TABLE "PostExtension" (
                    "PostId" TEXT NOT NULL PRIMARY KEY,
                    "Hits" INTEGER NOT NULL,
                    "Likes" INTEGER NOT NULL
                );
                CREATE TABLE "PostTag" (
                    "PostId" TEXT NOT NULL,
                    "TagId" INTEGER NOT NULL
                );
                CREATE TABLE "SubMenu" (
                    "Id" TEXT NOT NULL PRIMARY KEY,
                    "MenuId" TEXT NULL,
                    "Title" TEXT NULL,
                    "Url" TEXT NULL,
                    "IsOpenInNewTab" INTEGER NOT NULL
                );
                CREATE TABLE "Tag" (
                    "Id" INTEGER NOT NULL PRIMARY KEY,
                    "DisplayName" TEXT NULL,
                    "NormalizedName" TEXT NULL
                );
                """);
        }

        private static void SeedData(SqliteConnection connection)
        {
            var postId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
            var categoryId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
            var commentId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
            var accountId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
            var pageId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
            var menuId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
            var linkId = Guid.Parse("11111111-1111-1111-1111-111111111111");
            var now = "2026-01-15T12:00:00Z";

            Execute(connection, "INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\") VALUES ('20260115212706_AddUserProfileTable');");
            InsertSettings(connection, now);
            Execute(connection, """
                INSERT INTO "BlogTheme" ("Id", "ThemeName", "CssRules", "AdditionalProps", "ThemeType")
                VALUES (1, 'Default', '{}', '{}', 0);
                """);
            Execute(
                connection,
                """
                INSERT INTO "Category" ("Id", "RouteName", "DisplayName", "Note")
                VALUES ($id, 'default', 'Default', 'Default category');
                """,
                ("$id", categoryId.ToString()));
            Execute(
                connection,
                """
                INSERT INTO "Tag" ("Id", "DisplayName", "NormalizedName")
                VALUES (1, 'Test', 'test');
                """);
            Execute(
                connection,
                """
                INSERT INTO "Post" (
                    "Id", "Title", "Slug", "Author", "RawContent", "CommentEnabled", "CreateTimeUtc",
                    "ContentAbstractZh", "ContentLanguageCode", "IsFeedIncluded", "PubDateUtc", "LastModifiedUtc",
                    "IsPublished", "IsDeleted", "IsOriginal", "OriginLink", "HeroImageUrl", "InlineCss",
                    "IsFeatured", "HashCheckSum", "LocalizedChineseContent", "LocalizedEnglishContent",
                    "LocalizeJobRunAt", "ContentAbstractEn")
                VALUES (
                    $id, 'Hello', 'hello', 'Admin', '# Hello', 1, $now,
                    '摘要', 'en-US', 1, $now, $now,
                    1, 0, 1, NULL, NULL, NULL,
                    0, 12345, '', '',
                    NULL, 'Summary');
                """,
                ("$id", postId.ToString()),
                ("$now", now));
            Execute(
                connection,
                """
                INSERT INTO "PostExtension" ("PostId", "Hits", "Likes")
                VALUES ($postId, 10, 2);
                INSERT INTO "PostCategory" ("PostId", "CategoryId")
                VALUES ($postId, $categoryId);
                INSERT INTO "PostTag" ("PostId", "TagId")
                VALUES ($postId, 1);
                """,
                ("$postId", postId.ToString()),
                ("$categoryId", categoryId.ToString()));
            Execute(
                connection,
                """
                INSERT INTO "Comment" ("Id", "Username", "Email", "IPAddress", "CreateTimeUtc", "CommentContent", "PostId", "IsApproved")
                VALUES ($id, 'Reader', 'reader@example.com', '127.0.0.1', $now, 'Nice post', $postId, 1);
                """,
                ("$id", commentId.ToString()),
                ("$now", now),
                ("$postId", postId.ToString()));
            Execute(
                connection,
                """
                INSERT INTO "LocalAccount" ("Id", "Username", "NormalizedUsername", "Email", "NormalizedEmail", "PasswordSalt", "PasswordHash", "LastLoginTimeUtc", "LastLoginIp", "CreateTimeUtc")
                VALUES ($id, 'admin', 'admin', 'admin@example.com', 'admin@example.com', 'salt', 'hash', $now, '127.0.0.1', $now);
                """,
                ("$id", accountId.ToString()),
                ("$now", now));
            Execute(
                connection,
                """
                INSERT INTO "CustomPage" ("Id", "Title", "Slug", "HtmlContent", "CssContent", "HideSidebar", "IsPublished", "CreateTimeUtc", "UpdateTimeUtc")
                VALUES ($id, 'About', 'about', '<p>About</p>', '', 0, 1, $now, $now);
                """,
                ("$id", pageId.ToString()),
                ("$now", now));
            Execute(
                connection,
                """
                INSERT INTO "Menu" ("Id", "Title", "Url", "Icon", "DisplayOrder", "IsOpenInNewTab")
                VALUES ($id, 'Home', '/', '', 0, 0);
                INSERT INTO "FriendLink" ("Id", "Title", "LinkUrl")
                VALUES ($linkId, 'Aiursoft', 'https://www.aiursoft.com');
                """,
                ("$id", menuId.ToString()),
                ("$linkId", linkId.ToString()));
        }

        private static void InsertSettings(SqliteConnection connection, string now)
        {
            var settings = new[]
            {
                ("GeneralSettings", """{"SiteTitle":"Fixture Blog","TimeZoneId":"UTC","OwnerName":"Admin"}"""),
                ("ContentSettings", "{}"),
                ("FeedSettings", "{}"),
                ("ImageSettings", "{}"),
                ("AdvancedSettings", "{}"),
                ("CustomStyleSheetSettings", "{}")
            };

            var id = 1;
            foreach (var setting in settings)
            {
                Execute(
                    connection,
                    """
                    INSERT INTO "BlogConfiguration" ("Id", "CfgKey", "CfgValue", "LastModifiedTimeUtc")
                    VALUES ($id, $key, $value, $now);
                    """,
                    ("$id", id),
                    ("$key", setting.Item1),
                    ("$value", setting.Item2),
                    ("$now", now));
                id++;
            }
        }

        private static void Execute(SqliteConnection connection, string sql, params (string Name, object Value)[] parameters)
        {
            using var command = connection.CreateCommand();
            command.CommandText = sql;

            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }

            command.ExecuteNonQuery();
        }
    }
}
