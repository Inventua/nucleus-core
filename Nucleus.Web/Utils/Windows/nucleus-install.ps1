# set up Nucleus to use an IIS application/app pool
# usage powershell .\iis_config.ps1 

#Requires -RunAsAdministrator

<#
  .SYNOPSIS
    Set up Nucleus in a Windows/IIS environment
  .EXAMPLE
    .\nucleus-install.ps1 -Name "Nucleus"
    Install Nucleus with default settings and create an IIS Application in the default site named "Nucleus"
  .EXAMPLE
    .\nucleus_install.ps1  -Name "NucleusTest" -Environment "Testing" 
    Install Nucleus with default settings and create an IIS Application in the default site named "NucleusTest", set 
    web.config to use an ASP.NET environment "Testing", and create config files named appSettings.Testing.json and 
    databaseSettings.Testing.json
  .EXAMPLE
    .\nucleus-install.ps1 -NetCoreVersion ""
    Install Nucleus with default settings and prevent installation of ASP.NET core.
  .EXAMPLE
    .\nucleus-install.ps1 -AutoInstall -OverwriteExisting
    Install Nucleus with default settings, overwriting exsiting data and with no further user input required.
  .DESCRIPTION
    This script performs tasks to set up Nucleus in a Windows/IIS environment:
    - Install ASP.NET core if it is not already installed.
    - Detect Nucleus Install or Upgrade packages and un-zip the latest one.
    - If the specified IIS site does not already exist, create it.
    - If the specified IIS application does not already exist, create it.
    - If the specified Application Pool does not exist, create it.
    - Create an empty appSettings.[environment].json file if it does not already exist and set Read/Write 
      permissions for the Application Pool user.
    - Create an empty databaseSettings.[environment]].json file if it does not already exist and set Read/Write 
      permissions for the Application Pool user.
    - Set Read/Execute permissions on the Nucleus install folder for the Application Pool user.
    - Set the ASPNETCORE_ENVIRONMENT environment variable in web.config to the specified Environment.
  .PARAMETER Site
    Default: Default Web Site
    Specify the name of the IIS Web Site to create or update.
  .PARAMETER Port
    Default: 80
    Specify the port to use when creating an IIS Web Site.  
  .PARAMETER Name
    Default: Nucleus
    Specifies the name of the IIS application to create or update.
  .PARAMETER ApplicationPool
    Default: Application name (-Name) plus "AppPool"
    Specifies the name of the IIS Application Pool to create or update.
  .PARAMETER Path
    Default: If this script is run from within an installed Nucleus folder (in Utils\Windows), the existing 
    Nucleus install folder.  Otherwise, the folder which contains this script.
    Specifies the folder where Nucleus is installed.
  .PARAMETER DataPath
    Default: C:\ProgramData\Nucleus
    Specifies the folder where Nucleus stores data.
  .PARAMETER NetCoreVersion
    Default: 8.0.4
    Specifies the version of ASP.NET Core to check for and install if required.
  .PARAMETER Environment
    Default: Production
    Specifies the environment name to configure for your installation.
  .PARAMETER ZipFile
    Default: detect
    Specifies an install or upgrade package to unzip.  If there is no zip file present in the installation folder 
    and this parameter is not specified, the script assumes that a package has been manually un-zipped, so no 
    package file needs to be unzipped.
  .PARAMETER OverwriteExisting 
    Default: false
    If set, specifies that the script can update path, application pool and other settings on existing IIS 
    objects, if they already exist.  If this option is not used, existing objects are not updated, and may
    have the wrong values.
  .PARAMETER AutoInstall
    Default: false
    If set, the script will execute without user input.
#>

param (
		[string]$Site = "Default Web Site",
		[int]$Port = 80,
		[string]$Name = "Nucleus",
		[string]$ApplicationPool = "",
		[string]$Path = "",
		[string]$DataPath = "C:\ProgramData\Nucleus",
		[string]$NetCoreVersion = "8.0.4",
    [string]$Environment = "Production",
    [string]$ZipFile = "detect",
    [switch]$OverwriteExisting = $false,
    [switch]$AutoInstall = $false		
)

Import-Module IISAdministration

Set-Variable SHELL_SCRIPT_VERSION -Option Constant -Value "2024.05"

class WebSite
{
	[string]$Name 

  WebSite() { $this.Init(@{}) }
	WebSite([string]$Name) 
	{
    $this.Name = $Name
  }
}

class WebApplication 
{
  [string]$Site 
	[string]$Name
  [string]$ApplicationPool

  WebApplication() { $this.Init(@{}) }
	WebApplication([string]$Site, [string]$Name, [string]$ApplicationPool) 
	{
		$this.Site = $Site
		$this.Name = $Name
		$this.ApplicationPool = $ApplicationPool
  }
}

class ApplicationPool
{
	[string]$Name 

  ApplicationPool() { $this.Init(@{}) }
	ApplicationPool([string]$Name) 
	{
    $this.Name = $Name
  }
}

class Application  
{
	# Properties 
  [boolean]$OverwriteExisting
	[string]$Site 
	[string]$Name 
  [int]$Port
	[string]$ApplicationPool
	[string]$Path
	[string]$DataPath
	[string]$NetCoreVersion
  [string]$Environment
  [string]$ZipFile
  [string]$InstallType

	# Constructors
	Application() { $this.Init(@{}) }
	Application([boolean]$OverwriteExisting, [string]$Site, [string]$Name, [int]$Port, [string]$ApplicationPool, [string]$Path, [string]$DataPath, [string]$NetCoreVersion, [string]$Environment, [string]$ZipFile, [string]$InstallType) 
	{
    $this.OverwriteExisting = $OverwriteExisting
		$this.Site = $Site
		$this.Name = $Name
    $this.Port = $Port
		$this.ApplicationPool = $ApplicationPool
		$this.Path = $Path 
		$this.DataPath = $DataPath
		$this.NetCoreVersion = $NetCoreVersion
    $this.Environment = $Environment
    $this.ZipFile = $ZipFile
    $this.InstallType = $InstallType
	}

	# Functions
  
  # Return whether ASP.NET Core is installed 
	[boolean]IsDotNetInstalled ()
	{
		try
		{
		  $val = ((& "dotnet" --list-runtimes | Out-String -Stream | Select-String "Microsoft.AspNetCore.App $($this.NetCoreVersion)") -match "Microsoft.AspNetCore.App $($this.NetCoreVersion)")
		
			if ($val -eq $true)
			{
				return $true
			}
		}
		catch
		{
		  # suppress
		}    
		return $false
	}


  #
  # https://github.com/dotnet/AspNetCore.Docs/issues/16231#issuecomment-566369881
  #
  # install the windows hosting bundle for the configured version
  [void]InstallWindowsHostingBundle ()
  {
    Write-Host "Downloading and running .NET install script ..."

    # Run a separate PowerShell process because the script calls exit, so it will end the current PowerShell session.
    &powershell -NoProfile -ExecutionPolicy unrestricted -Command "[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12; &([scriptblock]::Create((Invoke-WebRequest -UseBasicParsing 'https://dot.net/v1/dotnet-install.ps1'))) -Runtime aspnetcore -Version $($this.NetCoreVersion)"
  }
    
  #
  # Create a folder 
  #
  #
  [void]CreateDirectory([string]$path)
  {
	  if (-not (Test-Path -Path $path))
	  {
		  New-Item -ItemType Directory -Path $path
	  }
  }

  [boolean] IISSiteExists ()
  {
    $val = (Get-IISSite $this.Site -WarningAction SilentlyContinue | select name) 

    return ($val -eq $null)
  }

  [WebSite] GetWebSite ([string]$Site)
  {
    $app = $null
    Get-IISSite -Name $Site -WarningAction SilentlyContinue | ForEach-Object {
		  $app = [WebSite]::new($_.Name)			      
	  }
  
    return $app
  }

  [WebSite] GetWebSite ()
  {
    return $this.GetWebSite($this.Site)
  }

  [Collections.Generic.List[WebApplication]] ListWebApplications ()
  { 
	  $apps = New-Object Collections.Generic.List[WebApplication] 

	  Get-IISSite -WarningAction SilentlyContinue | ForEach-Object {
			$site = $_

			$_.Applications | ForEach-Object {
				$app = [WebApplication]::new($site, $_.Path.Trim("/"), $_.applicationPoolName)
			  $apps += $app
			}			
	  }

	  return $apps
  }

  [WebApplication] GetWebApplication ([string]$Site, [string]$Name)
  {
    $app = $null

	  Get-IISSite -Name $Site -WarningAction SilentlyContinue | ForEach-Object {
			$site = $_
			
      $_.Applications | ForEach-Object {
				$val = [WebApplication]::new($site, $_.Path.Trim("/"), $_.applicationPoolName)
			  if ($val.Name -eq $Name)
			  {
				  $app = $val
			  }
			}
		}
	  
	  return $app
  }

  [WebApplication] GetWebApplication ()
  {
    return $this.GetWebApplication($this.Site, $this.Name)
  }

  [ApplicationPool] GetApplicationPool ([string] $Name)
  {
    $pool = $null

	  Get-IISAppPool -Name $Name -WarningAction SilentlyContinue | ForEach-Object {
		  $pool = [ApplicationPool]::new($_.Name)
		}	
	  
	  return $pool
  }
  
  [ApplicationPool] GetApplicationPool ()
  {
    return $this.GetApplicationPool($this.ApplicationPool)
  } 

  [boolean] IsPortInUse ()
  {
    $val = $false;
    $manager = Get-IISServerManager
    $manager.Sites | ForEach-Object { 
      $_.Bindings | ForEach-Object { 
        if ($_.bindingInformation -match ".*:$($this.Port):") 
        {
          $val = $true 
        }
      } 
    }
    return $val
  } 

  # return whether an upzip package was specified or detected
  [boolean]DoUnzip()
  {
    return ($this.ZipFile -ne "") -and ($this.ZipFile -ne $null)
  }
  
  # return whether the web site exists
  [boolean]WebSiteExists()
  {
    return $this.GetWebSite() -eq $null
  }
  
  # return whether to overwrite existing web site settings
  # That is, OverwriteExisting=true and the web application name is an empty string
  [boolean]DoUpdateWebSite()
  {
    return ($this.OverwriteExisting -eq $true) -and ($this.Name -eq "")
  }

  # return whether a web application should be created or updated
  # That is, Name has not been set to an empty string
  [boolean]DoCreateWebApplication()
  {
    return $this.Name -ne ""
  }

  # return whether to install ASP.net
  # That is, NetCoreVersion is not set to anm empty string, and the specified version is not already installed
  [boolean]DoInstallAspNet()
  {
    return (($this.NetCoreVersion -ne "") -and (-not ($this.IsDotNetInstalled)))
  }

  # return whether the application pool exists
  [boolean]ApplicationPoolExists()
  {
    return $this.GetApplicationPool() -ne $null
  }

  # return whether the DataPath folder already exists
  [boolean]DataPathExists()
  {
    return Test-Path -Path $this.DataPath
  }

  # return the appSettings configuration file name for the specified environment
  [string]GetAppSettingsFileName()
  {
    return ("{0}{1}{2}{3}" -f $this.Path, "\appSettings.", $this.Environment, ".json")
  }

  # return the databaseSettings configuration file name for the specified environment
  [string]GetDatabaseSettingsFileName()
  {
    return ("{0}{1}{2}{3}" -f $this.Path, "\databaseSettings.", $this.Environment, ".json")
  }

  [string]GetWebConfigEnvironment()
  {
    $webConfigFile = "$($this.Path)\web.config"
    if (Test-Path -Path $webConfigFile)
    {
      $xml = [xml](Get-Content -Path $webConfigFile)
      $setting = $xml.SelectSingleNode("//system.webServer/aspNetCore/environmentVariables/environmentVariable[@name='ASPNETCORE_ENVIRONMENT']")

      return $setting.Value
    }
    else
    {
      return $null
    }
  }

  # return whether to update the ASPNETCORE_ENVIRONMENT element in web.config
  # 1. web.config must exist or ZipFile must be set (the zip file contains a web.config)
  # 2. If the file exists, it must have no existing value, or have a value that does not match the configured environment
  [boolean]DoUpdateWebConfig()
  {
    $webConfigFile = "$($this.Path)\web.config"
    
    $val = $this.GetWebConfigEnvironment()
    return ((($this.ZipFile -ne "") -or (Test-Path -Path $webConfigFile)) -and ($val -ne $this.Environment))
  }

  # display a setting on-screen 
  [void]WriteLine([string]$caption, [string]$value)
  {
    $this.WriteLine($caption, $value, $false);
  }

  # display a setting on-screen
  [void]WriteLine([string]$caption, [string]$value, [boolean]$highlight)
  {
    [int]$chars = 30
    [string]$color = "Gray"
    if ($highlight -eq $true)
    {
      $color = "Yellow"
    }
    if ($caption -ne "")
    {
      Write-Host "- $("$($caption):".PadRight($chars)) $value" -ForeGroundColor $color
    }
    else
    {
      Write-Host "  $("$($caption) ".PadRight($chars)) $value" -ForeGroundColor $color
    }
  }
  
  # display current settings 
  [void] DisplaySettings()
  {
    Write-Host ""

    Write-Host "Your settings are:"
    Write-Host "------------------"
    $this.WriteLine("Nucleus Application path", $this.Path)
    $this.WriteLine("Application Environment", $this.Environment)
    $this.WriteLine("Application Install set", $this.ZipFile)
    
    # detect whether Nucleus is already installed, verify that the correct install set type was detected
    if (Test-Path -Path "$this.Path\Nucleus.Web.dll")
    {
      if ($this.InstallType -eq "Install")
      {
        # Nucleus.Web.dll was found, if the latest install set is an install, display warning
        $this.WriteLine("", "WARNING: Nucleus is already installed, but the detected install set is a new install set.", $true)
      }
    }
    else
    {
      if ($this.InstallType -eq "Upgrade")
      {
        # Nucleus.Web.dll was not found, if the latest install set is an upgrade, display warning
        $this.WriteLine("", "WARNING: Nucleus is not installed, but the detected install set is an upgrade set.", $true)
      }
    }

    $this.WriteLine("IIS Site Name", $this.Site)
    $this.WriteLine("IIS Application Name", $this.Name)
    $this.WriteLine("IIS Application Pool Name", $this.ApplicationPool)
    Write-Host ""
    $this.WriteLine("Overwrite Existing IIS Values", $this.OverwriteExisting, $this.OverwriteExisting)
    Write-Host ""
    
    Write-Host "This script will:"
    Write-Host "-----------------"

    if ($this.DoInstallAspNet())
    {
      Write-Host "- Install ASP.NET core $($this.NetCoreVersion)"
    }
    else
    {
	    if ($this.NetCoreVersion -eq "")
	    {
        Write-Host "- Not install ASP.NET core, because you set NetCoreVersion to an empty value."
      }
      else
      {
        Write-Host "- Not install ASP.NET core, because version $($this.NetCoreVersion) is already installed."
      }
    }

    if ($this.DoUnzip())
    {
      Write-Host "- Un-zip '$($this.ZipFile)' to '$($this.Path)'." -ForeGroundColor yellow
    }
    else
    {
      Write-Host "- Not un-zip any install or update package."
    }

    if ($this.WebSiteExists())
    {
	    Write-Host "- Create an IIS site named '$($this.Site)'." -ForeGroundColor yellow
      if ($this.IsPortInUse())
      {
        Write-Host "  WARNING: Port $($this.Port) is already in use.  You must change the port to another value." -ForeGroundColor red
      }
    }
    else
    {
      # we only overwrite web site settings if this.Name is empty and OverwriteExisting = true
      if ($this.DoUpdateWebSite())
	    {
        Write-Host "- Overwrite the existing values for the IIS site named '$($this.Site)'." -ForeGroundColor yellow
      }
      else
      {
        Write-Host "- Not create an IIS site named '$($this.Site)', because it already exists."
      }
    }

    if (-not $this.ApplicationPoolExists())
    {
	    Write-Host "- Create an Application Pool named '$($this.ApplicationPool)'." -ForeGroundColor yellow
    }
    else 
    {
	    Write-Host "- Not create an Application Pool named '$($this.ApplicationPool)' because it already exists."
    }
    
    if (-not ($this.DataPathExists()))
    {
      Write-Host "- Create an Application Data folder named '$($this.DataPath)' and set permissions`n  for the Application Pool user." -ForeGroundColor yellow
    }

    # Create the new application if it already doesn't exist
    if ($this.GetWebApplication() -eq $null)
    {
	    Write-Host "- Create an IIS application '$($this.Site)/$($this.Name)'." -ForeGroundColor yellow
    }
    else
    {
      if ($this.OverwriteExisting)
	    {
        Write-Host "- Overwrite the existing values for the IIS application '$($this.Site)/$($this.Name)'" -ForeGroundColor yellow
      }
      else
      {
	      Write-Host "- Not create an IIS application '$($this.Site)/$($this.Name)', because it already exists."
        if ($this.OverwriteExisting -eq $false)
        {
          Write-Host "  The 'OverwriteExisting setting is not set, so the existing site will not be updated. Use the Change settings" -ForeGroundColor yellow
          Write-Host "  option to enable it (press C), or specify OverwriteExisting on the command line." -ForeGroundColor yellow
        }
      }
    }

    $appSettingsFile = $this.GetAppSettingsFileName()
    $databaseSettingsFile = $this.GetDatabaseSettingsFileName()

    if (-not (Test-Path -Path $appSettingsFile))
    {      
      Write-Host "- Create an empty '$appSettingsFile' file, and set Read/Modify permissions`n  for the Application Pool user." -ForeGroundColor yellow
    }
    else
    {
      Write-Host "- Not create '$appSettingsFile' or set permissions because the file already exists."
     
      # check for an already-configured DataPath setting that doesn't match the current settings 
      $json = Get-Content -Path  $appSettingsFile -Force | ConvertFrom-Json 

      if (($json.Nucleus -ne $null) -and ($json.Nucleus.FolderOptions -ne $null) -and ($json.Nucleus.FolderOptions.DataFolder -ne $null))
      {
        if ($json.Nucleus.FolderOptions.DataFolder -ne $this.DataPath)
        {
          Write-Host "  WARNING:`n  '$appSettingsFile' has a DataFolder setting that does not match '$($this.DataPath)'." -ForeGroundColor red
          Write-Host "  This process will not automatically update the setting, you must edit the file manually." -ForeGroundColor red
        }
      }    
    }

    if (-not (Test-Path -Path $databaseSettingsFile))
    {      
      Write-Host "- Create an empty '$databaseSettingsFile' file, and set Read/Modify permissions`n  for the Application Pool user." -ForeGroundColor yellow
    }
    else
    {
      Write-Host "- Not create '$databaseSettingsFile' or set permissions because the file already exists."
    }

    Write-Host "- Set Read/Execute permissions on the '$($this.Path)' folder for the Application Pool user." -ForeGroundColor yellow
    
    $webConfigFile = "$($this.Path)\web.config"
      
    if ($this.DoUpdateWebConfig() -or $this.DoUnzip())
    {
      Write-Host "- Set the ASPNETCORE_ENVIRONMENT environment variable in $($webConfigFile)' to '$($this.Environment)'." -ForeGroundColor yellow
    }
    else
    {
      if (-not (Test-Path -Path $webConfigFile))
      {
        Write-Host "- Not set the ASPNETCORE_ENVIRONMENT environment variable in $($webConfigFile)' because the file does not exist." 
      }
      else
      {
        Write-Host "- Not set the ASPNETCORE_ENVIRONMENT environment variable in $($webConfigFile)' because it is already set." 
      }
    }

    Write-Host ""
  }

  # Execute the installation process
  [void]Create()
  {    
    # install ASP.NET Core 
    if ($this.DoInstallAspNet())
    {
		  Write-Host "Installing ASP.Net Core version '$($this.NetCoreVersion)' ..."
		  $this.InstallWindowsHostingBundle()
    }

    # unzip the application install or upgrade Set
    if ($this.DoUnzip())
    {
      Write-Host "Un-zipping $($this.ZipFile) to $($this.Path)  ..." -NoNewLine
      Expand-Archive -Path "$($this.Path)\$($this.ZipFile)" -DestinationPath $this.Path -Force
		  Write-Host " OK."
    }

    Start-IISCommitDelay
    $manager = Get-IISServerManager

    # Create the application pool 
    if (-not $this.ApplicationPoolExists())
    {      
      Write-Host "Creating Application Pool '$($this.ApplicationPool)' ..." -NoNewLine
      $pool = $manager.ApplicationPools.Add($this.ApplicationPool)      
      $pool.ManagedRuntimeVersion = "v4.0"
		  Write-Host " OK."
    }

    # Create the web site 
    if ($this.WebSiteExists())
		{      
      Write-Host "Creating IIS web site '$($this.Site)' ..." -NoNewLine
      $binding = ("{0}{1}{2}" -f "*:", $this.Port, ":")
			$newSite = New-IISSite -Name $this.Site -PhysicalPath $this.Path -BindingInformation $binding -PassThru
      $newSite.Applications["/"].ApplicationPoolName = $this.ApplicationPool
			Write-Host " OK."
		}    
    elseif ($this.DoUpdateWebSite())
    {
      # overwrite the web site settings 
      Write-Host "Updating IIS web site '$($this.Site)' ..." -NoNewLine
      $existingSite = $manager.Sites[$this.Site]
      $existingSite.Applications["/"].VirtualDirectories["/"].PhysicalPath = $this.Path
      $existingSite.Applications["/"].ApplicationPoolName = $this.ApplicationPool
			Write-Host " OK."
    }
		
    # Create the web application, if the configured name is not blank.  If the configured name is blank, that means that Nucleus 
    # is being set up at the "web site" level, rather than as an application within a web site.
    if ($this.DoCreateWebApplication())
    {
		  $existingWebSite = $manager.Sites[$this.Site]
      if ($this.GetWebApplication() -eq $null) 
      {
        Write-Host "Creating Web Application '$($this.Name)' using path '$($this.Path)' ..." -NoNewLine
        $newApplication = $existingWebSite.Applications.Add(("{0}{1}" -f "/", $this.Name), $this.Path)
        $newApplication.ApplicationPoolName = $this.ApplicationPool
        Write-Host " OK."
      }
      elseif ($this.OverwriteExisting -eq $true)
      {
        # overwrite the web application settings 
        Write-Host "Updating values for Web Application '$($this.Name)' using path '$($this.Path)' ..." -NoNewLine
        $existingApplication = $existingWebSite.Applications[("{0}{1}" -f "/", $this.Name)]
        $existingApplication.VirtualDirectories["/"].PhysicalPath = $this.Path
        $existingApplication.ApplicationPoolName = $this.ApplicationPool
        Write-Host " OK."
      }
    }
    
    Stop-IISCommitDelay
    $manager.CommitChanges();
		    
    $appPoolUser = ("{0}\{1}" -f "IIS AppPool", $this.ApplicationPool)
    
    # Set permissions for the install folder
    Write-Host "Setting Read/Execute permissions on the '$($this.Path)' folder for the user '$($appPoolUser)'... " -NoNewLine
    (& ICACLS $this.Path /grant ("{0}{1}" -f $appPoolUser, ":(OI)(CI)RX"))
    Write-Host "OK."

    # Create config files for the specified environment and set permissions 
    $appSettingsFile = $this.GetAppSettingsFileName()
    if (-not (Test-Path -Path $appSettingsFile))
    {
      Write-Host "Creating a $($appSettingsFile) file and setting Read/Modify permissions for the user '$($appPoolUser)'... " -NoNewLine
      $content = "{`n`t""Nucleus"": {`n`t`t""FolderOptions"": {`n`t`t`t""DataFolder"": """"`n`t`t }`n`t}`n}"
      New-Item -Path $appSettingsFile -ItemType "file" -Value $content
      (& ICACLS $appSettingsFile /grant ("{0}{1}" -f $appPoolUser, ":RM"))
      Write-Host "OK."
    }   
    
    # configure the app data Path in appSettings.[environment.json
    $json = Get-Content -Path  $appSettingsFile -Force | ConvertFrom-Json 

    if (($json.Nucleus.FolderOptions -ne $null) -and ($json.Nucleus.FolderOptions.DataFolder -ne $null))
    {
      $json.Nucleus.FolderOptions.DataFolder = $this.DataPath
    }    
    $json | ConvertTo-Json -Depth 100 | Out-File $appSettingsFile  


    $databaseSettingsFile = $this.GetDatabaseSettingsFileName()
    if (-not (Test-Path -Path $databaseSettingsFile))
    {
      Write-Host "Creating a $($databaseSettingsFile) file and setting Read/Modify permissions for the user '$($appPoolUser)'... " -NoNewLine
      New-Item -Path $databaseSettingsFile -ItemType "file" -Value "{}"
      (& ICACLS $databaseSettingsFile /grant ("{0}{1}" -f $appPoolUser, ":RM"))
      Write-Host "OK."
    }

    # add the environment name to web.config
    # aspNetCore/environmentVariables/environmentVariable @name="ASPNETCORE_ENVIRONMENT" value="{environment}" 
    if ($this.DoUpdateWebConfig())
    {
      $webConfigFile = "$($this.Path)\web.config"
      if (Test-Path -Path $webConfigFile)
      {
        Write-Host "Adding environment variables to $($webConfigFile)... " -NoNewLine
    
        $xml = [xml](Get-Content -Path $webConfigFile)
        $netcoreElement = $xml.configuration."system.webServer".aspNetCore
        if ($netcoreElement -eq $null)
        {
          Write-Host "Unable to update the environment name in $webConfigFile because a <aspNetCore> element was not found."
        }
        else
        {
          $environmentVariablesElement = $netcoreElement.SelectSingleNode("environmentVariables")
          if ($environmentVariablesElement -eq $null)
          {
            $environmentVariablesElement = $xml.CreateElement("environmentVariables")
            $netcoreElement.AppendChild($environmentVariablesElement)
          }

          $netCoreEnvironmentenvironmentVariableElement = $environmentVariablesElement.SelectSingleNode("environmentVariable[@name='ASPNETCORE_ENVIRONMENT']")
          if ($netCoreEnvironmentenvironmentVariableElement -eq $null)
          {
            $netCoreEnvironmentenvironmentVariableElement = $xml.CreateElement("environmentVariable")
            $netCoreEnvironmentenvironmentVariableElement.SetAttribute("name", "ASPNETCORE_ENVIRONMENT")
            $environmentVariablesElement.AppendChild($netCoreEnvironmentenvironmentVariableElement)          
          }
                
          $netCoreEnvironmentenvironmentVariableElement.SetAttribute("value", $this.Environment)

          $xml.Save("$webConfigFile")
        }
      
        Write-Host "OK."
      }
    }
    # Create data directory
    if (-not ($this.DataPathExists()))
    {
      Write-Host "Creating Application Data folder '$($this.DataPath)... " -NoNewLine
      New-Item -Path $this.DataPath -ItemType "directory"
      Write-Host "OK."

      Write-Host "Setting permissions on Data folder '$($this.DataPath)' for '$($appPoolUser)'... " -NoNewLine
      (& ICACLS $this.DataPath /grant ("IIS AppPool\{0}{1}" -f $appPoolUser, ":(OI)(CI)F"))
      Write-Host "OK."
    }
  }
  # end class 
}  

# Function to prompt the user for input with default value
function PromptWithDefault($message, $defaultValue) 
{
	$userInput = Read-Host "$message (current value is ""$defaultValue"")"
	if ([string]::IsNullOrWhiteSpace($userInput)) 
	{
		return $defaultValue
	} 
	else 
	{
		return $userInput
	}
}

# Function to prompt the user for input with default value, converting Y/N to a boolean 
function PromptWithDefaultBoolean($message, $defaultValue) 
{
  $currentValue = "Y"
  if ($defaultValue -eq $false)
  {
    $currentValue = "N"
  }
	$userInput = Read-Host "$message (current value is ""$currentValue"")"
	if ([string]::IsNullOrWhiteSpace($userInput)) 
	{
		return $defaultValue
	} 
	else 
	{
    if ($userInput -eq "y")
		{
      return $true
    }
    else
    {
      return $false
    }
	}
}


# 
# Main function point start
#


Write-Host "Nucleus IIS installer powershell script version $SHELL_SCRIPT_VERSION"

# if path was not specified on the command line, try to detect where the application is (to be) installed 
if ($Path -eq "")
{
  if ($PSScriptRoot -match "Windows\\Utils$")
  {
    $Path = [System.IO.Path]::GetDirectoryName([System.IO.Path]::GetDirectoryName($PSScriptRoot))
  }
  else
  {
    # if the ps1 script is not running from "Windows\Utils", assume that the application will be installed to the same folder as the script
    $Path = $PSScriptRoot
  }
}

# if it was not specified on the command line, determine whether there is an install set in the script location
if ($ZipFile -eq "detect")
{
  $ZipFile = ""

  if (Test-Path -Path $Path\Nucleus.*.zip)
  {
    $latestVersion = $null

      Get-ChildItem -Path $Path\Nucleus.*.*.zip | ForEach-Object {
        if ($_.Name -match "^([A-Za-z_]+)\.(?<Version>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.(?<Type>[A-Za-z]*)\.zip$")
        {
          $thisVersion = $matches.Version
          $InstallType = $matches.Type
          if (($ZipFile -eq "") -or ($latestVersion -eq $null) -or ([System.Version]$thisVersion -gt $latestVersion))
          {
            $ZipFile = $_.Name
            $latestVersion = [System.Version]$thisVersion            
          }
        }
      }    
    }
  }

# default ApplicationPool if it was not set in the command line
if ($ApplicationPool -eq "")
{
  if ($Name -eq "")
  {
    # default AppPool from site name
    $ApplicationPool = ("{0}{1}" -f $Site, "AppPool")
  }
  else
  {
    # default AppPool from application name
    $ApplicationPool = ("{0}{1}" -f $Name, "AppPool")
  }
}

$application = [Application]::new($OverwriteExisting, $Site, $Name, $Port, $ApplicationPool, $Path, $DataPath, $NetCoreVersion, $Environment, $ZipFile, $InstallType)

$operation = ""
while (($operation -eq "") -or ($operation -eq "change-settings"))
{
  $application.DisplaySettings()

  if ($AutoInstall -eq $true)
  {
    operation = "install"
  }
  else
  {
    Write-Host "Do you want to install Nucleus and set up IIS with these settings?"
    Write-Host ""
    $response = Read-Host "[C] Change settings, [Y] Yes, continue, [X] Cancel"

    switch ($response)
    {
      "c" {
        # change settings
        $operation = "change-settings"
      }
      "x" {
        # exit
        $operation = "exit"
      }
      "y" {
        # continue with install 
        $operation = "install"
      }
      Default {
        # invalid entry
        $operation = ""
      }
    }    
  }

  if ($operation-eq "change-settings")
  {
    # Prompt for whether to overwrite existing values
    $application.OverwriteExisting = PromptWithDefaultBoolean "Overwrite Existing IIS values?" $application.OverwriteExisting

    # Prompt for the installation folder (leave for default)
    $application.Path = PromptWithDefault "Application path" $application.Path

    # Prompt for the environment name
    $application.Environment = PromptWithDefault "Environment name (usually Production or Development)" $application.Environment

    # Prompt for the site name (leave for default)
    $application.Site = PromptWithDefault "Web site name" $application.Site

    # Prompt for web name 
    $application.Name = PromptWithDefault "IIS Application name ('none' to configure Nucleus in your Web Site root)" $application.Name
    
    # Prompt for app pool name 
    $application.ApplicationPool = PromptWithDefault "Application pool name" $application.ApplicationPool
  }
} # end loop 

 Write-Host ""
 
if ($operation -eq "install")
{
  if (($application.Name -eq "none") -or ($application.Name -eq "'none'"))
  {
    $application.Name = ""
  }

  # validate
  if ($application.GetWebSite() -eq $null)
  {
	  if ($application.IsPortInUse())
    {
      $operation = "exit"
      Write-Host "Port $($application.Port) is already in use.  You must use a different value." -ForeGroundColor red
      Write-Host ""
    }
  }
}

if ($operation -eq "install")
{
  $application.Create()
    
  Write-Host ""
  Write-Host "Operation Complete."
}
else
{
  Write-Host "Operation Cancelled."
}


