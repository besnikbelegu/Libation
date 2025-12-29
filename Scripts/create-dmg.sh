#!/bin/bash

# Usage: ./create-dmg.sh [APP_BUNDLE_PATH] [OUTPUT_DMG_PATH] [VOLNAME]

APP_BUNDLE="$1"
OUTPUT_DMG="$2"
VOLNAME="$3"

if [ -z "$APP_BUNDLE" ] || [ -z "$OUTPUT_DMG" ] || [ -z "$VOLNAME" ]; then
    echo "Usage: $0 [APP_BUNDLE_PATH] [OUTPUT_DMG_PATH] [VOLNAME]"
    exit 1
fi

# Create a temporary directory for the DMG content
DMG_TMP_DIR=$(mktemp -d)
echo "Creating DMG content in $DMG_TMP_DIR..."

# Copy the app bundle
cp -r "$APP_BUNDLE" "$DMG_TMP_DIR/"

# Create symlink to Applications
ln -s /Applications "$DMG_TMP_DIR/Applications"

# Create the DMG
echo "Creating DMG..."
rm -f "$OUTPUT_DMG"
hdiutil create -volname "$VOLNAME" -srcfolder "$DMG_TMP_DIR" -ov -format UDZO "$OUTPUT_DMG"

# Cleanup
rm -rf "$DMG_TMP_DIR"

echo "DMG created at $OUTPUT_DMG"
