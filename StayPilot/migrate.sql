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
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE TABLE [MarketAreas] (
        [Id] int NOT NULL IDENTITY,
        [Country] nvarchar(max) NOT NULL,
        [District] nvarchar(max) NOT NULL,
        [Municipality] nvarchar(max) NOT NULL,
        [Town] nvarchar(max) NOT NULL,
        [Zone] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_MarketAreas] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE TABLE [OwnedProperties] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [MarketAreaId] int NOT NULL,
        [PropertyType] int NOT NULL,
        [Typology] int NOT NULL,
        [PurchasePrice] decimal(18,2) NOT NULL,
        [PurchaseDate] datetime2 NOT NULL,
        [RenovationInvestment] decimal(18,2) NULL,
        [AreaM2] int NOT NULL,
        [Bathrooms] int NULL,
        [Floor] int NULL,
        [TotalFloors] int NULL,
        [HasElevator] bit NULL,
        [ConstructionYear] int NULL,
        [RenovationYear] int NULL,
        [Condition] int NOT NULL,
        [BalconyCount] int NOT NULL,
        [HasTerrace] bit NOT NULL,
        [HasGarage] bit NOT NULL,
        [HasParking] bit NOT NULL,
        [HasSwimmingPool] bit NOT NULL,
        [IsFurnished] bit NOT NULL,
        [HasSeaView] bit NOT NULL,
        [HasCityView] bit NOT NULL,
        [DistanceToBeachMeters] int NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [EnergyCertificate] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_OwnedProperties] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OwnedProperties_MarketAreas_MarketAreaId] FOREIGN KEY ([MarketAreaId]) REFERENCES [MarketAreas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE TABLE [PropertyListings] (
        [Id] int NOT NULL IDENTITY,
        [MarketAreaId] int NOT NULL,
        [PropertyType] int NOT NULL,
        [Typology] int NOT NULL,
        [SourceName] nvarchar(max) NOT NULL,
        [SourceUrl] nvarchar(450) NOT NULL,
        [AreaM2] int NOT NULL,
        [Bathrooms] int NOT NULL,
        [Floor] int NULL,
        [TotalFloors] int NULL,
        [HasElevator] bit NULL,
        [HasAirConditioning] bit NULL,
        [Condition] int NOT NULL,
        [ConstructionYear] int NULL,
        [RenovationYear] int NULL,
        [BalconyCount] int NOT NULL,
        [HasTerrace] bit NOT NULL,
        [HasGarage] bit NOT NULL,
        [HasParking] bit NOT NULL,
        [HasSwimmingPool] bit NOT NULL,
        [IsFurnished] bit NOT NULL,
        [HasSeaView] bit NOT NULL,
        [HasCityView] bit NOT NULL,
        [DistanceToBeachMeters] int NULL,
        [Latitude] decimal(9,6) NULL,
        [Longitude] decimal(9,6) NULL,
        [EnergyCertificate] nvarchar(max) NULL,
        [Notes] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        [UpdatedAtUtc] datetime2 NULL,
        CONSTRAINT [PK_PropertyListings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PropertyListings_MarketAreas_MarketAreaId] FOREIGN KEY ([MarketAreaId]) REFERENCES [MarketAreas] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE TABLE [ListingSnapshots] (
        [Id] int NOT NULL IDENTITY,
        [PropertyListingId] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [SnapshotDateUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_ListingSnapshots] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ListingSnapshots_PropertyListings_PropertyListingId] FOREIGN KEY ([PropertyListingId]) REFERENCES [PropertyListings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ListingSnapshots_PropertyListingId] ON [ListingSnapshots] ([PropertyListingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OwnedProperties_MarketAreaId] ON [OwnedProperties] ([MarketAreaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PropertyListings_MarketAreaId] ON [PropertyListings] ([MarketAreaId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PropertyListings_SourceUrl] ON [PropertyListings] ([SourceUrl]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260629154357_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260629154357_InitialCreate', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630104712_SeedMarketAreas'
)
BEGIN
    DROP INDEX [IX_ListingSnapshots_PropertyListingId] ON [ListingSnapshots];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630104712_SeedMarketAreas'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MarketAreas]') AND [c].[name] = N'CreatedAtUtc');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [MarketAreas] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [MarketAreas] ADD DEFAULT (GETUTCDATE()) FOR [CreatedAtUtc];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630104712_SeedMarketAreas'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'District', N'Municipality', N'Notes', N'Town', N'Zone') AND [object_id] = OBJECT_ID(N'[MarketAreas]'))
        SET IDENTITY_INSERT [MarketAreas] ON;
    EXEC(N'INSERT INTO [MarketAreas] ([Id], [Country], [District], [Municipality], [Notes], [Town], [Zone])
    VALUES (1, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Centro da Cidade''),
    (2, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Olhos de Água''),
    (3, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Montechoro''),
    (4, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Marina de Albufeira - Cerro da Piedade''),
    (5, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Balaia''),
    (6, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Praia da Falésia''),
    (7, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Clube Albufeira''),
    (8, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Praia da Oura - Areias de S. João''),
    (9, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''São Rafael''),
    (10, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Cerro de Águia - Patroves''),
    (11, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Sesmarias''),
    (12, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Albufeira e Olhos de Água'', N''Forte São João''),
    (13, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Ferreiras'', NULL),
    (14, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Guia'', N''Salgados''),
    (15, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Guia'', N''Galé''),
    (16, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Guia'', N''Vale de Parra''),
    (17, N''Portugal'', N''Faro'', N''Albufeira'', NULL, N''Paderne'', NULL),
    (18, N''Portugal'', N''Faro'', N''Alcoutim'', NULL, N''Alcoutim e Pereiro'', NULL),
    (19, N''Portugal'', N''Faro'', N''Alcoutim'', NULL, N''Giões'', NULL),
    (20, N''Portugal'', N''Faro'', N''Alcoutim'', NULL, N''Martim Longo'', NULL),
    (21, N''Portugal'', N''Faro'', N''Alcoutim'', NULL, N''Vaqueiros'', NULL),
    (22, N''Portugal'', N''Faro'', N''Aljezur'', NULL, N''Aljezur'', NULL),
    (23, N''Portugal'', N''Faro'', N''Aljezur'', NULL, N''Bordeira'', NULL),
    (24, N''Portugal'', N''Faro'', N''Aljezur'', NULL, N''Odeceixe'', NULL),
    (25, N''Portugal'', N''Faro'', N''Aljezur'', NULL, N''Rogil'', NULL),
    (26, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Altura'', NULL),
    (27, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Azinhal'', NULL),
    (28, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Castro Marim'', N''Centro''),
    (29, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Castro Marim'', N''Praia Verde''),
    (30, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Castro Marim'', N''Quinta do Sobral - São Bartolomeu''),
    (31, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Castro Marim'', N''Monte Francisco''),
    (32, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Castro Marim'', N''Junqueira - Beliche''),
    (33, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Castro Marim'', N''Golf Resort''),
    (34, N''Portugal'', N''Faro'', N''Castro Marim'', NULL, N''Odeleite'', NULL),
    (35, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Conceição'', NULL),
    (36, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Estoi'', NULL),
    (37, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Centro''),
    (38, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Arneiro - Braciais - Patacão''),
    (39, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Areal Gordo - Rio Seco - Ilha da Culatra''),
    (40, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Horta das Figuras - Lejana - Senhora da Saúde''),
    (41, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Penha - Vale da Amoreira''),
    (42, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''São Luís'');
    INSERT INTO [MarketAreas] ([Id], [Country], [District], [Municipality], [Notes], [Town], [Zone])
    VALUES (43, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Alto de Santo António - Bom João - João de Deus''),
    (44, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Faro'', N''Alto Rodes''),
    (45, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Montenegro'', NULL),
    (46, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Montenegro'', N''Quinta do Eucalipto - Ilha de Faro''),
    (47, N''Portugal'', N''Faro'', N''Faro'', NULL, N''Santa Bárbara de Nexe'', NULL),
    (48, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Estombar e Parchal'', NULL),
    (49, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Ferragudo'', NULL),
    (50, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Lagoa e Carvoeiro'', N''Cidade de Lagoa''),
    (51, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Lagoa e Carvoeiro'', N''Centro de Carvoeiro''),
    (52, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Lagoa e Carvoeiro'', N''Vale Milho - Vale Centeanes - Algar Seco''),
    (53, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Lagoa e Carvoeiro'', N''Salicos - Sesmarias - Boavista''),
    (54, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Lagoa e Carvoeiro'', N''Caramujeira - Vale d''''El Rei - Benagil''),
    (55, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Lagoa e Carvoeiro'', N''Mato Serrão - Vale da Lapa - Vale Currais''),
    (56, N''Portugal'', N''Faro'', N''Lagoa'', NULL, N''Porches'', NULL),
    (57, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Barão de São João'', NULL),
    (58, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Bensafrim'', NULL),
    (59, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Lagos'', NULL),
    (60, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Lagos'', N''Lagos Cidade''),
    (61, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Lagos'', N''Meia Praia''),
    (62, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Lagos'', N''Quinta da Boavista''),
    (63, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Lagos'', N''Falfeira - Monte Funchal''),
    (64, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Lagos'', N''Chinicato - Sargaçal''),
    (65, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Luz'', NULL),
    (66, N''Portugal'', N''Faro'', N''Lagos'', NULL, N''Odiaxere'', NULL),
    (67, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''Centro''),
    (68, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''Vale do Lobo''),
    (69, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''Quinta do Lago - Pinheiros Altos''),
    (70, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''Vale do Garrão - Varandas do Lago - Quinta das Salinas''),
    (71, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''The Village - Fonte Algarve - Quinta Verde''),
    (72, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''Vale Formoso - Vale d''''Éguas''),
    (73, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Almancil'', N''São Lourenço - São João da Venda''),
    (74, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Alte'', NULL),
    (75, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Ameixial'', NULL),
    (76, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Benafim'', NULL),
    (77, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Boliqueime'', NULL),
    (78, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Praia de Quarteira''),
    (79, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Centro - Quarteira Velha''),
    (80, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Fonte Santa''),
    (81, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Aldeia do Golf - Alto do Golf''),
    (82, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Vilamoura''),
    (83, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Marina de Vilamoura''),
    (84, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Pinhal Velho - Terraços do Pinhal - Encosta das Oliveiras'');
    INSERT INTO [MarketAreas] ([Id], [Country], [District], [Municipality], [Notes], [Town], [Zone])
    VALUES (85, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Quarteira'', N''Vila Sol - Morgadinho''),
    (86, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Querença'', NULL),
    (87, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Salir'', NULL),
    (88, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''São Clemente'', N''Centro Histórico Este de Loulé''),
    (89, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''São Clemente'', N''Centro Este da Cidade de Loulé''),
    (90, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''São Sebastião'', N''Centro Oeste da Cidade de Loulé''),
    (91, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''São Sebastião'', N''Cerro de Cabeça de Câmara - Estação de Loulé''),
    (92, N''Portugal'', N''Faro'', N''Loulé'', NULL, N''Tor'', NULL),
    (93, N''Portugal'', N''Faro'', N''Monchique'', NULL, N''Alferce'', NULL),
    (94, N''Portugal'', N''Faro'', N''Monchique'', NULL, N''Marmelete'', NULL),
    (95, N''Portugal'', N''Faro'', N''Monchique'', NULL, N''Monchique'', NULL),
    (96, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Fuseta'', NULL),
    (97, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Moncarapacho'', NULL),
    (98, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Olhão'', N''Baixa''),
    (99, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Olhão'', N''Marina''),
    (100, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Olhão'', N''Cavalinha - Bombeiros''),
    (101, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Olhão'', N''Estádio''),
    (102, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Pechão'', NULL),
    (103, N''Portugal'', N''Faro'', N''Olhão'', NULL, N''Quelfes'', NULL),
    (104, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Alvor'', NULL),
    (105, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Mexilhoeira Grande'', NULL),
    (106, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', NULL),
    (107, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Portimão Cidade''),
    (108, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Praia da Rocha''),
    (109, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Aldeia do Carrasco - Vale da Arrancada''),
    (110, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Amparo - Alto do Quintão''),
    (111, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Bemposta - Quatro Estradas''),
    (112, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Quinta da Malata''),
    (113, N''Portugal'', N''Faro'', N''Portimão'', NULL, N''Portimão'', N''Vale de Lagar - Quinta das Oliveiras - Pedra Mourinha''),
    (114, N''Portugal'', N''Faro'', N''São Brás de Alportel'', NULL, N''São Brás de Alportel'', N''Centro''),
    (115, N''Portugal'', N''Faro'', N''São Brás de Alportel'', NULL, N''São Brás de Alportel'', N''Campina - Mesquita''),
    (116, N''Portugal'', N''Faro'', N''São Brás de Alportel'', NULL, N''São Brás de Alportel'', N''São Romão - Fonte do Touro''),
    (117, N''Portugal'', N''Faro'', N''São Brás de Alportel'', NULL, N''São Brás de Alportel'', N''Funchais - Corotelo''),
    (118, N''Portugal'', N''Faro'', N''São Brás de Alportel'', NULL, N''São Brás de Alportel'', N''Barrabés - Peral''),
    (119, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Alcantarilha'', NULL),
    (120, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Algoz'', NULL),
    (121, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Armação de Pêra'', NULL),
    (122, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Pêra'', NULL),
    (123, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Centro da Cidade''),
    (124, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Zona Histórica''),
    (125, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Vila Fria''),
    (126, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Vale da Vila - Poço Barreto'');
    INSERT INTO [MarketAreas] ([Id], [Country], [District], [Municipality], [Notes], [Town], [Zone])
    VALUES (127, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Enxerim - Barrada''),
    (128, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Estação de Silves - Cerro de São Miguel''),
    (129, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Silves'', N''Serra - Barragem do Arade''),
    (130, N''Portugal'', N''Faro'', N''Silves'', NULL, N''São Bartolomeu de Messines'', NULL),
    (131, N''Portugal'', N''Faro'', N''Silves'', NULL, N''São Marcos da Serra'', NULL),
    (132, N''Portugal'', N''Faro'', N''Silves'', NULL, N''Tunes'', NULL),
    (133, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Cabanas de Tavira'', NULL),
    (134, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Cachopo'', NULL),
    (135, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Conceição'', NULL),
    (136, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Luz de Tavira'', NULL),
    (137, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Santa Catarina - Fonte do Bispo'', NULL),
    (138, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Santa Luzia'', NULL),
    (139, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Santo Estêvão'', NULL),
    (140, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', NULL),
    (141, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', N''Centro Histórico''),
    (142, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', N''Porta Nova - Colinas da Boavista''),
    (143, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', N''Pegada - Mato Santo Espírito - Vale Carangueijo''),
    (144, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', N''Quinta da Foz - Escolas''),
    (145, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', N''Colina de Asseca - Quinta de Perogil - São Pedro''),
    (146, N''Portugal'', N''Faro'', N''Tavira'', NULL, N''Tavira'', N''Serra''),
    (147, N''Portugal'', N''Faro'', N''Vila do Bispo'', NULL, N''Barão de São Miguel'', NULL),
    (148, N''Portugal'', N''Faro'', N''Vila do Bispo'', NULL, N''Budens'', NULL),
    (149, N''Portugal'', N''Faro'', N''Vila do Bispo'', NULL, N''Sagres'', NULL),
    (150, N''Portugal'', N''Faro'', N''Vila do Bispo'', NULL, N''Vila do Bispo e Raposeira'', NULL),
    (151, N''Portugal'', N''Faro'', N''Vila Real de Santo António'', NULL, N''Monte Gordo'', NULL),
    (152, N''Portugal'', N''Faro'', N''Vila Real de Santo António'', NULL, N''Vila Nova de Cacela'', NULL),
    (153, N''Portugal'', N''Faro'', N''Vila Real de Santo António'', NULL, N''Vila Real de Santo António'', NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Country', N'District', N'Municipality', N'Notes', N'Town', N'Zone') AND [object_id] = OBJECT_ID(N'[MarketAreas]'))
        SET IDENTITY_INSERT [MarketAreas] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630104712_SeedMarketAreas'
)
BEGIN
    CREATE INDEX [IX_ListingSnapshots_PropertyListingId_SnapshotDateUtc] ON [ListingSnapshots] ([PropertyListingId], [SnapshotDateUtc]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630104712_SeedMarketAreas'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630104712_SeedMarketAreas', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    ALTER TABLE [PropertyListings] ADD [DistanceToBeachMethod] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    ALTER TABLE [PropertyListings] ADD [NearestBeachMarkerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    ALTER TABLE [PropertyListings] ADD [NearestBeachName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    ALTER TABLE [ListingSnapshots] ADD [PricePerM2] decimal(18,2) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    CREATE TABLE [BeachMarkers] (
        [Id] int NOT NULL IDENTITY,
        [OsmId] bigint NOT NULL,
        [Name] nvarchar(max) NULL,
        [Latitude] decimal(9,6) NOT NULL,
        [Longitude] decimal(9,6) NOT NULL,
        [Region] nvarchar(max) NULL,
        [CreatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_BeachMarkers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    CREATE INDEX [IX_PropertyListings_NearestBeachMarkerId] ON [PropertyListings] ([NearestBeachMarkerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    ALTER TABLE [PropertyListings] ADD CONSTRAINT [FK_PropertyListings_BeachMarkers_NearestBeachMarkerId] FOREIGN KEY ([NearestBeachMarkerId]) REFERENCES [BeachMarkers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630141929_AddBeachMarkers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630141929_AddBeachMarkers', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630144455_SeedBeachMarkers'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[BeachMarkers]') AND [c].[name] = N'CreatedAtUtc');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [BeachMarkers] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [BeachMarkers] ADD DEFAULT (GETUTCDATE()) FOR [CreatedAtUtc];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630144455_SeedBeachMarkers'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Latitude', N'Longitude', N'Name', N'OsmId', N'Region') AND [object_id] = OBJECT_ID(N'[BeachMarkers]'))
        SET IDENTITY_INSERT [BeachMarkers] ON;
    EXEC(N'INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (1, 37.119263, -8.637639, N''Meia Praia'', CAST(432987903 AS bigint), NULL),
    (2, 37.120719, -8.626936, N''Meia Praia'', CAST(1069286046 AS bigint), NULL),
    (3, 37.109609, -8.655367, N''Meia Praia'', CAST(1245391449 AS bigint), NULL),
    (4, 37.176695, -7.447375, N''Monte Gordo'', CAST(590527191 AS bigint), NULL),
    (5, 37.088848, -8.668601, N''Pinheiro Beach'', CAST(93278498 AS bigint), NULL),
    (6, 37.176213, -7.466071, N''Praia Adão e Eva'', CAST(8211403118 AS bigint), NULL),
    (7, 37.102812, -8.507711, N''Praia Afurada Naturista'', CAST(1252978463 AS bigint), NULL),
    (8, 37.056394, -8.850439, N''Praia Andorinha'', CAST(1244215650 AS bigint), NULL),
    (9, 37.126854, -8.925067, N''Praia Cordama Naturista'', CAST(1239998563 AS bigint), NULL),
    (10, 37.161706, -8.48654, N''Praia Fluvial'', CAST(474645895 AS bigint), NULL),
    (11, 37.116416, -8.52026, N''Praia Grande (Ferragudo)'', CAST(101015524 AS bigint), NULL),
    (12, 37.093923, -8.340672, N''Praia Grande de Pêra'', CAST(252621789 AS bigint), NULL),
    (13, 37.061813, -8.834376, N''Praia Grodo Mexilhão'', CAST(1328895446 AS bigint), NULL),
    (14, 37.043951, -8.885063, N''Praia João Vaz'', CAST(810715094 AS bigint), NULL),
    (15, 37.116217, -8.567881, N''Praia João de Arens'', CAST(1245934887 AS bigint), NULL),
    (16, 37.115636, -8.567341, N''Praia João de Arens'', CAST(1246032644 AS bigint), NULL),
    (17, 37.076409, -8.309226, N''Praia Manuel Lourenço'', CAST(1250926293 AS bigint), NULL),
    (18, 37.088785, -8.201259, N''Praia Maria Luísa'', CAST(129581900 AS bigint), NULL),
    (19, 37.09592, -8.388262, N''Praia Nova'', CAST(119246518 AS bigint), NULL),
    (20, 37.116219, -8.574277, N''Praia RF'', CAST(1245711162 AS bigint), NULL),
    (21, 37.061441, -8.83676, N''Praia Santa'', CAST(393042815 AS bigint), NULL),
    (22, 37.115079, -7.623417, N''Praia Tavira-Ria'', CAST(79087228 AS bigint), NULL),
    (23, 37.077116, -8.310642, N''Praia Tomás Franco'', CAST(93448953 AS bigint), NULL),
    (24, 37.101802, -8.370281, N''Praia Vale do Olival'', CAST(1497785950 AS bigint), NULL),
    (25, 37.173954, -7.478928, N''Praia Verde'', CAST(79503288 AS bigint), NULL),
    (26, 37.172407, -7.485948, N''Praia Verdelago'', CAST(588367516 AS bigint), NULL),
    (27, 37.1034, -8.509231, N''Praia da Afurada'', CAST(1252978464 AS bigint), NULL),
    (28, 37.169481, -7.498076, N''Praia da Alagoa'', CAST(79503985 AS bigint), NULL),
    (29, 37.091049, -8.399888, N''Praia da Albandeira'', CAST(1249710119 AS bigint), NULL),
    (30, 37.090888, -8.400358, N''Praia da Albandeira'', CAST(1249710120 AS bigint), NULL),
    (31, 37.350858, -8.844728, N''Praia da Amoreira'', CAST(1242019126 AS bigint), NULL),
    (32, 37.482407, -8.794534, N''Praia da Amália'', CAST(922225754 AS bigint), NULL),
    (33, 37.121504, -8.522726, N''Praia da Angrinha'', CAST(101836680 AS bigint), NULL),
    (34, 37.018157, -7.7922, N''Praia da Armona-Mar'', CAST(79531240 AS bigint), NULL),
    (35, 37.02345, -7.804726, N''Praia da Armona-Ria'', CAST(222576671 AS bigint), NULL),
    (36, 37.292058, -8.865464, N''Praia da Arrifana'', CAST(1241777732 AS bigint), NULL),
    (37, 37.075513, -8.305551, N''Praia da Balbina'', CAST(1236137162 AS bigint), NULL),
    (38, 37.081766, -8.262189, N''Praia da Baleeira'', CAST(129580025 AS bigint), NULL),
    (39, 37.011318, -8.930381, N''Praia da Baleeira'', CAST(176818984 AS bigint), NULL),
    (40, 37.394556, -8.818939, N''Praia da Barradinha'', CAST(964252291 AS bigint), NULL),
    (41, 37.118858, -8.930705, N''Praia da Barriga'', CAST(1239998570 AS bigint), NULL),
    (42, 37.118974, -8.929653, N''Praia da Barriga'', CAST(1239998572 AS bigint), NULL);
    INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (43, 37.097853, -8.667959, N''Praia da Batata'', CAST(24461578 AS bigint), NULL),
    (44, 37.066364, -8.808916, N''Praia da Boca do Rio'', CAST(1244670407 AS bigint), NULL),
    (45, 37.199504, -8.900906, N''Praia da Bordeira'', CAST(1241619236 AS bigint), NULL),
    (46, 37.096044, -8.66724, N''Praia da Caldeira'', CAST(1253692196 AS bigint), NULL),
    (47, 37.362283, -8.839475, N''Praia da Carreagem'', CAST(1242776850 AS bigint), NULL),
    (48, 37.073684, -8.294141, N''Praia da Coelha'', CAST(1251789728 AS bigint), NULL),
    (49, 37.1093, -8.937528, N''Praia da Cordoama'', CAST(1327926836 AS bigint), NULL),
    (50, 37.08752, -8.421607, N''Praia da Corredoura'', CAST(1247825356 AS bigint), NULL),
    (51, 37.098578, -8.380612, N''Praia da Cova Redonda'', CAST(107913221 AS bigint), NULL),
    (52, 36.99488, -7.82439, N''Praia da Culatra'', CAST(159418246 AS bigint), NULL),
    (53, 37.003242, -7.802258, N''Praia da Culatra'', CAST(6757020738 AS bigint), NULL),
    (54, 37.090889, -8.401201, N''Praia da Estaquinha'', CAST(1249669603 AS bigint), NULL),
    (55, 37.408054, -8.811674, N''Praia da Esteveira'', CAST(957426818 AS bigint), NULL),
    (56, 37.075193, -8.132325, N''Praia da Falésia'', CAST(129581895 AS bigint), NULL),
    (57, 37.08044, -8.149166, N''Praia da Falésia'', CAST(675161585 AS bigint), NULL),
    (58, 37.083064, -8.158103, N''Praia da Falésia Alfamar'', CAST(129584057 AS bigint), NULL),
    (59, 37.086681, -8.169256, N''Praia da Falésia Açoteias'', CAST(677024792 AS bigint), NULL),
    (60, 37.060727, -8.840333, N''Praia da Figueira'', CAST(156815841 AS bigint), NULL),
    (61, 37.058393, -8.845473, N''Praia da Foia do Carro'', CAST(1244215657 AS bigint), NULL),
    (62, 37.073568, -8.296799, N''Praia da Fraternidade'', CAST(309582065 AS bigint), NULL),
    (63, 37.046321, -7.739255, N''Praia da Fuseta-Mar'', CAST(79522255 AS bigint), NULL),
    (64, 37.049962, -7.744352, N''Praia da Fuseta-Ria'', CAST(78427662 AS bigint), NULL),
    (65, 37.081841, -8.318332, N''Praia da Galé'', CAST(1250910960 AS bigint), NULL),
    (66, 37.080016, -8.315348, N''Praia da Galé (leste)'', CAST(1250910959 AS bigint), NULL),
    (67, 36.962919, -7.879166, N''Praia da Ilha Deserta'', CAST(82505950 AS bigint), NULL),
    (68, 36.972267, -7.923369, N''Praia da Ilha da Barreta'', CAST(471184443 AS bigint), NULL),
    (69, 37.003842, -7.990809, N''Praia da Ilha de Faro'', CAST(12567393 AS bigint), NULL),
    (70, 37.109911, -7.620998, N''Praia da Ilha de Tavira'', CAST(78432559 AS bigint), NULL),
    (71, 37.046621, -8.879296, N''Praia da Ingrina'', CAST(156814646 AS bigint), NULL),
    (72, 37.057651, -8.081173, N''Praia da Lagoa'', CAST(655688261 AS bigint), NULL),
    (73, 37.166069, -7.510443, N''Praia da Lota'', CAST(79504039 AS bigint), NULL),
    (74, 37.086769, -8.724609, N''Praia da Luz'', CAST(156815842 AS bigint), NULL),
    (75, 37.089945, -8.407134, N''Praia da Malhada do Baraço'', CAST(160455813 AS bigint), NULL),
    (76, 37.161878, -7.521573, N''Praia da Manta Rota'', CAST(79504187 AS bigint), NULL),
    (77, 37.005047, -8.938728, N''Praia da Mareta'', CAST(71340752 AS bigint), NULL),
    (78, 37.089777, -8.412608, N''Praia da Marinha'', CAST(120164558 AS bigint), NULL),
    (79, 37.089642, -8.414159, N''Praia da Marinha'', CAST(1249063477 AS bigint), NULL),
    (80, 37.073835, -8.295783, N''Praia da Maré das Porcas'', CAST(1251789729 AS bigint), NULL),
    (81, 37.089199, -8.415089, N''Praia da Mesquita'', CAST(160455810 AS bigint), NULL),
    (82, 37.093053, -8.393998, N''Praia da Morena'', CAST(1249669595 AS bigint), NULL),
    (83, 37.154764, -8.908356, N''Praia da Muração'', CAST(1240127219 AS bigint), NULL),
    (84, 37.085311, -8.223371, N''Praia da Oura'', CAST(1329493935 AS bigint), NULL);
    INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (85, 37.27495, -8.863148, N''Praia da Pedra da Agulha'', CAST(1491203599 AS bigint), NULL),
    (86, 37.085055, -8.218348, N''Praia da Pedra dos Bicos'', CAST(155804981 AS bigint), NULL),
    (87, 37.073297, -8.287443, N''Praia da Ponta Grande'', CAST(1251789696 AS bigint), NULL),
    (88, 37.073565, -8.285315, N''Praia da Ponta Pequena'', CAST(1106872193 AS bigint), NULL),
    (89, 37.068772, -8.964573, N''Praia da Ponta Ruiva'', CAST(1243482695 AS bigint), NULL),
    (90, 37.097147, -8.384048, N''Praia da Ponta da Adega'', CAST(470523247 AS bigint), NULL),
    (91, 37.118278, -8.578246, N''Praia da Prainha'', CAST(1326145039 AS bigint), NULL),
    (92, 37.024676, -8.024685, N''Praia da Quinta do Lago'', CAST(84374228 AS bigint), NULL),
    (93, 37.115647, -8.536209, N''Praia da Rocha'', CAST(98687797 AS bigint), NULL),
    (94, 37.064971, -8.821625, N''Praia da Salema'', CAST(1244670402 AS bigint), NULL),
    (95, 37.398341, -8.816673, N''Praia da Samouqueira'', CAST(1256654176 AS bigint), NULL),
    (96, 37.097102, -8.385648, N''Praia da Senhora da Rocha'', CAST(1249999772 AS bigint), NULL),
    (97, 37.098588, -7.639625, N''Praia da Terra Estreita na Ilha de Tavira'', CAST(78432429 AS bigint), NULL),
    (98, 37.075098, -8.278401, N''Praia da Viga'', CAST(1252144328 AS bigint), NULL),
    (99, 37.192942, -8.913909, N''Praia da Zimbreirinha'', CAST(1328233392 AS bigint), NULL),
    (100, 37.43918, -8.800545, N''Praia das Adegas'', CAST(1242990213 AS bigint), NULL),
    (101, 37.097444, -8.383522, N''Praia das Escaleiras'', CAST(1250076184 AS bigint), NULL),
    (102, 37.092007, -8.395595, N''Praia das Fontaínhas'', CAST(160455815 AS bigint), NULL),
    (103, 37.055223, -8.85443, N''Praia das Furnas'', CAST(156814659 AS bigint), NULL),
    (104, 37.168321, -7.503516, N''Praia das Primas'', CAST(12070720137 AS bigint), NULL),
    (105, 37.073274, -8.289793, N''Praia das Salamitras'', CAST(1251789705 AS bigint), NULL),
    (106, 37.065572, -8.795579, N''Praia de Almádena'', CAST(1244670399 AS bigint), NULL),
    (107, 37.100188, -8.359572, N''Praia de Armação de Pêra'', CAST(259211193 AS bigint), NULL),
    (108, 37.087351, -8.425864, N''Praia de Benagil'', CAST(465776072 AS bigint), NULL),
    (109, 37.132303, -7.59143, N''Praia de Cabanas'', CAST(79509901 AS bigint), NULL),
    (110, 37.064806, -8.792952, N''Praia de Cabanas Velhas (Naturista)'', CAST(1056515224 AS bigint), NULL),
    (111, 37.151574, -7.547415, N''Praia de Cacela Velha'', CAST(7212953 AS bigint), NULL),
    (112, 37.334504, -8.860224, N''Praia de Coelha'', CAST(38502852 AS bigint), NULL),
    (113, 37.092082, -8.668811, N''Praia de Dona Ana'', CAST(29437665 AS bigint), NULL),
    (114, 37.054563, -8.075932, N''Praia de Loulé Velho'', CAST(261606273 AS bigint), NULL),
    (115, 37.342187, -8.853054, N''Praia de Monte Clérigo'', CAST(55705498 AS bigint), NULL),
    (116, 37.176682, -7.447351, N''Praia de Monte Gordo'', CAST(587923984 AS bigint), NULL),
    (117, 37.442145, -8.797848, N''Praia de Odeceixe-Mar'', CAST(1242990216 AS bigint), NULL),
    (118, 37.472315, -7.47697, N''Praia de Pego Fundo'', CAST(78417257 AS bigint), NULL),
    (119, 37.065284, -8.099553, N''Praia de Quarteira'', CAST(142594780 AS bigint), NULL),
    (120, 37.087687, -8.213484, N''Praia de Santa Eulália'', CAST(62955217 AS bigint), NULL),
    (121, 37.171962, -7.417776, N''Praia de Santo António'', CAST(5448095 AS bigint), NULL),
    (122, 37.074783, -8.280342, N''Praia de São Rafael'', CAST(1252085905 AS bigint), NULL),
    (123, 37.091385, -8.455771, N''Praia de Vale Centianes'', CAST(1247020899 AS bigint), NULL),
    (124, 37.237411, -8.875412, N''Praia de Vale Figueira'', CAST(828824087 AS bigint), NULL),
    (125, 37.247352, -8.869399, N''Praia de Vale Figueira'', CAST(1241777810 AS bigint), NULL),
    (126, 37.04869, -8.065718, N''Praia de Vale do Lobo'', CAST(655688263 AS bigint), NULL);
    INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (127, 37.385319, -8.824572, N''Praia de Vale dos Homens'', CAST(1242844058 AS bigint), NULL),
    (128, 37.071106, -8.116533, N''Praia de Vilamoura'', CAST(85956082 AS bigint), NULL),
    (129, 37.119778, -8.562213, N''Praia do Alemão'', CAST(607027167 AS bigint), NULL),
    (130, 37.059271, -8.08437, N''Praia do Almargem'', CAST(655688259 AS bigint), NULL),
    (131, 37.121559, -8.58998, N''Praia do Alvor Nascente'', CAST(606996959 AS bigint), NULL),
    (132, 37.122701, -8.597534, N''Praia do Alvor Poente'', CAST(606996960 AS bigint), NULL),
    (133, 37.16434, -8.903388, N''Praia do Amado'', CAST(1239998556 AS bigint), NULL),
    (134, 37.16932, -8.903121, N''Praia do Amado'', CAST(1328449735 AS bigint), NULL),
    (135, 37.032438, -8.038003, N''Praia do Ancão'', CAST(655688262 AS bigint), NULL),
    (136, 37.042099, -8.895173, N''Praia do Barranco'', CAST(156814654 AS bigint), NULL),
    (137, 37.095394, -8.391569, N''Praia do Barranco'', CAST(1249999787 AS bigint), NULL),
    (138, 37.089869, -8.180487, N''Praia do Barranco das Belharucas'', CAST(129584051 AS bigint), NULL),
    (139, 37.119214, -8.564496, N''Praia do Barranco das Canas'', CAST(218009625 AS bigint), NULL),
    (140, 37.089894, -8.405567, N''Praia do Barranquinho'', CAST(1249300471 AS bigint), NULL),
    (141, 37.073892, -7.686754, N''Praia do Barril'', CAST(78432336 AS bigint), NULL),
    (142, 37.085207, -7.663338, N''Praia do Barril'', CAST(1501060262 AS bigint), NULL),
    (143, 37.025934, -8.96508, N''Praia do Beliche'', CAST(94838670 AS bigint), NULL),
    (144, 37.118387, -8.56607, N''Praia do Boião'', CAST(1246032617 AS bigint), NULL),
    (145, 37.089433, -8.411204, N''Praia do Buraco'', CAST(232240426 AS bigint), NULL),
    (146, 37.071302, -8.775575, N''Praia do Burgau'', CAST(38502661 AS bigint), NULL),
    (147, 37.175494, -7.468997, N''Praia do Cabeço'', CAST(79503102 AS bigint), NULL),
    (148, 37.087326, -8.66843, N''Praia do Camilo'', CAST(71500098 AS bigint), NULL),
    (149, 37.087981, -8.668616, N''Praia do Camilo'', CAST(71500099 AS bigint), NULL),
    (150, 37.26684, -8.860857, N''Praia do Canal'', CAST(208839203 AS bigint), NULL),
    (151, 37.270565, -8.860261, N''Praia do Canal'', CAST(1255794138 AS bigint), NULL),
    (152, 37.083841, -8.679296, N''Praia do Canavial'', CAST(288904496 AS bigint), NULL),
    (153, 37.500879, -8.792577, N''Praia do Carvalhal'', CAST(157818268 AS bigint), NULL),
    (154, 37.086638, -8.431714, N''Praia do Carvalho'', CAST(110468045 AS bigint), NULL),
    (155, 37.096037, -8.472388, N''Praia do Carvoeiro'', CAST(129212171 AS bigint), NULL),
    (156, 37.100071, -8.947016, N''Praia do Castelejo'', CAST(1244531551 AS bigint), NULL),
    (157, 37.073176, -8.2989, N''Praia do Castelo'', CAST(48433466 AS bigint), NULL),
    (158, 37.078995, -8.313542, N''Praia do Chiringuito'', CAST(1250594381 AS bigint), NULL),
    (159, 37.08745, -8.418131, N''Praia do Cão Raivoso'', CAST(1248930430 AS bigint), NULL),
    (160, 37.074228, -8.302827, N''Praia do Evaristo'', CAST(51789928 AS bigint), NULL),
    (161, 36.977569, -7.857225, N''Praia do Farol'', CAST(19450196 AS bigint), NULL),
    (162, 36.981104, -7.861517, N''Praia do Farol (ria)'', CAST(1421663412 AS bigint), NULL),
    (163, 37.061309, -8.088447, N''Praia do Forte Novo'', CAST(655494861 AS bigint), NULL),
    (164, 37.098841, -8.668077, N''Praia do Forte da Bandeira'', CAST(6802536 AS bigint), NULL),
    (165, 37.120012, -7.61198, N''Praia do Forte da Barra'', CAST(79511624 AS bigint), NULL),
    (166, 37.041329, -8.053124, N''Praia do Garrão'', CAST(655688264 AS bigint), NULL),
    (167, 37.061961, -7.709914, N''Praia do Homem Nú'', CAST(7523213 AS bigint), NULL),
    (168, 37.08577, -8.245228, N''Praia do Inatel'', CAST(1357090689 AS bigint), NULL);
    INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (169, 37.141966, -7.571321, N''Praia do Lacém'', CAST(490879098 AS bigint), NULL),
    (170, 37.018518, -8.925617, N''Praia do Martinhal'', CAST(1254208003 AS bigint), NULL),
    (171, 37.102211, -8.506231, N''Praia do Mato'', CAST(1246703809 AS bigint), NULL),
    (172, 37.324294, -8.869489, N''Praia do Medo da Fonte Santa'', CAST(1242614517 AS bigint), NULL),
    (173, 37.137011, -8.920089, N''Praia do Mirouço'', CAST(208834927 AS bigint), NULL),
    (174, 37.146929, -8.915884, N''Praia do Mirouço'', CAST(1240127231 AS bigint), NULL),
    (175, 37.110338, -8.519859, N''Praia do Molhe'', CAST(7093714 AS bigint), NULL),
    (176, 37.073473, -8.286632, N''Praia do Ninho de Andorinha'', CAST(1251789697 AS bigint), NULL),
    (177, 37.096949, -8.475614, N''Praia do Paraíso'', CAST(303665313 AS bigint), NULL),
    (178, 37.089741, -8.410205, N''Praia do Pau'', CAST(160455814 AS bigint), NULL),
    (179, 37.086098, -8.253657, N''Praia do Peneco'', CAST(129575837 AS bigint), NULL),
    (180, 37.252319, -8.866244, N''Praia do Penedo'', CAST(1255794144 AS bigint), NULL),
    (181, 37.094389, -8.668038, N''Praia do Pinhão'', CAST(93278485 AS bigint), NULL),
    (182, 37.108079, -8.51886, N''Praia do Pintadinho'', CAST(117570615 AS bigint), NULL),
    (183, 37.093443, -8.392541, N''Praia do Pontal'', CAST(1249669592 AS bigint), NULL),
    (184, 37.174365, -8.907809, N''Praia do Portinho do Forno'', CAST(487969876 AS bigint), NULL),
    (185, 37.08482, -8.689938, N''Praia do Porto de Mós'', CAST(886719362 AS bigint), NULL),
    (186, 37.11859, -7.625204, N''Praia do Rato'', CAST(517429596 AS bigint), NULL),
    (187, 37.079659, -8.264317, N''Praia do Risco'', CAST(1252443721 AS bigint), NULL),
    (188, 37.077844, -8.311647, N''Praia do Sao Lourenco'', CAST(1250910953 AS bigint), NULL),
    (189, 37.116502, -8.571263, N''Praia do Submarino'', CAST(1245711159 AS bigint), NULL),
    (190, 37.116397, -8.571722, N''Praia do Submarino'', CAST(1245711160 AS bigint), NULL),
    (191, 37.116586, -8.570437, N''Praia do Submarino'', CAST(1245934920 AS bigint), NULL),
    (192, 37.045636, -8.979094, N''Praia do Telheiro'', CAST(1243320967 AS bigint), NULL),
    (193, 37.005714, -8.947897, N''Praia do Tonel'', CAST(1255056880 AS bigint), NULL),
    (194, 37.105867, -8.515883, N''Praia do Torrado'', CAST(204181234 AS bigint), NULL),
    (195, 37.093147, -8.459854, N''Praia do Vale Covo'', CAST(481006224 AS bigint), NULL),
    (196, 37.093313, -8.460129, N''Praia do Vale Covo'', CAST(1247020903 AS bigint), NULL),
    (197, 37.08781, -8.440759, N''Praia do Vale Espinhaço'', CAST(470544217 AS bigint), NULL),
    (198, 37.119863, -8.559292, N''Praia do Vau'', CAST(607027170 AS bigint), NULL),
    (199, 37.046402, -8.870493, N''Praia do Zavial'', CAST(156814657 AS bigint), NULL),
    (200, 37.043959, -8.866545, N''Praia do Zavial Naturista'', CAST(1243839258 AS bigint), NULL),
    (201, 37.084011, -8.237966, N''Praia dos Alemães'', CAST(129577161 AS bigint), NULL),
    (202, 37.075921, -8.277528, N''Praia dos Arrifes'', CAST(1252144318 AS bigint), NULL),
    (203, 37.083406, -8.231314, N''Praia dos Aveiros'', CAST(129581901 AS bigint), NULL),
    (204, 37.100828, -8.373887, N''Praia dos Beijinhos'', CAST(108165451 AS bigint), NULL),
    (205, 37.07827, -8.312479, N''Praia dos Bés'', CAST(1250910956 AS bigint), NULL),
    (206, 37.104988, -8.514001, N''Praia dos Caneiros'', CAST(1246703830 AS bigint), NULL),
    (207, 37.119051, -8.555377, N''Praia dos Careanos'', CAST(607027161 AS bigint), NULL),
    (208, 37.098696, -8.481946, N''Praia dos Castelos'', CAST(621125242 AS bigint), NULL),
    (209, 37.03626, -7.796241, N''Praia dos Cavacos'', CAST(508415850 AS bigint), NULL),
    (210, 37.096498, -8.66749, N''Praia dos Estudantes'', CAST(93278502 AS bigint), NULL);
    INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (211, 37.492439, -8.794897, N''Praia dos Machados'', CAST(208117757 AS bigint), NULL),
    (212, 37.089709, -8.190699, N''Praia dos Olhos de Água'', CAST(1254991134 AS bigint), NULL),
    (213, 37.074524, -8.282057, N''Praia dos Paradinha'', CAST(1252085894 AS bigint), NULL),
    (214, 37.086563, -8.250091, N''Praia dos Pescadores'', CAST(129575835 AS bigint), NULL),
    (215, 37.08434, -8.667266, N''Praia dos Pinheiros'', CAST(1245077034 AS bigint), NULL),
    (216, 37.07385, -8.283768, N''Praia dos Piratas'', CAST(1252085915 AS bigint), NULL),
    (217, 37.022221, -8.920294, N''Praia dos Rebolinhos'', CAST(1254208004 AS bigint), NULL),
    (218, 37.068774, -8.782215, N''Praia dos Rebolos'', CAST(453722169 AS bigint), NULL),
    (219, 37.086338, -8.325025, N''Praia dos Salgados'', CAST(51986551 AS bigint), NULL),
    (220, 37.006532, -7.941509, N''Praia dos Tesos'', CAST(11384145 AS bigint), NULL),
    (221, 37.078372, -8.142019, N''Praia dos Tomates'', CAST(1324074429 AS bigint), NULL),
    (222, 37.099803, -8.377072, N''Praia dos Tremoços'', CAST(119246525 AS bigint), NULL),
    (223, 37.117654, -8.547906, N''Praia dos Três Castelos'', CAST(1246173354 AS bigint), NULL),
    (224, 37.119713, -8.581893, N''Praia dos Três Irmãos'', CAST(1245711193 AS bigint), NULL),
    (225, 37.084918, -8.730815, N''Prainha'', CAST(549029920 AS bigint), NULL),
    (226, 37.197106, -8.498926, N''Quinta Amoroso'', CAST(10224558485 AS bigint), NULL),
    (227, 37.319324, -8.876724, NULL, CAST(38502688 AS bigint), NULL),
    (228, 37.092326, -7.649813, NULL, CAST(78432449 AS bigint), NULL),
    (229, 37.032097, -7.763479, NULL, CAST(79527276 AS bigint), NULL),
    (230, 36.970832, -7.875069, NULL, CAST(82505947 AS bigint), NULL),
    (231, 37.052261, -7.741904, NULL, CAST(90144518 AS bigint), NULL),
    (232, 37.07568, -8.306681, NULL, CAST(92000516 AS bigint), NULL),
    (233, 37.094959, -8.667712, NULL, CAST(93278499 AS bigint), NULL),
    (234, 37.008333, -8.948965, NULL, CAST(94838668 AS bigint), NULL),
    (235, 37.133315, -8.59544, NULL, CAST(105689399 AS bigint), NULL),
    (236, 37.156158, -7.545889, NULL, CAST(132676483 AS bigint), NULL),
    (237, 37.125535, -8.609114, NULL, CAST(143915392 AS bigint), NULL),
    (238, 37.104999, -8.94194, NULL, CAST(157867259 AS bigint), NULL),
    (239, 37.055196, -7.722937, NULL, CAST(172993677 AS bigint), NULL),
    (240, 37.185821, -7.346082, NULL, CAST(192229778 AS bigint), NULL),
    (241, 37.001969, -7.81467, NULL, CAST(222572205 AS bigint), NULL),
    (242, 37.02179, -7.806153, NULL, CAST(222576674 AS bigint), NULL),
    (243, 37.128418, -8.523629, NULL, CAST(233235193 AS bigint), NULL),
    (244, 37.082952, -8.667795, NULL, CAST(288904494 AS bigint), NULL),
    (245, 37.083633, -8.667184, NULL, CAST(288904495 AS bigint), NULL),
    (246, 37.085573, -8.228386, NULL, CAST(308241715 AS bigint), NULL),
    (247, 37.471649, -7.468302, NULL, CAST(364956367 AS bigint), NULL),
    (248, 37.099216, -8.37783, NULL, CAST(437587998 AS bigint), NULL),
    (249, 37.035187, -8.024354, NULL, CAST(443102845 AS bigint), NULL),
    (250, 37.035168, -8.025976, NULL, CAST(443102849 AS bigint), NULL),
    (251, 37.117368, -7.617496, NULL, CAST(490880503 AS bigint), NULL),
    (252, 37.118929, -7.616337, NULL, CAST(490882066 AS bigint), NULL);
    INSERT INTO [BeachMarkers] ([Id], [Latitude], [Longitude], [Name], [OsmId], [Region])
    VALUES (253, 37.086565, -8.216681, NULL, CAST(564141456 AS bigint), NULL),
    (254, 37.088333, -8.207676, NULL, CAST(564274793 AS bigint), NULL),
    (255, 37.121797, -8.609001, NULL, CAST(604051046 AS bigint), NULL),
    (256, 37.113968, -8.934413, NULL, CAST(810707335 AS bigint), NULL),
    (257, 37.086824, -8.676214, NULL, CAST(986473094 AS bigint), NULL),
    (258, 37.292205, -8.467887, NULL, CAST(989678720 AS bigint), NULL),
    (259, 37.209234, -8.894058, NULL, CAST(1241717170 AS bigint), NULL),
    (260, 37.217422, -8.887937, NULL, CAST(1241717181 AS bigint), NULL),
    (261, 37.213668, -8.89075, NULL, CAST(1241777800 AS bigint), NULL),
    (262, 37.242035, -8.872296, NULL, CAST(1241777808 AS bigint), NULL),
    (263, 37.242805, -8.872015, NULL, CAST(1241777809 AS bigint), NULL),
    (264, 37.027507, -8.981808, NULL, CAST(1243513171 AS bigint), NULL),
    (265, 37.05726, -8.848322, NULL, CAST(1244215651 AS bigint), NULL),
    (266, 37.055562, -8.852719, NULL, CAST(1244215652 AS bigint), NULL),
    (267, 37.059629, -8.842395, NULL, CAST(1244215656 AS bigint), NULL),
    (268, 37.098203, -8.947404, NULL, CAST(1244531550 AS bigint), NULL),
    (269, 37.117078, -8.575344, NULL, CAST(1245711165 AS bigint), NULL),
    (270, 37.117322, -8.575883, NULL, CAST(1245711168 AS bigint), NULL),
    (271, 37.115041, -8.528531, NULL, CAST(1246423276 AS bigint), NULL),
    (272, 37.100624, -8.37496, NULL, CAST(1250076164 AS bigint), NULL),
    (273, 37.101448, -8.371729, NULL, CAST(1250076174 AS bigint), NULL),
    (274, 37.098062, -8.382532, NULL, CAST(1250076188 AS bigint), NULL),
    (275, 37.101399, -8.37216, NULL, CAST(1250548776 AS bigint), NULL),
    (276, 37.07561, -8.307255, NULL, CAST(1250926280 AS bigint), NULL),
    (277, 37.073671, -8.284536, NULL, CAST(1252085924 AS bigint), NULL),
    (278, 37.073731, -8.284654, NULL, CAST(1252085925 AS bigint), NULL),
    (279, 37.079226, -8.266801, NULL, CAST(1252443719 AS bigint), NULL),
    (280, 37.073749, -8.28377, NULL, CAST(1252495883 AS bigint), NULL),
    (281, 37.096106, -8.667517, NULL, CAST(1253692197 AS bigint), NULL),
    (282, 37.33695, -8.858353, NULL, CAST(1256162807 AS bigint), NULL),
    (283, 37.090052, -8.187091, NULL, CAST(1322900164 AS bigint), NULL),
    (284, 37.049898, -8.980785, NULL, CAST(1322931016 AS bigint), NULL),
    (285, 37.118003, -8.577588, NULL, CAST(1326145037 AS bigint), NULL),
    (286, 37.117964, -8.576956, NULL, CAST(1326145038 AS bigint), NULL),
    (287, 37.171007, -8.903739, NULL, CAST(1328449733 AS bigint), NULL),
    (288, 37.1711, -8.904012, NULL, CAST(1328449734 AS bigint), NULL),
    (289, 37.002627, -7.986199, NULL, CAST(1351185084 AS bigint), NULL),
    (290, 37.049158, -7.733876, NULL, CAST(5117235785 AS bigint), NULL),
    (291, 37.010991, -7.931671, NULL, CAST(8795678512 AS bigint), NULL)');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Latitude', N'Longitude', N'Name', N'OsmId', N'Region') AND [object_id] = OBJECT_ID(N'[BeachMarkers]'))
        SET IDENTITY_INSERT [BeachMarkers] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260630144455_SeedBeachMarkers'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260630144455_SeedBeachMarkers', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091209_AddCreatedAtUtcInProperties'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PropertyListings]') AND [c].[name] = N'CreatedAtUtc');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [PropertyListings] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [PropertyListings] ADD DEFAULT (GETUTCDATE()) FOR [CreatedAtUtc];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260703091209_AddCreatedAtUtcInProperties'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260703091209_AddCreatedAtUtcInProperties', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721163452_AddCollationForMarketArea'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MarketAreas]') AND [c].[name] = N'Zone');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [MarketAreas] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [MarketAreas] ALTER COLUMN [Zone] nvarchar(max) COLLATE Latin1_General_CI_AI NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721163452_AddCollationForMarketArea'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MarketAreas]') AND [c].[name] = N'Town');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [MarketAreas] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [MarketAreas] ALTER COLUMN [Town] nvarchar(max) COLLATE Latin1_General_CI_AI NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721163452_AddCollationForMarketArea'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MarketAreas]') AND [c].[name] = N'Municipality');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [MarketAreas] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [MarketAreas] ALTER COLUMN [Municipality] nvarchar(max) COLLATE Latin1_General_CI_AI NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721163452_AddCollationForMarketArea'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[MarketAreas]') AND [c].[name] = N'District');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [MarketAreas] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [MarketAreas] ALTER COLUMN [District] nvarchar(max) COLLATE Latin1_General_CI_AI NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260721163452_AddCollationForMarketArea'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260721163452_AddCollationForMarketArea', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    ALTER TABLE [OwnedProperties] ADD [HasAirConditioning] bit NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    ALTER TABLE [OwnedProperties] ADD [NearestBeachMarkerId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    ALTER TABLE [OwnedProperties] ADD [NearestBeachName] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    CREATE TABLE [PremiumFeatures] (
        [Id] int NOT NULL IDENTITY,
        [Feature] nvarchar(max) NOT NULL,
        [PremiumPercent] decimal(18,2) NOT NULL,
        [CalculatedAtUtc] datetime2 NOT NULL,
        CONSTRAINT [PK_PremiumFeatures] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    CREATE INDEX [IX_OwnedProperties_NearestBeachMarkerId] ON [OwnedProperties] ([NearestBeachMarkerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    ALTER TABLE [OwnedProperties] ADD CONSTRAINT [FK_OwnedProperties_BeachMarkers_NearestBeachMarkerId] FOREIGN KEY ([NearestBeachMarkerId]) REFERENCES [BeachMarkers] ([Id]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144110_AddedPremiumFeaturesTable'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723144110_AddedPremiumFeaturesTable', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144456_PremiumFeaturesPresisionsChanged'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PremiumFeatures]') AND [c].[name] = N'PremiumPercent');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [PremiumFeatures] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [PremiumFeatures] ALTER COLUMN [PremiumPercent] decimal(9,2) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260723144456_PremiumFeaturesPresisionsChanged'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260723144456_PremiumFeaturesPresisionsChanged', N'10.0.9');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260728142103_AddEnumsForPremiumFeatures'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260728142103_AddEnumsForPremiumFeatures', N'10.0.9');
END;

COMMIT;
GO

