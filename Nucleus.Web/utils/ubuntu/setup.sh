#! /usr/bin/bash

# Declare the options 
SHORT_OPTIONS=o:,u:,d:
LONG_OPTIONS=output:,createuser:,createdirectories:

# Option default values
CREATE_DIRECTORIES=true
CREATE_USER=true

# Default values
OUTPUT="verbose"
SERVICE_ACCOUNT="nucleus-service"  # user/group name for running Nucleus
DOTNET_INSTALL_FILE="dotnet-install.sh"
DIRECTORIES=("app", "data")

OPTS=$(getopt -a -n "Nucleus Setup" --options $SHORT_OPTIONS --longoptions $LONG_OPTIONS -- "$@") 

# If options are not set, then display the usage message
if [ "$?" != "0" ]; then
  usage
fi

eval set -- "$OPTS"

while :
do
  case "$1" in
    -u | --createuser)
      CREATE_USER="$2"        ; shift 2  ;;
    -d | --createdirectories)
      CREATE_DIRECTORIES="$2" ; shift 2  ;;
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

# Create the directories
if [ "$CREATE_DIRECTORIES" == true ]; then
  mkdir /home/nucleus
  mkdir /home/nucleus/app
  mkdir /home/nucleus/data
fi



# Create the user and setup the directory structure and permissions
if [ "$CREATE_USER" == true ]; then
  #if id -u "$SERVICE_ACCOUNT" >/dev/null ; then
  user_exists
  if [ "$? == 0" ]; then
    echo "$SERVICE_ACCOUNT user already exists."
    exit 1
  else
    echo "Creating $SERVICE_ACCOUNT user"
    read -s -p "Type in password:" PASSWORD
    useradd -M "$SERVICE_ACCOUNT" -p "$PASSWORD" -d /home/nucleus -c "Service account for Nucleus"
    unset PASSWORD
  fi
else
  if ! id -u "$SERVICE_ACCOUNT" > /dev/null ; then
    echo "User must already exists"
    exit 1
  fi
fi

#uname2="$(stat --format '%U' "$1")"
#if [ "x${uname2}" = "x${USER}" ]; then
#    echo owner
#else
#    echo no owner
#fi

if is_directory_owner /home/nucleus/app ; then
  echo "$?"
fi

# Make the group account the owner of app and data folders
chown -R :nucleus-service /home/nucleus/app
chown -R :nucleus-service /home/nucleus/data

# Set the folder permissions
chmod -R g+rx-w /home/nucleus/app
chmod -R g+rw-x /home/nucleus/data



# 
function user_exists()
{ 
  if [ id -u "$SERVICE_ACCOUNT" > /dev/null ]; then
    return 0
  else
    return 1
  fi
}

function is_directory_owner()
{
  local directory=$1
  local group="$(stat --format='%G' $directory)"
  if [ "$group" = "$SERVICE_ACCOUNT" ]; then
    return 0
  else
    return 1
  fi
}



# Check if the dotnet install script exists
if [ -f "$DOTNET_INSTALL_FILE" ] ; then
  echo "$DOTNET_INSTALL_FILE" exists
else
  echo "Please download $DOTNET_INSTALL_FILE to install dotnet runtime."
  exit 1
fi


# install aspnetcore-runtime-6.0
if [ -f "$DOTNET_INSTALL_FILE" ] ; then
  echo "$DOTNET_INSTALL_FILE" exists
  # Set the dotnet install script to be executable
  chmod +x "$DOTNET_INSTALL_FILE"
  # Installs the aspnetcore runtime (no SDK)
   "$DOTNET_INSTALL_FILE" --runtime aspnetcore --install-dir /usr/share/dotnet
  echo PATH=\"$PATH:/usr/share/dotnet\" > /etc/environment
else
  echo "Please download $DOTNET_INSTALL_FILE to install dotnet runtime."
  exit 1
fi

# add dotnet to the path
echo PATH=\"$PATH:/usr/share/dotnet\" > /etc/environment

unzip file??

# copies the service unit file to system directory and starts the service
sudo cp nucleus.service /etc/systemd/system
sudo systemctl daemon-reload
sudo systemctl enable --now nucleus

# set firewall


#make certificate??
#openssl x509 -noout -fingerprint -sha1 -inform pem -in /usr/local/share/ca-certificates/aspnet/https.crt
#~/.dotnet/corefx/cryptography/x509stores/my



usage()
{
  echo "Usage: $0 [--create_user true|false] [--createdirectories true|false] [-o verbose|brief|none"]
  exit 2
}
