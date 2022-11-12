# Moonglade Blog

[![Build Status](https://dev.azure.com/ediwang/Edi-GitHub/_apis/build/status/EdiWang.Moonglade?branchName=master)](https://dev.azure.com/ediwang/Moonglade%20DevOps/_build/latest?definitionId=68&branchName=master) 
![Docker Build and Push](https://github.com/EdiWang/Moonglade/workflows/Docker%20Build%20and%20Push/badge.svg) 
![.NET Build Linux](https://github.com/EdiWang/Moonglade/workflows/.NET%20Build%20Linux/badge.svg) 

The [.NET](https://dotnet.microsoft.com/) blog system of [edi.wang](https://edi.wang) that runs on [**Microsoft Azure**](https://azure.microsoft.com/en-us/). Designed for developers, enabling most common blogging features including posts, comments, categories, archive, tags and pages.

## 📦 Deployment

- It is recommended to use stable code from [Release](https://github.com/EdiWang/Moonglade/releases) rather than master branch.

- It is recommended to enable HTTP/2 support on your web server.

### ☁ Full Deploy on Azure (Recommend)

This is the way https://edi.wang is deployed, by taking advantage of as many Azure services as possible, the blog can run very fast and secure.

This diagram shows a full Azure deployment for Moonglade for reference.

![image](https://ediwang.cdn.moonglade.blog/web-assets/ediwang-azure-arch-visio.png)

### 🐋 Quick Deploy on Azure

Use automated deployment script to get your Moonglade up and running in 10 minutes, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Deploy-on-Azure)

### 🐧 Quick Deploy on Linux without Docker

To quickly get it running on a new Linux machine without Docker, follow instructions [here](https://github.com/EdiWang/Moonglade/wiki/Quick-Install-on-Linux-Machine). You can watch video tutorial [here](https://anduins-site.player.aiur.site/moonglade-install.mp4).

## 🐵 Development

Tools | Alternative
--- | ---
[Visual Studio 2022 v17.0+](https://visualstudio.microsoft.com/) | [Visual Studio Code](https://code.visualstudio.com/) with [.NET 6.0 SDK](http://dot.net)
[SQL Server 2019](https://www.microsoft.com/en-us/sql-server/sql-server-2019) | [SQL Server LocalDB](https://docs.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb?view=sql-server-ver15?WT.mc_id=AZ-MVP-5002809), PostgreSQL or MySQL 

### 💾 Setup Database

Moonglade supports three types of database. You can choose from SQL Server, PostgreSQL or MySQL.

#### SQL Server

Create a SQL Server 2019 database, e.g. ```moonglade```

Set the `MoongladeDatabase` to your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "Server=(localdb)\\MSSQLLocalDB;Database=moonglade;Trusted_Connection=True;"
```

#### MySQL

Set `DatabaseType` to `MySql`

```json
"DatabaseType": "MySql"
```

Set the `MoongladeDatabase` to your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "Server=localhost;Port=3306;Database=moonglade;Uid=root;Pwd=******;"
```

#### PostgreSql

Set `DatabaseType` to `PostgreSql`

```json
"DatabaseType": "PostgreSql"
```

Set the `MoongladeDatabase` to your database connection string in `appsettings.Development.json`

```json
"MoongladeDatabase": "User ID=****;Password=****;Host=localhost;Port=5432;Database=****;Pooling=true;"
```

### 🔨 Build Source

Build and run `./src/Moonglade.sln`
- Admin: `https://localhost:1055/admin`
- Default username: `admin`
- Default password: `admin123`

## ⚙ Configuration

> This section discuss system settings in **appsettings.[env].json**. For blog settings, please use "/admin/settings" UI.

**For production, it is strongly recommended to use Environment Variables over appsetting.json file.**

### 🛡 Authentication

#### Local Account

Set `Authentication:Provider` to `"Local"`. You can manage accounts in `/admin/settings/account`

### 🖼 Image Storage

`ImageStorage` controls how blog post images are stored.

#### File System

You can also choose File System for image storage if you don't have a cloud option.

```json
{
  "Provider": "filesystem",
  "FileSystemPath": "C:\\UploadedImages"
}
```

### 🔩 Others

- [System Settings](https://github.com/EdiWang/Moonglade/wiki/System-Settings)
- [Security Headers (CSP, XSS, etc.)](https://github.com/EdiWang/Moonglade/wiki/Security-Headers-(CSP,-XSS,-etc.))

## 🎉 Blog Protocols or Standards

- [X] RSS
- [X] Atom
- [X] OPML
- [X] Open Search
- [X] Pingback
- [X] Reader View
- [X] FOAF
- [X] RSD
- [X] MetaWeblog (Basic Support)
- [ ] BlogML - Under triage
- [ ] APML - Not planned
- [ ] Trackback - Not planned

