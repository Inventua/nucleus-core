#! /bin/bash
SHELL_SCRIPT_VERSION="2024.01"

# Declare the options
SHORT_OPTIONS=a:,o:,z:,t:,u:,d:,h
LONG_OPTIONS=auto-install:,output:,zipfile:,target-directory:,createuser:,createdirectories:,help

# Option default values
CREATE_DIRECTORIES=true
CREATE_USER=true
AUTO_INSTALL=false

# Default values
OUTPUT="verbose"
SERVICE_ACCOUNT="nucleus-service"  # user/group name for running Nucleus
DIRECTORIES=("app" "data" "certs") # array of directories for nucleus app
DIRECTORY_APP=0
DIRECTORY_DATA=1
DIRECTORY_CERTS=2
VERSION="1.0.0.0"
INSTALL_ZIPFILE=""
INSTALL_TYPE=""
UNZIP_PACKAGE="unzip"
TARGET_DIRECTORY="/home/nucleus"

printf "Nucleus installer shell script version %s. \n\n" "$SHELL_SCRIPT_VERSION"

OPTS=$(getopt -a -n "Nucleus Setup" --options $SHORT_OPTIONS --longoptions $LONG_OPTIONS -- "$@")

function usage()
{
  # Print out the usage.
  #printf "Usage: %s [-p '<Nucleus Application App path>'] [-z '<Zip file>']\n" "$0"
  printf "Usage: %s [-OPTION]...\n" "$0"
  printf "Installs Nucleus in the specified directory.\n\n"
  printf "  -t, --target-directory DIRECTORY      Home directory of Nucleus web. Defaults to '%s'.\n" "$TARGET_DIRECTORY"
  printf "  -z, --zipfile ZIPFILE                 Override auto-detection of the Nucleus install zip file\n"
  printf "                                        name and specify the file to use.\n"  
  printf "  -a, --auto-install                    Install automatically without a prompt.\n"
  printf "  -u, --createuser [true|false]         Use '--createuser false' to prevent the nucleus-service\n"
  printf "                                        user from being created.\n"
  printf "                                        You should only use this option if the user has already\n"
  printf "                                        been created.\n"
  printf "  -d, --createdirectories [true|false]  Use '--createdirectories false' to prevent creation of the\n"
  printf "                                        '%s', '%s'\n" "$TARGET_DIRECTORY" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"
  printf "                                        and '%s' directories.\n" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_DATA]}"
  printf "                                        This also prevents the commands which set the correct owner\n"
  printf "                                        and permissions to directories.\n"
  #printf "  -o, --output [verbose|brief|none]     Verbose mode."
  printf "  -h, --help                            Print this Help.\n\n"
  printf "The installation script can be executed without any options. It will install with the following\n"
  printf "defaults:\n"
  printf "  - Uses the latest Nucleus install zip file located in the same directory as the script.\n"
  printf "  - Creates a system account ('%s') that will run Nucleus.\n" "$SERVICE_ACCOUNT"
  printf "  - Creates the directories for Nucleus: ('%s', '%s' and\n" "$TARGET_DIRECTORY" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"
  printf "    '%s').\n" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_DATA]}"
  printf "  - Install or update ASP.NET Core Runtime.\n"
  printf "  - Install '%s' package utility.\n" "$UNZIP_PACKAGE"
  printf "  - Unzips the Nucleus files to the '%s' directory.\n" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"
  printf "  - Creates and starts the service using systemd.\n"
  printf "\n"
  printf "To install an upgrade, use the -z option to specify the name of the upgrade file.  Example:"
  printf "sudo bash ./nucleus-install.sh -z Nucleus.1.1.0.0.Upgrade.zip\n"
  printf "\n"
  exit 0
}

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
  if [ -d "$directory" ] >/dev/null 2>&1; then
    return 0
  else
    return 1
  fi
}

function dotnet_exists()
{
  if command -v "dotnet" >/dev/null 2>&1; then
    return 0
  else
    return 1
  fi
}

# Version compare function (do not modify)
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

#[-o verbose|brief|none]
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
    -a | --auto-install)
      AUTO_INSTALL="$2" ; shift 2  ;;
    -z | --zipfile)
      INSTALL_ZIPFILE="$2"    ; shift 2  ;;
    -t | --target-directory)
      TARGET_DIRECTORY="$2"   ; shift 2  ;;
    -o | --output)
      OUTPUT="$2"             ; shift 2  ;;
    -h | --help)
      usage                              ;;
    # -- means the end of the arguments; drop this, and break out of the while loop
    --) shift; break ;;
    # If invalid options were passed, then getopt should have reported an error,
    # which we checked when getopt was called...
    *) echo "Unexpected option: $1 - this should not happen."
      usage ;;
  esac
done


# If zip file isn't specified then get the latest version
if [ "$INSTALL_ZIPFILE" == "" ]; then
  # loop through nucleus install zip files and get the latest version
  for zipfileversion in Nucleus.*.zip;
  do
    if [[ $zipfileversion =~ ^([A-Za-z_]+)\.([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.Install-{0,1}([A-Za-z_0-9]*)\.zip ]]; then
      # default to the first zip file found
      if [ "$INSTALL_ZIPFILE" == "" ]; then
        INSTALL_ZIPFILE=$zipfileversion
      fi

      vercomp "$VERSION" "${BASH_REMATCH[2]}"
      if [ "$?" == 2 ]; then
        # set the latest version of install zip file
        INSTALL_ZIPFILE=$zipfileversion
        VERSION="${BASH_REMATCH[2]}"
        INSTALL_TYPE="Install"
      fi
    fi
  done

  if [ "$INSTALL_ZIPFILE" == "" ]; then
    # if INSTALL_ZIPFILE is blank, try an upgrade
    # loop through nucleus upgrade zip files and get the latest version
    for zipfileversion in Nucleus.*.zip;
    do
      if [[ $zipfileversion =~ ^([A-Za-z_]+)\.([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.Upgrade-{0,1}([A-Za-z_0-9]*)\.zip ]]; then
        # default to the first zip file found
        if [ "$INSTALL_ZIPFILE" == "" ]; then
          INSTALL_ZIPFILE=$zipfileversion
        fi

        vercomp "$VERSION" "${BASH_REMATCH[2]}"
        if [ "$?" == 2 ]; then
          # set the latest version of upgrade zip file
          INSTALL_ZIPFILE=$zipfileversion
          VERSION="${BASH_REMATCH[2]}"
          INSTALL_TYPE="Upgrade"
        fi
      fi
    done
  fi
else
  [[ $INSTALL_ZIPFILE =~ ^([A-Za-z_]+)\.([0-9]+\.[0-9]+\.[0-9]+\.[0-9]+)\.(.*)\.zip ]]
  VERSION="${BASH_REMATCH[2]}"
  INSTALL_TYPE="${BASH_REMATCH[3]}"
fi

# Check that we have an install file otherwise we warn users and exit script.
if [ "$INSTALL_ZIPFILE" == "" ]; then
  printf "Unable to auto-detect a Nucleus install or upgrade zip file to install.\n"
  printf "Please specify a Nucleus install or upgrade zip file with the -z option, or copy the file to\n"
  printf "'%s'.\n" $(pwd)
  exit 1
fi

INSTALL_MESSAGE="Install"
if [ "$INSTALL_TYPE" == "Upgrade" ]; then
  INSTALL_MESSAGE="Upgrade to"
else
  INSTALL_MESSAGE="$INSTALL_TYPE"
fi

if [ "$AUTO_INSTALL" == false ]; then
  # Print settings, ask for confirmation
  printf "Your settings are:\n"
  printf "  - App path: '%s'.\n" "$TARGET_DIRECTORY"
  printf "  - Zip file: '%s'.\n\n" "$INSTALL_ZIPFILE"
  printf "This script will:\n"
  printf "  - Create a service account '%s', if it does not already exist.\n" "$SERVICE_ACCOUNT"

  if [ "$VERSION" == "1.4.0.0" ] || [ "$VERSION" \> "1.4.0.0" ]; then
    printf "  - Install the ASP.NET Core 8 Runtime, if it is not already installed.\n"
    printf "  - Remove the old ASP.NET Core 6 Runtime, if it is installed.\n"
  else
    printf "  - Install the ASP.NET Core 6 Runtime, if it is not already installed.\n"
  fi

  printf "  - Install the %s package if it is not already installed.\n" "$UNZIP_PACKAGE"
  printf "  - %s Nucleus version %s.\n" "$INSTALL_MESSAGE" "$VERSION"
  printf "  - Set file and directory owner and permissions.\n"
  printf "  - Configure Nucleus to automatically run as a service.\n\n"

  read -r -p "Do you want to continue (Y/n)? " INSTALL_RESPONSE
  if [[ ! "$INSTALL_RESPONSE" =~ ^([yY][eE][sS]|[yY])$ ]]; then
    exit 1
  fi
fi

# Create the service account
if [ "$CREATE_USER" == true ]; then
  # check whether we need to create the service account

  if user_exists ; then
    printf "%s user already exists.\n" "$SERVICE_ACCOUNT"
    #exit 1
  else
    printf "Creating %s user...\n" "$SERVICE_ACCOUNT"
    useradd -r "$SERVICE_ACCOUNT" -d "$TARGET_DIRECTORY" -c "Service account for Nucleus" -s "/usr/sbin/nologin"
    # Add the current user (admin) to the group to allow them to traverse/read nucleus folders. $USER is a linux environment variable.
    usermod -a -G "$SERVICE_ACCOUNT" "$USER"
  fi
else
  if ! user_exists ; then
    printf "%s is false, user %s must already exist." "$CREATE_USER" "$SERVICE_ACCOUNT"
    exit 1
  fi
fi

# Create the directories, set the service account to be the group owner and the folder permissions
if [ "$CREATE_DIRECTORIES" == true ]; then
  # home directory set by user

  if ! directory_exists "$TARGET_DIRECTORY"; then
    mkdir "$TARGET_DIRECTORY"
  fi
  for folder in "${DIRECTORIES[@]}"
  do

    if ! directory_exists "$TARGET_DIRECTORY/$folder"; then
      mkdir "$TARGET_DIRECTORY/$folder"
    fi
    # Assign group ownership of app and data folders to service account
    chown -R :$SERVICE_ACCOUNT "$TARGET_DIRECTORY/$folder"
  done

  # Grant read, execute but not write for nucleus group to /app
  chmod g+rx-w "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"
  # Grant read, write and execute for nucleus group to /data
  chmod g+rwx "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_DATA]}"
  # Grant read, execute but write for nucleus group to /certs
  chmod g+rx-w "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_CERTS]}"
fi


# Download and install the dotnet runtime
if [ "$VERSION" == "1.4.0.0" ] || [ "$VERSION" \> "1.4.0.0" ]; then
  if ! dpkg-query -W -f='${Status}' "aspnetcore-runtime-8.0"|grep "ok installed" > /dev/null ; then
    printf "Installing .NET 8...\n"
    apt-get -q update && apt-get -q -y install aspnetcore-runtime-8.0
  else
    printf ".NET 8 is already installed.\n"
  fi

  # remove .net 6 runtime, if the .net 8 install was successful
  if dpkg-query -W -f='${Status}' "aspnetcore-runtime-6.0"|grep "ok installed" > /dev/null ; then
    if dpkg-query -W -f='${Status}' "aspnetcore-runtime-8.0"|grep "ok installed" > /dev/null ; then
      printf "Removing .NET 6 after upgrade to .NET 8 ...\n"
      apt remove -q -y aspnetcore-runtime-6.0
      apt -y autoremove
    fi
  fi
else
  # install the .NET 6 runtime 
  if ! dpkg-query -W -f='${Status}' "aspnetcore-runtime-6.0"|grep "ok installed" > /dev/null ; then
    printf "Installing .NET 6...\n"
    apt-get -q update && apt-get -q -y install aspnetcore-runtime-6.0
  else
    printf ".NET 6 is already installed.\n"
  fi
fi

# add dotnet to the path
echo PATH=\""$PATH":/usr/share/dotnet\" > /etc/environment

# Install unzip if it hasn't been installed
if ! dpkg-query -W -f='${Status}' "$UNZIP_PACKAGE"|grep "ok installed" > /dev/null ; then
  printf "Installing '%s' package...\n" "$UNZIP_PACKAGE"
  apt-get -q install $UNZIP_PACKAGE
  printf "Installed %s.\n" "$UNZIP_PACKAGE"
fi

# Unzip the nucleus zip file to app path
printf "Unzipping Nucleus files to %s.\n" "$TARGET_DIRECTORY"
unzip -q -o "$INSTALL_ZIPFILE" -d "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"

# Copy the Ubuntu appSettings.template to appSettings.Production.json
if [ ! -f "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/appSettings.Production.json" ]; then
  cp "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Utils/Ubuntu/appSettings.template" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/appSettings.Production.json"
fi
# Copy the Ubuntu appSettings.template to appSettings.Production.json
if [ ! -f "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/databaseSettings.Production.json" ]; then
  cp "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Utils/Ubuntu/databaseSettings.template" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/databaseSettings.Production.json"
fi

# Set the group ownership of the folders/files that we just unzipped
chown -R :$SERVICE_ACCOUNT "$TARGET_DIRECTORY"

# Create /Extensions directory and set group ownership to the Nucleus service account
if ! directory_exists "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Extensions"; then
  mkdir "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Extensions"
fi
chown -R :$SERVICE_ACCOUNT "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Extensions"

# Set read, write and directory execute permissions for user (root). When this script is run with sudo
# the user acts as root so directories created by this script are owned by the root user.
chmod -R u+rwX "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"
# Set read and directory execute permissions, remove write access for group (service account)
chmod -R g+rX-w "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}"

# allow nucleus to modify the production config files (the wizard and log settings screens write config settings)
chmod -R g+rw "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/appSettings.Production.json" 
chmod -R g+rw "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/databaseSettings.Production.json"

# Nucleus must have read, write and execute permissions to /Extensions in order to install Extensions
# Nucleus must have read, write and execute permissions to /Setup because we create install-log.config to indicate that the setup wizard has completed
chmod -R g+rwx "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Extensions" "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Setup"

# Copy the service unit file to system directory
printf "Configuring the Nucleus service.\n"
if [ ! -f "/etc/systemd/system/nucleus.service" ]; then
  cp "$TARGET_DIRECTORY/${DIRECTORIES[DIRECTORY_APP]}/Utils/Ubuntu/nucleus.service" "/etc/systemd/system"
fi

# Run the service
systemctl daemon-reload
systemctl enable --now nucleus

# Finish installation message.
printf "Installation successful. \n"

