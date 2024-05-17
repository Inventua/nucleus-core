Nucleus.WebAssembly is used to support Balzor WebAssembly components in Nucleus.
- It generates the wasn net core framework in \wwwroot\_framework, which is consumed by Nucleus.Web, which has this
  project as a project reference.
- The code in Program.cs runs client-side, and dynamically loads web assemblies for extensions.

Web Assemblies are added using the Nucleus.ViewFeatures.HtmlHelpers.AddWebAssemblyHtmlHelper helper.  This emits javascript 
which calls Page.AddWasmRuntimeAssemblies (in nucleus-shared.js) to populate the (private) Page._WasmRuntimeAssemblies property, 
which is returned by the Page.WasmRuntimeAssemblies() function.