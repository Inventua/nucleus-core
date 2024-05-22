# set up Nucleus to use an IIS application/app pool
# usage powershell .\iis_config.ps1 

#Requires -RunAsAdministrator

<#
  .SYNOPSIS
    Set up Nucleus in a Windows/IIS environment
  .EXAMPLE
    nucleus-install.ps1 -Site "Nucleus"
  .DESCRIPTION
    This script performs tasks to set up Nucleus in a Windows/IIS environment:
    - Install ASP.NET core if it is not already installed.
    - Detect Nucleus Install or Upgrade packages and un-zip the latest one.
    - If the specified IIS site does not already exist, create it.
    - If the specified IIS application does not already exist, create it.
    - If the specified Application Pool does not exist, create it.
    - Create an empty appSettings.Testing.json file if it does not already exist and set permissions for the Application Pool user.
    - Create an empty databaseSettings.Testing.json file if it does not already exist and set permissions for the Application Pool user.
    - Set permissions on the Nucleus install folder for the Application Pool user.
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
    Default: Application name plus "AppPool"
    Specifies the name of the IIS Application Pool to create or update.
  .PARAMETER Path
    Default: If this script is run from within an installed Nucleus folder (in Utils\Windows), the existing Nucleus install folder.  Otherwise
    the folder which contains this script.
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
    Default: Auto-detected
    Specifies a install or upgrade package to unzip.  If there is no zip file present in the script folder and this parameter is not specified, 
    the script assumes that a package has been manually un-zipped, so no package file needs to be unzipped.
  .PARAMETER OverwriteExisting 
    Default: false
    If true, specifies that the script can update path, application pool and other settings on existing IIS objects, if they already 
    exist.  If this value is false, existing objects are not updated.
  .PARAMETER AutoInstall
    Default: false
    If true, the script will execute without user input.
#>

param (
		[string]$Site = "Default Web Site",
		[int]$Port = 80,
		[string]$Name = "Nucleus",
		[string]$ApplicationPool = ("{0}{1}" -f $Name, "AppPool"),
		[string]$Path = "",
		[string]$DataPath = "C:\ProgramData\Nucleus",
		[string]$NetCoreVersion = "8.0.4",
    [string]$Environment = "Production",
    [string]$ZipFile = "",
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

	# Constructors
	Application() { $this.Init(@{}) }
	Application([boolean]$OverwriteExisting, [string]$Site, [string]$Name, [int]$Port, [string]$ApplicationPool, [string]$Path, [string]$DataPath, [string]$NetCoreVersion, [string]$Environment, [string]$ZipFile) 
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
	}

	# Functions
  
  # Return whether ASP.NET Core is installed 
	[boolean]IsDotNetInstalled ()
	{
		try
		{
		  $val = ((& "dotnet" --list-runtimes | Out-String -Stream | Select-String "Microsoft.AspNetCore.App $($this.NetCoreVersion)") -match "Microsoft.AspNetCore.App $($this.NetCoreVersion)");
		
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

  [void]WriteLine([string]$caption, [string]$value)
  {
    $this.WriteLine($caption, $value, $false);
  }

  [void]WriteLine([string]$caption, [string]$value, [boolean]$highlight)
  {
    [int]$chars = 30
    [string]$color = "Gray"
    if ($highlight -eq $true)
    {
      $color = "Yellow"
    }
    Write-Host - "$("$($caption):".PadRight($chars)) $value" -ForeGroundColor $color
  }
  
  [void] DisplaySettings()
  {
    Write-Host ""

    Write-Host "Your settings are:"
    Write-Host "------------------"
    $this.WriteLine("Nucleus Application path", $this.Path)
    $this.WriteLine("Application Environment", $this.Environment)
    $this.WriteLine("Application Install set", $this.ZipFile)
    $this.WriteLine("IIS Site Name", $this.Site)
    $this.WriteLine("IIS Application Name", $this.Name)
    $this.WriteLine("IIS Application Pool Name", $this.ApplicationPool)
    Write-Host ""
    $this.WriteLine("Overwrite Existing IIS Values", $this.OverwriteExisting, $this.OverwriteExisting)
    Write-Host ""
    
    Write-Host "This script will:"
    Write-Host "-----------------"

    if ($this.IsDotNetInstalled)
    {
	    Write-Host "- Not install ASP.NET core, because version $($this.NetCoreVersion) is already installed."
    }
    else
    {
	    Write-Host "- Install ASP.NET core $($this.NetCoreVersion)"
    }

    if ($this.ZipFile -eq "")
    {
      Write-Host "- Not un-zip any install or update package."
    }
    else
    {
      Write-Host "- Un-zip '$($this.ZipFile)' to '$($this.Path)'." -ForeGroundColor yellow
    }

    if ($this.GetWebSite() -eq $null)
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
      if (($this.OverwriteExisting) -and ($this.Name -eq ""))
	    {
        Write-Host "- Overwrite the existing values for the IIS site named '$($this.Site)'." -ForeGroundColor yellow
      }
      else
      {
        Write-Host "- Not create an IIS site named '$($this.Site)', because it already exists."
      }
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
      }
    }

    if ($this.GetApplicationPool() -eq $null)
    {
	    Write-Host "- Create an Application Pool named '$($this.ApplicationPool)'." -ForeGroundColor yellow
    }
    else 
    {
	    Write-Host "- Not create an Application Pool named '$($this.ApplicationPool)' because it already exists."
    }
    
    if (-not (Test-Path -Path $this.DataPath))
    {
      Write-Host "- Create an Application Data folder named '$($this.DataPath)' and set permissions for the Application Pool user." -ForeGroundColor yellow
    }

    $appSettingsFile = ("{0}{1}{2}{3}" -f $this.Path, "\appSettings.", $this.Environment, ".json")
    $databaseSettingsFile = ("{0}{1}{2}{3}" -f $this.Path, "\databaseSettings.", $this.Environment, ".json")

    if (-not (Test-Path -Path $appSettingsFile))
    {      
      Write-Host "- Create an empty '$appSettingsFile' file, and set permissions for the Application Pool user." -ForeGroundColor yellow
    }
    else
    {
      Write-Host "- Not create '$appSettingsFile' or change permissions because the file already exists."
    }

    if (-not (Test-Path -Path $databaseSettingsFile))
    {      
      Write-Host "- Create an empty '$databaseSettingsFile' file, and set permissions for the Application Pool user." -ForeGroundColor yellow
    }
    else
    {
      Write-Host "- Not create '$databaseSettingsFile' or change permissions because the file already exists."
    }

    Write-Host "- Set permissions on '$($this.Path)' for the Application Pool user." -ForeGroundColor yellow
    
    $webConfigFile = "$($this.Path)\web.config"
    Write-Host "- Set the ASPNETCORE_ENVIRONMENT environment variable in $($webConfigFile)'." -ForeGroundColor yellow

    Write-Host ""
  }

  # Execute the installation process
  [void]Create()
  {    
    # install ASP.NET Core 
    if (-not $this.IsDotNetInstalled) 
    {
		  Write-Host "Installing ASP.Net Core version '$($this.DotNetVersion)' ..."
		  $this.InstallWindowsHostingBundle()
    }

    # unzip the application install or upgrade Set
    if (($this.ZipFile -ne "") -and ($this.ZipFile -ne $null))
    {
      Write-Host "Un-zipping $($this.ZipFile) to $($this.Path)  ..."
      Expand-Archive -Path $this.ZipFile -DestinationPath $this.Path -Force
    }

    $manager = Get-IISServerManager

    # Create the application pool 
    if ($this.GetApplicationPool() -eq $null)
    {
      Write-Host "Creating Application Pool '$($this.ApplicationPool)' ..." -NoNewLine
      $pool = $manager.ApplicationPools.Add($this.ApplicationPool)      
      $pool.ManagedRuntimeVersion = "v4.0"
      $manager.CommitChanges();
		  # Set-ItemProperty -Path ("{0}{1}" -f "IIS:\AppPools\", $this.ApplicationPool) managedRuntimeVersion "v4.0"
		  Write-Host " OK."
    }

    # Create the web site 
    if ($this.GetWebSite() -eq $null)
		{
      Write-Host "Creating IIS web site '$($this.Site)' ..." -NoNewLine
      $binding = ("{0}{1}{2}" -f "*:", $this.Port, ":")
			$newSite = New-IISSite -Name $this.Site -PhysicalPath $this.Path -BindingInformation $binding -PassThru
      $newSite.Applications["/"].ApplicationPoolName = $this.ApplicationPool
      $manager.CommitChanges();
			Write-Host " OK."
		}    
    elseif (($this.OverwriteExisting -eq $true) -and ($this.Path -eq ""))
    {
      # overwrite the web site settings 
      Write-Host "Updating IIS web site '$($this.Site)' ..." -NoNewLine
      $existingSite = $manager.Sites[$this.Site]
      $existingSite.Applications["/"].VirtualDirectories["/"].PhysicalPath = $this.Path
      $existingSite.Applications["/"].ApplicationPoolName = $this.ApplicationPool
      $manager.CommitChanges();
			Write-Host " OK."
      #Set-ItemProperty IIS:\Sites\$this.Site -name physicalPath -value $this.Path
      #Set-ItemProperty IIS:\Sites\$this.Site -name applicationPool -value $this.ApplicationPool
    }
		
    # Create the web application, if the configured name is not blank.  If the configured name is blank, that means that Nucleus 
    # is being set up at the "web site" level, rather than as an application within a web site.
    if ($this.Name -ne "")
    {
		  $existingWebSite = $manager.Sites[$this.Site]
      if ($this.GetWebApplication() -eq $null) 
      {
        Write-Host "Creating Web Application '$($this.Name)' using path '$($this.Path)' ..." -NoNewLine
        $newApplication = $existingWebSite.Applications.Add(("{0}{1}" -f "/", $this.Name), $this.Path)
        $newApplication.ApplicationPoolName = $this.ApplicationPool
        $manager.CommitChanges();
		    #New-WebApplication -Site $this.Site -Name $this.Name -PhysicalPath $this.Path -ApplicationPool $this.ApplicationPool -Verbose
		    Write-Host " OK."
      }
      elseif ($this.OverwriteExisting -eq $true)
      {
        # overwrite the web application settings 
        Write-Host "Updating values for Web Application '$($this.Name)' using path '$($this.Path)' ..." -NoNewLine
        $existingApplication = $existingWebSite.Applications[("{0}{1}" -f "/", $this.Name)]
        $existingApplication.VirtualDirectories["/"].PhysicalPath = $this.Path
        $existingApplication.ApplicationPoolName = $this.ApplicationPool
        $manager.CommitChanges();
        Write-Host " OK."
        # Set-ItemProperty IIS:\Sites\$this.Site\$this.Name -name physicalPath -value $this.Path
        # Set-ItemProperty IIS:\Sites\$this.Site\$this.Name -name applicationPool -value $this.ApplicationPool
      }
    }
    $appPoolUser = ("{0}\{1}" -f "IIS AppPool", $this.ApplicationPool)
    
    # Set permissions for the install folder
    Write-Host "Setting permissions for user '$($appPoolUser)' on '$($this.Path)'... " -NoNewLine
    (& ICACLS $this.Path /grant ("{0}{1}" -f $appPoolUser, ":(OI)(CI)RX"))
    Write-Host "OK."

    # Create config files for the specified environment and set permissions 
    $appSettingsFile = ("{0}{1}{2}{3}" -f $this.Path, "\appSettings.", $this.Environment, ".json")
    if (-not (Test-Path -Path $appSettingsFile))
    {
      Write-Host "Creating $($appSettingsFile) and setting permissions for user '$($appPoolUser)'... " -NoNewLine
      $content = "{`n`t""Nucleus"": {`n`t`t""FolderOptions"": {`n`t`t`t""DataFolder"": """"`n`t`t }`n`t}`n}"
      New-Item -Path $appSettingsFile -ItemType "file" -Value $content
      (& ICACLS $appSettingsFile /grant ("{0}{1}" -f $appPoolUser, ":RM"))
      Write-Host "OK."
    }   
    
    # configure data Path
    $json = Get-Content -Path  $appSettingsFile -Force | ConvertFrom-Json 

    if (($json.Nucleus.FolderOptions -ne $null) -and ($json.Nucleus.FolderOptions.DataFolder -ne $null))
    {
      $json.Nucleus.FolderOptions.DataFolder = $this.DataPath
    }    
    $json | ConvertTo-Json | Out-File $appSettingsFile  


    $databaseSettingsFile = ("{0}{1}{2}{3}" -f $this.Path, "\databaseSettings.", $this.Environment, ".json")
    if (-not (Test-Path -Path $databaseSettingsFile))
    {
      Write-Host "Creating $($databaseSettingsFile) and setting permissions for user '$($appPoolUser)'... " -NoNewLine
      New-Item -Path $databaseSettingsFile -ItemType "file" -Value "{}"
      (& ICACLS $databaseSettingsFile /grant ("{0}{1}" -f $appPoolUser, ":RM"))
      Write-Host "OK."
    }

    # add the environment name to web.config
    # aspNetCore/environmentVariables/environmentVariable @name="ASPNETCORE_ENVIRONMENT" value="{environment}" />
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

    # Create data directory
    if (-not (Test-Path -Path $this.DataPath))
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

# if path was not specified on the command line, determine where the application is installed 
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
if ($ZipFile -eq "")
{
  if (Test-Path -Path $PSScriptRoot\Nucleus.*.zip)
  {
    $version = $null
    #$installType = ""

    # look for the latest install zip
    Get-ChildItem -Path $PSScriptRoot\Nucleus.*.zip | ForEach-Object {
      if ($_.Name -match "^([A-Za-z_]+)\.(?<version>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.Install\.zip")
      {
        $thisVersion = $matches.Version
        if (($ZipFile -eq "") -or ($version -eq $null) -or ([System.Version]$thisVersion > $version))
        {
          $ZipFile = $_.Name
          #$installType = "Install"
        }
      }
    }

    # if no install zip was found, look for the latest upgrade zip
    Get-ChildItem -Path $PSScriptRoot\Nucleus.*.zip | ForEach-Object {
      if ($_.Name -match "^([A-Za-z_]+)\.(?<version>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.Upgrade\.zip")
      {
        $thisVersion = $matches.Version
        if (($ZipFile -eq "") -or ($version -eq $null) -or ([System.Version]$thisVersion > $version))
        {
          $ZipFile = $_.Name
          #$installType = "Upgrade"
        }
      }
    }
  }
}

Write-Host "Nucleus IIS installer powershell script version $SHELL_SCRIPT_VERSION"
$application = [Application]::new($OverwriteExisting, $Site, $Name, $Port, $ApplicationPool, $Path, $DataPath, $NetCoreVersion, $Environment, $ZipFile)

$cancel = $false;
$changeSettings = $true;

while (($cancel -ne $true) -and ($changeSettings -eq $true))
{
  $application.DisplaySettings()

  if ($AutoInstall -eq $true)
  {
    $changeSettings = $false
    $cancel = $false
  }
  else
  {
    Write-Host "Do you want to install Nucleus and set up IIS with these settings?"
    Write-Host ""
    $response = Read-Host "[C] Change settings, [Y] Yes, continue, [X] Cancel"
    $changeSettings = ($response -eq "c" )
    $cancel = (($response -eq "x") -or ($response -eq ""))
  }

  if (($cancel -ne $true) -and ($changeSettings -eq $true))
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
}

 Write-Host ""
 
if ($cancel -ne $true)
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
      $cancel = $true
      Write-Host "Port $($application.Port) is already in use.  You must use a different value." -ForeGroundColor red
      Write-Host ""
    }
  }
}

if ($cancel -ne $true)
{
  $application.Create()
    
  Write-Host ""
  Write-Host "Operation Complete."
}
else
{
  Write-Host "Operation Cancelled."
}


