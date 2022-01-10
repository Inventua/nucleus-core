To auto-generate the manifest classes from package.xsd:

1.  Check out package.cs

2.  Run the developer command prompt (Visual Studio -> Tools -> Command Line -> Developer Command Prompt)

3.  Navigate to C:\Development\Inventua\Nucleus\Source Code\Nucleus.Web

4.  xsd package.xsd /c /f /n:Nucleus.Abstractions.Models.Manifest /out:"C:\Development\Inventua\Nucleus\Source Code\Nucleus.Abstractions\Models\Manifest"