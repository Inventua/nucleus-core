# Set up Nucleus in Linux 

See also: 
- [Set up Nucleus in Windows](/manage/hosting/windows/) 
- [Set up Nucleus in Azure App Service](/manage/hosting/azure-app-service/) 
- [Set up Nucleus in a Docker Container](/manage/hosting/docker/)

> **_NOTE:_**   The nucleus-install.sh shell script automates many of the steps.  This page describes the use of the shell script.  This 
process has been tested with [*Ubuntu Server 24.04*](https://ubuntu.com/download/server). 

## Linux Installation
Set up your Linux environment.  There are many options for setting up Linux, including: 
- [Install Ubuntu](https://ubuntu.com/server/docs/installation) on a standalone computer.  
- Use the [Raspberry Pi Imager](https://www.raspberrypi.com/software/) to [install Ubuntu for Raspberry Pi](https://ubuntu.com/download/raspberry-pi) to an SD card.  
- [Create a Linux virtual machine in Azure](https://learn.microsoft.com/en-us/azure/virtual-machines/linux/quick-create-portal).  
Choose Ubuntu Server 24.04 LTS or later when you are prompted for an Image.
- [Create a Linux virtual machine in Amazon Web Services](https://aws.amazon.com/getting-started/hands-on/launch-a-virtual-machine/).  
Choose Ubuntu Server 24.04 LTS or later when you are prompted for a Blueprint.
- [Use the Ubuntu docker image](https://hub.docker.com/_/ubuntu) to run Ubuntu in [Docker](https://www.docker.com/). 

## Nucleus Installation
1.  Connect to a SSH or terminal session.  
Once you have installed Linux, log in as an admin user.  If you are using a PC or Raspberry Pi, you may need to connect a 
keyboard and monitor to your machine in order to see its IP address.  If the IP address is not displayed automatically, you can log in
and view the IP address by using the <kbd>hostname -I</kbd> command.  
<br />
If you have [configured SSH](https://ubuntu.com/server/docs/service-openssh) or are using a virtual machine, you can use SSH to 
connect from another computer:  
<kbd>ssh your-admin-username@linux-machine-name-or-ip-address</kbd>

2.  Create a temporary directory in the root user's home directory for Nucleus installation assets and navigate to it:  
<kbd>mkdir nucleus-install-files</kbd>
<br/>
<kbd>cd nucleus-install-files</kbd>

3.  Download the installer shell script:  
<kbd>wget https://raw.githubusercontent.com/Inventua/nucleus-core/main/Nucleus.Web/Utils/Ubuntu/nucleus-install.sh</kbd>

4.  Download the installation set:  
    #### x64
    <kbd>wget https://github.com/Inventua/nucleus-core/releases/download/v2.0.0/Nucleus.2.0.0.0.Install-linux_x64.zip</kbd>  

    #### Arm64 (for example: Raspberry Pi 3, 4, 5)
    <kbd>wget https://github.com/Inventua/nucleus-core/releases/download/v2.0.0/Nucleus.2.0.0.0.Install-linux_arm64.zip</kbd>  

    #### Portable (other runtime environments: for example: MacOS)
    <kbd>wget https://github.com/Inventua/nucleus-core/releases/download/v2.0.0/Nucleus.2.0.0.0.Install-portable.zip</kbd>  

    If you are installing a later version of Nucleus, download the zip file for that version instead - the installer shell script automatically 
checks the folder which contains the shell script for the zip file with the most recent version of Nucleus. 
[Download the latest Nucleus version](https://github.com/Inventua/nucleus-core/releases/latest)

5.  Run the installer shell script:  
<kbd>sudo bash ./nucleus-install.sh</kbd>
<br />
<br />
The installation shell script creates a user named `nucleus-service`, installs the ASP.NET Core runtime, unzips the install set Nucleus, and then copies 
the application settings template with settings for Linux, sets file system object ownership and permissions and configures systemd to 
automatically start, monitor and restart Nucleus.  The `nucleus-service` user is used to run Nucleus, and does not have a password or a shell 
configured, so you can't log in as this user.
<br />
<br />
**_TIP:_**   If you are installing to a Linux distribution other than Ubuntu Linux, some parts of the shell script may not work, because
different Linux distributions use different commands to install packages.  In most cases, you can work around this issue by 
[installing the ASP.NET runtime](https://learn.microsoft.com/en-us/dotnet/core/install/linux) and [unzip package](https://www.tecmint.com/install-zip-and-unzip-in-linux/) 
before running the script:
<br />
<br />

    ### Custom installation
    > For a fresh install you will not generally need to specify any command-line options.  

    #### Shell script command-line options
    |                                  |                                                             |
    |----------------------------------|--------------------------------------------------------------------------------------|
    | -u, --createuser                 | Use `--createuser false` to prevent the `nucleus-service` user from being created.  You should only use this option if the user has already been created.  |
    | -d, --createdirectories          | Use `--createdirectories false` to prevent creation of the `/home/nucleus`, `/home/nucleus/app` and /home/nucleus/data directories.  This also prevents the commands which set the correct owner and permissions to directories.   |
    | -z, --zipfile                    | Override auto-detection of the Nucleus install zip file name and specify the file to use.  |
    | -t,  --target-directory          | Override the default application path (/home/nucleus).  If used in combination with `--createuser true` (the default), the specified directory will be assigned as the user's home directory.    |

    Example:  
    To install to the `/home/services/nucleus-production` folder instead of using the default folder: 
    <kbd>sudo bash ./nucleus-install.sh --zipfile Nucleus.2.0.0.0.Install.zip --target-directory /home/services/nucleus-production</kbd>

6. Once you have completed this process, [run the setup wizard](/getting-started/#setup-wizard).
Open a web browser and enter the IP address of your server, or its host name.  By default, Nucleus is configured to use http on port 5000.

    ```
    http://host-name:5000
    
    http://ip-address:5000
    ```

[Click here](/getting-started/#setup-wizard) to return to the **Installing Nucleus** page.

## Extended Configuration

### Set up a reverse proxy 
Depending on your environment and objectives, you may need to configure a reverse proxy server.  Refer to ['When to use Kestrel with a reverse proxy'](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/when-to-use-a-reverse-proxy) 
for more information.  
You can set up [Nginx](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-nginx#install-nginx), Kubernetes, 
[Apache](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/linux-apache#install-apache) or another reverse 
proxy.  

See also:  [Nginx Walkthrough](/manage/hosting/linux/#nginx-walkthrough)

If you don't want to use a reverse proxy, you can use Nucleus with the default ports (http: 5000, https: 5001), or you can configure 
Nucleus to run Kestrel using different ports (8080/8443) by editing `/home/nucleus/appSettings.Production.json`.

<kbd>sudo nano /home/nucleus/app/appSettings.Production.json</kbd>

Locate the Kestrel section, and set the port for the Http endpoint and Https endpoints.

> **_NOTE:_**   Linux will not let you use port numbers less than 1024, because only the root user can use those ports.  Use a reverse 
proxy if you want to use the conventional web server ports 80 and 443.

### Set up a certificate and configure https

#### Create a self-signed Certificate 
This command will create a self-signed certficate with no subject defined.  A self-signed certificate is useful in a testing or 
development environment, but is not useful for a production environment, because your browser will display a security warning.  When you 
browse to your site, you will have to ignore or disable security warnings, or "trust" the certificate in your browser, as it is not issued by 
a recognized certification authority.  

<kbd>sudo openssl req -nodes -new -keyout nucleus.key -x509 -days 365 -out nucleus.crt -subj "/"</kbd>

<kbd>sudo cp nucleus.crt /home/nucleus/certs</kbd>

<kbd>sudo cp nucleus.key /home/nucleus/certs</kbd>

<kbd>sudo chown :nucleus-service /home/nucleus/certs/nucleus.key /home/nucleus/certs/nucleus.crt</kbd>

<kbd>sudo chmod g+rw /home/nucleus/certs/nucleus.key /home/nucleus/certs/nucleus.crt</kbd>
<br />
#### Configure Nucleus to use the certificate
If you are using a reverse proxy, you will generally want to configure the reverse proxy to use your certificate and manage ("terminate") SSL 
connections, so you won't need to configure Nucleus for https, and can skip this section.

If you will be running Nucleus without a reverse proxy, configure https and certificate settings with:

<kbd>sudo nano /home/nucleus/app/appSettings.Production.json</kbd>

Add or un-comment the following setting in Kestrel:Endpoints section.  In the default Linux settings template, the section is already 
present (commented out), so you can just un-comment the section and fill in the password:

      "HttpsInlineCertAndKeyFile": {
        "Url": "https://*:5001",  // or "https://your-hostname:5001" if you want to specify a host name
        "Certificate": {
          "Path": "/home/nucleus/certs/nucleus.crt",
          "KeyPath": "/home/nucleus/certs/nucleus.key"
        }
      }
              
### Nginx Walkthrough
Follow the steps in this section if you want to install and configure Nginx as a reverse proxy.

Install:  
<kbd>sudo apt install nginx</kbd>

Create configuration file:  
<kbd>sudo nano /etc/nginx/sites-enabled/nucleus</kbd>

Insert these settings.  If you have not created a certificate, omit or comment out the two `listen` commands which refer to port 443, and the two 
`ssl_certificate` lines:

```
server {
    listen        80;
    listen        443 ssl default_server;
    listen        [::]:443 ssl default_server;
    ssl_certificate     /home/nucleus/certs/nucleus.crt;
    ssl_certificate_key /home/nucleus/certs/nucleus.key;

    server_name _;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_connect_timeout 30s;
        proxy_timeout 30s;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   "Connection" "";
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
    proxy_connect_timeout       300;
    proxy_send_timeout          300;
    proxy_read_timeout          300;
    send_timeout                300;
}
```
To comment out a line, add # to the start of the line. 

Remove the default configuration file:  
<kbd>sudo rm /etc/nginx/sites-enabled/default</kbd>

Restart nginx:  
<kbd>sudo systemctl restart nginx</kbd>

At this point, Nginx is configured to listen on port 80 (and/or 443 if you have created a certificate), and will communicate with
Nucleus on the local machine/port 5000.

### Configure a firewall

Allow SSH connections:  
<kbd>sudo ufw allow "OpenSSH"</kbd>

Allow http and https connections:  

- If you are using a reverse proxy (like Nginx) which listens on ports 80/433:  
<kbd>sudo ufw allow proto tcp from any to any port 80,443</kbd>

- If you are using Kestrel (no reverse proxy), which listens on ports 5000/5001 by default:  
<kbd>sudo ufw allow proto tcp from any to any port 5000,5001</kbd>

Allow FTP:  
<kbd>sudo ufw allow 22</kbd>

Enable firewall on next boot:  
<kbd>sudo ufw enable</kbd>

Review settings:  
<kbd>sudo ufw status</kbd>

Restart to enable the firewall:  
<kbd>sudo shutdown -r now</kbd>


## Troubleshooting
If you are not able to access Nucleus using your browser, you can try the following steps.

1.  Check service status.  
<kbd>systemctl status nucleus</kbd>

2.  Check service logs.  
<kbd>journalctl -xeu nucleus</kbd>

3.  Check the Nucleus error log.  
Navigate to the Nucleus log folder:  
<kbd>cd /home/nucleus/data/Logs</kbd>  
then list the contents:  
<kbd>ls</kbd>.  
Choose today's log - log filenames use UTC dates, so 
the file name might not match your local time zone - then open the log file in an editor:  
<kbd>nano 14-Dec-2022 UTC_MYCOMPUTER.log</kbd>.

4.  Try running Nucleus interactively.  
First, you will need to configure the nucleus-service user to allow logins.
<br />
<kbd>sudo passwd nucleus-service</kbd>
<br />
<kbd>sudo usermod --shell /bin/bash nucleus-service</kbd>
<br />
<br />
Then, login as the `nucleus-service` user and try running Nucleus interactively.  Log messages will be displayed on-screen.
<br />
<kbd>cd /home/nucleus/app</kbd>
<br />
<kbd>sudo systemctl stop nucleus</kbd>
<br />
<kbd>/usr/share/dotnet/dotnet Nucleus.Web.dll</kbd>
<br /><br />
After you have finished troubleshooting, you can disable login for the Nucleus service user with:  
<kbd>sudo passwd --delete nucleus-service</kbd>
<br />
<kbd>sudo usermod --shell /usr/sbin/nologin nucleus-service</kbd>
<br />