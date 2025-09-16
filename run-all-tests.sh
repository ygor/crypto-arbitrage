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
    echo "  Crypto Arbitrage Test Runner - ALL TESTS"
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

# Function to show help
show_help() {
    echo "Usage: ./run-all-tests.sh [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -h, --help              Show this help message"
    echo "  --include-skipped       Run skipped tests by temporarily removing Skip attributes"
    echo "                          (Warning: This will modify test files but restore them after)"
    echo "  --backend-only          Run only backend tests (CryptoArbitrage.Tests project)"
    echo "  --ui-only               Run only UI tests (CryptoArbitrage.UI.Tests project)"
    echo "  --integration-only      Run only separate integration tests"
    echo ""
    echo "Examples:"
    echo "  ./run-all-tests.sh                     # Run ALL test projects (recommended)"
    echo "  ./run-all-tests.sh --backend-only      # Run only main backend tests"
    echo "  ./run-all-tests.sh --ui-only           # Run only UI tests"
    echo "  ./run-all-tests.sh --include-skipped   # Run all tests including skipped ones"
    echo ""
}

# Default values
INCLUDE_SKIPPED=""
BACKEND_ONLY=""
UI_ONLY=""
INTEGRATION_ONLY=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -h|--help)
            show_help
            exit 0
            ;;
        --include-skipped)
            INCLUDE_SKIPPED="--include-skipped"
            shift
            ;;
        --backend-only)
            BACKEND_ONLY="true"
            shift
            ;;
        --ui-only)
            UI_ONLY="true"
            shift
            ;;
        --integration-only)
            INTEGRATION_ONLY="true"
            shift
            ;;
        *)
            echo "Unknown option: $1"
            show_help
            exit 1
            ;;
    esac
done

# Main execution
print_banner

# Set start time
start_time=$(date +%s)

# Get root directory
ROOT_DIR="$(pwd)"
BACKEND_RESULT=0
UI_RESULT=0
INTEGRATION_RESULT=0
TOTAL_TESTS=0
PASSED_TESTS=0

# Function to run tests for a specific project
run_test_project() {
    local project_name="$1"
    local project_path="$2"
    local description="$3"
    
    print_section "Running $description"
    
    if [ ! -d "$ROOT_DIR/backend/$project_path" ]; then
        print_warning "$description project not found at $project_path - skipping"
        return 0
    fi
    
    cd "$ROOT_DIR/backend"
    
    # Check if project exists and has tests
    local test_count=$(dotnet test "$project_path" --list-tests 2>/dev/null | grep -c "^    " || echo "0")
    
    if [ "$test_count" -eq 0 ]; then
        print_warning "No tests found in $description - skipping"
        return 0
    fi
    
    echo "Found $test_count tests in $description"
    
    # Run the tests
    if [ "$project_name" = "backend" ] && [ -n "$INCLUDE_SKIPPED" ]; then
        # Use the existing backend script with skipped tests for main backend project
        ./run-tests.sh $INCLUDE_SKIPPED
        local result=$?
    else
        # Run tests directly for other projects
        echo "Running: dotnet test $project_path --verbosity normal"
        dotnet test "$project_path" --verbosity normal
        local result=$?
    fi
    
    if [ $result -eq 0 ]; then
        print_success "$description completed successfully"
        PASSED_TESTS=$((PASSED_TESTS + test_count))
    else
        print_error "$description failed"
    fi
    
    TOTAL_TESTS=$((TOTAL_TESTS + test_count))
    return $result
}

# Determine which tests to run
if [ -n "$BACKEND_ONLY" ]; then
    print_section "Running Backend Tests Only"
    run_test_project "backend" "tests/CryptoArbitrage.Tests" "Backend Tests (Business Behavior, Contract, Unit, Integration, E2E)"
    BACKEND_RESULT=$?
elif [ -n "$UI_ONLY" ]; then
    print_section "Running UI Tests Only"
    run_test_project "ui" "tests/CryptoArbitrage.UI.Tests" "UI Tests (Blazor Components)"
    UI_RESULT=$?
elif [ -n "$INTEGRATION_ONLY" ]; then
    print_section "Running Integration Tests Only"
    run_test_project "integration" "tests/CryptoArbitrage.IntegrationTests" "Separate Integration Tests"
    INTEGRATION_RESULT=$?
else
    # Run all test projects
    print_section "Running ALL Test Projects"
    
    # 1. Main Backend Tests (CryptoArbitrage.Tests) - 168 tests
    run_test_project "backend" "tests/CryptoArbitrage.Tests" "Backend Tests (Business Behavior, Contract, Unit, Integration, E2E)"
    BACKEND_RESULT=$?
    
    # 2. UI Tests (CryptoArbitrage.UI.Tests) - ~22 tests  
    run_test_project "ui" "tests/CryptoArbitrage.UI.Tests" "UI Tests (Blazor Components)"
    UI_RESULT=$?
    
    # 3. Separate Integration Tests (CryptoArbitrage.IntegrationTests)
    run_test_project "integration" "tests/CryptoArbitrage.IntegrationTests" "Separate Integration Tests"
    INTEGRATION_RESULT=$?
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

if [ -n "$BACKEND_ONLY" ]; then
    if [ $BACKEND_RESULT -eq 0 ]; then
        print_success "Backend Tests: PASSED"
    else
        print_error "Backend Tests: FAILED"
    fi
elif [ -n "$UI_ONLY" ]; then
    if [ $UI_RESULT -eq 0 ]; then
        print_success "UI Tests: PASSED"
    else
        print_error "UI Tests: FAILED"
    fi
elif [ -n "$INTEGRATION_ONLY" ]; then
    if [ $INTEGRATION_RESULT -eq 0 ]; then
        print_success "Integration Tests: PASSED"
    else
        print_error "Integration Tests: FAILED"
    fi
else
    # Show results for all projects
    if [ $BACKEND_RESULT -eq 0 ]; then
        print_success "Backend Tests: PASSED"
    else
        print_error "Backend Tests: FAILED"
    fi
    
    if [ $UI_RESULT -eq 0 ]; then
        print_success "UI Tests: PASSED"
    else
        print_error "UI Tests: FAILED"
    fi
    
    if [ $INTEGRATION_RESULT -eq 0 ]; then
        print_success "Integration Tests: PASSED"
    else
        print_error "Integration Tests: FAILED"
    fi
fi

echo ""
print_section "Overall Results"
echo -e "Total tests discovered: ${BLUE}$TOTAL_TESTS${NC}"
echo -e "Tests passed: ${GREEN}$PASSED_TESTS${NC}"

if [ $TOTAL_TESTS -gt 0 ]; then
    failed_tests=$((TOTAL_TESTS - PASSED_TESTS))
    if [ $failed_tests -gt 0 ]; then
        echo -e "Tests failed: ${RED}$failed_tests${NC}"
    fi
fi

# Determine overall exit code
OVERALL_RESULT=0

if [ -n "$BACKEND_ONLY" ]; then
    OVERALL_RESULT=$BACKEND_RESULT
elif [ -n "$UI_ONLY" ]; then
    OVERALL_RESULT=$UI_RESULT
elif [ -n "$INTEGRATION_ONLY" ]; then
    OVERALL_RESULT=$INTEGRATION_RESULT
else
    # Any test project failure means overall failure
    if [ $BACKEND_RESULT -ne 0 ] || [ $UI_RESULT -ne 0 ] || [ $INTEGRATION_RESULT -ne 0 ]; then
        OVERALL_RESULT=1
    fi
fi

# Final result
if [ $OVERALL_RESULT -eq 0 ]; then
    print_success "All tests completed successfully! 🎉"
    echo -e "${GREEN}Ready for production deployment${NC}"
else
    print_error "Some tests failed"
    echo -e "${RED}Please fix failing tests before proceeding${NC}"
fi

exit $OVERALL_RESULT 