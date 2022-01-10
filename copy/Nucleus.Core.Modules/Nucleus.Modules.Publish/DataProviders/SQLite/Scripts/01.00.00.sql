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
	PRIMARY KEY([Id]),
	FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id]) ON DELETE CASCADE,
	UNIQUE ([ModuleId],[Title])
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
	PRIMARY KEY([Id]),
	FOREIGN KEY([ArticleId]) REFERENCES Articles([Id]) ON DELETE CASCADE,
	FOREIGN KEY([CategoryListItemId]) REFERENCES ListItems([Id]) ON DELETE CASCADE,
	UNIQUE ([ArticleId],[CategoryListItemId])
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
	PRIMARY KEY([Id]),
	FOREIGN KEY([ArticleId]) REFERENCES Articles([Id]) ON DELETE CASCADE,
	UNIQUE ([ArticleId],[FileId])
);
GO