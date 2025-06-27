#!/bin/bash

# Variables
SCRIPT_DIR=$(dirname "$(readlink -f "$0")")
user_domain=""
web_console_domain=""
server_domain=""
web_console_port_http=80
web_console_http_enabled=true
web_console_port_https=443
web_console_https_enabled=false
web_console_server_port=""
publicOverrideUrl=""
server_port_http=7080
server_http_enabled=true
server_port_https=7443
server_https_enabled=true
server_https_forced=false
mysql_password=""
le_enabled=false
le_email=""
le_pfx_password=""
timezone=""
reverse_proxy_ip=""
reverse_proxy_forward_host=""
known_proxies_json="[]"
IpWhitelist=()
web_console_title="NetLock RMM"
apiKeyOverride=""

cat << "EOF"
//  
  _   _      _   _                _      _____  __  __ __  __ 
 | \ | |    | | | |              | |    |  __ \|  \/  |  \/  |
 |  \| | ___| |_| |     ___   ___| | __ | |__) | \  / | \  / |
 | . ` |/ _ \ __| |    / _ \ / __| |/ / |  _  /| |\/| | |\/| |
 | |\  |  __/ |_| |___| (_) | (__|   <  | | \ \| |  | | |  | |
 |_| \_|\___|\__|______\___/ \___|_|\_\ |_|  \_\_|  |_|_|  |_|                                                              
EOF

set -e
echo ""
echo "Welcome to the NetLock RMM Docker Quick Setup Script!"
echo "Verifing docker installation..."

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo "Docker is not installed. Please install Docker and try again."
    exit 1
fi

echo "Docker is installed."

# Check if Docker Compose (docker-compose or docker compose) is installed
if ! command -v docker-compose &> /dev/null && ! command -v docker compose &> /dev/null; then
    echo "Docker Compose is not installed. Please install Docker Compose and try again."
    exit 1
fi

echo "Docker Compose is installed."

# Check if the device has at least 3 GB of free RAM (adjusted from original comment)
free_ram=$(free -m | awk '/^Mem:/{print $7}')

if (( free_ram < 3072 )); then
    echo "Insufficient RAM. At least 3 GB of free working memory (RAM) is required. You have ${free_ram} MB available."
    echo "Please free up some memory or upgrade your system."

    # Ask user if they want to continue anyway
    read -p "Do you want to continue with the setup anyway? (Y/n): " continue_setup
    continue_setup=${continue_setup:-Y}  # default to Y if input is empty

    if [[ ! "$continue_setup" =~ ^[Yy]$ ]]; then
        echo "Exiting setup."
        exit 1
    fi

    echo "Continuing setup with ${free_ram} MB of RAM available, but this may lead to performance issues or a failed installation."
else
    echo "RAM available: ${free_ram} MB."
fi

echo ""
echo "[Requirements]"
echo "[Ubuntu 24.04 or simliar]"
echo "[Docker installed]"
echo "[4 GB of free working memory (RAM)]"
echo ""
echo "[ATTENTION!!!]"
echo "The script only covers some scenarious and tries to guide you through the setup process. NetLock RMM supports any environment (single ip, reverse proxies etc.). If you fail with your installation, please read the documentation at: https://docs.netlockrmm.com/installation/docker-quick-setup"
echo "If you have any questions and need community support, please join our Discord server: https://discord.gg/HqUpZgtX4U"
echo "If you are a business, need support with your setup and you are interested into the Pro or Cloud membership, please contact us at: support@0x101-cyber-security.de"
echo ""
echo "Our pro membership includes code signing, unlimited devices and we assist you with the setup of your NetLock RMM instance in your own environment."
echo "We also offer a managed hosting solution for NetLock RMM, please contact us for more information or check our website."

read -p "Continue if you have read. (Y/n): " read_ok
read_ok=${read_ok:-Y}

if [[ ! "$read_ok" =~ ^[Yy]$ ]]; then
    echo "Aborted. Please ensure you read the text."
    exit 1
fi

echo ""

# Set time zone
echo "If your timezone is not listed below, you can set it manually later in the docker-compose.yml file."
PS3="Select a timezone (enter number): "
options=(
  "Europe/Berlin"
  "America/New_York"
  "America/Los_Angeles"
  "Europe/London"
  "Europe/Paris"
  "Europe/Moscow"
  "Asia/Tokyo"
  "Asia/Shanghai"
  "Asia/Singapore"
  "Asia/Kolkata"
  "Asia/Dubai"
  "Australia/Sydney"
  "Australia/Perth"
  "Africa/Johannesburg"
  "America/Sao_Paulo"
  "America/Chicago"
  "America/Toronto"
  "Pacific/Auckland"
)

echo "Please select your timezone:"
select opt in "${options[@]}"; do
    if [[ -n "$opt" ]]; then
        timezone="$opt"
        echo "Setting timezone to $timezone..."
        sudo timedatectl set-timezone "$timezone"
        break
    else
        echo "Invalid selection. Please try again."
    fi
done

# Fallback if empty for any reason
if [[ -z "$timezone" ]]; then
    timezone="Europe/Berlin"
    echo "No timezone selected, defaulting to $timezone..."
    sudo timedatectl set-timezone "$timezone"
fi

echo ""

# Ask the user for the MySQL root password
read -p "Please enter the MySQL root password (will be used for NetLock RMM): " mysql_password
if [[ -z "$mysql_password" ]]; then
    echo "No MySQL password entered. Exiting."
    exit 1
fi

echo ""

# Ask for the web console title
read -p "Please enter the title for the web console (PRO only!, leave blank if you do not have a pro membership): " web_console_title
if [[ -z "$web_console_title" ]]; then
    echo "No title entered. Defaulting to 'NetLock RMM'."
    web_console_title="NetLock RMM"
fi

echo ""

# Prompt the user for a comma-separated list of IPs to whitelist
echo "The web console can be secured with IP whitelisting."
echo "WARNING: If you enable IP whitelisting, Let's Encrypt will not be able to issue certificates for the web console domain. You can enable IP whitelisting later in the web console appsettings.json!"
echo "Please enter the IP addresses you want to whitelist for the web console."
echo "Leave blank to disable IP whitelisting."
read -p "IP Whitelist (e.g. 192.168.1.100,203.0.113.5): " ip_input

# Trim spaces and split into array
if [[ -n "$ip_input" ]]; then
    # Remove spaces and split into array by comma
    IFS=',' read -ra IpWhitelist <<< "$(echo "$ip_input" | tr -d ' ')"

    echo "Whitelisted IPs:"
    for ip in "${IpWhitelist[@]}"; do
        echo " - $ip"
    done
else
    IpWhitelist=()
    echo "No IPs whitelisted. The web console will be accessible from all IPs."
fi

# Generate JSON-safe array string
if [ ${#IpWhitelist[@]} -eq 0 ]; then
    ip_whitelist_json="[]"
else
    ip_whitelist_json=$(printf '"%s",' "${IpWhitelist[@]}")
    ip_whitelist_json="[${ip_whitelist_json%,}]" # remove trailing comma
fi

# User needs to provide members portal api key, demand again if left empty
echo ""
read -p "Please provide your members portal api key now (for OSS & Pro): " apiKeyOverride
apiKeyOverride=$(echo "$apiKeyOverride" | xargs)  # removes leading/trailing whitespace

if [[ -z "$apiKeyOverride" ]]; then
    echo "No API key entered. Exiting."
    exit 1
fi

# Pick one of the following options:
# 1. Reverse proxy (nginx, traefik, caddy, etc.)
# 2. No reverse proxy (direct access with Let's Encrypt)
# 3. Local test environment without SSL (for testing purposes only)

echo ""
echo "Please choose your setup option:"
PS3="Select an option (enter number): "
options=(
  "No Reverse Proxy (direct access with Let's Encrypt)"
  "Reverse Proxy (nginx, traefik, caddy, etc.)"
  "Local Test Environment (without SSL, for testing purposes only)"
)

# Set default
setup_option="1"

select opt in "${options[@]}"; do
    case $opt in
        "${options[0]}")
            setup_option="1"
            echo "You selected: ${options[0]}"
            break
            ;;
        "${options[1]}")
            setup_option="2"
            echo "You selected: ${options[1]}"
            break
            ;;
        "${options[2]}")
            setup_option="3"
            echo "You selected: ${options[2]}"
            break
            ;;
        *)
            echo "Invalid selection. Please try again."
            ;;
    esac
done

echo ""

# Let the user enter their main domain (Please enter your domain you will use for NetLock RMM. NOT YOUR SUBDOMAIN!!! Example: netlockrmm.com)
read -p "Please enter the domain (NOT SUBDOMAIN) you will use (example: netlockrmm.com). The subdomains to be created will be displayed afterwards: " le_domain
if [[ -z "$le_domain" ]]; then
    echo "No domain entered. Exiting."
    exit 1
fi

# Generate web console and server domains
web_console_domain="nl-webconsole.$le_domain"
server_domain="nl-server.$le_domain"

echo ""

# Show the user the domains
echo "Please create the following DNS records (subdomains) for your domain:"
echo "A record for web console: $web_console_domain -> pointing to the IP of your server"
echo "A record for server: $server_domain -> pointing to the IP of your server"

echo ""

# Ask the user to confirm the DNS records
read -p "Have you created the DNS records? (Y/n): " dns_records
dns_records=${dns_records:-Y}

if [[ ! "$dns_records" =~ ^[Yy]$ ]]; then
    echo "Please create the DNS records and try again."
    exit 1
fi

if [[ "$setup_option" == "1" ]]; then # No Reverse Proxy with Let's Encrypt
    echo "Continueing with: No Reverse Proxy (direct access to the internet. Uses inbuilt Let's Encrypt integration for SSL)"

    # Lets Encrypt setup
    le_enabled=true
    echo "Let's Encrypt will be used for SSL certificates. Make sure your DNS records are set up correctly."
    read -p "Please enter your email address for Let's Encrypt: " le_email
    if [[ -z "$le_email" ]]; then
        echo "No email address entered. Exiting."
        exit 1
    fi

    read -p "Please enter the password for the Let's Encrypt .pfx certificate: " le_pfx_password
    if [[ -z "$le_pfx_password" ]]; then
        echo "No password entered for Let's Encrypt .pfx certificate. Exiting."
        exit 1
    fi

    echo ""

    echo "Make sure you have the following ports open on your server:"
    echo "80 (HTTP) and 443 (HTTPS) for web console access. Port 80 is used for Let's Encrypt certificate generation. Hold it open until the certificate is generated."
    echo "7443 for the NetLock RMM server (remote access, file transfer, etc.)."

    # Confirm the ports
    read -p "Have you opened the ports 80, 443 and 7443 on your server? (Y/n): " ports_open
    ports_open=${ports_open:-Y}

    if [[ ! "$ports_open" =~ ^[Yy]$ ]]; then
        echo "Please open the ports 80, 443 and 7443 on your server and try again."
        exit 1
    fi

    # Set the web console and server ports
    web_console_port_http=80
    web_console_http_enabled=true
    web_console_port_https=443
    web_console_https_enabled=true

    web_console_server_port=7080

    server_http_enabled=true
    server_port_http=7080
    
    server_https_enabled=true
    server_port_https=7443
    server_https_forced=false # No forced HTTPS because of the internal server communication

    # Set the public override URL
    publicOverrideUrl="https://$server_domain:$server_port_https"

elif [[ "$setup_option" == "2" ]]; then # Reverse Proxy
    echo "You chose: Reverse Proxy (nginx, traefik, caddy, etc.)"

    # Get internal ip of the server
    server_ip=$(hostname -I | awk '{print $1}')

    read -p "Is this the server where your reverse proxy is running? (Y/n): " reverse_proxy_server
    reverse_proxy_server=${reverse_proxy_server:-Y}
    echo ""
    read -p "Please enter the IP of the server where your reverse proxy is running: " reverse_proxy_ip
    if [[ -z "$reverse_proxy_ip" ]]; then
        echo "No IP entered. Exiting."
        exit 1
    fi

    # JSON-safe output for KnownProxies
    if [[ -n "$reverse_proxy_ip" ]]; then
        known_proxies_json="[\"$reverse_proxy_ip\"]"
    else
        known_proxies_json="[]"
    fi

    echo ""
    echo "Reverse proxy IP: $reverse_proxy_ip"
    echo "Internal IP of this server (auto detected): $server_ip"
    echo ""

    # Ask if the server ip is correct
    read -p "Is the internal IP of this server correct? (Y/n): " server_ip_confirmation
    server_ip_confirmation=${server_ip_confirmation:-Y}

    if [[ ! "$server_ip_confirmation" =~ ^[Yy]$ ]]; then
      # If the user says no, ask for the correct IP
      read -p "Please enter the correct internal IP of this server: " server_ip
      if [[ -z "$server_ip" ]]; then
          echo "No IP entered. Exiting."
          exit 1
      fi # here is a missing fi which causes the script to work
    fi

    # If the reverse proxy is on the same server, we can use localhost for forwarding, otherwise we use the server's IP
    if [[ "$reverse_proxy_server" =~ ^[Yy]$ ]]; then
        reverse_proxy_forward_host="localhost"
    else
        reverse_proxy_forward_host="$server_ip"
    fi

    echo "For the web console domain forward like this:"
    echo "$web_console_domain: http -> $server_ip -> 443"
    echo ""
    echo "For the server domain forward like this:"
    echo "$server_domain: http -> $server_ip -> 7443"

    echo ""

    # Ask the user to confirm the reverse proxy setup
    read -p "Have you set up the reverse proxy to forward the domains $web_console_domain and $server_domain to the internal IP of this server? (Y/n): " reverse_proxy
    reverse_proxy=${reverse_proxy:-Y}

    if [[ ! "$reverse_proxy" =~ ^[Yy]$ ]]; then
        echo "Please set up the reverse proxy and try again."
        exit 1
    fi

    # Set the web console and server ports
    web_console_port_http=443
    web_console_http_enabled=true
    web_console_port_https=443
    web_console_https_enabled=false

    web_console_server_port=7443

    server_http_enabled=true
    server_port_http=7443

    server_https_enabled=false
    server_port_https=7443
    server_https_forced=false # No forced HTTPS because of the internal server communication

    # Set the public override URL
    publicOverrideUrl="https://$server_domain:443"

elif [[ "$setup_option" == "3" ]]; then # Local Test Environment
    echo "You chose: Local Test Environment (without SSL, for testing purposes only)"
    echo "This setup is for local testing purposes only and does not use SSL. It is not recommended for production use."

    # Let the user confirm they understand this is for testing only
    read -p "Do you understand that this setup is for testing purposes only and does not use SSL? (Y/n): " test_env_confirmation
    test_env_confirmation=${test_env_confirmation:-Y}
    if [[ ! "$test_env_confirmation" =~ ^[Yy]$ ]]; then
        echo "Exiting setup. Please choose a different option."
        exit 1
    fi

    # Set the web console and server ports for local testing
    web_console_port_http=80
    web_console_http_enabled=true
    web_console_port_https=443
    web_console_https_enabled=false

    web_console_server_port=7080

    server_http_enabled=true
    server_port_http=7080

    server_https_enabled=false
    server_port_https=7443

    publicOverrideUrl="http://localhost:$server_port_http"
    le_enabled=false # No Let's Encrypt, no SSL termination for testing

else
    echo "Invalid option selected. Exiting."
    exit 1
fi

# Start the setup process
echo "Starting the setup process..."
echo "Create directories..."

# Directories
sudo mkdir -p /home/netlock/{mysql/data,letsencrypt,web_console/internal,web_console/logs,server/logs,server/files,server/internal}

# Create empty appsettings files
sudo touch /home/netlock/web_console/appsettings.json
sudo touch /home/netlock/server/appsettings.json

echo "Creating web console appsettings.json..."

# Write appsettings.json for Web Console (correct path!)
sudo tee /home/netlock/web_console/appsettings.json > /dev/null <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error",
      "Microsoft.Hosting.Lifetime": "Warning"
    },
    "Custom": {
      "Enabled": true
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoint": {
      "Http": {
        "Enabled": $web_console_http_enabled,
        "Port": $web_console_port_http
      },
      "Https": {
        "Enabled": $web_console_https_enabled,
        "Port": $web_console_port_https,
        "Force": true,
        "Hsts": {
          "Enabled": true
        },
        "Certificate": {
          "Path": "/path/to/certificates/certificate.pfx",
          "Password": "$le_pfx_password"
        }
      }
    },
    "IpWhitelist": $ip_whitelist_json,
    "KnownProxies": $known_proxies_json
  },
  "NetLock_Remote_Server": {
    "Server": "netlock-rmm-server",
    "Port": $web_console_server_port,
    "UseSSL": false
  },
  "NetLock_File_Server": {
    "Server": "netlock-rmm-server",
    "Port": $web_console_server_port,
    "UseSSL": false
  },
  "MySQL": {
    "Server": "mysql8-container",
    "Port": 3306,
    "Database": "netlock",
    "User": "root",
    "Password": "$mysql_password",
    "SslMode": "None",
    "AdditionalConnectionParameters": "AllowPublicKeyRetrieval=True;"
  },
  "LettuceEncrypt": {
    "Enabled": $le_enabled,
    "AcceptTermsOfService": true,
    "DomainNames": [
      "$web_console_domain"
    ],
    "EmailAddress": "$le_email",
    "AllowedChallengeTypes": "Http01",
    "CertificateStoredPfxPassword": "$le_pfx_password"
  },
  "Webinterface": {
    "Title": "$web_console_title",
    "Language": "en-US",
    "Membership_Reminder": true,
    "PublicOverrideUrl": "$publicOverrideUrl"
  },
  "Members_Portal_Api": {
    "Enabled": true,
    "Cloud": false,
    "ApiKeyOverride": "$apiKeyOverride"
  }
}
EOF

echo "appsettings.json for web console was created."

echo "Creating server appsettings.json..."

sudo tee /home/netlock/server/appsettings.json > /dev/null <<EOF
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error",
      "Microsoft.Hosting.Lifetime": "Warning",
      "Microsoft.AspNetCore.SignalR": "Error",
      "Microsoft.AspNetCore.Http.Connections": "Error"
    },
    "Custom": {
      "Enabled": false
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoint": {
      "Http": {
        "Enabled": $server_http_enabled,
        "Port": $server_port_http
      },
      "Https": {
        "Enabled": $server_https_enabled,
        "Port": $server_port_https,
        "Force": $server_https_forced,
        "Hsts": {
          "Enabled": true
        },
        "Certificate": {
          "Path": "/path/to/certificates/certificate.pfx",
          "Password": "$le_pfx_password"
        }
      }
    },
    "Roles": {
      "Comm": true,
      "Update": true,
      "Trust": true,
      "Remote": true,
      "Notification": true,
      "File": true,
      "LLM": true
    }
  },
  "MySQL": {
    "Server": "mysql8-container",
    "Port": 3306,
    "Database": "netlock",
    "User": "root",
    "Password": "$mysql_password",
    "SslMode": "None",
    "AdditionalConnectionParameters": "AllowPublicKeyRetrieval=True;"
  },
  "LettuceEncrypt": {
    "Enabled": $le_enabled,
    "AcceptTermsOfService": true,
    "DomainNames": [
      "$le_domain"
    ],
    "EmailAddress": "$le_email",
    "AllowedChallengeTypes": "Http01",
    "CertificateStoredPfxPassword": "$le_pfx_password"
  },
  "Members_Portal_Api": {
    "Enabled": true,
    "ApiKeyOverride": "$apiKeyOverride"
  },
  "Environment": {
    "Docker": true
  }
}
EOF

echo "appsettings.json for server created."

# Create docker compose yml
sudo touch /home/netlock/docker-compose.yml

sudo tee /home/netlock/docker-compose.yml > /dev/null <<EOF
services:
  mysql:
    image: mysql:8.0
    container_name: mysql8-container
    environment:
      MYSQL_ROOT_PASSWORD: $mysql_password
      MYSQL_DATABASE: netlock
    volumes:
      - /home/netlock/mysql/data:/var/lib/mysql
      - /etc/localtime:/etc/localtime:ro
    ports:
      - "3306:3306"
    networks:
      - netlock-network
    restart: always
    command: --skip-log-bin

  netlock-web-console:
    image: nicomak101/netlock-rmm-web-console:latest
    container_name: netlock-web-console
    environment:
      - TZ=$timezone
    volumes:
      - "/home/netlock/web_console/appsettings.json:/app/appsettings.json"
      - "/home/netlock/web_console/internal:/app/internal"
      - "/home/netlock/web_console/logs:/var/0x101 Cyber Security/NetLock RMM/Web Console/"
      - "/home/netlock/letsencrypt:/app/letsencrypt"
      - /etc/localtime:/etc/localtime:ro
    ports:
      - "80:80"
      - "443:443"
    networks:
      - netlock-network
    restart: always

  netlock-rmm-server:
    image: nicomak101/netlock-rmm-server:latest
    container_name: netlock-rmm-server
    environment:
      - TZ=$timezone
    volumes:
      - "/home/netlock/server/appsettings.json:/app/appsettings.json"
      - "/home/netlock/server/internal:/app/internal"
      - "/home/netlock/server/files:/app/www/private/files"
      - "/home/netlock/server/logs:/var/0x101 Cyber Security/NetLock RMM/Server/"
      - "/home/netlock/letsencrypt:/app/letsencrypt"
      - /etc/localtime:/etc/localtime:ro
    ports:
      - "7080:7080"
      - "7443:7443"
    networks:
      - netlock-network
    restart: always

networks:
  netlock-network:
    driver: bridge
EOF

echo "docker-compose.yml created in /home/netlock"

echo ""

read -p "Everything seems to be ready. Startup NetLock RMM containers now? (Y/n): " start_now
start_now=${start_now:-Y}

if [[ "$start_now" =~ ^[Yy]$ ]]; then
    echo "Starting NetLock containers..."
    sudo docker compose -f /home/netlock/docker-compose.yml up -d
else
    echo "You can start it later with:"
    echo "   sudo docker compose -f /home/netlock/docker-compose.yml up -d"
fi

echo ""

echo "You can access the web console at:"

if [[ "$web_console_https_enabled" == true ]]; then
    echo "  → https://$web_console_domain"
else
    echo "  → http://$web_console_domain"
fi

echo "Depending on your setup, it might take a few minutes until the web console & server is available."
echo ""
echo "If you have any issues, please check the logs with:"
echo "   sudo docker logs netlock-web-console"
echo "   sudo docker logs netlock-rmm-server"
echo ""
echo "If you need help, please join our Discord server: https://discord.gg/HqUpZgtX4U"
echo "If you are a business and need support with your setup, please contact us at: support@0x101-cyber-security.de"
echo ""
echo "Default credentials for web console:"
echo "Username: admin"
echo "Password: admin"
echo "(You will be prompted to change the password on first login.)"
echo ""
echo "Thank you for using NetLock RMM!"
