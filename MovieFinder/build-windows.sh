#!/bin/bash

# This script builds the MovieFinder application for Windows x64.

# Set variables
PROJECT_NAME="MovieFinder"
CONFIGURATION="Release"
RUNTIME="win-x64"
OUTPUT_DIRECTORY="./bin/$CONFIGURATION/net9.0/$RUNTIME/publish"
ZIP_FILE_NAME="$PROJECT_NAME-$RUNTIME.zip"

# Clean the output directory
if [ -d "$OUTPUT_DIRECTORY" ]; then
    rm -rf "$OUTPUT_DIRECTORY"
fi

# Build the application
dotnet publish -c $CONFIGURATION -r $RUNTIME --self-contained true /p:PublishSingleFile=true

# Check if the build was successful
if [ $? -ne 0 ]; then
    echo "Build failed. Aborting script."
    exit 1
fi

# Copy Zadig to the publish directory before zipping
cp "$(dirname "$0")/WindowsRequired/zadig-2.9.exe" "$OUTPUT_DIRECTORY/"

# Compress the output, including Zadig
(cd "$OUTPUT_DIRECTORY" && zip -r "../../../../../$ZIP_FILE_NAME" .)

echo "Build successful. The application is zipped in $ZIP_FILE_NAME"
