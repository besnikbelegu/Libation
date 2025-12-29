#!/bin/bash

SOURCE_ICON="$1"
OUTPUT_DIR=$(dirname "$SOURCE_ICON")

if [ -z "$SOURCE_ICON" ]; then
    echo "Usage: $0 [SOURCE_IMAGE]"
    exit 1
fi

# Create .icns (macOS)
ICONSET_DIR="$OUTPUT_DIR/Libation.iconset"
mkdir -p "$ICONSET_DIR"

# Resize for iconset
sips -z 16 16     "$SOURCE_ICON" --out "$ICONSET_DIR/icon_16x16.png"
sips -z 32 32     "$SOURCE_ICON" --out "$ICONSET_DIR/icon_16x16@2x.png"
sips -z 32 32     "$SOURCE_ICON" --out "$ICONSET_DIR/icon_32x32.png"
sips -z 64 64     "$SOURCE_ICON" --out "$ICONSET_DIR/icon_32x32@2x.png"
sips -z 128 128   "$SOURCE_ICON" --out "$ICONSET_DIR/icon_128x128.png"
sips -z 256 256   "$SOURCE_ICON" --out "$ICONSET_DIR/icon_128x128@2x.png"
sips -z 256 256   "$SOURCE_ICON" --out "$ICONSET_DIR/icon_256x256.png"
sips -z 512 512   "$SOURCE_ICON" --out "$ICONSET_DIR/icon_256x256@2x.png"
sips -z 512 512   "$SOURCE_ICON" --out "$ICONSET_DIR/icon_512x512.png"
sips -z 1024 1024 "$SOURCE_ICON" --out "$ICONSET_DIR/icon_512x512@2x.png"

echo "Creating .icns..."
iconutil -c icns "$ICONSET_DIR" -o "$OUTPUT_DIR/Libation.icns"
rm -rf "$ICONSET_DIR"

# Create .ico (Windows)
# Using ffmpeg if available, otherwise fallback or warn
if command -v ffmpeg &> /dev/null; then
    echo "Creating .ico with ffmpeg..."
    ffmpeg -i "$SOURCE_ICON" -vf "scale=256:256" "$OUTPUT_DIR/Libation.ico" -y
else
    echo "ffmpeg not found, skipping .ico creation"
fi
