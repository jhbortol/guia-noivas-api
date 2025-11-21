IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE TABLE [Categorias] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Slug] nvarchar(200) NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [Order] int NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE TABLE [Fornecedores] (
        [Id] uniqueidentifier NOT NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Slug] nvarchar(200) NOT NULL,
        [Descricao] nvarchar(max) NULL,
        [Cidade] nvarchar(450) NULL,
        [Telefone] nvarchar(max) NULL,
        [Email] nvarchar(max) NULL,
        [Website] nvarchar(max) NULL,
        [Destaque] bit NOT NULL DEFAULT CAST(0 AS bit),
        [SeloFornecedor] bit NOT NULL DEFAULT CAST(0 AS bit),
        [Rating] decimal(18,2) NULL,
        [Visitas] int NOT NULL DEFAULT 0,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_Fornecedores] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE TABLE [Media] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NULL,
        [Url] nvarchar(max) NULL,
        [Filename] nvarchar(max) NULL,
        [ContentType] nvarchar(max) NULL,
        [Width] int NULL,
        [Height] int NULL,
        [IsPrimary] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Media] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE TABLE [Usuarios] (
        [Id] uniqueidentifier NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [PasswordHash] nvarchar(max) NOT NULL,
        [Roles] nvarchar(max) NULL,
        [DisplayName] nvarchar(max) NULL,
        [CreatedAt] datetimeoffset NOT NULL DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT [PK_Usuarios] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Categorias_Slug] ON [Categorias] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_Cidade] ON [Fornecedores] ([Cidade]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_Destaque_Rating] ON [Fornecedores] ([Destaque], [Rating]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Fornecedores_Slug] ON [Fornecedores] ([Slug]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Usuarios_Email] ON [Usuarios] ([Email]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121121605_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251121121605_InitialCreate', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121124402_AddRefreshToken'
)
BEGIN
    DROP INDEX [IX_Fornecedores_Destaque_Rating] ON [Fornecedores];
    DECLARE @var0 sysname;
    SELECT @var0 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Fornecedores]') AND [c].[name] = N'Rating');
    IF @var0 IS NOT NULL EXEC(N'ALTER TABLE [Fornecedores] DROP CONSTRAINT [' + @var0 + '];');
    ALTER TABLE [Fornecedores] ALTER COLUMN [Rating] decimal(5,2) NULL;
    CREATE INDEX [IX_Fornecedores_Destaque_Rating] ON [Fornecedores] ([Destaque], [Rating]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121124402_AddRefreshToken'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] uniqueidentifier NOT NULL,
        [UsuarioId] uniqueidentifier NOT NULL,
        [Token] nvarchar(max) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [Revoked] bit NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121124402_AddRefreshToken'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251121124402_AddRefreshToken', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121125141_AddContatoAndInstitucional'
)
BEGIN
    CREATE TABLE [ContatoSubmissions] (
        [Id] uniqueidentifier NOT NULL,
        [FornecedorId] uniqueidentifier NULL,
        [Nome] nvarchar(200) NOT NULL,
        [Email] nvarchar(200) NOT NULL,
        [Telefone] nvarchar(50) NULL,
        [Mensagem] nvarchar(max) NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        CONSTRAINT [PK_ContatoSubmissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121125141_AddContatoAndInstitucional'
)
BEGIN
    CREATE TABLE [InstitucionalContents] (
        [Key] nvarchar(100) NOT NULL,
        [Title] nvarchar(200) NULL,
        [ContentHtml] nvarchar(max) NULL,
        [Version] int NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_InstitucionalContents] PRIMARY KEY ([Key])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121125141_AddContatoAndInstitucional'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251121125141_AddContatoAndInstitucional', N'8.0.10');
END;
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121192849_AddCategoriaAndMediaRelations'
)
BEGIN
    ALTER TABLE [Fornecedores] ADD [CategoriaId] uniqueidentifier NULL;
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121192849_AddCategoriaAndMediaRelations'
)
BEGIN
    CREATE INDEX [IX_Media_FornecedorId] ON [Media] ([FornecedorId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121192849_AddCategoriaAndMediaRelations'
)
BEGIN
    CREATE INDEX [IX_Fornecedores_CategoriaId] ON [Fornecedores] ([CategoriaId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121192849_AddCategoriaAndMediaRelations'
)
BEGIN
    ALTER TABLE [Fornecedores] ADD CONSTRAINT [FK_Fornecedores_Categorias_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121192849_AddCategoriaAndMediaRelations'
)
BEGIN
    ALTER TABLE [Media] ADD CONSTRAINT [FK_Media_Fornecedores_FornecedorId] FOREIGN KEY ([FornecedorId]) REFERENCES [Fornecedores] ([Id]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20251121192849_AddCategoriaAndMediaRelations'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20251121192849_AddCategoriaAndMediaRelations', N'8.0.10');
END;
GO

COMMIT;
GO

