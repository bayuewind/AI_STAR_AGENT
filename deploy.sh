#!/bin/bash

# AI_STAR_AGENT Deployment Script for Stardew Valley
# This script builds and deploys the mod to the Stardew Valley Mods folder

set -e  # Exit on error

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
RED='\033[0;31m'
NC='\033[0m' # No Color

echo -e "${BLUE}🤖 AI_STAR_AGENT Deployment Script${NC}"
echo "======================================"

# Build the project
echo -e "\n${BLUE}📦 Building project...${NC}"
dotnet build

if [ $? -ne 0 ]; then
    echo -e "${RED}❌ Build failed!${NC}"
    exit 1
fi

echo -e "${GREEN}✅ Build successful!${NC}"

# Define paths
MODS_PATH="/Users/bayue/Library/Application Support/Steam/steamapps/common/Stardew Valley/Contents/MacOS/Mods/AI_Agent"
BUILD_PATH="StardewAgentMod/bin/Debug/net6.0"

# Create mod directory if it doesn't exist
echo -e "\n${BLUE}📁 Creating mod directory...${NC}"
mkdir -p "$MODS_PATH"

# Copy files
echo -e "\n${BLUE}📋 Copying files to Mods folder...${NC}"
cp -r "$BUILD_PATH"/* "$MODS_PATH/"
cp StardewAgentMod/manifest.json "$MODS_PATH/"
cp -r StardewAgentMod/assets "$MODS_PATH/" 2>/dev/null || true

echo -e "${GREEN}✅ Deployment complete!${NC}"
echo -e "\n${BLUE}📍 Mod location:${NC} $MODS_PATH"
echo -e "\n${GREEN}🎮 You can now launch Stardew Valley!${NC}"
echo -e "${BLUE}💡 Press F5 in-game to give AI commands${NC}"
echo -e "${BLUE}💡 Press F6 to stop all AI tasks${NC}"
