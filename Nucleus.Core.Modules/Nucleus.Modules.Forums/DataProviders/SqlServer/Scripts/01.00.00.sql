IF OBJECT_ID(N'ForumGroups', N'U') IS NULL
BEGIN
CREATE TABLE [ForumGroups] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ModuleId] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(255) NOT NULL,
	[SortOrder] INT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumGroups_ModuleId] FOREIGN KEY([ModuleId]) REFERENCES PageModules([Id])
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumGroups] ON [ForumGroups]([Id]);
GO

CREATE UNIQUE INDEX [IX_ForumGroups_Name] ON [ForumGroups]([Name]);
GO

IF OBJECT_ID(N'Forums', N'U') IS NULL
BEGIN
CREATE TABLE [Forums] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ForumGroupId] UNIQUEIDENTIFIER NOT NULL,
	[Name] NVARCHAR(255) NOT NULL,
	[Description] NVARCHAR(255) NULL,
	[SortOrder] INT NOT NULL,
	[UseGroupSettings] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_Forums_ForumGroupId] FOREIGN KEY([ForumGroupId]) REFERENCES ForumGroups([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_Forums] ON [Forums]([Id]);
GO

CREATE UNIQUE INDEX [IX_Forums_Name] ON [Forums]([Name]);
GO

IF OBJECT_ID(N'ForumSettings', N'U') IS NULL
BEGIN
CREATE TABLE [ForumSettings] 
(
	[RelatedId] UNIQUEIDENTIFIER NOT NULL,
	[Enabled] BIT NOT NULL,
	[Visible] BIT NOT NULL,
	[StatusListId] UNIQUEIDENTIFIER NULL,
	[IsModerated] BIT NOT NULL,
	[AllowAttachments] BIT NOT NULL,
	[AllowSearchIndexing] BIT NOT NULL,
	[SubscriptionMailTemplateId] UNIQUEIDENTIFIER NULL,
	[ModerationRequiredMailTemplateId] UNIQUEIDENTIFIER NULL,
	[ModerationApprovedMailTemplateId] UNIQUEIDENTIFIER NULL,
	[ModerationRejectedMailTemplateId] UNIQUEIDENTIFIER NULL,
	[AttachmentsFolderId] UNIQUEIDENTIFIER NULL,	
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumSettings_StatusListId] FOREIGN KEY([StatusListId]) REFERENCES Lists([Id]) ON DELETE SET NULL	
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumSettings] ON [ForumSettings]([RelatedId]);
GO

IF OBJECT_ID(N'ForumPosts', N'U') IS NULL
BEGIN
CREATE TABLE [ForumPosts] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ForumId] UNIQUEIDENTIFIER NOT NULL,
	[Subject] NVARCHAR(255) NOT NULL,
	[Body] NVARCHAR(255) NOT NULL,
	[IsLocked] BIT NOT NULL,
	[IsPinned] BIT NOT NULL,
	[IsApproved] BIT NOT NULL,
	[StatusId] UNIQUEIDENTIFIER NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumPosts_ForumId] FOREIGN KEY([ForumId]) REFERENCES Forums([Id]),
	CONSTRAINT [FK_ForumPosts_StatusId] FOREIGN KEY([StatusId]) REFERENCES ListItems([Id]) ON DELETE SET NULL
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumPosts] ON [ForumPosts]([Id]);
GO

IF OBJECT_ID(N'ForumReplies', N'U') IS NULL
BEGIN
CREATE TABLE [ForumReplies] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ForumPostId] UNIQUEIDENTIFIER NOT NULL,
	[ReplyToId] UNIQUEIDENTIFIER NULL,
	[Body] NVARCHAR(255) NOT NULL,
	[IsApproved] BIT NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumReplies_ReplyToId] FOREIGN KEY([ReplyToId]) REFERENCES ForumReplies([Id]),
	CONSTRAINT [FK_ForumReplies_ForumPostId] FOREIGN KEY([ForumPostId]) REFERENCES ForumPosts([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumReplies] ON [ForumReplies]([Id]);
GO

IF OBJECT_ID(N'ForumAttachments', N'U') IS NULL
BEGIN
CREATE TABLE [ForumAttachments] 
(
	[Id] UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
	[ForumPostId] UNIQUEIDENTIFIER NOT NULL,
	[ForumReplyId] UNIQUEIDENTIFIER NULL,
	[FileId] UNIQUEIDENTIFIER NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumAttachments_ForumPostId] FOREIGN KEY([ForumPostId]) REFERENCES ForumPosts([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ForumAttachments_ForumReplyId] FOREIGN KEY([ForumReplyId]) REFERENCES ForumReplies([Id])
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumAttachments] ON [ForumAttachments]([Id]);
GO

IF OBJECT_ID(N'ForumSubscriptions', N'U') IS NULL
BEGIN
CREATE TABLE [ForumSubscriptions] 
(
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[ForumId] UNIQUEIDENTIFIER NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumSubscriptions_ForumId] FOREIGN KEY([ForumId]) REFERENCES Forums([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ForumSubscriptions_UserId] FOREIGN KEY([UserId]) REFERENCES Users([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumSubscriptions] ON [ForumSubscriptions]([UserId],[ForumId]);
GO

IF OBJECT_ID(N'ForumPostTracking', N'U') IS NULL
BEGIN
CREATE TABLE [ForumPostTracking] 
(
	[UserId] UNIQUEIDENTIFIER NOT NULL,
	[ForumPostId] UNIQUEIDENTIFIER NOT NULL,
	[DateAdded] DATETIME NULL,
	[AddedBy] UNIQUEIDENTIFIER NULL,
	[DateChanged] DATETIME NULL,
	[ChangedBy] UNIQUEIDENTIFIER NULL,
	CONSTRAINT [FK_ForumPostTracking_ForumPostId] FOREIGN KEY([ForumPostId]) REFERENCES ForumPosts([Id]) ON DELETE CASCADE,
	CONSTRAINT [FK_ForumPostTracking_UserId] FOREIGN KEY([UserId]) REFERENCES Users([Id]) ON DELETE CASCADE
);
END;
GO

CREATE UNIQUE CLUSTERED INDEX [PK_ForumPostTracking] ON [ForumPostTracking]([UserId],[ForumPostId]);
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