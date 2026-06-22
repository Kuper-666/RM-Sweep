#!/bin/bash
# RM-Sweep macOS Uninstaller

APP_NAME="RM-Sweep"
APP_DST="/Applications/${APP_NAME}.app"
DESKTOP="$HOME/Desktop"

echo "Uninstalling ${APP_NAME}..."

if [ -d "$APP_DST" ]; then
    rm -rf "$APP_DST"
    echo "  Removed: ${APP_DST}"
fi

if [ -L "${DESKTOP}/${APP_NAME}.app" ]; then
    rm -f "${DESKTOP}/${APP_NAME}.app"
    echo "  Removed: ~/Desktop/${APP_NAME}.app"
fi

echo "Done."
