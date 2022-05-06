## Forums module
The forums module is a message board which allows your user community to participate in discussions, post questions and answers.

> In version 1, the forums module is incomplete.  The subscription, moderation (all email notifications), post tracking (read/unread) and 
search indexing features are not implemented.

## Groups
Forums are always displayed within a forum group, and all forums must belong to a group.  Most settings for forums can be set at the 
group level, but you can choose whether to use group settings or assign settings for each individual forum.

### Group Properties
|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | The group name is displayed to users in the forum menu and in the administrative user interface.  |
| Enabled                     | Groups can be disabled.  Disabled groups and their forums, posts and replies are visible, but users cannot create new posts.   |
| Visible                     | An invisible group is not displayed in the forums menu.  |
| Allow Attachments           | You can control whether users can attach files to posts.  If you allow attachments, you must specify an attachments folder.  |
| Attachments Folder          | The attachments folder specifies where user attachments are stored.  |
| Moderated                   | Controls whether new posts are visible immediately, or whether a moderator user must approve them first.  |
| Allow Search Indexing       | Controls whether forum posts and replies are added to the search index.  This function is not yet implemented.  |
| Status List                 | If specified, forum posts may be assigned a status.  The status list values may contain special values to help automatic assignment of status values, see below.  |
| Subscription mail template  | Specifies a mail template to use when sending a notification to subscribers when a new post is created.  |
| Moderator mail template     | Specifies a mail template to use when sending a notification to moderators when a new post is created.   |
| Post Approved mail template | Specifies a mail template to use when sending a notification to a post author when their post is approved.  |
| Post Rejected mail template | Specifies a mail template to use when sending a notification to a post author when their post is rejected.  |

> If a mail template is not selected, no email notification will be sent for the relevant operation.

### Permissions
Forum groups have a set of permissions assigned to roles.  When users are in any role with the specified permission, they will have that 
permission.  Forums can be configured to inherit group settings (including permissions) or can specify their own permissions.

|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| View                        | View the group, its forums and forum posts.  |
| Post                        | Create new posts in the group's forums .  |
| Reply                       | Reply to existing posts in the group's forums.  |
| Delete                      | Delete posts, if the current user created it, or is a system or site administrator.  |
| Lock                        | Lock a post.  Users can't reply to locked posts.  |
| Attach                      | Attach files to a post or reply.  |
| Subscribe                   | Subscribe to a post or forum in order to receive an email notification when a new post or reply is added.  |
| Pin                         | Flag the post so that its sort order is at the start of a forum's posts.  |
| Moderate                    | Moderator users receive moderator email notifications and can approve/reject new posts and replies.  |

### Forum and Forum Post Urls
Each forums has an url which starts with the url of the page containing the module, plus the "friendly-encoded" name of the forum.  The "friendly encoded" name 
is in lower case, and has non-alphanumeric characters replaced with a dash.

### Status List
If the forum has a status list defined, users with edit rights to a post can select a status for the post.  When you create a list in 
`Manage`\`Lists`, you can use list item special values (you can use any text values you like, and these are what are shown on screen):

|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| default                     | If a list item with the value `default` is present in the list, new posts have their status set to this value automatically.  |
| rejected                    | If a list item with the value `rejected` is present in the list, rejected posts have their status set to this value automatically..  |

## Forums
Use the `Forums` tab to add forums to a forum group.  The `Forums` tab is not available until after you save a new group for the first time.

### Forum Properties

|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | Forum name, shown on screen in the forums menu and in the administration user interface.  |
| Description                 | Forum description, shown on screen in the forums menu.  |
| Use Group Settings          | Specifies whether to inherit settings from the forum's forum group, or whether to specify settings which are specific for the forum.  |

> The other settings and permissions for forums are the same as for forum groups (see above).

## Notes
System administrator users do not receive moderation notification emails, because system administrators can't be assigned to a role.