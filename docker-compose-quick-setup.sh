#!/bin/bash
set -e

echo "Create directories..."

# Directories
sudo mkdir -p /home/netlock/{mysql/data,letsencrypt,web_console/internal,web_console/logs,server/logs,server/files,server/internal}

# Create empty appsettings files
sudo touch /home/netlock/web_console/appsettings.json
sudo touch /home/netlock/server/appsettings.json

# Set time zone
echo "Please choose your timezone:"
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
select timezone in "${options[@]}"; do
    if [[ -n "$timezone" ]]; then
        echo "Setting timezone to $timezone..."
        sudo timedatectl set-timezone "$timezone"
        break
    else
        echo "Invalid selection. Please try again."
    fi
done

# Abfragen
read -p "Please enter the domain you will use (example: demo.netlockrmm.com): " le_domain

read -p "Webconsole HTTP Port (press enter for default: 80): " web_port_http
web_port_http=${web_port_http:-80}

read -p "Webconsole HTTP(s) Port (press enter for default: 443): " web_port_https
web_port_https=${web_port_https:-443}

read -p "Remote & File Server Port (press Enter for default: 7443): " port_input
remote_port=${port_input:-7443}
file_port=${port_input:-7443}

read -p "Provide the MySQL root password you wish to use: " mysql_password

read -p "Email address for Let's Encrypt: " le_email
read -p "Password for the Let's Encrypt .pfx certificate: " le_pfx_password

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
        "Enabled": true,
        "Port": $web_port_http
      },
      "Https": {
        "Enabled": true,
        "Port": $web_port_https,
        "Force": true,
        "Hsts": {
          "Enabled": true
        },
        "Certificate": {
          "Path": "/path/to/certificates/certificate.pfx",
          "Password": "$le_pfx_password"
        }
      }
    }
  },
  "NetLock_Remote_Server": {
    "Server": "$le_domain",
    "Port": $remote_port,
    "UseSSL": true
  },
  "NetLock_File_Server": {
    "Server": "$le_domain",
    "Port": $file_port,
    "UseSSL": true
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
    "Enabled": true,
    "AcceptTermsOfService": true,
    "DomainNames": [
      "$le_domain"
    ],
    "EmailAddress": "$le_email",
    "AllowedChallengeTypes": "Http01",
    "CertificateStoredPfxPassword": "$le_pfx_password"
  },
  "Webinterface": {
    "Title": "Your company name",
    "Language": "en-US",
    "Membership_Reminder": true
  },
  "Members_Portal_Api": {
    "Enabled": true,
    "Cloud": false
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
        "Enabled": true,
        "Port": 7080
      },
      "Https": {
        "Enabled": true,
        "Port": $remote_port,
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
    "Enabled": true,
    "AcceptTermsOfService": true,
    "DomainNames": [
      "$le_domain"
    ],
    "EmailAddress": "$le_email",
    "AllowedChallengeTypes": "Http01",
    "CertificateStoredPfxPassword": "$le_pfx_password"
  },
  "Members_Portal_Api": {
    "Enabled": true
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
    volumes:
      - "/home/netlock/web_console/appsettings.json:/app/appsettings.json"
      - "/home/netlock/web_console/internal:/app/internal"
      - "/home/netlock/web_console/logs:/var/0x101 Cyber Security/NetLock RMM/Web Console/"
      - "/home/netlock/letsencrypt:/app/letsencrypt"
      - /etc/localtime:/etc/localtime:ro
    ports:
      - "$web_port_http:$web_port_http"
      - "$web_port_https:$web_port_https"
    networks:
      - netlock-network
    restart: always

  netlock-rmm-server:
    image: nicomak101/netlock-rmm-server:latest
    container_name: netlock-rmm-server
    volumes:
      - "/home/netlock/server/appsettings.json:/app/appsettings.json"
      - "/home/netlock/server/internal:/app/internal"
      - "/home/netlock/server/files:/app/www/private/files"
      - "/home/netlock/server/logs:/var/0x101 Cyber Security/NetLock RMM/Server/"
      - "/home/netlock/letsencrypt:/app/letsencrypt"
      - /etc/localtime:/etc/localtime:ro
    ports:
      - "7080:7080"
      - "$remote_port:$remote_port"
    networks:
      - netlock-network
    restart: always

networks:
  netlock-network:
    driver: bridge
EOF

echo "docker-compose.yml created in /home/netlock"

read -p "Start NetLock now? (Y/n): " start_now
start_now=${start_now:-Y}

if [[ "$start_now" =~ ^[Yy]$ ]]; then
    echo "Starting NetLock containers..."
    sudo docker compose -f /home/netlock/docker-compose.yml up -d
else
    echo "You can start it later with:"
    echo "   sudo docker compose -f /home/netlock/docker-compose.yml up -d"
fi

echo "Default credentials for web console:"
echo "Username: admin"
echo "Password: admin"

read -p "Now make sure you set your members portal api key in the web console under: System -> Settings. Once you have done that, confirm. (Y/n): " api_key_set
api_key_set=${api_key_set:-Y}

if [[ "$api_key_set" =~ ^[Yy]$ ]]; then
    echo "Restarting NetLock RMM containers..."
    sudo docker compose -f /home/netlock/docker-compose.yml down && docker compose -f /home/netlock/docker-compose.yml up -d
else
    echo "Please restart the containers after setting the members portal api key:"
    echo "   sudo docker compose -f /home/netlock/docker-compose.yml down && docker compose -f /home/netlock/docker-compose.yml up -d"
fi

echo "NetLock RMM is ready now. It might take a few minutes until the server backend comes up. Make sure to open the web console in a new browser tab now. Happy monitoring!"