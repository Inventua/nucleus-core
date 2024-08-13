# Set up Nucleus in Docker

See also: 
- [Set up Nucleus in Windows](/manage/hosting/windows/) 
- [Set up Nucleus in Azure App Service](/manage/hosting/azure-app-service/) 
- [Set up Nucleus in Linux](/manage/hosting/linux/) 

[Docker](https://www.docker.com/) is an open-source platform that automates the deployment, scaling, and management of applications inside lightweight, portable 
containers. Containers allow developers to package an application with all of its dependencies and configurations into a single, consistent unit that can run 
anywhere, whether on a developer's local machine, on-premises servers, or in the cloud.

Running Nucleus in a Docker container is useful for testing, evaluation or even in some production scenarios. [Kubernetes](https://kubernetes.io/) is an open source 
system for automating deployment, scaling, and management of containerized applications and is often used in production environments.

# How to create a Docker container for Nucleus
1. Download the [Nucleus install set for Linux](https://github.com/Inventua/nucleus-core/releases/download/v2.0.0/Nucleus.2.0.0.0.Install-linux_x64.zip).
2. Download the [Linux installer shell script](https://raw.githubusercontent.com/Inventua/nucleus-core/main/Nucleus.Web/Utils/Ubuntu/nucleus-install.sh).
3. Copy both files to a folder, and create a file named `dockerfile` in the same folder.

```
FROM ubuntu:24.10

ADD Nucleus.2.0.0.0.Install-linux_x64.zip /home/ubuntu
ADD nucleus-install.sh /home/ubuntu

ENV ASPNETCORE_ENVIRONMENT=Production

# install Nucleus
WORKDIR /home/ubuntu
RUN bash nucleus-install.sh --auto-install true

# open ports
EXPOSE 5000

# run nucleus
WORKDIR /home/nucleus/app
CMD ["dotnet", "bin/Nucleus.Web.dll"]
```

## Create the Docker image
<kbd>docker build -f dockerfile -t nucleus:2.0.0.0 .</kbd>
> The dot at the end of the command is required.

> The Linux installer script for Nucleus has some steps which configure the service manager using [systemctl](https://manpages.ubuntu.com/manpages/kinetic/man1/systemctl.1.html), 
and the Ubuntu docker image does not contain systemctl. In Docker, Nucleus is started by the CMD instruction in your docker file, so the service manager steps are not
required, and you can ignore the error messages.

## Create a Container
<kbd>docker run --publish 8090:5000 --restart=unless-stopped --name nucleus2.0 nucleus:2.0.0.0</kbd>
> You can change the port used to access Nucleus (8090) in the command above. Once you have created your Docker container, you can delete the installation zip file, installer script
and dockerfile if you want to.

## Run the Container
The `docker run` command above will automatically start the container. If it is stopped, you can start it again with this command:
<kbd>docker container start nucleus2.0</kbd>

> You can also use [Docker Desktop](https://www.docker.com/products/docker-desktop/) to run and stop the container.

## Run Nucleus
Browse to [http://localhost:8090](http://localhost:8090). The first time you browse to Nucleus, it will run the [Setup Wizard](/getting-started/#setup-wizard).

[Click here](/getting-started/#setup-wizard) to return to the **Installing Nucleus** page.