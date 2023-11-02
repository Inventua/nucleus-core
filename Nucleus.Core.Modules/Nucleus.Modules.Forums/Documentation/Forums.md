## Forums module
The forums module is a message board which allows your user community to participate in discussions, post questions and answers.

## Groups
Forums are always displayed within a forum group, and all forums must belong to a group.  Most settings for forums can be set at the 
group level, but you can choose whether to use group settings or assign settings for each individual forum.

### Group Properties

{.table-25-75}
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

{.table-25-75}
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
Each forum has an url which starts with the url of the page containing the module, plus the "friendly-encoded" name of the forum.  The "friendly encoded" name 
is in lower case, and has non-alphanumeric characters replaced with a dash.

### Status List
If the forum has a status list defined, users with edit rights to a post can select a status for the post.  When you create a list in 
`Manage` \ `Lists`, you can use list item special values (you can use any text values you like, and these are what are shown on screen):

{.table-25-75}
| Special List Item Value     |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| default                     | If a list item with the value `default` is present in the list, new posts have their status set to this value automatically.  |
| rejected                    | If a list item with the value `rejected` is present in the list, rejected posts have their status set to this value automatically..  |

## Forums
Use the `Forums` tab to add forums to a forum group.  The `Forums` tab is not available until after you save a new group for the first time.

### Forum Properties

{.table-25-75}
|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | Forum name, shown on screen in the forums menu and in the administration user interface.  |
| Description                 | Forum description, shown on screen in the forums menu.  |
| Use Group Settings          | Specifies whether to inherit settings from the forum's forum group, or whether to specify settings which are specific for the forum.  |

> The other settings and permissions for forums are the same as for forum groups (see above).

## Notes
- System administrator users do not receive moderation notification emails, because system administrators can't be assigned to a role, and roles are used to control which users can moderate forum posts and replies.  
- Users who create a reply or post are automatically subscribed to the post.
- Users who are subscribed to a forum or post do not receive a subscription notification for their own messages.
- The forums module includes a content meta-data (search content) provider, which provides content from forum posts for the search engine.

## Email Templates
The forums module can send email notifications for:
- Subscriptions, to inform subscribed users of new posts and replies.
- Moderators, to inform moderators that new posts or replies require approval.
- Users who have created a new post or reply, to inform them that their message was approved or rejected.

Email templates are managed by system administrators or site administrators in the `Manage`/`Mail Templates` control panel.  Refer 
[here](/manage/mail-templates/) for more information on mail templates.  You must manually create mail templates for the forums to use.  After 
you create the mail template(s), assign them to the forum in the forum group or forum configuration.

### Scheduled Task
You must set up a scheduled task to send forum notifications.  Use the `Settings`/`Scheduler` control panel to create a new scheduled task.  Refer 
[here](/manage/task-scheduler/) for more information on scheduled tasks.

{.table-25-75}
|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | You can name your scheduled task anything you want.  A good example would be 'Send Forum Emails'.  |
| Type Name                   | The type name is selected from a drop-down list.  Select `Nucleus.Modules.Forums.ScheduledTasks.SendForumsEmailsScheduledTask,Nucleus.Modules.Forums`.  |
| Enabled                     | Set the task to enabled. |
| Inverval                    | Select the interval and interval type.  This is how often a notification will be sent to users.  The forums subscription email is designed to be a summary of all activity in subscribed forums and posts (per forum module instance), sent as a single email, so you would generally want to set this to about 1 day. |
| Instance Type               | Select 'Per Instance'. |
| Keep History                | You can set this to whatever setting you require. |

### Forum Email Data
Email templates for forum messages can use the following data objects:

{.table-25-75}
| Forum Data Model            | Contains properties which describe new forum activity.                               |
|-----------------------------|--------------------------------------------------------------------------------------|
| Site                        | Site information.  |
| Page                        | Information on the page which contains the forums module.  |
| Forums                      | A list of forums with new activity.  |
| User                        | Iformation on the user who is receiving the email.  |
| Summary                     | A comma-delimited list of forum names which have new activity.  |

{.table-25-75}
| Site                        | Information on the site which has forums with new activity.                          |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | Site name.  |

You can use the site as a parameter for the AbsoluteUrl() extension to create a link to the site.  You must provide a `UseSsl` value 
of true (use https) or false (use http).  You should only use `false` if your site does not support SSL.

```
<a href="@Model.Site.AbsoluteUrl(true)">@post.Subject</a>
```

{.table-25-75}
| Page                        | Information on the forum page.                                                       |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | Page name.  |
| 

You can use the page as a parameter for the AbsoluteUrl() extension to create a link to the page.  

```
<a href="@Model.Site.AbsoluteUrl(@Model.Page, true)">Click here to visit the forums page</a>
```

{.table-25-75}
| Forums                      | A list of forums with new activity.                                                  |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | Forum name.  |
| Description                 | Forum description.  |
| Posts                       | A list of posts with new activity.  |
| Replies                     | Specifies whether to inherit settings from the forum's forum group, or whether to specify settings which are specific for the forum.  |

You can use the forum as a parameter for the AbsoluteUrl() extension to create a link to the forum.  

```
<a href="@Model.Site.AbsoluteUrl(@Model.Page, @forum.Name.FriendlyEncode(), true)">@forum.Name</a>
```

{.table-25-75}
| Post                        | An item in each forum's `Posts` list, representing a post with activity.             | 
|-----------------------------|--------------------------------------------------------------------------------------|
| Subject                     | The post subject.  |
| Body                        | The post body.  |
| PostedBy                    | A [user](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Models.User/) object representing the user who posted the forum post.  |

You can use the post as a parameter for the AbsoluteUrl() extension to create a link to the forum.  

```
<a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@post.Id}", true)">@post.Subject</a>
```

{.table-25-75}
| Reply                       | An item in each forum's `Replies` list, representing a new post reply.               |
|-----------------------------|--------------------------------------------------------------------------------------|
| Body                        | The reply body.  |
| PostedBy                    | A [user](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Models.User/) object representing the user who posted the forum reply.  |

```
<a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@reply.Post.Id}", true)">@reply.Post.Subject</a>
```

## Mail Template Samples

### Subscriber Notification
```
@Model.User?.UserName,

<p>New messages have been posted to forums or forum posts that you are tracking at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>: </p>
<br />
@foreach (var forum in Model.Forums)
{
  <h2><a href="@Model.Site.AbsoluteUrl(@Model.Page, @forum.Name.FriendlyEncode(), true)">@forum.Name</a></h2>

  <table>
    <tr>
      <th>Subject</th>
      <th>Author</th>
      <th>Date/Time</th>
    </tr>

    @if (forum.Posts?.Any() == true)
    {
      <tr>
        <th colspan="3"><h3>New Posts</h3></th>
      </tr>
      @foreach (var post in forum.Posts)
      {
        <tr>
          <td><a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@post.Id}", true)">@post.Subject</a></td>
          <td>@post.PostedBy?.UserName</td>
          <td>@post?.DateAdded</td>
        </tr>
      }
    }
    @if (forum.Replies?.Any() == true)
    {
      <tr>
        <th colspan="3"><h3>New Replies</h3></th>
      </tr>
      @foreach (var reply in forum.Replies)
      {
        <tr>
          <td><a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@reply.Post.Id}", true)">@reply.Post.Subject</a></td>
          <td>@reply.PostedBy?.UserName</td>
          <td>@reply?.DateAdded</td>
        </tr>
        <tr colspan="3">
          <td>@reply.Body</td>
        </tr>
      }
    }
  </table>
}
<br />
<div>
  You were sent this email because you are subscribed to a forum or post at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>, or are the
  original poster of this forum message.  <a href="@Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true)">Click here</a>
  to manage your subscriptions, or browse to @Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true).
</div>
```

### Moderation
```
@Model.User.UserName,

<p>New messages have been posted that require moderation in the forums at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>: </p>

@foreach (var forum in Model.Forums)
{
  <a href="@Model.Site.AbsoluteUrl(@Model.Page, @forum.Name.FriendlyEncode(), true)">@forum.Name</a>

  <table>
    <tr>
      <th>Subject</th>
      <th>Author</th>
      <th>Date</th>
    </tr>
    <tr>
      <td colspan="3"><h2>Posts</h2></td>
    </tr>
    @foreach (var post in forum.Posts)
    {
      <tr>
        <td><a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@forum.Name.FriendlyEncode()}/{@post.Id}", true)">@post.Subject</a></td>
        <td>@post.PostedBy.UserName</td>
        <td>@post.DateAdded</td>
      </tr>
    }
    @if (((IEnumerable<object>)forum.Replies).Any())
    {
      <tr>
        <td colspan="3"><h2>Replies</h2></td>
      </tr>
      @foreach (var reply in forum.Replies)
      {
        <tr>
          <td><a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@reply.Post.Id}", true)">@reply.Post.Subject</a></td>
          <td>@reply.PostedBy.UserName</td>
          <td>@reply.DateAdded</td>
        </tr>
      }
    }
  </table>
}
```

### Post Approved
```
@Model.User.UserName,

<p>One or more of your forum posts have been approved at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>: </p>

@foreach (var forum in Model.Forums)
{
  <a href="@Model.Site.AbsoluteUrl(@Model.Page, @forum.Name.FriendlyEncode(), true)">@forum.Name</a>

  <table>
    <tr>
      <th>Subject</th>
      <th>Author</th>
      <th>Date</th>
    </tr>
    <tr>
      <td colspan="3"><h2>Posts</h2></td>
    </tr>
    @foreach (var post in forum.Posts)
    {
      <tr>
        <td><a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@forum.Name.FriendlyEncode()}/{@post.Id}", true)">@post.Subject</a></td>
        <td>@post.PostedBy.UserName</td>
        <td>@post.DateAdded</td>
      </tr>
    }
    @if (((IEnumerable<object>)forum.Replies).Any())
    {
      <tr>
        <td colspan="3"><h2>Replies</h2></td>
      </tr>
      @foreach (var reply in forum.Replies)
      {
        <tr>
          <td><a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@reply.Post.Id}", true)">@reply.Post.Subject</a></td>
          <td>@reply.PostedBy.UserName</td>
          <td>@reply.DateAdded</td>
        </tr>
      }
    }
  </table>
}
```

### Post Rejected
```
@Model.User.UserName,

<p>One or more of your forum posts have been rejected <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>: </p>

@foreach (var forum in Model.Forums)
{
  <a href="@Model.Site.AbsoluteUrl(@Model.Page, @forum.Name.FriendlyEncode(), true)">@forum.Name</a>

  <table>
    <tr>
      <th>Subject</th>
      <th>Author</th>
      <th>Date</th>
    </tr>
    <tr>
      <td colspan="3"><h2>Posts</h2></td>
    </tr>
    @foreach (var post in forum.Posts)
    {
      <tr>
        <td><a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@forum.Name.FriendlyEncode()}/{@post.Id}", true)">@post.Subject</a></td>
        <td>@post.PostedBy.UserName</td>
        <td>@post.DateAdded</td>
      </tr>
    }
    @if (((IEnumerable<object>)forum.Replies).Any())
    {
      <tr>
        <td colspan="3"><h2>Replies</h2></td>
      </tr>
      @foreach (var reply in forum.Replies)
      {
        <tr>
          <td><a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@reply.Post.Id}", true)">@reply.Post.Subject</a></td>
          <td>@reply.PostedBy.UserName</td>
          <td>@reply.DateAdded</td>
        </tr>
      }
    }
  </table>
}
```
