# Setup Guide for Azure App Service 
1.  If you don't already have one, [create an Azure free account](https://azure.microsoft.com/en-au/free)

2.  Sign in to the Azure portal, click "App Services" and create an App service.

3.  Identify your app service IP address.  In the Azure portal App services page, click your App Service, then choose the 
"Networking" option under "Settings".  The IP address is displayed as "Inbound Address".  Copy the IP address, you need this 
value for step 6.

4.  Select the "Configuration" menu item for your App Service, then click the "Default documents" link at the top of the 
page.  Remove all of the default documents, and add a single "/" default document.  Click Save at the top of the page.

5.  Return to the Azure portal home page, click "SQL Databases" and create a database.

6.  In the Azure portal SQL databases page, select your database, and click the "Set server firewall" link at the top of the 
page.  Add the IP address that you collected in step 3, then click "Save" at the top of the page.

7.  Get your database connection information by clicking the "Connection strings" link.  Note that the connection string 
password is not set in the displayed connection string - you must substitute the password that you entered when you created the
database.  The connection string is required for step 10.

8.  Connect to your App service using an FTP client.  You can get your FTP credentials and endpoint by using the 
"Deployment Center" option after selecting your App Service.  Click the "FTPS credentials" link at the top of the page to view 
FTP information.

9.  Un-zip the install package (zip file) locally, then upload the files to your Azure /site/wwwroot folder.

10.  Edit the databaseSettings.json file.  Paste your database connection string in to the CoreSqlServer connection string, and 
alter the existing Schemas connection key named "*" from "CoreSqlite" to "CoreSqlServer". 

11.  Get your site Url from the Azure portal by clicking "App services", then selecting your App Service.  Click the site Url to 
launch your site.

##Troubleshooting
If the site does not launch or returns an error, click the "App Service Logs" option in Azure Portal.  Enable 
Application Logging(Filesystem) and Web Server logging, then click "Log Stream" (in the left-hand menu).  Try to open your site
again, and error details will be shown in the Log Stream page.