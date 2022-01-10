CREATE TABLE IF NOT EXISTS [Documents] 
(
	[Id] GUID NOT NULL,
	[ModuleId] GUID NOT NULL,
	[Title] TEXT NOT NULL,
	[Description] TEXT NULL,
	[CategoryId] GUID NULL,
	[SortOrder] INT NOT NULL,
	[FileId] GUID NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_Documents] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Documents_ModuleId] FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id]) ON DELETE CASCADE
);
GO

