# set up Nucleus to use an IIS application/app pool
# usage powershell .\iis_config.ps1 

#Requires -RunAsAdministrator

param (
		[boolean]$OverwriteExisting,
		[string]$Site = "Default Web Site",
		[int]$Port = 80,
		[string]$Name = "Nucleus",
		[string]$ApplicationPool = ("{0}{1}" -f $Name, "AppPool"),
		[string]$Path = "",
		[string]$DataPath = "C:\ProgramData\Nucleus",
		[string]$NetCoreVersion = "8.0.4",
    [string]$Environment = "Production",
    [string]$InstallZipFile = ""
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
  [string]$InstallZipFile

	# Constructors
	Application() { $this.Init(@{}) }
	Application([boolean]$OverwriteExisting, [string]$Site, [string]$Name, [int]$Port, [string]$ApplicationPool, [string]$Path, [string]$DataPath, [string]$NetCoreVersion, [string]$Environment, [string]$InstallZipFile) 
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
    $this.InstallZipFile = $InstallZipFile
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

  #
  # Get folder permissions
  #
  #
  [void] GetFolderPermissions ()
  {
		#$Folder = "D:\share"
		#$User = Read-Host "Input the AccountName of user"
		#$permission = (Get-Acl $APP_DATA_DIRECTORY).Access | ?{$_.IdentityReference -match $User} | Select IdentityReference,FileSystemRights
		#[System.Security.Principal.WindowsIdentity]::GetCurrent().Name  - returns Domain/Username

		$permission = (Get-Acl $this.DataPath).Access | Select IdentityReference, FileSystemRights

		if ($permission)
		{
		  $permission | % {Write-Host "$($_.IdentityReference) has '$($_.FileSystemRights)' rights on folder $this.DataPath."}
		}
		else 
		{
			Write-Host "$Env:UserName doesn't have any permission on $this.DataPath."
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

  [void]WriteLine([string]$caption, [string]$value)
  {
    $this.WriteLine($caption, $value, $false);
  }

  [void]WriteLine([string]$caption, [string]$value, [boolean]$highlight)
  {
    [int]$chars = 26
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
    $this.WriteLine("Application Install set", $this.InstallZipFile)
    $this.WriteLine("IIS Site Name", $this.Site)
    $this.WriteLine("IIS Application Name", $this.Name)
    $this.WriteLine("IIS Application Pool Name", $this.ApplicationPool)
    Write-Host ""
    $this.WriteLine("Overwrite Existing Values", $this.OverwriteExisting, $this.OverwriteExisting)
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

    if ($this.InstallZipFile -eq "")
    {
      Write-Host "- Not un-zip any install or update package."
    }
    else
    {
      Write-Host "- Un-zip '$($this.InstallZipFile)' to '$($this.Path)'." -ForeGroundColor yellow
    }

    if ($this.GetWebSite() -eq $null)
    {
	    Write-Host "- Create an IIS site named '$($this.Site)'."
    }
    else
    {
      if ($this.OverwriteExisting)
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
	    Write-Host "- Create an IIS application '$($this.Site)/$($this.Name)'."
    }
    else
    {
      if ($this.OverwriteExisting)
	    {
        Write-Host "- Overwrite the existing values for the IIS site IIS application '$($this.Site)/$($this.Name)'" -ForeGroundColor yellow
      }
      else
      {
	      Write-Host "- Not create an IIS application '$($this.Site)/$($this.Name)', because it already exists."
      }
    }

    if ($this.GetApplicationPool() -eq $null)
    {
	    Write-Host "- Create an Application Pool named '$($this.ApplicationPool)'."
    }
    else 
    {
	    Write-Host "- Not create an Application Pool named '$($this.ApplicationPool)' because it already exists."
    }
    
    if (-not (Test-Path -Path $this.DataPath))
    {
      Write-Host "- Create an Application Data folder named '$($this.DataPath)' and set permissions for the Application Pool user."
    }
    Write-Host "- Check for environment-specific configuration files: "
    Write-Host "    $($this.Path)\appSettings.$($this.Environment).json"
    Write-Host "    $($this.Path)\databaseSettings.$($this.Environment).json"
    Write-Host "  If either one is not present, create an empty file, and set permissions for the Application Pool user."

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
    if (($this.InstallZipFile -ne "") -and ($this.InstallZipFile -ne $null))
    {
      Write-Host "Un-zipping $($this.InstallZipFile) to $($this.Path)  ..."
      Expand-Archive -Path $this.InstallZipFile -DestinationPath $this.Path -Force
    }

    # Create the application pool 
    if ($this.GetApplicationPool() -eq $null)
    {
      Write-Host "Creating Application Pool '$($this.ApplicationPool)' ..." -NoNewLine
		  New-WebAppPool -Name $this.ApplicationPool 
		  Set-ItemProperty -Path ("{0}{1}" -f "IIS:\AppPools\", $this.ApplicationPool) managedRuntimeVersion "v4.0"
		  Write-Host " OK."
    }

    # Create the web site 
    if ($this.GetWebSite() -eq $null)
		{
      Write-Host "Creating IIS web site '$(this.Site)' ..." -NoNewLine
			New-Website -Name $this.Site -PhysicalPath $this.Path -ApplicationPool $this.ApplicationPool -Port $this.Port 
			Write-Host " OK."
		}    
    elseif ($this.OverwriteExisting -eq $true)
    {
      # overwrite the web site settings 
      Set-ItemProperty IIS:\Sites\$this.Site -name physicalPath -value $this.Path
      Set-ItemProperty IIS:\Sites\$this.Site -name applicationPool -value $this.ApplicationPool
    }
		
    # Create the web application, if the configured name is not blank.  If the configured name is blank, that means that Nucleus 
    # is being set up at the "web site" level, rather than as an application within a web site.
    if ($this.Name -ne "")
    {
      if ($this.GetWebApplication() -eq $null) 
      {
		    #  CreateDirectory $this.DataPath
        Write-Host "Creating Web Application '$($this.Name)' ..." -NoNewLine
		    New-WebApplication -Site $this.Site -Name $this.Name -PhysicalPath $this.Path -ApplicationPool $this.ApplicationPool -Verbose
		    Write-Host " OK."
      }
      elseif ($this.OverwriteExisting -eq $true)
      {
        # overwrite the web application settings 
        Set-ItemProperty IIS:\Sites\$this.Site\$this.Name -name physicalPath -value $this.Path
        Set-ItemProperty IIS:\Sites\$this.Site\$this.Name -name applicationPool -value $this.ApplicationPool
      }
    }
    $appPoolUser = ("{0}\{1}" -f "IIS AppPool", $this.ApplicationPool)
    
    # Set permissions for the install folder
    Write-Host "Setting permissions for '$($appPoolUser)' on '$($this.Path)'."
    (& ICACLS $this.Path /grant ("{0}{1}" -f $appPoolUser, ":(OI)(CI)RX"))
    
    # Create config files for the specified environment and set permissions 
    $appSettingsFile = ("{0}{1}{2}{3}" -f $this.Path, "\appSettings.", $this.Environment, ".json")
    if (-not (Test-Path -Path $appSettingsFile))
    {
      Write-Host "Creating $($appSettingsFile) and setting permissions for '$($appPoolUser)'."
      New-Item -Path $appSettingsFile -ItemType "file" -Value "{}"
      (& ICACLS $appSettingsFile /grant ("{0}{1}" -f $appPoolUser, ":RM"))
    }
    
    $databaseSettingsFile = ("{0}{1}{2}{3}" -f $this.Path, "\databaseSettings.", $this.Environment, ".json")
    if (-not (Test-Path -Path $databaseSettingsFile))
    {
      Write-Host "Creating $($databaseSettingsFile) and setting permissions for '$($appPoolUser)'."
      New-Item -Path $databaseSettingsFile -ItemType "file" -Value "{}"
      (& ICACLS $databaseSettingsFile /grant ("{0}{1}" -f $appPoolUser, ":RM"))
    }

    # add the environment name to web.config
    # aspNetCore/environmentVariables/environmentVariable @name="ASPNETCORE_ENVIRONMENT" value="{environment}" />
    $webConfigFile = "$($this.Path)\web.config"
    Write-Host "Adding environment variables to $($webConfigFile)."
    if (Test-Path -Path $webConfigFile)
    {
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
      
    }

    # Create data directory
    if (-not (Test-Path -Path $this.DataPath))
    {
      Write-Host "Creating Application Data folder '$($this.DataPath)."
      New-Item -Path $this.DataPath -ItemType "directory"
      Write-Host "Created directory '$this.DataPath'."

      Write-Host "Setting permissions on Data folder '$($this.DataPath)' for '$($appPoolUser)'."
      (& ICACLS $this.DataPath /grant ("IIS AppPool\{0}{1}" -f $appPoolUser, ":(OI)(CI)F"))
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
if ($InstallZipFile -eq "")
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
        if (($InstallZipFile -eq "") -or ($version -eq $null) -or ([System.Version]$thisVersion > $version))
        {
          $InstallZipFile = $_.Name
          #$installType = "Install"
        }
      }
    }

    # if no install zip was found, look for the latest upgrade zip
    Get-ChildItem -Path $PSScriptRoot\Nucleus.*.zip | ForEach-Object {
      if ($_.Name -match "^([A-Za-z_]+)\.(?<version>[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.Upgrade\.zip")
      {
        $thisVersion = $matches.Version
        if (($InstallZipFile -eq "") -or ($version -eq $null) -or ([System.Version]$thisVersion > $version))
        {
          $InstallZipFile = $_.Name
          #$installType = "Upgrade"
        }
      }
    }
  }
}

Write-Host "Nucleus IIS installer powershell script version $SHELL_SCRIPT_VERSION"
$application = [Application]::new($OverwriteExisting, $Site, $Name, $Port, $ApplicationPool, $Path, $DataPath, $NetCoreVersion, $Environment, $InstallZipFile)

$cancel = $false;
$changeSettings = $true;

while (($cancel -ne $true) -and ($changeSettings -eq $true))
{
  $application.DisplaySettings()
  Write-Host "Do you want to continue with these settings?"
  Write-Host ""
  $response = Read-Host "[C] Change settings, [Y] Yes, continue, [X] Cancel"
  $changeSettings = ($response -eq "c" )
  $cancel = (($response -eq "x") -or ($response -eq ""))

  if (($cancel -ne $true) -and ($changeSettings -eq $true))
  {
    # Prompt for whether to overwrite existing values
    $application.OverwriteExisting = PromptWithDefaultBoolean "Overwrite Existing IIS values" $application.OverwriteExisting

    # Prompt for the installation folder (leave for default)
    $application.Path = PromptWithDefault "Enter the application path" $application.Path

    # Prompt for the environment name
    $application.Environment = PromptWithDefault "Enter the environment name (usually Production or Development)" $application.Environment

    # Prompt for the site name (leave for default)
    $application.Site = PromptWithDefault "Enter the site name" $application.Site

    # Prompt for web name 
    $application.Name = PromptWithDefault "Enter the application name, or enter 'none' to configure Nucleus in your site root." $application.Name
    
    # Prompt for app pool name 
    $application.ApplicationPool = PromptWithDefault "Enter the application pool name" $application.ApplicationPool
  }
}

if ($cancel -ne $true)
{
  if (($application.Name -eq "none") -or ($application.Name -eq "'none'"))
  {
    $application.Name = ""
  }

  $application.Create()
  Write-Host "Operation Complete."
}
else
{
  Write-Host "Operation Cancelled."
}


