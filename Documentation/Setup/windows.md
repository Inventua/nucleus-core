# Set up Nucleus in Windows

See also: 
- [Set up Nucleus in Azure App Service](/manage/hosting/azure-app-service/) 
- [Set up Nucleus in Linux](/manage/hosting/linux/) 
- [Set up Nucleus in a Docker Container](/manage/hosting/docker/)

## Basic Setup 
1. Download the install set (zip format) from the [downloads](/downloads) page.  For a new installation, you will need to download the 
Nucleus.[version].Install-win_x64.zip file.

2. Create an installation folder, and un-zip the install set to that folder.

3. You can use the Powershell script to automatically install prerequisites and update settings, or you can perform each step manually.


   ### Powershell Script
   
   1. [Download the Powershell script](https://raw.githubusercontent.com/Inventua/nucleus-core/main/Nucleus.Web/Utils/Windows/nucleus-install.ps1) and
      save it to your installation folder.

   2. Run Powershell "as administrator". Open the Start menu, type Windows PowerShell, select Windows PowerShell, and then select Run as administrator. If 
   you do not already have Powershell, download it [here](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell-on-windows?view=powershell-7.4).

   3. At the prompt, navigate to your installation directory using the `CD` command:
      ```
      cd c:\Nucleus  
      ```

   4. Run the Powershell script:
      ```
      .\nucleus-install.ps1  
      ```
      Follow the prompts to install Nucleus. Once the script is complete, open a web browser and enter the IP address of your server, or its host name.  When you use Internet 
      Information Services (IIS) in Windows, the default port is 80, so you don't need to specify a port in your web browser.

      ```
      http://host-name

      http://ip-address
      ```
      
      The [Setup Wizard](/getting-started/#setup-wizard) will run.  

      [Return to the Installing Nucleus page](/getting-started/#setup-wizard) to continue. 

      #### Powershell script command-line options
      > You can optionally specify command-line options for the PowerShell script. For a new installation you will not generally need to specify any command-line options. You can 
      view command-line options with the "Get-Help" command:
 
      ```
      Get-Help .\nucleus-install.ps1  
      ```

      {.table-25-75}
      |                                  |                                                                                       |
      |----------------------------------|---------------------------------------------------------------------------------------|
      | -Site                            | Specify the name of the IIS Web Site to create or update.  Default: Default Web Site  |
      | -Name                            | Specifies the name of the IIS application to create or update. Default: Nucleus       |
      | -Port                            | Specify the port to use when creating an IIS Web Site. Default: 80                    |
      | -ApplicationPool                 | Specifies the name of the IIS Application Pool to create or update. Default: Application name (-Name) plus "AppPool".  |
      | -Path                            | Specifies the folder where Nucleus is installed. Default: The folder which contains this script.  |
      | -DataPath                        | Specifies the folder where Nucleus stores data. Default: C:\ProgramData\Nucleus.  |
      | -NetCoreVersion                  | Specifies the version of ASP.NET Core to check for and install if required. Default: 8.0.4. |
      | -Environment                     | Specifies the environment name to configure for your installation. Default: Production.  |
      | -ZipFile                         | Specifies an install or upgrade package to unzip.  If there is no zip file present in the installation folder and this parameter is not specified, the script assumes that a package has been manually un-zipped, so no package file needs to be unzipped. Default: detect  |
      | -OverwriteExisting               | If set, specifies that the script can update path, application pool and other settings on existing IIS objects, if they already exist.  If this option is not used, existing objects are not updated, and may have the wrong values. Default: false.  |
      | -AutoInstall                     | If set, the script will execute without user input. Default: false,  |

   ### Manual Setup
   > If you use the powershell script, you do not need to perform these steps.

   1. Install the [Microsoft .Net Core Hosting Bundle](https://dotnet.microsoft.com/en-us/download/dotnet/8.0).  

   2. In Internet Information Services (IIS) manager, add an application pool for Nucleus to use.  .Net core applications
require a unique (not shared) application pool.  In the `.NET CLR Version` drop-down, select `NET CLR version v4.0.30319` and in the `Managed pipeline mode`
drop-down, select `Integrated`. 

   3. In Internet Information Services (IIS) manager, add a web site or application with the path set to your installation folder, and assign
the application pool that you created in the previous step.  Nucleus ships with a web.config which is pre-configured with settings to run Nucleus in IIS.  

   4. Assign 'Full Control' permissions to your Nucleus data folder for the `IIS AppPool\[AppPool Name]` user.  By default, your data folder 
is `C:\ProgramData\Nucleus`.  You may need to create the folder in order to assign permissions to it. The Nucleus data folder is used for cached 
files, logs and the database file, if you are using Sqlite.
You can use the command-line command:  
<kbd>ICACLS "C:\ProgramData\Nucleus" /grant "IIS AppPool\\[AppPool Name]:(OI)(CI)F"</kbd>

   5.  Assign 'Read' permissions to your installation folder for the `IIS AppPool\[AppPool Name]` user. 
You can use the command-line command:  
<kbd>ICACLS "[your-installation-folder]" /grant "IIS AppPool\\[AppPool Name]:(OI)(CI)RX"</kbd>

   6.  Set permissions to allow Nucleus to modify configuration files.  This is so that the setup wizard and log settings pages can write configuration settings.
You can use the command-line command:  
<kbd>ICACLS "[your-installation-folder]\\*.Production.json" /grant "IIS AppPool\\[AppPool Name]:RM"</kbd>  
If you are setting up a development environment, replace `Production` with `Development`. 

   7.  Open a web browser and enter the IP address of your server, or its host name.  When you use Internet 
      Information Services (IIS) in Windows, the default port is 80, so you don't need to specify a port in your web browser.

      ```
      http://host-name

      http://ip-address
      ```
      
      The [Setup Wizard](/getting-started/#setup-wizard) will run.  

[Return to the Installing Nucleus page](/getting-started/#setup-wizard) to continue. 