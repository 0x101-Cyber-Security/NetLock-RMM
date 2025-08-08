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

# Ask for deployment directory
echo "Where would you like to deploy NetLock RMM?"
read -p "Deployment directory (default: /opt/netlockrmm): " deployment_dir
if [[ -z "$deployment_dir" ]]; then
    deployment_dir="/opt/netlockrmm"
    echo "Using default deployment directory: $deployment_dir"
else
    # Remove trailing slash if present
    deployment_dir="${deployment_dir%/}"
    echo "Using deployment directory: $deployment_dir"
fi

# Validate deployment directory
if [[ ! "$deployment_dir" = /* ]]; then
    echo "Error: Deployment directory must be an absolute path (starting with /)"
    exit 1
fi

echo ""

# Set time zone
echo "Current system timezone: $(timedatectl show --property=Timezone --value)"
read -p "Do you want to keep the current timezone? (Y/n): " keep_timezone
keep_timezone=${keep_timezone:-Y}

if [[ "$keep_timezone" =~ ^[Yy]$ ]]; then
    timezone=$(timedatectl show --property=Timezone --value)
    echo "Using current timezone: $timezone"
else
    echo "Enter your timezone (e.g., Europe/Berlin, America/New_York, Asia/Tokyo):"
    echo "You can find available timezones with: timedatectl list-timezones"
    read -p "Timezone: " timezone
    
    if [[ -z "$timezone" ]]; then
        timezone="Europe/Berlin"
        echo "No timezone entered, defaulting to $timezone"
    fi
    
    # Validate timezone
    if timedatectl list-timezones | grep -q "^$timezone$"; then
        echo "Setting timezone to $timezone..."
        sudo timedatectl set-timezone "$timezone"
    else
        echo "Invalid timezone. Using current system timezone."
        timezone=$(timedatectl show --property=Timezone --value)
    fi
fi

echo ""

# Ask the user for the MySQL root password
read -p "Please enter the MySQL root password (leave blank to auto-generate): " mysql_password
if [[ -z "$mysql_password" ]]; then
    # Generate a random 16-character password using UUID
    mysql_password=$(uuidgen | tr -d '-' | head -c 16)
    echo "Auto-generated MySQL password: $mysql_password"
    echo "Please save this password securely!"
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
while [[ -z "$apiKeyOverride" ]]; do
    read -p "Please provide your members portal api key now (for OSS & Pro): " apiKeyOverride
    apiKeyOverride=$(echo "$apiKeyOverride" | xargs)  # removes leading/trailing whitespace
    
    if [[ -z "$apiKeyOverride" ]]; then
        echo "API key is required to continue. Please enter a valid API key."
    fi
done

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

    echo "Make sure the following ports are available and not in use by other applications:"
    echo "80 (HTTP) and 443 (HTTPS) for web console access. Port 80 is used for Let's Encrypt certificate generation."
    echo "7443 for the NetLock RMM server (remote access, file transfer, etc.)."
    echo ""
    echo "Note: Docker Compose will handle port binding automatically. You only need to:"
    echo "- Ensure these ports are not used by other services (check with: sudo netstat -tlnp | grep :80)"
    echo "- If using cloud hosting (AWS, Azure, etc.), configure your security groups/firewall to allow these ports"
    echo "- Local firewalls (ufw, iptables) don't affect Docker port mappings"

    # Confirm the ports
    read -p "Are these ports available and cloud firewall configured (if applicable)? (Y/n): " ports_open
    ports_open=${ports_open:-Y}

    if [[ ! "$ports_open" =~ ^[Yy]$ ]]; then
        echo "Please ensure ports are available and configure your cloud firewall before continuing."
        exit 1
    fi

    # Set the web console and server ports
    web_console_port_http=80
    web_console_http_enabled=true
    web_console_port_https=443
    web_console_https_enabled=true

    web_console_server_port=7443
    web_console_server_ssl=true
    web_console_cert_path="/app/letsencrypt/certs/certificate.pfx"
    web_console_server_host="$server_domain"

    server_http_enabled=true
    server_port_http=7080
    
    server_https_enabled=true
    server_port_https=7443
    server_https_forced=false # No forced HTTPS because of the internal server communication

    # Docker port bindings for direct access (no reverse proxy)
    web_console_docker_ports="
      - \"80:80\"
      - \"443:443\""
    server_docker_ports="
      - \"7443:7443\""

    # Set the public override URL
    publicOverrideUrl="https://$server_domain:$server_port_https"

elif [[ "$setup_option" == "2" ]]; then # Reverse Proxy
    echo "You chose: Reverse Proxy (nginx, traefik, caddy, etc.)"

    # Get internal ip of the server
    server_ip=$(hostname -I | awk '{print $1}')

    read -p "Is this the server where your reverse proxy is running? (Y/n): " reverse_proxy_server
    reverse_proxy_server=${reverse_proxy_server:-Y}
    echo ""
    read -p "Please enter the IP of the server where your reverse proxy is running (default: 127.0.0.1): " reverse_proxy_ip
    if [[ -z "$reverse_proxy_ip" ]]; then
        reverse_proxy_ip="127.0.0.1"
        echo "Using default reverse proxy IP: $reverse_proxy_ip"
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
        read -p "Please enter the correct internal IP of this server (default: 127.0.0.1): " server_ip
        if [[ -z "$server_ip" ]]; then
            server_ip="127.0.0.1"
            echo "Using default server IP: $server_ip"
        fi
    fi

    # If the reverse proxy is on the same server, we can use localhost for forwarding, otherwise we use the server's IP
    if [[ "$reverse_proxy_server" =~ ^[Yy]$ ]]; then
        reverse_proxy_forward_host="localhost"
    else
        reverse_proxy_forward_host="$server_ip"
    fi

    echo "For the web console domain forward like this:"
    echo "$web_console_domain: https -> $server_ip:8880"
    echo ""
    echo "For the server domain forward like this:"
    echo "$server_domain: https -> $server_ip:7080"

    echo ""

    # Ask the user to confirm the reverse proxy setup
    read -p "Have you set up the reverse proxy to forward the domains $web_console_domain and $server_domain to the internal IP of this server? (Y/n): " reverse_proxy
    reverse_proxy=${reverse_proxy:-Y}

    if [[ ! "$reverse_proxy" =~ ^[Yy]$ ]]; then
        echo "Please set up the reverse proxy and try again."
        exit 1
    fi

    # Set the web console and server ports for reverse proxy
    web_console_port_http=80
    web_console_http_enabled=true
    web_console_port_https=443
    web_console_https_enabled=false

    web_console_server_port=7080
    web_console_server_ssl=false
    web_console_cert_path="/path/to/certificates/certificate.pfx"
    web_console_server_host="netlock-rmm-server"

    server_http_enabled=true
    server_port_http=7080

    server_https_enabled=false
    server_port_https=7443
    server_https_forced=false # No forced HTTPS because of the internal server communication

    # Docker port bindings for reverse proxy
    web_console_docker_ports="
      - \"127.0.0.1:8880:80\""
    server_docker_ports="
      - \"127.0.0.1:7080:7080\""

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
    web_console_server_ssl=false
    web_console_cert_path="/path/to/certificates/certificate.pfx"
    web_console_server_host="netlock-rmm-server"

    server_http_enabled=true
    server_port_http=7080

    server_https_enabled=false
    server_port_https=7443
    server_https_forced=false

    # Docker port bindings for local test environment (direct access, no SSL)
    web_console_docker_ports="
      - \"80:80\""
    server_docker_ports="
      - \"7080:7080\""

    # No reverse proxy settings for local test
    known_proxies_json="[]"
    
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
sudo mkdir -p "$deployment_dir"/{mysql/data,letsencrypt,web_console/internal,web_console/logs,server/logs,server/files,server/internal}

# Create empty appsettings files
sudo touch "$deployment_dir"/web_console/appsettings.json
sudo touch "$deployment_dir"/server/appsettings.json

echo "Creating web console appsettings.json..."

# Export variables for envsubst
export mysql_password web_console_http_enabled web_console_port_http web_console_https_enabled web_console_port_https
export web_console_server_port web_console_server_ssl web_console_cert_path web_console_server_host server_http_enabled server_port_http server_https_enabled server_port_https server_https_forced
export ip_whitelist_json known_proxies_json le_enabled web_console_domain server_domain le_email le_pfx_password
export web_console_title publicOverrideUrl apiKeyOverride

# Use template file and substitute variables
envsubst < "$SCRIPT_DIR/template_web-console-appsettings.json" | sudo tee "$deployment_dir"/web_console/appsettings.json > /dev/null

echo "appsettings.json for web console was created."

echo "Creating server appsettings.json..."

# Use template file and substitute variables
envsubst < "$SCRIPT_DIR/template_server-appsettings.json" | sudo tee "$deployment_dir"/server/appsettings.json > /dev/null

echo "appsettings.json for server created."

# Create docker compose yml
echo "Creating docker-compose.yml..."

# Export additional variables for docker-compose
export timezone web_console_docker_ports server_docker_ports

# Use template file and substitute variables
envsubst < "$SCRIPT_DIR/template_docker-compose.yml" | sudo tee "$deployment_dir"/docker-compose.yml > /dev/null

echo "docker-compose.yml created in $deployment_dir"

echo ""

read -p "Everything seems to be ready. Startup NetLock RMM containers now? (Y/n): " start_now
start_now=${start_now:-Y}

if [[ "$start_now" =~ ^[Yy]$ ]]; then
    echo "Starting NetLock containers..."
    sudo docker compose -f "$deployment_dir"/docker-compose.yml up -d
    
    # For Let's Encrypt setup, check containers and run certificate update script
    if [[ "$le_enabled" == "true" ]]; then
        echo ""
        echo "Checking if containers started successfully..."
        sleep 5
        
        # Check if containers are running
        WEB_CONSOLE_RUNNING=$(sudo docker ps --filter "name=netlock-web-console" --filter "status=running" -q)
        SERVER_RUNNING=$(sudo docker ps --filter "name=netlock-rmm-server" --filter "status=running" -q)
        MYSQL_RUNNING=$(sudo docker ps --filter "name=mysql8-container" --filter "status=running" -q)
        
        if [[ -z "$WEB_CONSOLE_RUNNING" ]] || [[ -z "$SERVER_RUNNING" ]] || [[ -z "$MYSQL_RUNNING" ]]; then
            echo ""
            echo "⚠️  WARNING: Some containers failed to start properly!"
            echo ""
            echo "Check container status with:"
            echo "   sudo docker ps -a"
            echo ""
            echo "Check container logs with:"
            echo "   sudo docker logs netlock-web-console"
            echo "   sudo docker logs netlock-rmm-server" 
            echo "   sudo docker logs mysql8-container"
            echo ""
            echo "After fixing any issues, you can manually run the certificate update script:"
            
            # Copy the script anyway for manual use
            sudo cp "$SCRIPT_DIR/02_update-ssl-certificates.sh" "$deployment_dir/"
            sudo chmod +x "$deployment_dir/02_update-ssl-certificates.sh"
            echo "   sudo $deployment_dir/02_update-ssl-certificates.sh $deployment_dir"
            echo ""
        else
            echo "✅ All containers are running successfully!"
            echo ""
            echo "Copying certificate update script..."
            sudo cp "$SCRIPT_DIR/02_update-ssl-certificates.sh" "$deployment_dir/"
            sudo chmod +x "$deployment_dir/02_update-ssl-certificates.sh"
            
            echo "Running certificate update script..."
            sudo "$deployment_dir/02_update-ssl-certificates.sh" "$deployment_dir"
        fi
    fi
else
    echo "You can start it later with:"
    echo "   sudo docker compose -f \"$deployment_dir\"/docker-compose.yml up -d"
    
    # Copy the certificate update script for later use
    if [[ "$le_enabled" == "true" ]]; then
        echo ""
        echo "Copying certificate update script for when you start the containers..."
        sudo cp "$SCRIPT_DIR/02_update-ssl-certificates.sh" "$deployment_dir/"
        sudo chmod +x "$deployment_dir/02_update-ssl-certificates.sh"
        echo "After starting containers, run: sudo $deployment_dir/02_update-ssl-certificates.sh $deployment_dir"
    fi
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
