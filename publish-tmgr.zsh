#!/bin/zsh

set -e

script_directory=${0:A:h}
install_directory="$HOME/.local/share/tmgr"

mkdir -p "$install_directory"

dotnet publish "$script_directory/tmgr.csproj" \
  -c Release \
  -o "$install_directory"

print "Published tmgr to $install_directory"
