#!/bin/bash

# Build the project in Release mode
dotnet build Pipelink/Pipelink.csproj -c Release

# Create the NuGet package
dotnet pack Pipelink/Pipelink.csproj -c Release -o ./nupkg

echo "NuGet package created in ./nupkg directory" 