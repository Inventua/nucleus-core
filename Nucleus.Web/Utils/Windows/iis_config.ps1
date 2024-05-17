#<Tab>

param (
    [switch]$isWebSite,
    [string]$siteName = "Default Web Site",
    [string]$appName = "Nucleus",
    [string]$appPoolName = "NucleusAppPool"
)

Set-Variable DOT_NET_VERSION -Option Constant -Value ([string]"8.0.1")
Set-Variable TEMP_DIRECTORY -Option Constant -Value ([string]"C:\Temp")
Set-Variable ROOT_PATH -Option Constant -Value ([string]"C:\inetpub\wwwroot")
Set-Variable APP_DIRECTORY -Option Constant -Value ([string]"C:\ProgramData\Nucleus")
Set-Variable APP_DATA_DIRECTORY -Option Constant -Value ([string]"$APP_DIRECTORY\Data")

# Function to prompt the user for input with default value
function PromptWithDefault($message, $defaultValue) 
{
  $userInput = Read-Host "$message (Default is ""$defaultValue"")"
  if ([string]::IsNullOrWhiteSpace($userInput)) 
  {
    return $defaultValue
  } 
  else 
  {
    return $userInput
  }
}

#
# https://github.com/dotnet/AspNetCore.Docs/issues/16231#issuecomment-566369881
#
# function to install windows hosting
function InstallWindowsHostingBundle
{
#https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.1-windows-hosting-bundle-installer

  $whbInstallerUrl = "https://download.visualstudio.microsoft.com/download/pr/016c6447-764a-4210-a260-bf7a2880d5c0/a5746437a3862d7803284ae8c2290200/dotnet-hosting-$DOT_NET_VERSION-win.exe"

  $whbInstallerFile = "$TEMP_DIRECTORY\" + [System.IO.Path]::GetFileName( $whbInstallerUrl )
  if (-not ([System.IO.File]::Exists($whbInstallerFile)))
  {
    DownloadWindowsHostingBundle $whbInstallerUrl
  }kk
  Try
  {
    Start-Process -FilePath $whbInstallerFile
    Write-Host "Starting Windows Hosting Bundle runtime installation"
  }
  Catch
  {
    Write-Output ( $_.Exception.ToString() )
    Break
  }


  <#
  
  Try
  {
     ##Invoke-WebRequest -Uri $whbInstallerUrl -OutFile $whbInstallerFile
  
     Write-Host ""
     Write-Host "Windows Hosting Bundle Installer downloaded"
     Write-Host "- Execute the $whb_installer_file to install the .Net Core Runtime"
     Write-Host ""
  }
  Catch
  {
     Write-Output ( $_.Exception.ToString() )
     Break
  }
  #>
}

function DownloadWindowsHostingBundle([string]$url)
{

  $whbInstallerFile = "$TEMP_DIRECTORY\" + [System.IO.Path]::GetFileName( $url )

  Try
  {
     $response = Invoke-WebRequest -Uri $url -OutFile $whbInstallerFile

     Write-Host ""
     Write-Host "Windows Hosting Bundle Installer downloaded"
     #Write-Host "- Execute the $whb_installer_file to install the .Net Core Runtime"
     Write-Host ""
  }
  Catch
  {
     Write-Output ( $_.Exception.ToString() )
     Break
  }
}


#
# Create folder 
#
#
function CreateDirectory([string]$path)
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
function GetFolderPermissions
{
    #$Folder = "D:\share"
    #$User = Read-Host "Input the AccountName of user"
    #$permission = (Get-Acl $APP_DATA_DIRECTORY).Access | ?{$_.IdentityReference -match $User} | Select IdentityReference,FileSystemRights
    #[System.Security.Principal.WindowsIdentity]::GetCurrent().Name  - returns Domain/Username

    $permission = (Get-Acl $APP_DATA_DIRECTORY).Access | Select IdentityReference, FileSystemRights

    if ($permission)
    {
        $permission | % {Write-Host "$($_.IdentityReference) has '$($_.FileSystemRights)' rights on folder $folder"}
    }
    else 
    {
        Write-Host "$User doesn't have any permission on $Folder"
    }
}




# Prompt for new web site 
if (-not $isWebSite) 
{
    Write-Host "Do you want to create a new site for Nucleus?"
    #$webType = Read-Host "Do you want to create a new site for Nucleus? (Type 'yes' or 'no')"
    $webType = Read-Host "[Y] Yes  [N] No (Default is ""N"")"
    $isWebSite = ($webType -eq "y" )
}

# Prompt for the site name (leave for default)
$siteName = PromptWithDefault "Enter the site name" $siteName

# Prompt for web name 
$appName = PromptWithDefault "Enter the application name" $appName

# Prompt for app pool name 
$appPoolName = PromptWithDefault "Enter the app pool name" $appPoolName

# 
# Main function point start
#
# Create the application pool if it doesn't exist
Import-Module WebAdministration
if (Test-Path -Path "IIS:\AppPools\$appPoolName")
{
    Write-Host "$appPoolName already exists."
}
else 
{
    New-WebAppPool -Name $appPoolName 
    Set-ItemProperty -Path IIS:\AppPools\$appPoolName managedRuntimeVersion "v4.0"
    Write-Host "Created application pool ""$appPoolName""."
}

# Create the web site or application based on user input
if ($isWebSite) 
{
    if (-not (Get-Website $siteName))
    {
        New-Website -Name $siteName -PhysicalPath "$ROOT_PATH\$siteName" -ApplicationPool $appPoolName -Port 80 
        Write-Host "IIS web site '$siteName' created successfully."
    }
}
else
{
    Write-Host "IIS web site '$siteName' already exists."
}

# Get the site info from IIS
$siteInfo = Get-Website $siteName

$existingApp = Get-WebApplication -Site $siteName -Name $appName -ErrorAction SilentlyContinue


# Create the new application if it already doesn't exist
if ($existingApp -eq $null) 
{
    CreateDirectory $APP_DIRECTORY
    New-WebApplication -Site $siteName -Name $appName -PhysicalPath $APP_DIRECTORY -ApplicationPool $appPoolName -Verbose
    Write-Host "Application '$appName' created successfully."
}
else
{
    Write-Host "Application '$appName' already exists."
}



# Install ASP.NET Core if required
$isDotNetInstalled = Test-Path -Path "Registry::HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Updates\.NET"
if (-not ($isDotNetInstalled))
{
    #.Net Core not installed at all
    Write-Host ".NET Core not detected. Installing version ""$DOT_NET_VERSION""."
    InstallWindowsHostingBundle
}


# Check version
$isDotNetCurrent = (& "dotnet" --list-runtimes | Out-String -Stream | Select-String "Microsoft.AspNetCore.App $DOT_NET_VERSION") -match "Microsoft.AspNetCore.App $DOT_NET_VERSION"

if (-not $isDotNetCurrent) 
{
    Write-Host ".NET Core version is not current. Nucleus requires ""$DOT_NET_VERSION"". Installing required version."
    InstallWindowsHostingBundle
}
else
{
    Write-Host ".Net Core version is ""$DOT_NET_VERSION""."
}



# Create directories
#if (-not (Test-Path -Path $APP_DATA_DIRECTORY))
#{
#    New-Item -Path $APP_DIRECTORY -Name "Data" -ItemType "directory"
#    Write-Host "Created directory ""$APP_DATA_DIRECTORY""."
#}




