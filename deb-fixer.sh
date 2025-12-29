#!/bin/bash
set -e

# --- Configuration ---
INPUT_DEB="${1:-GottaManagePlus.deb}"
ICON_SOURCE="${2:-icon.png}"
APP_NAME="GottaManagePlus"
OUTPUT_DEB="GottaManagePlus.deb"
# ---

if [[ ! -f "$INPUT_DEB" ]]; then
    echo "Error: Input file $INPUT_DEB not found."
    exit 1
fi

if [[ ! -f "$ICON_SOURCE" ]]; then
    echo "Error: Icon file $ICON_SOURCE not found."
    exit 1
fi

# Create a temporary working directory
WORKING_DIR=$(mktemp -d)
echo "Processing $INPUT_DEB in $WORKING_DIR..."

# 1. Unpack the .deb file (both data and control)
# dpkg-deb -R is the standard way to extract everything into a directory
dpkg-deb -R "$INPUT_DEB" "$WORKING_DIR"

# 2. Locate the actual application binary
# dotnet-releaser typically puts the binary in /usr/share/<appname>/
# We need the absolute path relative to the final root system.
REAL_BIN_PATH=$(find "$WORKING_DIR/usr" -type f -executable -name "$APP_NAME" | head -n 1)
TARGET_SYS_PATH=${REAL_BIN_PATH#$WORKING_DIR}

if [[ -z "$REAL_BIN_PATH" ]]; then
    echo "Error: Could not locate the executable binary inside the package."
    exit 1
fi

# 3. Remove the terminal shortcut (symlink in /usr/bin)
# The user requested NOT a terminal path.
if [[ -L "$WORKING_DIR/usr/bin/$APP_NAME" ]]; then
    echo "Removing terminal symlink at /usr/bin/$APP_NAME"
    rm "$WORKING_DIR/usr/bin/$APP_NAME"
fi

# 4. Create the .desktop file
DESKTOP_PATH="$WORKING_DIR/usr/share/applications"
mkdir -p "$DESKTOP_PATH"

echo "Creating .desktop entry..."
cat <<EOF > "$DESKTOP_PATH/$APP_NAME.desktop"
[Desktop Entry]
Version=1.0
Type=Application
Name=$APP_NAME
Comment=Launch $APP_NAME GUI
Exec=GottaManagePlus
Icon=$APP_NAME
Terminal=false
Categories=Utility;Application;
EOF

# 5. Install the Icon
# Standard path for high-res icons is /usr/share/icons/hicolor/256x256/apps/
ICON_DIR="$WORKING_DIR/usr/share/icons/hicolor/256x256/apps"
mkdir -p "$ICON_DIR"
cp "$ICON_SOURCE" "$ICON_DIR/$APP_NAME.png"

# 6. Repack the .deb file
echo "Repacking into $OUTPUT_DEB..."
dpkg-deb -b "$WORKING_DIR" "$OUTPUT_DEB"

# Cleanup
rm -rf "$WORKING_DIR"
rm -f "$INPUT_DEB"
echo "Modification complete."
