#!/bin/bash

# Color definitions
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Controllers to check
CONTROLLERS=("Arbitrage" "Trades" "Settings" "Statistics" "Health")

# Special case for Opportunities controller which is in ArbitrageController.cs
SPECIAL_CONTROLLERS=("Opportunities")

# Directory where controllers are located
CONTROLLERS_DIR="Controllers"

# Directory where interfaces are located
INTERFACES_DIR="Controllers/Generated"

echo -e "${BLUE}Verifying controller interfaces implementation...${NC}"

# Success counter
SUCCESS_COUNT=0
FAILURE_COUNT=0

# Create output directory if it doesn't exist
mkdir -p "$INTERFACES_DIR"

# Check each controller
for CONTROLLER in "${CONTROLLERS[@]}"
do
    CONTROLLER_FILE="$CONTROLLERS_DIR/${CONTROLLER}Controller.cs"
    INTERFACE_FILE="$INTERFACES_DIR/I${CONTROLLER}Controller.g.cs"
    
    # Check if controller file exists
    if [ ! -f "$CONTROLLER_FILE" ]; then
        echo -e "${RED}Controller file not found: $CONTROLLER_FILE${NC}"
        ((FAILURE_COUNT++))
        continue
    fi
    
    # Check if controller implements interface
    if grep -q "I${CONTROLLER}Controller" "$CONTROLLER_FILE"; then
        echo -e "${GREEN}✓ ${CONTROLLER}Controller implements I${CONTROLLER}Controller${NC}"
        ((SUCCESS_COUNT++))
    else
        echo -e "${RED}✗ ${CONTROLLER}Controller does not implement I${CONTROLLER}Controller${NC}"
        ((FAILURE_COUNT++))
    fi
done

# Check special controllers
for CONTROLLER in "${SPECIAL_CONTROLLERS[@]}"
do
    # For OpportunitiesController, check in ArbitrageController.cs
    if [ "$CONTROLLER" == "Opportunities" ]; then
        CONTROLLER_FILE="$CONTROLLERS_DIR/ArbitrageController.cs"
    else
        CONTROLLER_FILE="$CONTROLLERS_DIR/${CONTROLLER}Controller.cs"
    fi
    
    # Check if controller file exists
    if [ ! -f "$CONTROLLER_FILE" ]; then
        echo -e "${RED}Controller file not found: $CONTROLLER_FILE${NC}"
        ((FAILURE_COUNT++))
        continue
    fi
    
    # Check if controller implements interface
    if grep -q "I${CONTROLLER}Controller" "$CONTROLLER_FILE"; then
        echo -e "${GREEN}✓ ${CONTROLLER}Controller implements I${CONTROLLER}Controller${NC}"
        ((SUCCESS_COUNT++))
    else
        echo -e "${RED}✗ ${CONTROLLER}Controller does not implement I${CONTROLLER}Controller${NC}"
        ((FAILURE_COUNT++))
    fi
done

# Print summary
echo -e "\n${BLUE}Interface Verification Summary:${NC}"
echo -e "${GREEN}✓ $SUCCESS_COUNT controllers implement their interfaces${NC}"
if [ $FAILURE_COUNT -gt 0 ]; then
    echo -e "${RED}✗ $FAILURE_COUNT controllers do not implement their interfaces${NC}"
    echo -e "${YELLOW}Please check the error messages above and fix the issues.${NC}"
else
    echo -e "${GREEN}All controllers implement their interfaces correctly!${NC}"
fi

exit $FAILURE_COUNT 