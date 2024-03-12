To auto-generate the manifest classes from package.xsd:

1.  Run the developer command prompt (Visual Studio -> Tools -> Command Line -> Developer Command Prompt)

2.  Navigate to C:\Development\Inventua\Nucleus\Source Code\Nucleus.Web

3.  xsd package.xsd /c /f /n:Nucleus.Abstractions.Models.Extensions /out:"C:\Development\Inventua\Nucleus\Source Code\Nucleus.Abstractions\Models\Extensions"