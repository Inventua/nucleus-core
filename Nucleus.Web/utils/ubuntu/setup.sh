#! /usr/bin/bash

# Default values
OUTPUT="verbose"
SERVICE_ACCOUNT="nucleus-service"  # user/group name for running Nucleus
CREATE_DIRECTORIES=true
CREATE_USER=true

SHORT_OPTIONS=o:,u:,d:
LONG_OPTIONS=output:,createuser:,createdirectories:

OPTS=$(getopt -a -n "Nucleus Setup" --options $SHORT_OPTIONS --longoptions $LONG_OPTIONS -- "$@") 

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

# TODO! work out how to do this
user_exists()
{ 
  if [ id "nucleus-service" ] ; then
    return true
  else
    return false
  fi
}

# Create the user and setup the directory structure and permissions
#if [-p 1] then
# Create the user and setup the directory structure and permissions
if [ "$CREATE_USER" == true ]; then
  if id -u "SERVICE_ACCOUNT" >/dev/null ; then
    echo "user already exists"
  else
    echo "Creating $SERVICE_ACCOUNT user."
    read -s -p "Type in password:" PASSWORD
    useradd -M "$SERVICE_USER" -p "$PASSWORD" -d /home/nucleus -c "Service account for Nucleus"
    unset PASSWORD
  fi

  # Make the group account the owner of app and data folders
  chown -R :nucleus-service /home/nucleus/app
  chown -R :nucleus-service /home/nucleus/data

  # Set the folder permissions
  chmod -R g+rx-w /home/nucleus/app
  chmod -R g+rw-x /home/nucleus/data
fi

if [ -f "$DOTNET_INSTALL_FILE" ] ; then
  echo "$DOTNET_INSTALL_FILE" exists
else
  echo "Please download $DOTNET_INSTALL_FILE to install dotnet runtime."
fi


# install aspnetcore-runtime-6.0
if [ -f "$DOTNET_INSTALL_FILE" ] ; then
  echo "$DOTNET_INSTALL_FILE" exists
  # Set the dotnet install script to be executable
  chmod +x ./"$DOTNET_INSTALL_FILE"
  # Installs the aspnetcore runtime (no SDK)
  #./"$DOTNET_INSTALL_FILE" --runtime aspnetcore --install-dir /usr/share/dotnet
  #echo PATH=\"$PATH:/usr/share/dotnet\" > /etc/environment
else
  echo "Please download $DOTNET_INSTALL_FILE to install dotnet runtime."
fi

# add dotnet to the path
#echo PATH=\"$PATH:/usr/share/dotnet\" > /etc/environment

unzip file??

# copies the script file to system directory and starts the service
sudo cp nucleus.service /etc/systemd/system
sudo systemctl enable --now nucleus

#make certificate??
#openssl x509 -noout -fingerprint -sha1 -inform pem -in /usr/local/share/ca-certificates/aspnet/https.crt
#~/.dotnet/corefx/cryptography/x509stores/my



usage()
{
  echo "Usage: $0 [--create_user true|false] [--createdirectories true|false] [-o verbose|brief|none"]
  exit 2
}
