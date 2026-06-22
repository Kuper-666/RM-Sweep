#!/bin/bash
# RM-Sweep macOS Installer
# Copies app to /Applications and creates Desktop shortcut

APP_NAME="RM-Sweep"
APP_SRC="$(dirname "$0")/../RM-Sweep.app"
APP_DST="/Applications/${APP_NAME}.app"
DESKTOP="$HOME/Desktop"

echo "========================================="
echo "  RM-Sweep Installer for macOS"
echo "========================================="
echo ""

# Check source app exists
if [ ! -d "$APP_SRC" ]; then
    echo "Error: RM-Sweep.app not found next to this script."
    echo "Make sure RM-Sweep.app is in the same folder as this script."
    exit 1
fi

# Check if already installed
if [ -d "$APP_DST" ]; then
    echo "Previous installation found. Removing..."
    rm -rf "$APP_DST"
fi

# Copy to /Applications
echo "[1/2] Installing to /Applications..."
cp -R "$APP_SRC" "$APP_DST"
chmod +x "${APP_DST}/Contents/MacOS/RMSweep"
echo "  Done: ${APP_DST}"

# Create Desktop shortcut (alias)
echo "[2/2] Creating Desktop shortcut..."
ln -sf "$APP_DST" "${DESKTOP}/${APP_NAME}.app"
echo "  Done: ~/Desktop/${APP_NAME}.app"

echo ""
echo "========================================="
echo "  Installation complete!"
echo "  App:  /Applications/${APP_NAME}.app"
echo "  Link: ~/Desktop/${APP_NAME}.app"
echo "========================================="
