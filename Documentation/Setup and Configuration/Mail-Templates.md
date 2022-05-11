## Mail Templates
To manage mail templates, after logging in as a system administrator or site administrator, click the `Manage` button to display the Management control panel, then click `Mail Templates`. 

![Mail Templates](Mail-Templates.png)

## Properties
|                  |                                                                                      |
|------------------|--------------------------------------------------------------------------------------|
| Name             | A name for your template.  This is for your reference only, it is displayed in the control panel and anywhere that an email template can be selected.  |
| Subject          | Specifies the subject for emails generated using the template.  The subject can contain tokens and script (see below).  |
| Body             | Specifies the body for emails generated using the template.  The body can contain tokens and script (see below).  |

## Scripting
The email template parser generates emails by combining your template with data passed by the component which uses the template (a module or other extension, or Nucleus).  Email 
templates can contain simple token replacements or Razor code, or a combination of both.  The data objects which are passed to the email template parser are different depending on 
which component calls it to generate emails and are normally described in the documentation for the Nucleus extension.

The subject will normally contain static text and simple tokens.  The mail body will normally include HTML elements and simple tokens, or Razor code.

### Simple Tokens
Simple tokens are included in your subject or body in the form:

```
(~property)
(~object.property)
(~object.property.property)
```

The start characters `(~` and end character `)` are used to identify tokens.  The entire token is replaced with the value of the object and property.  If the object and/or property 
does not exist, the token is replaced with an empty string.  All email templates can use the "date" and "time" tokens, and all other available values are submitted by the component which 
calls the email template parser.  Object and property names are not case-sensitive in the simple token parser.

#### Example
```
You requested a user name reminder for (~Site.Name).  Your user name is (~User.UserName).
```

### Collections
The simple token parser can repeat a section if the data is represented as a collection type by using the `[~collection_object(content)]` syntax.

#### Example
```
[posts(
<tr>
  <td>(~post.Subject)</a></td>
  <td>(~post.PostedBy.UserName)</td>
  <td>(~post.DateAdded)</td>
</tr>
)]
```

In the example above, the "posts" object is a collection of post objects.  The content is repeated for each item ("post") in the collection.

### Razor Templates
The email template parser can parse Razor code.  Razor templates are used to implement more complex logic when generating emails.  Razor templates can also 
make use of the extension methods implemented in the Nucleus.Extensions assembly, which makes it easier to generate site and page Urls.

#### Example
```
You requested a user name reminder for <a href="@Model.Site.AbsoluteUrl(true)">@Model.Site.Name<a>.  Your user name is (~User.UserName).
```

Razor templates are more powerful than simple tokens, but are more difficult to prepare.  Razor templates must conform to Razor syntax rules, object names and 
properties are case-sensitive, and if you refer to an object or property which does not exist or the template has any other errors, the email template parser will generate 
an error, and the email will not be sent.  Errors are written to the Nucleus logs.

Unlike the .net core asp.net razor engine, the Razor template parser does not allow @model, @using or other directives.  `Using` directives for the `System`, 
`System.Collections.Generic`, `System.Linq`, `System.Text`, `Nucleus.Extensions`, `Nucleus.Abstractions`, and `Nucleus.Abstractions.Models` namespaces are automatically 
applied.


