function WriteHeading {
    param (
        [string]$value = "",
        [ConsoleColor]$ForegroundColor = 'Black',
        [ConsoleColor]$BackgroundColor = 'White'
    )
    Write-Host $value.PadRight($Host.UI.RawUI.WindowSize.Width, " ")  -ForegroundColor $ForegroundColor -BackgroundColor $BackgroundColor
}

WriteHeading ""
WriteHeading " Nucleus 'Publish all'" 
WriteHeading ""

$solutionDirectory = (get-item $PSScriptRoot).parent.fullname
$developerToolsDirectory = ((get-item $PSScriptRoot).parent.parent.fullname + "\Developer Tools")
Write-Host "`nApplication Solution directory: $solutionDirectory" 
Write-Host "Developer Tools directory:      $developerToolsDirectory" 

# build application runtime-specific and portable install sets
Get-ChildItem "Properties\PublishProfiles" -Filter *.pubxml | 
Foreach-Object {
  $profile = $_.Name

  Write-Host "`nPublishing $profile"

  # remove publish folder.  The DeleteExistingFiles property does not work from dotnet publish 
  Remove-Item "Publish" -Force  -Recurse -ErrorAction SilentlyContinue

  # create install/upgrade set.  -warnAsMessage:NETSDK1198 suppresses NETSDK1198 warning for the Nucleus.WebAssembly project, which is caused by command-line parameters being 
  # incorrectly propagated by msbuild to "inner" builds
  $cmd = "dotnet publish -nologo -c Release -verbosity:quiet -warnAsMessage:NETSDK1198 -property:PublishProfile=$profile ""-property:SolutionDir=$solutionDirectory"" Nucleus.Web.csproj"
  Write-Host $cmd -ForegroundColor DarkGray
  Invoke-Expression ". $cmd"
}

# build developer Tools
Write-Host "`nBuilding Developer Tools"
$developerToolsSolution = $developerToolsDirectory + ("\Nucleus.DeveloperTools.sln")
$devToolsCmd = "msbuild -nologo /t:Build /p:Configuration=Release -restore -verbosity:quiet ""$developerToolsSolution"""
Write-Host $devToolsCmd -ForegroundColor DarkGray
Invoke-Expression ". $devToolsCmd"

WriteHeading "`n" white blue
WriteHeading " 'Publish all' operation complete." white blue
WriteHeading "" white blue
Write-Host "`n"