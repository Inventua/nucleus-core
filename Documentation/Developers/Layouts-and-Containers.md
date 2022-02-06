### Layouts
Layouts control the visual presentation of modules on a page.  Layouts are often called "Themes" or "Skins" in other content management systems.  Layouts 
are Razor pages which use the built-in Nucleus _Layout, define sections of the page (`Panes`) using HTML, and use @await Html.RenderPaneAsync("PANE-NAME")
to render modules which have been assigned to a pane.  An associated CSS file is used to style the page and panes.

> **_Tip:_**  You can name your panes anything you want, but keep in mind that your pane names are how your users identify which area of the page that they
are adding modules to, so try to make them easy to understand within in the context of your layout.

#### Sample Layout
This is a sample of a three row layout, with a top banner, a middle content pane and a fixed height bottom pane.  This layout uses the `Logo` Tag Helper
to render the site logo, the `Account` Tag helper to provide login/logout and account controls, the `menu` Tag helper to draw a menu, the `Breadcrumb`
Html Helper to draw a breadcumb trail to show the user's location within your site, and the `Terms` Tag Helper to show your site's terms of use.  

Also note the use of the Bootstrap classes `navbar ms-auto flex-row justify-content-end` for the `account` tag.  You don't have to use them, but 
'Bootstrap classes are always available in Nucleus.

Most Nucleus view feature extensions that are indended for use in layouts are provided as both Tag Helpers and Html Helpers, so you can use whichever you 
prefer.

`your-layout.cshtml`
```
@addTagHelper "*, Nucleus.ViewFeatures"
@using Nucleus.ViewFeatures.HtmlHelpers
@using Nucleus.ViewFeatures
@using Nucleus.Extensions
@{
  Layout = "_Layout";
}

@Html.AddStyle("~!/your-layout.css")

<div class="LayoutWrapper">
  <div class="BannerPane">
    <div class="d-flex">
      <Logo />
      <account class="AccountControl navbar ms-auto flex-row justify-content-end"></account>
    </div>
    <menu maxLevels="3" menuStyle="RibbonPortrait"></menu>
    @Html.Breadcrumb()
    @await Html.RenderPaneAsync("BannerPane")
  </div>

  <div class="ContentWrapper">
    <div class="ContentPane">
      @await Html.RenderPaneAsync("ContentPane")
    </div>
  </div>

  <div class="BottomPane">
    <terms caption="Terms" />
    @await Html.RenderPaneAsync("BottomPane")
  </div>
</div>
```

`your-layout.css`
```
.LayoutWrapper { display: flex;  flex-direction: column;  height: 100%; }
.BannerPane { display: flex; flex-direction: column; }
.BannerPane .Breadcrumbs { margin-left: 4px; vertical-align: middle; }
.BannerPane .AccountControl { margin-right: 2px; gap: 2px; }
.BannerPane .AccountControl .dropdown-menu { left: auto; margin: auto; }
.BannerPane .AccountControl > * { flex-grow: 0; }
.ContentWrapper { flex-grow: 1; display: flex; overflow: auto; flex-direction: row; }
.ContentPane { display: flex; flex-direction: column;  flex-grow: 1; }
.ContentPane > div { display: flex; flex-direction: column; }
.BottomPane { display: flex; flex-direction: column; background-color: #535353;  padding: 0.5rem; }
.BottomPane * { color: white; }
}
```

> **_Note:_**  You must set the Layout property to `_Layout`.  The built-in _Layout renders a full HTML page including all required elements.

> **_Tip:_**  Use `@Html.AddStyle` to add your CSS stylesheets.  Nucleus automatically detects duplicate CSS stylesheets, and renders `<link>` tags with the appropriate attributes.  Use
the special characters `~!` to represent the path of your layout (cshtml file) - the example above is referencing a default.css file in the same location as the layout.  The AddStyle function is
provided by the `Nucleus.ViewFeatures` namespace.  If you need to add links to javascript files, use `@Html.AddScript`.  You can also use the characters `~#` to represent the root 
path for your extension.

### Containers
Containers control the visual presentation of specific modules that they are assigned to.  Containers are "wrapped" around a module in a layout pane.

#### Sample Container
The simplest form of a container is one which simply renders `@Model.Content`, but your container may add other Html, as well as CSS styles to control prenentation.  You can also access 
the properties of the container **Model**, which is of type `Nucleus.Abstractions.Layout.ContainerContext` to access the `Site`, `Page` and `Module` properties.  The example below uses 
the `Model.Module.Title` property to draw the module's title.

`your-container.cshtml`
```
@model Nucleus.Abstractions.Layout.ContainerContext
@Html.AddStyle("~!/your-container.css")
<div class="layout-yourcontainer">
	@if (!String.IsNullOrEmpty(Model.Module.Title))
	{
		<h2>@Model.Module.Title</h2>
	}
	@Model.Content
</div>
```

`your-container.css`
```
.container-default { padding: 4px; display: flex; flex-direction: column; overflow: auto; }
```

> **_Tip:_**  The example layout and container above use different CSS stylesheets for the layout and container, but it's a good idea to merge small CSS files together and share them between 
your layouts and associated containers, if they are designed to work together, because it reduces the number of HTTP requests that are required to load a page.  




