CREATE TABLE IF NOT EXISTS [Articles] 
(
	[Id] GUID NOT NULL,
	[ModuleId] GUID NOT NULL,
	[Title] TEXT NOT NULL,
	[EncodedTitle] TEXT NOT NULL,
	[SubTitle] TEXT NULL,
	[Description] TEXT NULL,
	[Summary] TEXT NULL,
	[Body] TEXT NULL,
	[ImageFileId] GUID NULL,
	[PublishDate] DATETIME NULL,
	[ExpireDate] DATETIME NULL,
	[Enabled] BIT NOT NULL,
	[Featured] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Articles] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Articles_ModuleId] FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id]) ON DELETE CASCADE,
	CONSTRAINT [IX_Articles_Title] UNIQUE ([ModuleId],[Title])
);
GO

CREATE TABLE IF NOT EXISTS [ArticleCategories] 
(
	[Id] GUID NOT NULL,
	[ArticleId] GUID NOT NULL,
	[CategoryListItemId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ArticleCategories] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ArticleCategories_ArticleId] FOREIGN KEY([ArticleId]) REFERENCES Articles([Id]) ON DELETE CASCADE,
	CONSTRAINT [IX_ArticleCategories_ArticleCategory] UNIQUE ([ArticleId],[CategoryListItemId])
);
GO

CREATE TABLE IF NOT EXISTS [ArticleAttachments] 
(
	[Id] GUID NOT NULL,
	[ArticleId] GUID NOT NULL,
	[FileId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ArticleAttachments] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ArticleAttachments_ArticleId] FOREIGN KEY([ArticleId]) REFERENCES Articles([Id]) ON DELETE CASCADE,
	CONSTRAINT [IX_ArticleAttachments_FileId] UNIQUE ([ArticleId],[FileId])
);
GO