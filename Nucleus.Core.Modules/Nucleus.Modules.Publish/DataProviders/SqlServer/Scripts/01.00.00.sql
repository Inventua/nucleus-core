IF OBJECT_ID(N'Articles', N'U') IS NULL
BEGIN
CREATE TABLE [Articles] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ModuleId] UNIQUEIDENTIFIER NOT NULL,
	[Title] NVARCHAR(255) NOT NULL,
	[EncodedTitle] NVARCHAR(255) NOT NULL,
	[SubTitle] NVARCHAR(255) NULL,
	[Description] NVARCHAR(255) NULL,
	[Summary] NVARCHAR(255) NULL,
	[Body] NVARCHAR(255) NULL,
	[ImageFileId] UNIQUEIDENTIFIER NULL,
	[PublishDate] DATETIME NULL,
	[ExpireDate] DATETIME NULL,
	[Enabled] BIT NOT NULL,
	[Featured] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Articles_ModuleId] FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Articles] ON [Articles]([Id]);
GO

CREATE UNIQUE INDEX [IX_Articles_Title] ON [Articles]([ModuleId],[Title]);
GO

IF OBJECT_ID(N'ArticleCategories', N'U') IS NULL
BEGIN
CREATE TABLE [ArticleCategories] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ArticleId] UNIQUEIDENTIFIER NOT NULL,
	[CategoryListItemId] UNIQUEIDENTIFIER NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ArticleCategories_ArticleId] FOREIGN KEY([ArticleId]) REFERENCES Articles([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ArticleCategories] ON [ArticleCategories]([Id]);
GO

CREATE UNIQUE INDEX [IX_ArticleCategories_ArticleCategory] ON [ArticleCategories]([ArticleId],[CategoryListItemId]);
GO

IF OBJECT_ID(N'ArticleAttachments', N'U') IS NULL
BEGIN
CREATE TABLE [ArticleAttachments] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ArticleId] UNIQUEIDENTIFIER NOT NULL,
	[FileId] UNIQUEIDENTIFIER NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ArticleAttachments_ArticleId] FOREIGN KEY([ArticleId]) REFERENCES Articles([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ArticleAttachments] ON [ArticleAttachments]([Id]);
GO

CREATE UNIQUE INDEX [PK_ArticleAttachments_FileId] ON [ArticleAttachments]([ArticleId],[FileId]);
GO