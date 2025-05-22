#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}===============================================${NC}"
echo -e "${BLUE}  Coinbase Exchange Configuration Update Tool  ${NC}"
echo -e "${BLUE}===============================================${NC}"
echo ""

CONFIG_FILE="coinbase-config.json"

# Check if the user wants to update an existing config or create a new one
read -p "Do you want to update an existing configuration file? (y/n): " update_existing

if [ "$update_existing" = "y" ]; then
    read -p "Enter path to existing configuration file: " config_path
    if [ -f "$config_path" ]; then
        echo -e "${GREEN}Loading existing configuration from $config_path${NC}"
        cp "$config_path" "$CONFIG_FILE"
    else
        echo -e "${RED}File not found. Creating a new configuration.${NC}"
        update_existing="n"
    fi
fi

if [ "$update_existing" = "n" ]; then
    # Create a new configuration template
    echo -e "${YELLOW}Creating a new Coinbase configuration${NC}"
    
    cat > "$CONFIG_FILE" << EOF
{
  "ExchangeId": "coinbase",
  "IsEnabled": true,
  "ApiKey": "",
  "ApiSecret": "",
  "AdditionalAuthParams": {
    "passphrase": ""
  },
  "ApiTimeoutMs": 5000,
  "WebSocketReconnectIntervalMs": 1000,
  "ApiUrl": "https://api.exchange.coinbase.com",
  "WebSocketUrl": "wss://ws-feed.exchange.coinbase.com"
}
EOF
fi

# Read API key if not present
if ! grep -q '"ApiKey": "[^"]\+"' "$CONFIG_FILE"; then
    read -p "Enter Coinbase API Key: " api_key
    sed -i '' -e 's/"ApiKey": ""/"ApiKey": "'"$api_key"'"/' "$CONFIG_FILE"
fi

# Read API secret if not present
if ! grep -q '"ApiSecret": "[^"]\+"' "$CONFIG_FILE"; then
    read -p "Enter Coinbase API Secret: " api_secret
    sed -i '' -e 's/"ApiSecret": ""/"ApiSecret": "'"$api_secret"'"/' "$CONFIG_FILE"
fi

# Check if AdditionalAuthParams exists, add if not
if ! grep -q '"AdditionalAuthParams"' "$CONFIG_FILE"; then
    sed -i '' -e '/"ApiSecret"/a\\
  "AdditionalAuthParams": {\
    "passphrase": ""\
  },' "$CONFIG_FILE"
fi

# Read passphrase
read -p "Enter Coinbase API Passphrase: " passphrase

# Update passphrase
if grep -q '"passphrase": "[^"]*"' "$CONFIG_FILE"; then
    sed -i '' -e 's/"passphrase": "[^"]*"/"passphrase": "'"$passphrase"'"/' "$CONFIG_FILE"
else
    # Add passphrase to AdditionalAuthParams if it doesn't exist
    sed -i '' -e '/"AdditionalAuthParams": {/a\\
    "passphrase": "'"$passphrase"'"' "$CONFIG_FILE"
fi

echo -e "${GREEN}Configuration updated successfully!${NC}"
echo "Configuration saved to $CONFIG_FILE"
echo ""
echo -e "${YELLOW}Next steps:${NC}"
echo "1. Copy this configuration to your application settings"
echo "2. If your application has an API, you can use:"
echo "   curl -X POST -H 'Content-Type: application/json' -d @$CONFIG_FILE http://localhost:5001/api/configuration/exchange"
echo "3. Restart your application to apply the changes"
echo ""
echo "For more information, consult the application documentation." 