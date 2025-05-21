#!/bin/bash

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Function to print banner
print_banner() {
    echo -e "${BLUE}"
    echo "====================================================="
    echo "  Crypto Arbitrage Test Runner"
    echo "====================================================="
    echo -e "${NC}"
}

# Function to print section header
print_section() {
    echo -e "\n${YELLOW}>> $1${NC}\n"
}

# Function to print success message
print_success() {
    echo -e "${GREEN}✓ $1${NC}"
}

# Function to print error message
print_error() {
    echo -e "${RED}✗ $1${NC}"
}

# Function to print warning message
print_warning() {
    echo -e "${YELLOW}! $1${NC}"
}

# Main execution
print_banner

# Set start time
start_time=$(date +%s)

# Get root directory
ROOT_DIR="$(pwd)"
BACKEND_RESULT=0
FRONTEND_RESULT=0

# Run backend tests
print_section "Running Backend Tests"
cd "$ROOT_DIR/backend"
./run-tests.sh
BACKEND_RESULT=$?

if [ $BACKEND_RESULT -eq 0 ]; then
    print_success "Backend tests completed successfully"
else
    print_error "Backend tests failed"
fi

# Run frontend tests
print_section "Running Frontend Tests"
cd "$ROOT_DIR/frontend"
npm test -- --watchAll=false
FRONTEND_RESULT=$?

if [ $FRONTEND_RESULT -eq 0 ]; then
    print_success "Frontend tests completed successfully"
else
    print_error "Frontend tests failed"
fi

# Calculate execution time
end_time=$(date +%s)
execution_time=$((end_time - start_time))
minutes=$((execution_time / 60))
seconds=$((execution_time % 60))

echo ""
echo -e "${BLUE}====================================================="
echo "  Test execution completed in ${minutes}m ${seconds}s"
echo "=====================================================${NC}"

# Print summary
print_section "Test Summary"
if [ $BACKEND_RESULT -eq 0 ]; then
    print_success "Backend: PASSED"
else
    print_error "Backend: FAILED"
fi

if [ $FRONTEND_RESULT -eq 0 ]; then
    print_success "Frontend: PASSED"
else
    print_error "Frontend: FAILED"
fi

# Exit with failure if any test suite failed
if [ $BACKEND_RESULT -ne 0 ] || [ $FRONTEND_RESULT -ne 0 ]; then
    print_error "One or more test suites failed"
    exit 1
else
    print_success "All test suites passed"
    exit 0
fi 