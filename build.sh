#!/bin/bash
# Restores first
dotnet restore
# All OS available
dotnet-releaser build --force dotnet-releaser.toml

# ** Deb fixer
./deb-fixer.sh artifacts-dotnet-releaser/GottaManagePlus.*.linux-x64.deb GottaManagePlus.png

# Attempt to move deb file
mv GottaManagePlus.deb artifacts-dotnet-releaser/GottaManagePlus.deb

# MAC OS Bundle
dotnet msbuild -r -t:BundleApp -p:RuntimeIdentifier=osx-x64;Configuration=Release;PublishTrimmed=false;PublishSingleFile=false;SelfContained=true;PublishReadyToRun=true;CopyOutputSymbolsToPublishDirectory=false;SkipCopyingSymbolsToOutputDirectory=true

# ** Bundle Fix
# Relative path to the Info.plist
# The beginning is basically a way to get full path
TARGET_FILE="bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app/Contents/Info.plist"
TARGET_FOLDER="bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app/Contents/"

# Check if the file exists before anything
if [ ! -f "$TARGET_FILE" ]; then
    echo "Error: Info.plist not found at $TARGET_FILE"
    exit 1
fi

# Define the valid Plist XML block for URL types
URL_BLOCK="    <key>CFBundleURLTypes</key>\n    <array>\n      <dict>\n        <key>CFBundleURLName</key>\n        <string>GottaManagePlus URL</string>\n        <key>CFBundleURLSchemes</key>\n        <array>\n          <string>gottamanageplus</string>\n        </array>\n      </dict>\n    </array>"

# Append block
sed -i "/<true \/>/a $URL_BLOCK" "$TARGET_FILE"

echo "Successfully updated $TARGET_FILE"

# ** Create .icns for the macOS bundle and move it to the bundle

INPUT_IMAGE="./GottaManagePlus_mac.png"
ICNS_FILE_NAME="GottaManagePlus.icns"
FILENAME=$(basename -- "$INPUT_IMAGE")
BASE_NAME="${FILENAME%.*}"
TEMP_DIR="temp_icons"

# Create temporary directory for resized versions
mkdir -p "$TEMP_DIR"

echo "Resizing images..."

# Standard Apple ICNS sizes
SIZES=(16 32 48 128 256 512 1024)

for SIZE in "${SIZES[@]}"; do
    convert "$INPUT_IMAGE" -resize "${SIZE}x${SIZE}" "$TEMP_DIR/icon_${SIZE}.png"
done

echo "Generating $BASE_NAME.icns..."

# png2icns <output.icns> <input_files...>
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
mv "bin/Debug/net9.0/osx-x64/publish/Gotta Manage Plus.app" "artifacts-dotnet-releaser/Gotta Manage Plus.app"
echo "Finalized builds!"
