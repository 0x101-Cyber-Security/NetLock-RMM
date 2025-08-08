#!/bin/bash

# Configuration
DEPLOYMENT_DIR="${1:-/opt/netlockrmm}"
WEB_CONSOLE_APPSETTINGS="$DEPLOYMENT_DIR/web_console/appsettings.json"
SERVER_APPSETTINGS="$DEPLOYMENT_DIR/server/appsettings.json"
CERT_DIR="$DEPLOYMENT_DIR/letsencrypt/certs"

echo "Waiting 30 seconds for containers to start and Let's Encrypt to generate certificate..."
sleep 30

echo "Looking for Let's Encrypt certificate in $CERT_DIR..."

# Wait up to 5 minutes for certificate to appear
TIMEOUT=300  # 5 minutes
ELAPSED=0
INTERVAL=15

while [ $ELAPSED -lt $TIMEOUT ]; do
    # Find the first .pfx file
    CERT_FILE=$(find "$CERT_DIR" -name "*.pfx" -type f | head -1)
    
    if [ -n "$CERT_FILE" ]; then
        echo "Certificate found: $(basename "$CERT_FILE")"
        
        # Update both appsettings.json files with the actual certificate path
        CERT_FILENAME=$(basename "$CERT_FILE")
        NEW_CERT_PATH="/app/letsencrypt/certs/$CERT_FILENAME"
        
        echo "Updating certificate path in both appsettings files..."
        
        # Create backups
        cp "$WEB_CONSOLE_APPSETTINGS" "$WEB_CONSOLE_APPSETTINGS.backup"
        cp "$SERVER_APPSETTINGS" "$SERVER_APPSETTINGS.backup"
        
        # Replace the certificate path in web console appsettings
        echo "Updating web console appsettings.json..."
        sed -i "s|\"Path\": \"/app/letsencrypt/certs/certificate.pfx\"|\"Path\": \"$NEW_CERT_PATH\"|g" "$WEB_CONSOLE_APPSETTINGS"
        WEB_CONSOLE_UPDATE=$?
        
        # Replace the certificate path in server appsettings  
        echo "Updating server appsettings.json..."
        sed -i "s|\"Path\": \"/path/to/certificates/certificate.pfx\"|\"Path\": \"$NEW_CERT_PATH\"|g" "$SERVER_APPSETTINGS"
        SERVER_UPDATE=$?
        
        if [ $WEB_CONSOLE_UPDATE -eq 0 ] && [ $SERVER_UPDATE -eq 0 ]; then
            echo "Certificate path updated successfully in both files to: $NEW_CERT_PATH"
            
            # Restart both containers to pick up the new certificate path
            echo "Restarting containers..."
            docker compose -f "$DEPLOYMENT_DIR/docker-compose.yml" restart netlock-web-console netlock-rmm-server
            
            echo "Certificate configuration completed!"
            exit 0
        else
            echo "Error: Failed to update one or both appsettings.json files"
            echo "Web console update result: $WEB_CONSOLE_UPDATE"
            echo "Server update result: $SERVER_UPDATE"
            exit 1
        fi
    fi
    
    echo "Certificate not found yet, waiting $INTERVAL more seconds... ($(((TIMEOUT-ELAPSED)/60)) minutes remaining)"
    sleep $INTERVAL
    ELAPSED=$((ELAPSED + INTERVAL))
done

echo "Timeout: Certificate was not generated within $((TIMEOUT/60)) minutes"
echo "Please check the containers logs:"
echo "  docker logs netlock-web-console"
echo "  docker logs netlock-rmm-server"
exit 1