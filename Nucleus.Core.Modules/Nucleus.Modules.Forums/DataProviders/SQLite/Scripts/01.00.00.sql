CREATE TABLE IF NOT EXISTS [ForumGroups] 
(
	[Id] GUID NOT NULL,
	[ModuleId] GUID NOT NULL,
	[Name] TEXT NOT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [IX_ForumGroups_Name] UNIQUE([Name]),
	CONSTRAINT [PK_ForumGroups] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ForumGroups_ModuleId] FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id])
);
GO

CREATE TABLE IF NOT EXISTS [Forums] 
(
	[Id] GUID NOT NULL,
	[ForumGroupId] GUID NOT NULL,
	[Name] TEXT NOT NULL,
	[Description] TEXT NULL,
	[SortOrder] INT NOT NULL,
	[UseGroupSettings] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [IX_Forums_Name] UNIQUE([Name]),
	CONSTRAINT [PK_Forums] PRIMARY KEY([Id]),
	CONSTRAINT [FK_Forums_ForumGroupId] FOREIGN KEY([ForumGroupId]) REFERENCES ForumGroups([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [ForumSettings] 
(
	[RelatedId] GUID NOT NULL,
	[Enabled] BIT NOT NULL,
	[Visible] BIT NOT NULL,
	[StatusListId] GUID NULL,
	[IsModerated] BIT NOT NULL,
	[AllowAttachments] BIT NOT NULL,
	[AllowSearchIndexing] BIT NOT NULL,
	[SubscriptionMailTemplateId] GUID NULL,
	[ModerationRequiredMailTemplateId] GUID NULL,
	[ModerationApprovedMailTemplateId] GUID NULL,
	[ModerationRejectedMailTemplateId] GUID NULL,
	[AttachmentsFolderId] GUID NULL,	
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ForumSettings] PRIMARY KEY([RelatedId]),
	CONSTRAINT [FK_ForumSettings_StatusListId] FOREIGN KEY([StatusListId]) REFERENCES Lists([Id]) ON DELETE SET NULL	
);
GO

CREATE TABLE IF NOT EXISTS [ForumPosts] 
(
	[Id] GUID NOT NULL,
	[ForumId] GUID NOT NULL,
	[Subject] TEXT NOT NULL,
	[Body] TEXT NOT NULL,
	[IsLocked] BIT NOT NULL,
	[IsPinned] BIT NOT NULL,
	[IsApproved] BIT NOT NULL,
	[StatusId] GUID NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ForumPosts] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ForumPosts_ForumId] FOREIGN KEY([ForumId]) REFERENCES Forums([Id]),
	CONSTRAINT [FK_ForumPosts_StatusId] FOREIGN KEY([StatusId]) REFERENCES ListItems([Id]) ON DELETE SET NULL
);
GO

CREATE TABLE IF NOT EXISTS [ForumReplies] 
(
	[Id] GUID NOT NULL,
	[ForumPostId] GUID NOT NULL,
	[ReplyToId] GUID NULL,
	[Body] TEXT NOT NULL,
	[IsApproved] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ForumReplies] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ForumReplies_ReplyToId] FOREIGN KEY([ReplyToId]) REFERENCES ForumReplies([Id]),
	CONSTRAINT [FK_ForumReplies_ForumPostId] FOREIGN KEY([ForumPostId]) REFERENCES ForumPosts([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [ForumAttachments] 
(
	[Id] GUID NOT NULL,
	[ForumPostId] GUID NOT NULL,
	[ForumReplyId] GUID NULL,
	[FileId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ForumAttachments] PRIMARY KEY([Id]),
	CONSTRAINT [FK_ForumAttachments_ForumPostId] FOREIGN KEY([ForumPostId]) REFERENCES ForumPosts([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ForumAttachments_ForumReplyId] FOREIGN KEY([ForumReplyId]) REFERENCES ForumReplies([Id])
);
GO

CREATE TABLE IF NOT EXISTS [ForumSubscriptions] 
(
	[UserId] GUID NOT NULL,
	[ForumId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ForumSubscriptions] PRIMARY KEY([UserId],[ForumId]),
	CONSTRAINT [FK_ForumSubscriptions_ForumId] FOREIGN KEY([ForumId]) REFERENCES Forums([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ForumSubscriptions_UserId] FOREIGN KEY([UserId]) REFERENCES Users([Id]) ON DELETE CASCADE
);
GO

CREATE TABLE IF NOT EXISTS [ForumPostTracking] 
(
	[UserId] GUID NOT NULL,
	[ForumPostId] GUID NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] GUID NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] GUID NULL,
	CONSTRAINT [PK_ForumPostTracking] PRIMARY KEY([UserId],[ForumPostId]),
	CONSTRAINT [FK_ForumPostTracking_ForumPostId] FOREIGN KEY([ForumPostId]) REFERENCES ForumPosts([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ForumPostTracking_UserId] FOREIGN KEY([UserId]) REFERENCES Users([Id]) ON DELETE CASCADE
);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('91E80ABF-29BD-4526-B054-8164080321A4', NULL, 'urn:nucleus:entities:forum/permissiontype/view', 'View', 0);
GO
	
INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('3A81E8B0-7018-475D-ABDB-07A788468F78', NULL, 'urn:nucleus:entities:forum/permissiontype/createpost', 'Post', 1);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('3A9CDB24-A956-4D07-B79A-4380005E0E2C', NULL, 'urn:nucleus:entities:forum/permissiontype/reply', 'Reply', 2);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('8974847C-DFFC-411E-8699-D5B6965FDD8D', NULL, 'urn:nucleus:entities:forum/permissiontype/delete', 'Delete', 3);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('38D9B880-6977-4412-8CF5-B6A883161BDA', NULL, 'urn:nucleus:entities:forum/permissiontype/lock', 'Lock', 4);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('33E7380D-6BFC-4791-A675-CDFB60921054', NULL, 'urn:nucleus:entities:forum/permissiontype/attach', 'Attach', 5);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('29455DD9-EADC-42D5-A2CD-5F2F5932D4D3', NULL, 'urn:nucleus:entities:forum/permissiontype/subscribe', 'Subscribe', 6);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('EEA8A7FD-55A4-4761-A0C2-415412FF73E0', NULL, 'urn:nucleus:entities:forum/permissiontype/pin', 'Pin', 7);
GO

INSERT INTO PermissionTypes
([Id], [ModuleDefinitionId], [Scope], [Name], [SortOrder])
VALUES
('6B464A41-7F8F-4B61-8E96-251001F6A444', NULL, 'urn:nucleus:entities:forum/permissiontype/moderate', 'Moderate', 8);
GO