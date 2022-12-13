#! /bin/bash

# Declare the options 
SHORT_OPTIONS=o:,z:,p:,u:,d:
LONG_OPTIONS=output:,zipfile:,apppath:,createuser:,createdirectories:

# Option default values
CREATE_DIRECTORIES=true
CREATE_USER=true

# Default values
OUTPUT="verbose"
SERVICE_ACCOUNT="nucleus-service"  # user/group name for running Nucleus
DOTNET_INSTALL_FILE="dotnet-install.sh"
DIRECTORIES=("app" "data" "certs") # array of directories for nucleus app
DIRECTORY_APP=0
DIRECTORY_DATA=1
DIRECTORY_CERTS=2
VERSION="1.0.0.0"
INSTALL_ZIPFILE=""
UNZIP_PACKAGE="unzip"
APP_PATH="/home/nucleus"

OPTS=$(getopt -a -n "Nucleus Setup" --options $SHORT_OPTIONS --longoptions $LONG_OPTIONS -- "$@") 

function usage()
{
  # Print out the usage.
  printf "Usage: $0 [--p '<Nucleus Application App path>'] [-z '<Zip file>'] [-o verbose|brief|none] "
  exit 2
}

# If options are not set, then display the usage message
#if [ "$?" != "0" ]; then
#  usage
#fi

eval set -- "$OPTS"

while :
do
  case "$1" in
    -u | --createuser)
      CREATE_USER="$2"        ; shift 2  ;;
    -d | --createdirectories)
      CREATE_DIRECTORIES="$2" ; shift 2  ;;
    -z | --zipfile)
      INSTALL_ZIPFILE="$2"    ; shift 2  ;;
    -p | --apppath)
      APP_PATH="$2"           ; shift 2  ;;
    -o | --output)
      OUTPUT="$2"             ; shift 2  ;;
    # -- means the end of the arguments; drop this, and break out of the while loop
    --) shift; break ;;
    # If invalid options were passed, then getopt should have reported an error,
    # which we checked when getopt was called...
    *) echo "Unexpected option: $1 - this should not happen."
       usage ;;
  esac
done

# Version compare function 
function vercomp () {
    if [[ $1 == $2 ]]
    then
        return 0
    fi
    local IFS=.
    local i ver1=($1) ver2=($2)
    # fill empty fields in ver1 with zeros
    for ((i=${#ver1[@]}; i<${#ver2[@]}; i++))
    do
        ver1[i]=0
    done
    for ((i=0; i<${#ver1[@]}; i++))
    do
        if [[ -z ${ver2[i]} ]]
        then
            # fill empty fields in ver2 with zeros
            ver2[i]=0
        fi
        if ((10#${ver1[i]} > 10#${ver2[i]}))
        then
            return 1
        fi
        if ((10#${ver1[i]} < 10#${ver2[i]}))
        then
            return 2
        fi
    done
    return 0
}

# If zip file isn't specified then get the latest version
if [ "$INSTALL_ZIPFILE" == "" ]; then
  if ! compgen -G "Nucleus.[1-9].[0-9].[0-9].[0-9].Install.zip" > /dev/null ; then
    # no install zipfiles found in current directory so exit
    printf "Install zip file was not specified and unable to locate in current directory. Please specify the full path of zip file or copy the file to current directory. \n"
    exit 1
  else
    # loop through nucleus install zip files and get the latest version
    for zipfileversion in Nucleus.*.Install.zip;
    do
      [[ $zipfileversion =~ ^([A-Za-z_]+)\.([1-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.Install\.zip ]]
      vercomp $VERSiON ${BASH_REMATCH[2]}
      if [ $? = 1 ]; then
        # set the latest version of install zip file
        INSTALL_ZIPFILE=$zipfileversion
      fi
    done
  fi
fi

# Print settings, ask for confirmation 
printf "Nucleus installer shell script. \n"
printf "Your settings are app path: $APP_PATH, zip file: $INSTALL_ZIPFILE. \n"
printf "This script will also create a service account '$SERVICE_ACCOUNT' (if it does not already exists) used to run the service. \n"
printf "The dotnet ASPNetCore runtime and the unzip package will also be installed. \n\n"

read -r -p "Do you want to continue (Y/n)? " INSTALL_RESPONSE
if [[ ! "$INSTALL_RESPONSE" =~ ^([yY][eE][sS]|[yY])$ ]]; then
  exit 1
fi


# User function
function user_exists()
{ 
  if id "$SERVICE_ACCOUNT" >/dev/null 2>&1; then
    return 0
  else
    return 1
  fi
}

function directory_exists()
{
  local directory=$1
  if [ -d "$directory" ] >/dev/null 2>&1 ; then
    return 0
  else
    return 1
  fi
}

# Create the service account
if [ "$CREATE_USER" == true ]; then
  # check whether we need to create the service account
  user_exists
  if [ "$?" == 0 ]; then
    echo "$SERVICE_ACCOUNT user already exists."
    #exit 1
  else
    echo "Creating $SERVICE_ACCOUNT user"
    useradd -r "$SERVICE_ACCOUNT" -d $APP_PATH -c "Service account for Nucleus" 
  fi
else
  user_exists
  if [ "$?" != 0 ]; then
    echo "$CREATE_USER is false, user $SERVICE_ACCOUNT must already exist."
    exit 1
  fi
fi

# Create the directories, set the service account to be the group owner and the folder permissions
if [ "$CREATE_DIRECTORIES" == true ]; then
  # home directory set by user
  directory_exists $APP_PATH
  if [ "$?" != 0 ]; then
    mkdir $APP_PATH
  fi
  for folder in "${DIRECTORIES[@]}"
  do
    directory_exists $APP_PATH/$folder
    if [ "$?" != 0 ]; then
      mkdir $APP_PATH/$folder
    fi
    # Assign group ownership of app and data folders to service account 
    chown -R :$SERVICE_ACCOUNT $APP_PATH/$folder
  done

  # Grant read, execute but not write for nucleus group to /app
  chmod -R g+rx-w $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}
  # Grant read, write and execute for nucleus group to /data
  chmod -R g+rwx $APP_PATH/${DIRECTORIES[DIRECTORY_DATA]}
  # Grant read, execute but write for nucleus group to /certs
  chmod -R g+rx-w $APP_PATH/${DIRECTORIES[DIRECTORY_CERTS]}
fi

# Download and install the dotnet runtime 
wget -q -O - https://dot.net/v1/dotnet-install.sh | bash -s -- --runtime aspnetcore --install-dir /usr/share/dotnet

# add dotnet to the path
echo PATH=\"$PATH:/usr/share/dotnet\" > /etc/environment

# Install unzip if it hasn't been installed
if ! dpkg-query -W -f='${Status}' "$UNZIP_PACKAGE"|grep "ok installed" > /dev/null ; then
  read -r -p "$UNZIP_PACKAGE not found, nucleus setup requires $UNZIP_PACKAGE to be installed. Continue (Y/n)? " RESPONSE
  if [[ "$RESPONSE" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    apt install -qq $UNZIP_PACKAGE
    printf "Installed $UNZIP_PACKAGE."
  else
    printf "Nucleus requires $UNZIP_PACKAGE to continue and configure the system. You can manually install $UNZIP_PACKAGE and rerun the setup script to skip this step. \n"
    exit 1
  fi
fi

# Unzip the nucleus zip file to app path
unzip -q "$INSTALL_ZIPFILE" -d $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}

# Copy the Ubuntu appSettings.template to appSettings.Production.json
if [ ! -f "$APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/appSettings.Production.json" ]; then
  cp $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Utils/Ubuntu/appSettings.template $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/appSettings.Production.json
fi

# Set the group ownership of the folders/files that we just unzipped
chown -R :$SERVICE_ACCOUNT $APP_PATH

# Create /Extensions directory and set group ownership to the nucleus service group
directory_exists $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Extensions
if [ "$?" != 0 ]; then
  mkdir $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Extensions
  chown -R :$SERVICE_ACCOUNT $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Extensions
fi

# Nucleus must have read, write and execute permissions to /Extensions in order to install Extensions
# Nucleus must have read, write and execute permissions to /Setup because we create install-log.config to indicate that the setup wizard has completed
chmod -R g+rxw $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Extensions $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Setup

# Copy the service unit file to system directory 
if [ ! -f "/etc/systemd/system/nucleus.service" ]; then
  cp $APP_PATH/${DIRECTORIES[DIRECTORY_APP]}/Utils/Ubuntu/nucleus.service /etc/systemd/system
fi

# Run the service
systemctl daemon-reload
systemctl enable --now nucleus

# Finish installation message.
printf "Nucleus is installed. \n"
printf "You should review your firewall settings (ufw status). \n"

