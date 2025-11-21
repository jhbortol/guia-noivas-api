-- alter_fks_ondelete.sql
-- Safe script to alter foreign key delete behaviors
-- WARNING: backup the database before running. Run in a transaction.

SET NOCOUNT ON;

BEGIN TRANSACTION;

-- Fornecedores.CategoriaId -> ON DELETE SET NULL
DECLARE @fkName nvarchar(200);
SELECT @fkName = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fc ON fk.object_id = fc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Fornecedores')
  AND fk.referenced_object_id = OBJECT_ID('dbo.Categorias')
  AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = 'CategoriaId';

IF @fkName IS NOT NULL
BEGIN
    PRINT 'Dropping FK: ' + @fkName;
    EXEC('ALTER TABLE dbo.Fornecedores DROP CONSTRAINT [' + @fkName + ']');
END

PRINT 'Adding FK: FK_Fornecedores_Categorias_CategoriaId (ON DELETE SET NULL)';
EXEC('ALTER TABLE dbo.Fornecedores ADD CONSTRAINT [FK_Fornecedores_Categorias_CategoriaId] FOREIGN KEY (CategoriaId) REFERENCES dbo.Categorias(Id) ON DELETE SET NULL');

-- Media.FornecedorId -> ON DELETE CASCADE
SET @fkName = NULL;
SELECT @fkName = fk.name
FROM sys.foreign_keys fk
JOIN sys.foreign_key_columns fc ON fk.object_id = fc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.Media')
  AND fk.referenced_object_id = OBJECT_ID('dbo.Fornecedores')
  AND COL_NAME(fc.parent_object_id, fc.parent_column_id) = 'FornecedorId';

IF @fkName IS NOT NULL
BEGIN
    PRINT 'Dropping FK: ' + @fkName;
    EXEC('ALTER TABLE dbo.Media DROP CONSTRAINT [' + @fkName + ']');
END

PRINT 'Adding FK: FK_Media_Fornecedores_FornecedorId (ON DELETE CASCADE)';
EXEC('ALTER TABLE dbo.Media ADD CONSTRAINT [FK_Media_Fornecedores_FornecedorId] FOREIGN KEY (FornecedorId) REFERENCES dbo.Fornecedores(Id) ON DELETE CASCADE');

COMMIT TRANSACTION;

PRINT 'FK alteration script completed.';
-- Script to alter foreign keys delete behavior
-- Fornecedores.CategoriaId -> ON DELETE SET NULL
-- Media.FornecedorId -> ON DELETE CASCADE

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Fornecedores_Categorias_CategoriaId')
BEGIN
    ALTER TABLE dbo.Fornecedores DROP CONSTRAINT FK_Fornecedores_Categorias_CategoriaId;
END

IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Media_Fornecedores_FornecedorId')
BEGIN
    ALTER TABLE dbo.Media DROP CONSTRAINT FK_Media_Fornecedores_FornecedorId;
END

ALTER TABLE dbo.Fornecedores
    ADD CONSTRAINT FK_Fornecedores_Categorias_CategoriaId FOREIGN KEY (CategoriaId) REFERENCES dbo.Categorias (Id) ON DELETE SET NULL;

ALTER TABLE dbo.Media
    ADD CONSTRAINT FK_Media_Fornecedores_FornecedorId FOREIGN KEY (FornecedorId) REFERENCES dbo.Fornecedores (Id) ON DELETE CASCADE;

PRINT 'FKs altered: Fornecedores -> Categoria (SET NULL), Media -> Fornecedor (CASCADE)';
