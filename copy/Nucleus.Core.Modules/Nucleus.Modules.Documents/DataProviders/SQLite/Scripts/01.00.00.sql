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
	PRIMARY KEY([Id],[ModuleId]),
	FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id]),
	FOREIGN KEY([FileId]) REFERENCES FileSystemItems([Id]) ON DELETE SET NULL
);
GO

