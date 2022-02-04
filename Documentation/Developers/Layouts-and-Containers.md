### Layouts
Layouts control the visual presentation of modules on a page.  Layouts are often called "Themes" or "Skins" in other content management systems.  Layouts 
are Razor pages which use the built-in Nucleus _Layout, define sections of the page (`Panes`) using HTML, and use @await Html.RenderPaneAsync("PANE-NAME")
to render modules which have been assigned to a pane.  An associated CSS file is used to style the page and panes.

#### Sample Layout
```
@addTagHelper "*, Nucleus.ViewFeatures"
@using Nucleus.ViewFeatures.HtmlHelpers
@using Nucleus.ViewFeatures
@using Nucleus.Extensions
@{
  Layout = "_Layout";
}

@Html.AddStyle("~!/default.css")

<div class="LayoutWrapper">
  <div class="BannerPane">
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

> **_Note:_**  You must set the Layout property to `_Layout`.  The built-in _Layout renders a full HTML page including all required elements.

> **_TIP:_**  Use `@Html.AddStyle` to add your CSS stylesheet.  Nucleus automatically detects duplicate CSS stylesheets, and renders `<link>` tags with the appropriate attributes.  Use
the special characters `~!` to represent the path of your layout - the example above is referencing a default.css file in the same location as the layout.  The AddStyle function is
provided by the `Nucleus.ViewFeatures` namespace.

### Containers
Containers control the visual presentation of specific modules that they are assigned to.  






