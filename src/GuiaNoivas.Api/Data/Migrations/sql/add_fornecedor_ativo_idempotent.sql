-- Idempotent script to add 'Ativo' column to Fornecedores
-- Works for SQL Server and SQLite (SQLite will ignore DEFAULT and NOT NULL semantics but will add the column)

-- SQL Server
IF COL_LENGTH('Fornecedores', 'Ativo') IS NULL
BEGIN
    ALTER TABLE Fornecedores ADD Ativo bit NOT NULL CONSTRAINT DF_Fornecedores_Ativo DEFAULT(1);
END

-- SQLite: add column if not exists (SQLite supports ADD COLUMN but not IF NOT EXISTS prior to newer versions).
-- Run the following block only in SQLite environments, not in SQL Server.

-- For SQLite, execute these statements using a SQLite client:
-- PRAGMA foreign_keys=off;
-- BEGIN TRANSACTION;
-- ALTER TABLE Fornecedores ADD COLUMN Ativo INTEGER DEFAULT 1;
-- COMMIT;
-- PRAGMA foreign_keys=on;

-- Note: For production use, prefer generating an EF Core migration and applying it via `dotnet ef database update`.
-- To apply manually against SQL Server: run this script with sqlcmd or via your DB admin tools.
