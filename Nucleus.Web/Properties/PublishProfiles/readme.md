# Overview

The publish profiles in this folder are used to build install and upgrade sets which target:
- Portable (all platforms) 
- Windows x64
- Linux x64 
- Linux arm64

The platform-specific install and upgrade sets are about 60% of the size of the portable sets.

> Information on the most common runtime IDs is available at:
https://learn.microsoft.com/en-us/dotnet/core/rid-catalog#known-rids

> Microsoft notes that "some properties in the .pubxml file are honored only by Visual Studio and have no effect on dotnet 
publish [when used from the command line]".  See https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish
The affected properties are:
LastUsedBuildConfiguration, Configuration, Platform, LastUsedPlatform, TargetFramework, TargetFrameworks, 
RuntimeIdentifier, RuntimeIdentifiers.  
The effect of this is that the publish profiles only work from within Visual Studio, not from the command line (at least, not
without specifying the --framework and --runtime options). 

---

All of the .pubxml files import _shared-publish-settings.targets and _shared-publish.targets so that they do not repeat the same content.

**NOTE** In the Visual Studio "publish" user interface, the initial "settings" page does not show the correct values because the user
  interface does not process the imports.  However, publishing works fine.

All of the publish profiles (.pubxml files) are configured to write their final output (zip file) to $(SolutionDir)..\Publish\Release.

## Publish Profiles:

|Type              | Target Runtime | Profile                           | Output Filename                           | Notes
|------------------|----------------|-----------------------------------|-------------------------------------------|------------
| Install          | portable       | PublishInstall.pubxml             | Nucleus.[version].Install-portable.zip    | [^1]
| Upgrade          | portable       | PublishUpgrade.pubxml             | Nucleus[version].Upgrade-portable.zip     | [^1] [^2]
| Install          | win-x64        | PublishInstall-win-x64.pubxml     | Nucleus.[version].Install-win_x64.zip     | [^3]
| Upgrade          | win-x64        | PublishUpgrade-win-x64.pubxml     | Nucleus[version].Upgrade-win_x64.zip      | [^2] [^3]
| Install          | linux-x64      | PublishInstall-linux-x64.pubxml   | Nucleus.[version].Install-linux_x64.zip   | [^3]
| Upgrade          | linux-x64      | PublishUpgrade-linux-x64.pubxml   | Nucleus[version].Upgrade-linux_x64.zip    | [^2] [^3]
| Install          | linux-arm64    | PublishInstall-linux-arm64.pubxml | Nucleus.[version].Install-linux_arm64.zip | [^3]
| Upgrade          | linux-arm64    | PublishUpgrade-linux-arn64.pubxml | Nucleus[version].Upgrade-linux_arm64.zip  | [^2] [^3]

[^1]: The portable install sets are the largest install sets, use for platforms where we do not have a specific install set.
[^2]: Upgrades are the same as installs, but do not include databaseSettings.json or hosting.json files, which may have been modified by end users.
[^3]: Platform-specific installs do not contain a bin/runtimes folder, because the runtimes are in /bin.  The portable install set has a runtimes 
    folder which contains runtimes for each platform.


