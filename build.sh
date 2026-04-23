#!/bin/bash

# ------------------------------------------------------------
# Extract project metadata from .csproj file
# ------------------------------------------------------------
extract_from_csproj() {
    local csproj_file="$1"
    local prop_name="$2"
    # Match <PropertyGroup>...<PropName>value</PropName>...
    # Handles multi-line and nested PropertyGroup
    sed -n "/<PropertyGroup/,/<\/PropertyGroup>/p" "$csproj_file" \
        | grep -o "<$prop_name>[^<]*</$prop_name>" \
        | sed "s/<$prop_name>//;s/<\/$prop_name>//" \
        | head -n1
}

# Determine project root (where the .csproj is)
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$SCRIPT_DIR"

# Find the .csproj file (assume exactly one)
CSPROJ_FILE=$(find "$PROJECT_ROOT" -maxdepth 1 -name "*.csproj" | head -n1)
if [[ -z "$CSPROJ_FILE" ]]; then
    echo "ERROR: No .csproj file found in $PROJECT_ROOT"
    exit 1
fi

PROJECT_NAME=$(basename "$CSPROJ_FILE" .csproj)
echo "Found project: $PROJECT_NAME"

# Extract Version from <AppVersion> (fallback to <Version> if missing)
APP_VERSION=$(extract_from_csproj "$CSPROJ_FILE" "AppVersion")
if [[ -z "$APP_VERSION" ]]; then
    APP_VERSION=$(extract_from_csproj "$CSPROJ_FILE" "Version")
fi
if [[ -z "$APP_VERSION" ]]; then
    echo "ERROR: Could not find <AppVersion> or <Version> in $CSPROJ_FILE"
    exit 1
fi
echo "Version: $APP_VERSION"

# Extract TargetFramework
TARGET_FRAMEWORK=$(extract_from_csproj "$CSPROJ_FILE" "TargetFramework")
if [[ -z "$TARGET_FRAMEWORK" ]]; then
    echo "ERROR: <TargetFramework> not found in $CSPROJ_FILE"
    exit 1
fi
echo "Target framework: $TARGET_FRAMEWORK"

# Optional: try to extract Author from <Authors> (not present by default, but you can add it)
AUTHOR=$(extract_from_csproj "$CSPROJ_FILE" "Authors")
if [[ -z "$AUTHOR" ]]; then
    AUTHOR="PixelGuy123"   # fallback to your original value
    echo "Warning: <Authors> not found, using default: $AUTHOR"
fi

# Optional: try to extract Description
DESCRIPTION=$(extract_from_csproj "$CSPROJ_FILE" "Description")
if [[ -z "$DESCRIPTION" ]]; then
    DESCRIPTION="Gotta Manage Plus is a project made to manage the modding aspect of the game Baldi's Basics Plus, through the usage of BepInEx."
    echo "Warning: <Description> not found, using default."
fi

# ------------------------------------------------------------
# Configuration
# ------------------------------------------------------------
PROJECT_DOTNET_VERSION="net10.0"   # or whatever your target framework is
SELF_CONTAINED=true
PUBLISH_SINGLE_FILE=true
PUBLISH_AOT=false
PUBLISH_TRIMMED=$PUBLISH_AOT

# Use absolute paths
OUTPUT_DIR="$PROJECT_ROOT/bin/builds"
BUILD_TEMP="$PROJECT_ROOT/bin/temp"

WIN_RID="win-x64"
LINUX_RID="linux-x64"

# ------------------------------------------------------------
# Prepare directories
# ------------------------------------------------------------
mkdir -p "$OUTPUT_DIR"
mkdir -p "$BUILD_TEMP"

echo "Starting build process for $PROJECT_NAME v${APP_VERSION}..."

# ------------------------------------------------------------
# Helper function to run dotnet publish and check success
# ------------------------------------------------------------
publish_for_rid() {
    local rid="$1"
    local output_subdir="$BUILD_TEMP/$rid"
    echo "Publishing for $rid..."
    dotnet publish "$CSPROJ_FILE" \
        -c Release \
        -r "$rid" \
        --self-contained "$SELF_CONTAINED" \
        -p:PublishSingleFile="$PUBLISH_SINGLE_FILE" \
        -p:PublishTrimmed="$PUBLISH_TRIMMED" \
        -p:PublishAot="$PUBLISH_AOT" \
        -o "$output_subdir"
    
    if [[ ! -d "$output_subdir" ]]; then
        echo "ERROR: Publish failed for $rid (output directory missing)"
        exit 1
    fi
}
# ------------------------------------------------------------
# 1. Windows build
# ------------------------------------------------------------
publish_for_rid "$WIN_RID"
echo "Creating Windows zip..."
cd "$BUILD_TEMP/$WIN_RID"
zip -r "$BUILD_TEMP/${PROJECT_NAME}_Windows.zip" .
mv "$BUILD_TEMP/${PROJECT_NAME}_Windows.zip" "$OUTPUT_DIR/${PROJECT_NAME}_Windows.zip"
cd "$PROJECT_ROOT"

# ------------------------------------------------------------
# 2. Linux build
# ------------------------------------------------------------
publish_for_rid "$LINUX_RID"
echo "Setting permissions for Linux binaries..."
chmod 666 "$BUILD_TEMP/$LINUX_RID/AppSettings.json" 2>/dev/null || true
chmod 777 "$BUILD_TEMP/$LINUX_RID/$PROJECT_NAME" 2>/dev/null || true

echo "Creating Linux tarball..."
cd "$BUILD_TEMP/$LINUX_RID"
tar -czvf "$BUILD_TEMP/${PROJECT_NAME}_Linux.tar.gz" .
mv "$BUILD_TEMP/${PROJECT_NAME}_Linux.tar.gz" "$OUTPUT_DIR/${PROJECT_NAME}_Linux.tar.gz"
cd "$PROJECT_ROOT"

# ------------------------------------------------------------
# 3. Debian package (optional, only if Linux publish succeeded)
# ------------------------------------------------------------
echo "Constructing Debian package..."
DEB_ROOT="$BUILD_TEMP/debian_pkg"
mkdir -p "$DEB_ROOT/DEBIAN"
mkdir -p "$DEB_ROOT/opt/$PROJECT_NAME"
mkdir -p "$DEB_ROOT/usr/local/bin"
mkdir -p "$DEB_ROOT/usr/share/applications"
mkdir -p "$DEB_ROOT/usr/share/pixmaps"

cp -r "$BUILD_TEMP/$LINUX_RID/"* "$DEB_ROOT/opt/$PROJECT_NAME/"
chmod 666 "$DEB_ROOT/opt/$PROJECT_NAME/AppSettings.json" 2>/dev/null || true
chmod 777 "$DEB_ROOT/opt/$PROJECT_NAME/$PROJECT_NAME"
ln -s "/opt/$PROJECT_NAME/$PROJECT_NAME" "$DEB_ROOT/usr/local/bin/$PROJECT_NAME"

# Icon (make sure the file exists)
if [[ -f "$PROJECT_ROOT/$PROJECT_NAME.png" ]]; then
    cp "$PROJECT_ROOT/$PROJECT_NAME.png" "$DEB_ROOT/usr/share/pixmaps/$PROJECT_NAME.png"
else
    echo "Warning: Icon not found at $PROJECT_ROOT/$PROJECT_NAME.png"
fi

cat <<EOF > "$DEB_ROOT/DEBIAN/control"
Package: gottamanageplus
Version: $APP_VERSION
Section: utils
Priority: optional
Architecture: amd64
Maintainer: $AUTHOR
Description: $DESCRIPTION
EOF

cat <<EOF > "$DEB_ROOT/usr/share/applications/$PROJECT_NAME.desktop"
[Desktop Entry]
Name=Gotta Manage Plus
Comment=$DESCRIPTION
Exec=$PROJECT_NAME
Icon=$PROJECT_NAME
Terminal=false
Type=Application
Categories=Game;Utility;
EOF

dpkg-deb --build "$DEB_ROOT" "$OUTPUT_DIR/${PROJECT_NAME}_${APP_VERSION}_amd64.deb"

# ------------------------------------------------------------
# 4. Cleanup temp
# ------------------------------------------------------------
rm -rf "$BUILD_TEMP"

echo "Build complete. Files located in $OUTPUT_DIR"

# ------------------------------------------------------------
# 5. macOS bundle
# ------------------------------------------------------------
 if command -v dotnet msbuild >/dev/null 2>&1; then
     dotnet msbuild "$CSPROJ_FILE" -r -t:BundleApp \
         -p:RuntimeIdentifier=osx-x64 \
         -p:Configuration=Release \
         -p:PublishAot="$PUBLISH_AOT" \
         -p:PublishTrimmed="$PUBLISH_TRIMMED" \
         -p:PublishSingleFile="$PUBLISH_SINGLE_FILE" \
         -p:SelfContained="$SELF_CONTAINED" \
         -p:PublishReadyToRun=true \
         -p:CopyOutputSymbolsToPublishDirectory=false \
         -p:SkipCopyingSymbolsToOutputDirectory=true
 fi

# ** Bundle Fix
# Relative path to the Info.plist
# The beginning is basically a way to get full path
TARGET_FILE="bin/Debug/$PROJECT_DOTNET_VERSION/osx-x64/publish/Gotta Manage Plus.app/Contents/Info.plist"

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
    
# 7. Move the app image to the build
mv "bin/Debug/$PROJECT_DOTNET_VERSION/osx-x64/publish/Gotta Manage Plus.app" "$OUTPUT_DIR/Gotta Manage Plus.app"

# Cleanup
rm -rf "$TEMP_DIR"

echo "Done! Created ${ICNS_FILE_NAME}"
echo "Moving the file to the bundle..."
mv "${ICNS_FILE_NAME}" "bin/Debug/$PROJECT_DOTNET_VERSION/osx-x64/publish/Gotta Manage Plus.app/Contents/Resources/${ICNS_FILE_NAME}"
echo "Moved successfully to the right path!"
echo "Moving bundle to the builds folder..."
mv "bin/Debug/$PROJECT_DOTNET_VERSION/osx-x64/publish/Gotta Manage Plus.app" "${OUTPUT_DIR}"
echo "Finalized builds!"
