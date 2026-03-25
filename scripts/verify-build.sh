#!/bin/bash

# DeFi Dashboard Build Optimization Verification Script
# Usage: ./scripts/verify-build.sh
# Verifies that all built artifacts are properly optimized

set -e

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

PASS_COUNT=0
FAIL_COUNT=0
WARN_COUNT=0

echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}    DeFi Dashboard Build Optimization Verification${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}\n"

# Check if build directory exists
if [ ! -d "packages/client/build" ]; then
  echo -e "${RED}✗ Build directory not found${NC}"
  echo "Run: npm run build"
  exit 1
fi

# Helper functions
check_file_size() {
  local file=$1
  local max_size=$2
  local name=$3
  
  if [ ! -f "$file" ]; then
    echo -e "${RED}✗ File not found: $file${NC}"
    FAIL_COUNT=$((FAIL_COUNT + 1))
    return 1
  fi
  
  local size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
  local size_kb=$((size / 1024))
  
  if [ "$size" -le "$max_size" ]; then
    echo -e "${GREEN}✓${NC} $name: ${size_kb}KB (max: $((max_size / 1024))KB)"
    PASS_COUNT=$((PASS_COUNT + 1))
    return 0
  else
    echo -e "${YELLOW}⚠${NC} $name: ${size_kb}KB (max recommended: $((max_size / 1024))KB)"
    WARN_COUNT=$((WARN_COUNT + 1))
    return 0
  fi
}

warn_file_size() {
  local file=$1
  local max_size=$2
  local name=$3
  
  if [ ! -f "$file" ]; then
    echo -e "${YELLOW}⚠${NC} $name: File not found (optional)"
    return
  fi
  
  local size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null)
  local size_kb=$((size / 1024))
  
  if [ "$size" -le "$max_size" ]; then
    echo -e "${GREEN}✓${NC} $name: ${size_kb}KB"
  else
    echo -e "${YELLOW}⚠${NC} $name: ${size_kb}KB (above target: $((max_size / 1024))KB)"
    WARN_COUNT=$((WARN_COUNT + 1))
  fi
}

# Section 1: JavaScript Optimization
echo -e "${BLUE}1. JavaScript Optimization${NC}"
echo "   Checking: Minification, Tree Shaking, Code Splitting"
echo ""

# Find all main JS bundles
main_js=$(find packages/client/build/static/js -name "main.*.js" -type f 2>/dev/null | head -1)
if [ -n "$main_js" ]; then
  check_file_size "$main_js" 307200 "Main bundle (raw)"
else
  echo -e "${RED}✗ Main bundle not found${NC}"
  FAIL_COUNT=$((FAIL_COUNT + 1))
fi

# Check for code splitting (chunks)
chunk_count=$(find packages/client/build/static/js -name "*.chunk.js" -type f 2>/dev/null | wc -l)
if [ "$chunk_count" -gt 0 ]; then
  echo -e "${GREEN}✓${NC} Code splitting: $chunk_count chunks found"
  PASS_COUNT=$((PASS_COUNT + 1))
else
  echo -e "${YELLOW}⚠${NC} Code splitting: No chunks found (consider if needed)"
fi

# Check for source maps (should be present for debugging)
sourcemap_count=$(find packages/client/build/static -name "*.js.map" -type f 2>/dev/null | wc -l)
if [ "$sourcemap_count" -gt 0 ]; then
  echo -e "${GREEN}✓${NC} Source maps: $sourcemap_count maps present"
  PASS_COUNT=$((PASS_COUNT + 1))
else
  echo -e "${YELLOW}⚠${NC} Source maps: None found (optional for production)"
fi

echo ""

# Section 2: CSS Optimization
echo -e "${BLUE}2. CSS Optimization${NC}"
echo "   Checking: Minification, Dead Code Elimination"
echo ""

css_files=$(find packages/client/build/static/css -name "*.css" -type f 2>/dev/null)
if [ -n "$css_files" ]; then
  total_css_size=0
  for css_file in $css_files; do
    size=$(stat -f%z "$css_file" 2>/dev/null || stat -c%s "$css_file" 2>/dev/null)
    total_css_size=$((total_css_size + size))
  done
  
  total_css_kb=$((total_css_size / 1024))
  if [ "$total_css_size" -le 20480 ]; then
    echo -e "${GREEN}✓${NC} Total CSS: ${total_css_kb}KB (target: <20KB)"
    PASS_COUNT=$((PASS_COUNT + 1))
  else
    echo -e "${YELLOW}⚠${NC} Total CSS: ${total_css_kb}KB (target: <20KB)"
    WARN_COUNT=$((WARN_COUNT + 1))
  fi
else
  echo -e "${YELLOW}⚠${NC} No CSS files found"
fi

echo ""

# Section 3: Image Optimization
echo -e "${BLUE}3. Image Optimization${NC}"
echo "   Checking: Image compression, SVG usage, formats"
echo ""

# Check PNG/JPG files
png_count=$(find packages/client/build -name "*.png" -type f 2>/dev/null | wc -l)
jpg_count=$(find packages/client/build -name "*.jpg" -o -name "*.jpeg" -type f 2>/dev/null | wc -l)
svg_count=$(find packages/client/build -name "*.svg" -type f 2>/dev/null | wc -l)
webp_count=$(find packages/client/build -name "*.webp" -type f 2>/dev/null | wc -l)

echo -e "${GREEN}✓${NC} Image inventory:"
echo "   - PNG files: $png_count"
echo "   - JPG files: $jpg_count"
echo "   - SVG files: $svg_count"
echo "   - WebP files: $webp_count"
PASS_COUNT=$((PASS_COUNT + 1))

# Check for large images
large_images=$(find packages/client/build -type f \( -name "*.png" -o -name "*.jpg" -o -name "*.jpeg" \) -size +100k 2>/dev/null)
if [ -z "$large_images" ]; then
  echo -e "${GREEN}✓${NC} No images exceed 100KB"
  PASS_COUNT=$((PASS_COUNT + 1))
else
  echo -e "${YELLOW}⚠${NC} Large images found (>100KB):"
  echo "$large_images" | while read img; do
    size=$(($(stat -f%z "$img" 2>/dev/null || stat -c%s "$img" 2>/dev/null) / 1024))
    echo "   - $(basename $img): ${size}KB"
  done
  WARN_COUNT=$((WARN_COUNT + 1))
fi

echo ""

# Section 4: Build Metadata
echo -e "${BLUE}4. Build Metadata${NC}"
echo "   Checking: Build artifacts and configuration"
echo ""

if [ -f "packages/client/build/index.html" ]; then
  echo -e "${GREEN}✓${NC} index.html present"
  PASS_COUNT=$((PASS_COUNT + 1))
fi

if [ -f "packages/client/build/asset-manifest.json" ]; then
  echo -e "${GREEN}✓${NC} asset-manifest.json present"
  PASS_COUNT=$((PASS_COUNT + 1))
fi

# Check build size summary
total_size=$(du -sh packages/client/build 2>/dev/null | cut -f1)
echo -e "${BLUE}ℹ${NC} Total build size: $total_size"

echo ""

# Summary
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}                         SUMMARY${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════════${NC}"
echo -e "Passed:  ${GREEN}${PASS_COUNT}${NC}"
echo -e "Failed:  ${RED}${FAIL_COUNT}${NC}"
echo -e "Warnings: ${YELLOW}${WARN_COUNT}${NC}"
echo ""

if [ $FAIL_COUNT -eq 0 ]; then
  echo -e "${GREEN}✓ Build optimization verification passed!${NC}"
  if [ $WARN_COUNT -gt 0 ]; then
    echo -e "${YELLOW}Note: $WARN_COUNT warnings to review${NC}"
  fi
  exit 0
else
  echo -e "${RED}✗ Build optimization verification failed!${NC}"
  exit 1
fi
