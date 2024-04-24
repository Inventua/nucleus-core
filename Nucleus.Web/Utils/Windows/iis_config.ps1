

# https://github.com/dotnet/AspNetCore.Docs/issues/16231#issuecomment-566369881
#
# Reference: https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/hosting-bundle?view=aspnetcore-8.0
#            
# Quick way to download the Windows Hosting Bundle and Web Deploy installers which may
# then be executed on the VM ...
#

#
# Set path where installer files will be downloaded ...
#
param ($temp_path = "C:\temp\")

if( ![System.IO.Directory]::Exists( $temp_path ) )
{
   Write-Output "Path not found ($temp_path), create the directory and try again"
   Break
}


#
# Download the Windows Hosting Bundle Installer for ASP.NET Core 8.0 Runtime (v8.0.4)
#
# The installer URL was obtained from:
# https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-aspnetcore-8.0.4-windows-hosting-bundle-installer
#
function InstallWindowsHostingBundle
{

  $whb_installer_url = "https://download.visualstudio.microsoft.com/download/pr/00397fee-1bd9-44ef-899b-4504b26e6e96/ab9c73409659f3238d33faee304a8b7c/dotnet-hosting-8.0.4-win.exe"

  $whb_installer_file = $temp_path + [System.IO.Path]::GetFileName( $whb_installer_url )

  Try
  {
     #todo undo comment
     ##Invoke-WebRequest -Uri $whb_installer_url -OutFile $whb_installer_file

     Write-Output ""
     Write-Output "Windows Hosting Bundle Installer downloaded"
     Write-Output "- Execute the $whb_installer_file to install the .Net Core Runtime"
     Write-Output ""
  }
  Catch
  {
     Write-Output ( $_.Exception.ToString() )
     Break
  }
}


#
# Download Web Deploy v3.6
#
# The installer URL was obtained from:
# https://www.iis.net/downloads/microsoft/web-deploy
# x86 installer: https://download.microsoft.com/download/0/1/D/01DC28EA-638C-4A22-A57B-4CEF97755C6C/WebDeploy_x86_en-US.msi
# x64 installer: https://download.microsoft.com/download/0/1/D/01DC28EA-638C-4A22-A57B-4CEF97755C6C/WebDeploy_amd64_en-US.msi
#
function InstallWebDeploy
{
  $wd_installer_url = "https://download.microsoft.com/download/0/1/D/01DC28EA-638C-4A22-A57B-4CEF97755C6C/WebDeploy_amd64_en-US.msi"

  $wd_installer_file = $temp_path + [System.IO.Path]::GetFileName( $wd_installer_url )

  Try
  {
     #todo undo comment
     ##Invoke-WebRequest -Uri $wd_installer_url -OutFile $wd_installer_file

     Write-Output "Web Deploy installer downloaded"
     Write-Output "- Execute $wd_installer_file and choose the [Complete] option to install all components"
     Write-Output ""
  }
  Catch
  {
     Write-Output ( $_.Exception.ToString() )
     Break
  }
}