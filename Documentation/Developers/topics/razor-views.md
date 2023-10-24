Nucleus modules have a ==Viewer== view and a ==Settings== view.  Control panel extensions have a ==Settings== view only.  

The ==Viewer== view is displayed to end users when they visit a page which contains your module.  The ==Settings== view is displayed 
to users with edit rights to your module when they click Edit Content/Settings or click Edit in the Pages Editor Modules tab or when 
they access your control panel extension from the Manage or Settings control panel.

Your views can make use of [Nucleus view features](/api-documentation/Nucleus.ViewFeatures.xml/#asm-Nucleus-ViewFeatures) (tag helpers, 
html helpers and url helpers) to make it easier to interact with Nucleus.

All parts of Nucleus (including extensions) can use jQuery and Bootstrap.  You can use other UI frameworks too, as long as they are 
compatible with jQuery and Bootstrap.  You will need to include third-party components in your install set and add code to load the 
relevant Javascript and/or CSS files.

## Settings Views
Settings pages are displayed within the Nucleus administrative user interface.  They are Razor views which render partial HTML.

> This example is from the ==Accept Terms== module.  Some elements have been removed for brevity.

```
@model Nucleus.Modules.AcceptTerms.ViewModels.Settings
@Html.AddStyle("~!/../settings.css")
@using (Html.BeginNucleusForm("Settings", "AcceptTerms", FormMethod.Post, new { @enctype = "multipart/form-data" }))
{
  <fieldset role="group" aria-labelledby="heading">
    <h2 class="nucleus-control-panel-heading">Settings</h2>
    <SettingsControl caption="Title" helptext="Enter the title for the user agreement.">
      @Html.TextBoxFor(model => model.Title, new {  })
    </SettingsControl>

    <div class="nucleus-flex-fields">
      <SettingsControl caption="Accept Button Text" helptext="Enter the text for the accept button.">
        @Html.TextBoxFor(model => model.AcceptText, new { })
      </SettingsControl>

      <SettingsControl caption="Cancel Button Text" helptext="Enter the text for the cancel button.">
        @Html.TextBoxFor(model => model.CancelText, new { })
      </SettingsControl>
    </div>

    <div class="nucleus-form-buttonrow">
      @Html.SubmitButton("", "Save Settings", 
        @Url.NucleusAction("SaveSettings", "AcceptTermsAdmin", "AcceptTerms"), 
        new { @class = "btn btn-primary" })
    </div>
  </fieldset>
}
```

### Adding stylesheets
Use the [AddStyle](/api-documentation/Nucleus.ViewFeatures.HtmlHelpers.AddStyleHtmlHelper/#mnu-Nucleus-ViewFeatures-HtmlHelpers-AddStyleHtmlHelper) 
Html helper to add styles to your module output.  By using `AddStyle`, Nucleus can add your styles in the Html `<head>` element, 
can merge style sheets, and will use the minified versions of your style sheets when configured to do so.

```
@Html.AddStyle("~!/../settings.css")
```

> The `AddStyle` Html helper recognizes special value prefixes in your script path.  The `~!` prefix represents the currently executing view path, 
and `~#` represents the root path for the currently executing extension (`/extensions/your-extension`).

### Headings
In a ==Settings== view, the contents of any header element with the class `nucleus-modal-caption` are copied to the header of the modal dialog which
is displayed to contain your settings view, and is then removed from the document.
```
<h1 class="nucleus-modal-caption">Documents Settings</h1>
```

### BeginNucleusForm
The BeginNucleusForm Html helper is similar to the asp.net BeginForm Html helper.  It creates a `<form>` tag which routes to an extension controller 
action (that is, the form has a `formaction` attribute which starts with `/extensions/your-extension`).

```
@using (Html.BeginNucleusForm("Settings", "AcceptTerms", FormMethod.Post, new { @enctype = "multipart/form-data" }))
{
  <fieldset role="group" aria-labelledby="heading">
    /* your editor page controls */
  </fieldset>
}
```

> In the example above, we are using the `BeginNucleusForm` overload which does not require an extension name.  The name of the currently executing extension 
is used.  If you want to specify the extension name, there are other `BeginNucleusForm` overloads which allow you to do so.

### SettingsControl tag helper
The `<SettingsControl>` tag helper wraps your control with standardized markup which renders a title and popup help text.  Using the `<SettingsControl>` 
tag helper is a good way to keep your settings view presentation consistent with the rest of the Nucleus administration user interface.

```
<SettingsControl caption="Title" helptext="Enter the title for the user agreement.">
  @Html.TextBoxFor(model => model.Title, new {  })
</SettingsControl>
```

### data-target attribute
The `data-target` attribute on a `<form>`, `<input>`, `<a>` or `<button>` is used to specify that a form POST should be intercepted by the Nucleus 
client-side components (javascript).  The Html response is written to the element specified by the `data-target`, which works like a jQuery selector.  You 
can specify a `data-target` by #id, or using any other jQuery selector syntax.  

> In a ==Settings== view, if you do not specify a `data-target`, your form will use a data-target of `form.parent()`.  

> data-target can understand `.parent()` at the end of your selector.  This is not standard jQuery selector syntax.

### data-useurl attribute
The `data-useurl` attribute on a `<form>`, `<input>`, `<a>` or `<button>` is used to specify an url which is pushed to the browser address bar.  It is used 
in conjunction with the data-target attribute.  The `data-useurl` attribute has a limited use case: Use it when you are populating content within a specific 
element (using data-target) when your module is also capable of displaying the requested content using the specified url.  In order to do this, your module 
controller must use [this.Context.LocalPath](https://www.nucleus-cms.com/api-documentation/Nucleus.Abstractions.xml/Nucleus.Abstractions.Models.LocalPath/) to 
parse additional elements from the path (url) to navigate within your module.

### NucleusAction url helper
In your submit button(s), you can use `@Url.NucleusAction` to specify a `formaction` for your control.  Similar to `BeginNucleusForm`, the `NucleusAction` 
url helper works like the asp.net `Action` helper, but generates a Url which routes to an extension controller action (that is, the url starts with 
`/extensions/your-extension`).

```
<div class="nucleus-form-tools">
  @Html.SubmitButton("", "Save Settings", @Url.NucleusAction("SaveSettings", "Documents", "Documents"), new { })
</div>
```

> The example above uses the SubmitButton Html helper, which renders a `<button type='submit' class='btn btn-primary' formaction='value'/>` element.  If you 
prefer not to use the SubmitButton Html helper you could achieve the same result with `<button type='submit' class='btn btn-primary' formaction='@Url.NucleusAction("SaveSettings", "Documents", "Documents")'/>`

## Viewer Views
==Viewer== views are less constrained than ==Settings== views.  You can still use Nucleus Tag Helpers, Html Helpers and Url helpers, but depending on your
module's functionality you may not need to.