-- Run by the db-init Job with: sqlcmd -v DB=... SVC=... APP_PASSWORD=... MIGRATOR_PASSWORD=...
IF DB_ID(N'$(DB)') IS NULL CREATE DATABASE [$(DB)];
GO

IF SUSER_ID(N'$(SVC)_app') IS NULL
    CREATE LOGIN [$(SVC)_app] WITH PASSWORD = N'$(APP_PASSWORD)';
ELSE
    ALTER LOGIN [$(SVC)_app] WITH PASSWORD = N'$(APP_PASSWORD)';

IF SUSER_ID(N'$(SVC)_migrator') IS NULL
    CREATE LOGIN [$(SVC)_migrator] WITH PASSWORD = N'$(MIGRATOR_PASSWORD)';
ELSE
    ALTER LOGIN [$(SVC)_migrator] WITH PASSWORD = N'$(MIGRATOR_PASSWORD)';
GO

USE [$(DB)];
GO

IF USER_ID(N'$(SVC)_app') IS NULL CREATE USER [$(SVC)_app] FOR LOGIN [$(SVC)_app];
IF USER_ID(N'$(SVC)_migrator') IS NULL CREATE USER [$(SVC)_migrator] FOR LOGIN [$(SVC)_migrator];

-- The service can read and write rows, and nothing else: no DDL, no other databases.
ALTER ROLE db_datareader ADD MEMBER [$(SVC)_app];
ALTER ROLE db_datawriter ADD MEMBER [$(SVC)_app];

-- The migration Job owns its own database, only for the duration of the migration.
ALTER ROLE db_owner ADD MEMBER [$(SVC)_migrator];
GO