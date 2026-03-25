#!/bin/bash

# DeFi Dashboard Server Endpoint Testing Script
# Usage: ./scripts/test-endpoints.sh
# Prerequisites: Server must be running on localhost:5000

set -e

BASE_URL="http://localhost:5000"
PASS_COUNT=0
FAIL_COUNT=0

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Helper function to test endpoint
test_endpoint() {
  local method=$1
  local path=$2
  local expected_status=$3
  local description=$4

  echo -n "Testing ${method} ${path}... "
  
  local response=$(curl -s -w "\n%{http_code}" "${BASE_URL}${path}")
  local body=$(echo "$response" | head -n -1)
  local status=$(echo "$response" | tail -n 1)

  if [ "$status" = "$expected_status" ]; then
    echo -e "${GREEN}✓ PASS${NC} (HTTP $status)"
    PASS_COUNT=$((PASS_COUNT + 1))
    
    # Print response for verification
    if [ -n "$5" ]; then
      echo "  Response: $(echo "$body" | head -c 100)..."
    fi
  else
    echo -e "${RED}✗ FAIL${NC} (Expected HTTP $expected_status, got $status)"
    echo "  Response: $body"
    FAIL_COUNT=$((FAIL_COUNT + 1))
  fi
}

# Helper function to validate JSON response structure
validate_json_structure() {
  local path=$1
  local required_fields=$2
  
  echo -n "Validating JSON structure for ${path}... "
  
  local response=$(curl -s "${BASE_URL}${path}")
  
  local all_valid=true
  for field in $required_fields; do
    if ! echo "$response" | grep -q "\"$field\""; then
      all_valid=false
      break
    fi
  done
  
  if [ "$all_valid" = true ]; then
    echo -e "${GREEN}✓ PASS${NC}"
    PASS_COUNT=$((PASS_COUNT + 1))
  else
    echo -e "${RED}✗ FAIL${NC} (Missing required fields)"
    echo "  Response: $response"
    FAIL_COUNT=$((FAIL_COUNT + 1))
  fi
}

# Check if server is running
echo -e "${YELLOW}Checking server availability...${NC}"
if ! curl -s -f -o /dev/null "${BASE_URL}/api/pools"; then
  echo -e "${RED}✗ Server not running on ${BASE_URL}${NC}"
  echo "Start the server with: npm --workspace=@defi-dashboard/server start"
  exit 1
fi
echo -e "${GREEN}✓ Server is running${NC}\n"

# Test endpoints
echo -e "${YELLOW}Testing Discovery Endpoint${NC}"
test_endpoint "GET" "/api/pools" "200" "List available pool types"

echo -e "\n${YELLOW}Testing Pool Data Endpoints${NC}"
test_endpoint "GET" "/api/pools/ETH" "200" "Get ETH pools"
test_endpoint "GET" "/api/pools/STABLES" "200" "Get stablecoin pools"
test_endpoint "GET" "/api/pools/LST" "200" "Get liquid staking pools"
test_endpoint "GET" "/api/pools/HIGH_YIELD" "200" "Get high yield pools"
test_endpoint "GET" "/api/pools/LOW_TVL" "200" "Get emerging pools"
test_endpoint "GET" "/api/pools/BLUE_CHIP" "200" "Get blue chip pools"

echo -e "\n${YELLOW}Testing Error Cases${NC}"
test_endpoint "GET" "/api/pools/INVALID" "400" "Invalid pool type should return 400"

echo -e "\n${YELLOW}Validating Response Structures${NC}"
validate_json_structure "/api/pools" "status data name displayName"
validate_json_structure "/api/pools/ETH" "status data"

# Summary
echo -e "\n${YELLOW}========== Test Summary ==========${NC}"
echo -e "Passed: ${GREEN}${PASS_COUNT}${NC}"
echo -e "Failed: ${RED}${FAIL_COUNT}${NC}"
echo -e "${YELLOW}==================================${NC}\n"

if [ $FAIL_COUNT -eq 0 ]; then
  echo -e "${GREEN}All tests passed!${NC}"
  exit 0
else
  echo -e "${RED}Some tests failed!${NC}"
  exit 1
fi
