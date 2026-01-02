#!/bin/bash

# Configuration
PROJECT_ROOT="/home/pixeldesktop/Documentos/Github/GottaManagePlus"
PROJECT_NAME="GottaManagePlus"
# Extract version from <AssemblyVersion> or <Version> tags
VERSION=$(grep -oPm1 "(?<=<AssemblyVersion>)[^<]+" "$CSPROJ_FILE")

# Fallback: If AssemblyVersion is missing, try the Version tag
if [ -z "$VERSION" ]; then
    VERSION=$(grep -oPm1 "(?<=<Version>)[^<]+" "$CSPROJ_FILE")
fi
AUTHOR="PixelGuy123"
DESCRIPTION="Gotta Manage Plus is a project made to manage the modding aspect of the game Baldi's Basics Plus, through the usage of BepInEx."
OUTPUT_DIR="$PROJECT_ROOT/bin/builds"
BUILD_TEMP="$PROJECT_ROOT/bin/temp"

# Runtime identifiers
WIN_RID="win-x64"
LINUX_RID="linux-x64"

# Create output directories
mkdir -p "$OUTPUT_DIR"
mkdir -p "$BUILD_TEMP"

echo "Starting build process for $PROJECT_NAME v$VERSION..."

# 1. Windows Build (Single File, Contained, No AOT/Trim)
echo "Publishing for Windows (x64)..."
dotnet publish "$PROJECT_ROOT" \
    -c Release \
    -r "$WIN_RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:PublishAot=false \
    -o "$BUILD_TEMP/$WIN_RID"

# Create Windows Zip
cd "$BUILD_TEMP/$WIN_RID"
zip -r "$OUTPUT_DIR/${PROJECT_NAME}_Windows.zip" .
cd "$PROJECT_ROOT"

# 2. Linux Build (Not Single File, Contained, No AOT/Trim)
echo "Publishing for Linux (x64)..."
dotnet publish "$PROJECT_ROOT" \
    -c Release \
    -r "$LINUX_RID" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -p:PublishAot=false \
    -o "$BUILD_TEMP/$LINUX_RID"

# Apply Permissions for Linux build
echo "Setting permissions for Linux binaries..."
chmod 666 "$BUILD_TEMP/$LINUX_RID/AppSettings.json"
chmod 777 "$BUILD_TEMP/$LINUX_RID/$PROJECT_NAME"

# Create Linux Tar.gz
cd "$BUILD_TEMP/$LINUX_RID"
tar -czvf "$OUTPUT_DIR/${PROJECT_NAME}_Linux.tar.gz" .
cd "$PROJECT_ROOT"

# 3. Debian Package Creation
echo "Constructing Debian package..."
DEB_ROOT="$BUILD_TEMP/debian_pkg"
mkdir -p "$DEB_ROOT/DEBIAN"
mkdir -p "$DEB_ROOT/opt/$PROJECT_NAME"
mkdir -p "$DEB_ROOT/usr/local/bin"
mkdir -p "$DEB_ROOT/usr/share/applications"
mkdir -p "$DEB_ROOT/usr/share/pixmaps"

# Copy published files to /opt (standard for self-contained apps)
cp -r "$BUILD_TEMP/$LINUX_RID/"* "$DEB_ROOT/opt/$PROJECT_NAME/"

# Set permissions within the package structure
chmod 666 "$DEB_ROOT/opt/$PROJECT_NAME/AppSettings.json"
chmod 777 "$DEB_ROOT/opt/$PROJECT_NAME/$PROJECT_NAME"

# Create symlink for CLI access
ln -s "/opt/$PROJECT_NAME/$PROJECT_NAME" "$DEB_ROOT/usr/local/bin/$PROJECT_NAME"

# Copy Icon
cp "$PROJECT_ROOT/$PROJECT_NAME.png" "$DEB_ROOT/usr/share/pixmaps/$PROJECT_NAME.png"

# Create Control File
cat <<EOF > "$DEB_ROOT/DEBIAN/control"
Package: gottamanageplus
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Maintainer: $AUTHOR
Description: $DESCRIPTION
EOF

# Create .desktop File
cat <<EOF > "$DEB_ROOT/usr/share/applications/$PROJECT_NAME.desktop"
[Desktop Entry]
Name=Gotta Manage Plus
Comment=$DESCRIPTION
Exec=GottaManagePlus
Icon=$PROJECT_NAME
Terminal=false
Type=Application
Categories=Game;Utility;
EOF

# Build the .deb package
dpkg-deb --build "$DEB_ROOT" "$OUTPUT_DIR/${PROJECT_NAME}_${VERSION}_amd64.deb"

# Cleanup
rm -rf "$BUILD_TEMP"

echo "Build complete. Files located in $OUTPUT_DIR"

# MAC OS Bundle
dotnet msbuild -r -t:BundleApp -p:RuntimeIdentifier=osx-x64;Configuration=Release;PublishTrimmed=false;PublishSingleFile=false;SelfContained=true;PublishReadyToRun=true;CopyOutputSymbolsToPublishDirectory=false;SkipCopyingSymbolsToOutputDirectory=true

# ** Bundle Fix
# Relative path to the Info.plist
# The beginning is basically a way to get full path
TARGET_FILE="bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app/Contents/Info.plist"
TARGET_FOLDER="bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app/Contents/"

# 1. Check if the file exists before anything
if [ ! -f "$TARGET_FILE" ]; then
    echo "Error: Info.plist not found at $TARGET_FILE"
    exit 1
fi

# 2. Define the valid Plist XML block for URL types
URL_BLOCK="    <key>CFBundleURLTypes</key>\n    <array>\n      <dict>\n        <key>CFBundleURLName</key>\n        <string>GottaManagePlus URL</string>\n        <key>CFBundleURLSchemes</key>\n        <array>\n          <string>gottamanageplus</string>\n        </array>\n      </dict>\n    </array>"

# 3. Append block
sed -i "/<true \/>/a $URL_BLOCK" "$TARGET_FILE"

echo "Successfully updated $TARGET_FILE"

# ** Create .icns for the macOS bundle and move it to the bundle

INPUT_IMAGE="./GottaManagePlus_mac.png"
ICNS_FILE_NAME="GottaManagePlus.icns"
FILENAME=$(basename -- "$INPUT_IMAGE")
BASE_NAME="${FILENAME%.*}"
TEMP_DIR="temp_icons"

# 4. Create temporary directory for resized versions
mkdir -p "$TEMP_DIR"

echo "Resizing images..."

# 5. Standard Apple ICNS sizes
SIZES=(16 32 48 128 256 512 1024)

for SIZE in "${SIZES[@]}"; do
    convert "$INPUT_IMAGE" -resize "${SIZE}x${SIZE}" "$TEMP_DIR/icon_${SIZE}.png"
done

echo "Generating $BASE_NAME.icns..."

# 6. png2icns <output.icns> <input_files...>
png2icns "${ICNS_FILE_NAME}" \
    "$TEMP_DIR/icon_16.png" \
    "$TEMP_DIR/icon_32.png" \
    "$TEMP_DIR/icon_48.png" \
    "$TEMP_DIR/icon_128.png" \
    "$TEMP_DIR/icon_256.png" \
    "$TEMP_DIR/icon_512.png" \
    "$TEMP_DIR/icon_1024.png"

# Cleanup
rm -rf "$TEMP_DIR"

echo "Done! Created ${ICNS_FILE_NAME}"
echo "Moving the file to the bundle..."
mv "${ICNS_FILE_NAME}" "bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app/Contents/Resources/${ICNS_FILE_NAME}"
echo "Moved successfully to the right path!"
echo "Moving bundle to the builds folder..."
mv "bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app" "${OUTPUT_DIR}"
echo "Finalized builds!"
