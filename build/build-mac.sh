#!/bin/bash
# Build script for macOS
echo "========================================"
echo " RM-Sweep - Building for macOS"
echo "========================================"

echo ""
echo "[1/3] Building self-contained macOS x64 executable..."
dotnet publish src/RMSweep/RMSweep.csproj -c Release -r osx-x64 --self-contained true -o output/osx-x64

if [ $? -ne 0 ]; then
    echo ""
    echo "ERROR: Build failed!"
    exit 1
fi

echo ""
echo "[2/3] Creating macOS .app bundle..."

APP_DIR="output/RM-Sweep.app"
CONTENTS="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS/MacOS"
RESOURCES_DIR="$CONTENTS/Resources"

mkdir -p "$MACOS_DIR"
mkdir -p "$RESOURCES_DIR"

# Copy binary
cp output/osx-x64/RMSweep "$MACOS_DIR/RMSweep"
chmod +x "$MACOS_DIR/RMSweep"

# Copy all runtime files into MacOS
cp -r output/osx-x64/* "$MACOS_DIR/"

# Copy icon if exists
if [ -f "src/RMSweep/Assets/icon.icns" ]; then
    cp src/RMSweep/Assets/icon.icns "$RESOURCES_DIR/"
fi

# Create Info.plist
cat > "$CONTENTS/Info.plist" << 'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key>
    <string>RM-Sweep</string>
    <key>CFBundleDisplayName</key>
    <string>RM-Sweep System Cleaner</string>
    <key>CFBundleIdentifier</key>
    <string>com.kuper-666.rmsweep</string>
    <key>CFBundleVersion</key>
    <string>1.0.0</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleExecutable</key>
    <string>RMSweep</string>
    <key>CFBundleIconFile</key>
    <string>icon.icns</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
</dict>
</plist>
PLIST

echo ""
echo "[3/3] Build complete!"
echo "App bundle: output/RM-Sweep.app"
echo "Standalone: output/osx-x64/RMSweep"
echo ""
