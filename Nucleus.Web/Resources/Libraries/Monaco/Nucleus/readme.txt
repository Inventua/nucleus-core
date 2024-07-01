Monaco Editor Upgrades
----------------------

1. Download the new version from https://microsoft.github.io/monaco-editor/
2. Create a new folder (for the new version) in Nucleus.Web\Resources\Libraries\Monaco\
3. Uncompress the new Monaco version to a temporary folder, then copy the files in the root, and the "min" and "min-maps" folders to the 
   new version folder.
4. In Nucleus.Web.csproj, update the ItemGroup/None and ItemGroup/Content entries for package.json
5. Update the paths in Nucleus.ViewFeatures/HtmlHelpers/AddStyleHtmlHelper.cs and Nucleus.ViewFeatures/HtmlHelpers/AddScriptHtmlHelper.cs
6. Update the path in Resources\Libraries\Monaco\Nucleus\monaco-editor.js 