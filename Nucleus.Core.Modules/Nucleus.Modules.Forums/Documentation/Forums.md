## Forums module
The forums module is a message board which allows your user community to participate in discussions, share information, ask questions and post answers. 

## Groups
Forums are always displayed within a forum group, and all forums must belong to a group.  Most settings for forums can be set at the 
group level, but you can choose whether to use group settings. or assign settings for each individual forum.

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
| Subscription mail template  | Specifies a mail template to use when sending notifications for a single activity.  |
| Subscription Summary mail template  | Specifies a mail template to use when sending a summary notification which may contain multiple forum activities.  |
| Moderator mail template     | Specifies a mail template to use when sending a notification to moderators when a new post is created.   |
| Post Approved mail template | Specifies a mail template to use when sending a notification to a post author when their post is approved.  |
| Post Rejected mail template | Specifies a mail template to use when sending a notification to a post author when their post is rejected.  |

> If a mail template is not selected, no email notification will be sent for the relevant operation.

### Permissions
Forum groups have a set of permissions which are assigned to roles.  When users are in any role with the specified permission, they will have that 
permission.  Forums can be configured to inherit group settings (including permissions) or can specify their own permissions.

{.table-25-75}
|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| View                        | View the group, its forums and forum posts and replies.  |
| Edit                        | Edit existing posts and replies in the group's forums .  |
| Post                        | Create new posts in the group's forums.  |
| Reply                       | Reply to existing posts in the group's forums.  |
| Delete                      | Delete posts, if the current user created it, or is a system or site administrator.  |
| Lock                        | Lock a post.  Users can't reply to locked posts.  |
| Attach                      | Attach files to a post or reply.  |
| Subscribe                   | Subscribe to a post or forum in order to receive an email notification when a new post or reply is added.  |
| Pin                         | Flag the post so that its sort order is at the start of a forum's posts.  |
| Moderate                    | Moderator users receive moderator email notifications and can approve/reject new posts and replies.  |
| Unmoderated                 | Unmoderated users posts and replies are automatically approved.  |

### Forum and Forum Post Urls
Each forum has an url which starts with the url of the page containing the module, plus the "friendly-encoded" name of the forum.  The "friendly encoded" name 
is in lower case, and has non-alphanumeric characters replaced with a dash.  Click your forum name in the forums list to see the Url.

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
- Users who are subscribed to a forum group, forum or post do not receive a subscription notification for their own messages.
- The forums module includes a content meta-data (search content) provider, which provides content from forum posts for your search engine(s).

## Email Notifications and Templates
A single activity email is a message sent to subscribers to a forum group or forum for an individual new post or reply.  Moderator emails and approval/rejection emails are also single activity emails.

A forum activity summary email is a message sent to subscribers to a forum group or forum that provides an overview of the recent activities within the forum. This summary includes a collection of new 
posts/discussions and replies since the last summary email was sent.

The forums module can send email notifications for:
- Subscriptions, to inform subscribed users of new posts and replies.  Users can select whether they want to receive single activity emails or a forum activity summary email in the `Manage Subscriptions` page.
- Moderators, to inform moderators that new posts or replies require approval.
- Users who have created a new post or reply, to inform them that their message was approved or rejected.

Email templates are managed by system administrators or site administrators in the `Manage`/`Mail Templates` control panel.  Refer 
[here](/manage/mail-templates/) for more information on mail templates.  You must manually create mail templates for the forums to use.  After 
you create the mail template(s), assign them to the forum in the forum group or forum configuration.  There is a set of example templates below.

### Scheduled Task
You must set up two scheduled tasks to send forum notifications, one for single-activity emails, and another for summary emails.  Use the 
`Settings`/`Scheduler` control panel to create a new scheduled task.  Refer [here](/manage/task-scheduler/) for more information on 
scheduled tasks.

{.table-25-75}
|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | You can name your scheduled task anything you want.  A good example would be 'Send Forum Emails'.  |
| Type Name                   | The type name is selected from a drop-down list.  Select `Nucleus Forums: Send Forum Emails (immediate)`.  |
| Enabled                     | Set the task to enabled. |
| Inverval                    | Select the interval and interval type.  This is how often a notification will be sent to users.  For the forums single-activity subscription emails you would generally want to set this to a short inverval, like 5 minutes. |
| Instance Type               | Select 'Per Instance'. |
| Keep History                | You can set this to whatever setting you require. |

{.table-25-75}
|                             |                                                                                      |
|-----------------------------|--------------------------------------------------------------------------------------|
| Name                        | You can name your scheduled task anything you want.  A good example would be 'Send Forum Emails'.  |
| Type Name                   | The type name is selected from a drop-down list.  Select `Nucleus Forums: Send Forum Emails (summary)`.  |
| Enabled                     | Set the task to enabled. |
| Inverval                    | Select the interval and interval type.  This is how often a notification will be sent to users.  The forums summary subscription email is designed to be a summary of all activity in subscribed forums and posts (per forum module instance), sent as a single email, so you would generally want to set this to about 1 day. |
| Instance Type               | Select 'Per Instance'. |
| Keep History                | You can set this to whatever setting you require. |

### Forum Email Data
Email templates for forum messages can use the following data objects:

#### Single Activity
{.table-25-75}
| Subscription-Single         | Contains properties which describe new forum activity.                               |
|-----------------------------|--------------------------------------------------------------------------------------|
| Site                        | Site information.  |
| Page                        | Information on the page which contains the forums module.  |
| Forum                       | The forum with new activity.  |
| User                        | Information on the user who is receiving the email.  |
| Post                        | Information on the post with new activity.  |
| Reply                       | Information on the reply, if the activity is a post reply.  This value may be null for new posts.  |

#### Activity Summary
{.table-25-75}
| Subscription-Summary        | Contains properties which describe new forum activity.                               |
|-----------------------------|--------------------------------------------------------------------------------------|
| Site                        | Site information.  |
| Page                        | Information on the page which contains the forums module.  |
| Forums                      | A list of forums with new activity.  Each forum object contains a list of posts and replies with new activity. |
| User                        | Information on the user who is receiving the email.  |
| ForumNames                  | A comma-delimited list of forum names which are included in the email.  |

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
| Post                        | Represents a post with activity.             | 
|-----------------------------|--------------------------------------------------------------------------------------|
| Subject                     | The post subject.  |
| Body                        | The post body.  |
| PostedBy                    | A [user](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Models.User/) object representing the user who posted the forum post.  |

You can use the post as a parameter for the AbsoluteUrl() extension to create a link to the forum.  

```
<a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@post.Id}", true)">@post.Subject</a>
```

{.table-25-75}
| Reply                       | Represents a new post reply.               |
|-----------------------------|--------------------------------------------------------------------------------------|
| Body                        | The reply body.  |
| PostedBy                    | A [user](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Models.User/) object representing the user who posted the forum reply.  |

```
<a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@forum.Name.FriendlyEncode()}/{@reply.Post.Id}", true)">@reply.Post.Subject</a>
```

## Mail Template Samples

### Subscriber Notification - Single 
```
@Model.User?.UserName,

<p>
  At @(Model.Reply != null ? Model.Reply.DateAdded : Model.Post.DateAdded) a new message was posted to a forum or forum post that you are tracking at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>: 
</p>
<br />
<strong>
  <a href="@Model.Site.AbsoluteUrl(@Model.Page, @Model.Forum.Name.FriendlyEncode(), true)">
    @Model.Forum.Name
  </a>
</strong>

@if (Model.Post != null)
{
  <h3>
    <text>@Model.Post.Subject</text>, posted by @(Model.Reply != null ? Model.Reply.PostedBy?.UserName : Model.Post.PostedBy?.UserName)
  </h3>
}

@if (Model.Reply != null)
{
  <div>
    @Model.Reply.Body
  </div>
}

<div>
  To view the complete thread and reply, please visit:
  @if (Model.Reply == null)
  {
    <a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{Model.Forum.Name.FriendlyEncode()}/{@Model.Post.Id}", true)">
      @Model.Site.AbsoluteUrl(Model.Page, $"{Model.Forum.Name.FriendlyEncode()}/{Model.Post.Id}", true)
    </a>
  }
  else
  {
    <a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{Model.Forum.Name.FriendlyEncode()}/{@Model.Reply.Post.Id}", true)">
      @Model.Site.AbsoluteUrl(Model.Page, $"{Model.Forum.Name.FriendlyEncode()}/{Model.Reply.Post.Id}#{Model.Reply.Id}", true)
    </a>
  }
</div>

<br />
<div>
  You were sent this email because you are subscribed to a forum or post at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>, 
  or are the original poster of this forum message.  
  <a href="@Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true)">Click here</a>to manage your subscriptions, 
  or browse to @Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true).
</div>
```

### Subscriber Notification - Summary
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

### Moderation Required
```
@Model.User.UserName,

@if (@Model.Reply == null)
{
  <p>A forum post at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a> requires approval.</p>
  <p>@Model.Post.Body</p>
  <p>
    To view the complete thread and reply, please visit:
    <a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@Model.Forum.Name.FriendlyEncode()}/{@Model.Post.Id}", true)">
      @Model.Post.Subject
    </a>
  </p>
}
else
{
  <p>A forum post reply at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a> requires approval.</p>
  <p>@Model.Reply.Body</p>
  <p>
    To view the complete thread and reply, please visit:
    <a href="@Model.Site.AbsoluteUrl(@Model.Page, $"{@Model.Forum.Name.FriendlyEncode()}/{@Model.Reply.Post.Id}", true)/#_@Model.Reply.Id">
      @Model.Reply.Post.Subject
    </a>
  </p>
}

<br />
<div>
  You were sent this email because you are a form moderator at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>.
</div>
```

### Post/Reply Approved
```
@Model.User.UserName,

@if (@Model.Reply == null)
{
  <p>Your forum post at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a> was approved : </p>
  <p>@Model.Post.Body</p>
  <p>
    To view the complete thread and reply, please visit: 
    <a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@Model.Forum.Name.FriendlyEncode()}/{@Model.Post.Id}", true)">
      @Model.Post.Subject
    </a>
  </p>
}
else
{
  <p>Your forum reply at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a> was approved: </p>
  <p>@Model.Reply.Body</p>
  <p>
    To view the complete thread and reply, please visit:
    <a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@Model.Forum.Name.FriendlyEncode()}/{@Model.Reply.Post.Id}", true)/#_@Model.Reply.Id">
      @Model.Reply.Post.Subject
    </a>
  </p>
}

<br />
<div>
  You were sent this email because you posted to a forum at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>.  
  <a href="@Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true)">Click here</a> 
  to manage your subscriptions, or browse to @Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true).
</div>
```

### Post Rejected
```
@Model.User.UserName,

@if (@Model.Reply == null)
{
  <p>Your forum post at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a> was rejected: </p>
  <p>@Model.Post.Body</p>
  <p>
    To view the complete thread and reply, please visit:
    <a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@Model.Forum.Name.FriendlyEncode()}/{@Model.Post.Id}", true)">
      @Model.Post.Subject
    </a>
  </p>
}
else
{
  <p>Your forum reply at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a> was rejected: </p>
  <p>@Model.Reply.Body</p>
  <p>
    To view the complete thread and reply, please visit:
    <a href="@Model.Site.AbsoluteUrl( @Model.Page, $"{@Model.Forum.Name.FriendlyEncode()}/{@Model.Reply.Post.Id}", true)/#_@Model.Reply.Id">
      @Model.Reply.Post.Subject
    </a>
  </p>
}

<br />
<div>
  You were sent this email because you posted to a forum at <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name</a>.
  <a href="@Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true)">Click here</a>
  to manage your subscriptions, or browse to @Model.Site.AbsoluteUrl(@Model.Page.ManageSubscriptionsRelativeUrl, true).
</div>
```
