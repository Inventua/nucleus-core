﻿--IF OBJECT_ID(N'$nucleus_extension_name', N'U') IS NULL
--BEGIN
--CREATE TABLE [$nucleus_extension_name] 
--(
--	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
--	[ModuleId] UNIQUEIDENTIFIER NOT NULL,
--	[Title] NVARCHAR(255) NOT NULL,
--	[Description] NVARCHAR(255) NULL,
--	[CategoryId] UNIQUEIDENTIFIER NULL,
--	[SortOrder] INT NOT NULL,
--	[FileId] UNIQUEIDENTIFIER NULL,
--	[DateAdded] DATETIME NULL,
--	[AddedBy] UNIQUEIDENTIFIER NULL,
--	[DateChanged] DATETIME NULL,
--	[ChangedBy] UNIQUEIDENTIFIER NULL,
--	CONSTRAINT [FK_$nucleus_extension_name_ModuleId] FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id]) ON DELETE CASCADE	
--);
--END;
--GO

--CREATE UNIQUE CLUSTERED INDEX [PK_$nucleus_extension_name] ON [Documents]([Id]);
--GO

