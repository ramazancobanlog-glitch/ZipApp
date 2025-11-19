MySQL setup and EF Core for ZipApp

This repo originally used LocalDB/SQL Server. I updated the project to use Pomelo.MySql (EF Core provider) in `Program.cs` and `ApplicationDbContextFactory.cs`.

Steps to run with MySQL locally:

1. Install MySQL Server (or use Docker create a container):

docker run --name zipapp-mysql -e MYSQL_ROOT_PASSWORD=secret -e MYSQL_DATABASE=ZipApp -p 3306:3306 -d mysql:8

2. Update `appsettings.json` (or `appsettings.Development.json`) with connection string OR set an environment variable `DefaultConnection`.

Example appsettings:

"DefaultConnection": "Server=localhost;Port=3306;Database=ZipApp;User=root;Password=secret;"

Recommended: store connection string in environment variable and never commit the plain password to source control.

Example (PowerShell):

```powershell
$env:DefaultConnection = "Server=sql7.freesqldatabase.com;Port=3306;Database=sql7808503;User=sql7808503;Password=ZNzeqYbxrG;"
```

Or use the .NET config style name (recommended by many hosts):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=sql7.freesqldatabase.com;Port=3306;Database=sql7808503;User=sql7808503;Password=ZNzeqYbxrG;"
```

Local .env example is included at `.env.example` — copy it to `.env` and run: `docker run --env-file .env ...` or `source .env` in Unix-like shells.

3. Add EF tools (if not present):

dotnet tool install --global dotnet-ef

4. Generate new migrations for MySQL (recommended to recreate migrations for MySQL):

# remove old migrations if you want a fresh start
# be careful — removing migrations will lose schema history
# dotnet ef migrations remove -- --force
# add new migration

dotnet ef migrations add InitialCreateMySql

dotnet ef database update

To apply migrations against the remote freesqldatabase (SQL host you shared), set the environment variable first (see example above) so `dotnet ef` uses that connection string.

5. Optional: export data from SQL Server and import into MySQL if you need to migrate existing data. There are several tools and guides, e.g.:
- MySQL Workbench migration wizard
- Convert using scripts or ETL

Notes & tips
- Pomelo supports ServerVersion.AutoDetect(connectionString).
 - Pomelo supports ServerVersion.AutoDetect(connectionString).
 - The project `ApplicationDbContextFactory` and `Program.cs` now include a fallback to a default MySQL server version (8.0.32) when AutoDetect fails. This prevents EF design-time tools from failing when the MySQL host is temporarily unreachable.
 - If you still see "Unable to connect to any of the specified MySQL hosts" during `dotnet ef` operations, try:
	 - Run a connectivity test from PowerShell: `Test-NetConnection -ComputerName sql7.freesqldatabase.com -Port 3306` to verify the remote host and port are reachable.
	 - If the port is blocked, check host provider documentation for IP whitelisting or remote connection settings.
	 - As a fallback, generate the SQL migration script: `dotnet ef migrations script -o migration.sql` and apply it with a MySQL client like MySQL Workbench or DBeaver.
- If you need to adjust types, check for EF mappings (e.g. decimal precision) — MySQL and SQL Server differ.
- For production, store the connection string in an environment variable (not in appsettings.json).

If you want, I can create and apply a fresh migration and copy over the model snapshot compatible with MySQL, but I’ll need your confirmation as it will reset the DB schema under the project.