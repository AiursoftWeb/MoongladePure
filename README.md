# MoongladePure

MoongladePure is a fork of Moonglade. Regain control over your data.

MoongladePure can be deployed completely on-premises without coupling to any particular cloud.

MoongladePure supports AirGap deployment. It doesn't require an Internet connection to use. 100% local!

## Deployment

It is suggested to use Ubuntu 22.04 LTS as the operating system.

### Prerequisites

* MySQL
* .NET
* Caddy(Optional)

### Prepare MySQL Server

Install MySQL server as the database of MoongladePure.

The database can be a different server from the Web app server.

```bash
sudo apt install mysql-server -y
sudo systemctl enable mysql
sudo systemctl start mysql
```

Set admin password:

```bash
sudo mysql_secure_installation
```

Or

```bash
sudo mysql
ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password by 'mynewpassword';
exit;
```

To sign in your MySQL:

```bash
sudo mysql -u root -p
```

Create a database for MoongladePure:

```sql
CREATE DATABASE MoongladePure;
CREATE USER 'moongladepure'@'localhost' IDENTIFIED BY 'YOUR_STRONG_PASSWORD';
GRANT ALL PRIVILEGES ON MoongladePure.* TO 'moongladepure'@'localhost';
FLUSH PRIVILEGES;
exit;
```

### Prepare a storage path

Create a folder for MoongladePure to store data:

```bash
sudo mkdir /mnt/datastore
sudo chown -R www-data:www-data /mnt/datastore
```

### Download MoongladePure

You can download Moonglade via:

```bash
wget https://git.aiursoft.cn/Aiursoft/MoongladePure/archive/master.tar.gz
tar -zxvf ./master.tar.gz
ls
```

### Build MoongladePure

Install dotnet6 first.

```bash
sudo apt install -y dotnet6
```

Prepare a directory:

```bash
sudo mkdir -p /opt/apps/MoongladePure
sudo chown -R www-data:www-data /opt/apps/MoongladePure
```

Then build it:

```bash
dotnet publish -c Release -o ./bin -r linux-x64 --no-self-contained ./moongladepure/src/Moonglade.Web/MoongladePure.Web.csproj
```

Copy the files to the directory:

```bash
sudo cp ./bin/ /opt/apps/MoongladePure -rv
sudo chown -R www-data:www-data /opt/apps/MoongladePure
```

### Edit the configuration

Copy the configuration file as production first:

```bash
sudo -u www-data cp /opt/apps/MoongladePure/appsettings.json /opt/apps/MoongladePure/appsettings.Production.json
```

Then edit the production JSON file.

* Make the database connection string to your real database.
* Make the storage path to be your real storage path.

```bash
sudo -u www-data vim /opt/apps/MoongladePure/appsettings.Production.json
```

### Run MoongladePure

First register MoongladePure as a service:

```bash
echo '[Unit]
Description=MoongladePure Service
After=network.target
Wants=network.target

[Service]
Type=simple
User=www-data
ExecStart=/usr/bin/dotnet /opt/apps/MoongladePure/MoongladePure.Web.dll --urls=http://0.0.0.0:48466/
WorkingDirectory=/opt/apps/MoongladePure
Restart=always
RestartSec=10
KillSignal=SIGINT
Environment="ASPNETCORE_ENVIRONMENT=Production"
Environment="DOTNET_PRINT_TELEMETRY_MESSAGE=false"
Environment="DOTNET_CLI_TELEMETRY_OPTOUT=1"
Environment="ASPNETCORE_FORWARDEDHEADERS_ENABLED=true"

[Install]
WantedBy=multi-user.target' | sudo tee -a /etc/systemd/system/moongladepure.service
```

Then start the service.

```bash
sudo systemctl daemon-reload
sudo systemctl enable moongladepure
sudo systemctl start moongladepure
```

Now you can visit your MoongladePure site via `http://your-ip:48466`.

The admin panel is at `http://your-ip:48466/admin`. The default username is `admin` and password is `admin123`.

### Prepare HTTPS

Please make sure you have a domain name ready and point to your server's IP address.

Then install a reverse proxy server. For example, I'm using Caddy.

```bash
echo "deb [trusted=yes] https://apt.fury.io/caddy/ /" | sudo tee -a /etc/apt/sources.list.d/caddy-fury.list
sudo apt update
sudo apt install caddy -y
```

Then edit the Caddyfile:

```bash
sudo vim /etc/caddy/Caddyfile
```

Add the following content:

```bash
your.domain.com {
        reverse_proxy http://localhost:48466 {
        }
}
```

Then restart Caddy:

```bash
sudo systemctl restart caddy
```

Now try to open your browser and try:

```bash
https://your.domain.com
```

### 🔩 Others

- [System Settings](https://github.com/EdiWang/Moonglade/wiki/System-Settings)
- [Security Headers (CSP, XSS, etc.)](https://github.com/EdiWang/Moonglade/wiki/Security-Headers-(CSP,-XSS,-etc.))
