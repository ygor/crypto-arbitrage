#!/bin/bash

# Crypto Arbitrage Test Runner Script
# This script runs tests for the Crypto Arbitrage application

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
    echo "  Crypto Arbitrage Tests"
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

# Function to show help
show_help() {
    echo "Usage: ./run-tests.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help                  Show this help message"
    echo "  -v, --verbose               Show verbose output"
    echo "  -f, --filter FILTER         Run tests matching the filter expression"
    echo "  -p, --project PROJECT       Run tests from a specific project (defaults to tests/CryptoArbitrage.Tests)"
    echo "  --no-restore                Skip the restore step"
    echo "  --no-build                  Skip the build step"
    echo "  --coverage                  Generate code coverage report"
    echo ""
    echo "Examples:"
    echo "  ./run-tests.sh                           # Run all tests"
    echo "  ./run-tests.sh --filter UnitTests        # Run tests with 'UnitTests' in the name"
    echo "  ./run-tests.sh --coverage                # Run tests with code coverage"
    echo ""
}

# Default values
VERBOSE=""
FILTER=""
PROJECT="tests/CryptoArbitrage.Tests"
RESTORE="true"
BUILD="true"
COVERAGE=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        -v|--verbose)
            VERBOSE="--verbosity detailed"
            shift
            ;;
        -f|--filter)
            FILTER="--filter $2"
            shift 2
            ;;
        -p|--project)
            PROJECT="$2"
            shift 2
            ;;
        --no-restore)
            RESTORE="false"
            shift
            ;;
        --no-build)
            BUILD="false"
            shift
            ;;
        --coverage)
            COVERAGE="--collect:\"XPlat Code Coverage\""
            shift
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Print banner
print_banner

# Set start time
start_time=$(date +%s)

# Restore packages if needed
if [ "$RESTORE" = "true" ]; then
    print_section "Restoring packages"
    dotnet restore
    if [ $? -ne 0 ]; then
        print_error "Package restore failed"
        exit 1
    else
        print_success "Packages restored successfully"
    fi
fi

# Build solution if needed
if [ "$BUILD" = "true" ]; then
    print_section "Building solution"
    dotnet build --no-restore -c Debug
    if [ $? -ne 0 ]; then
        print_error "Build failed"
        exit 1
    else
        print_success "Build completed successfully"
    fi
fi

# Run tests
print_section "Running tests from $PROJECT project"
echo -e "Command: dotnet test $PROJECT $VERBOSE $FILTER $COVERAGE\n"

dotnet test $PROJECT $VERBOSE $FILTER $COVERAGE

# Check test result
if [ $? -ne 0 ]; then
    print_error "Tests failed"
    TEST_RESULT=1
else
    print_success "All tests passed"
    TEST_RESULT=0
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

exit $TEST_RESULT 