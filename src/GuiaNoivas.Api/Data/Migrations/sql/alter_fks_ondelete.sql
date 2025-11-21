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
