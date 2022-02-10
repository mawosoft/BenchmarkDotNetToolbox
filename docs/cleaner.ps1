# Copyright (c) 2022 Matthias Wolf, Mawosoft.

# Delete bin/obj/_site subfolders and generated files in api subfolder

[CmdletBinding(SupportsShouldProcess)]
Param ()

Get-ChildItem -Path $PSScriptRoot -Directory -Recurse -Include bin, obj, _site | Remove-Item -Recurse
Get-ChildItem -Path $PSScriptRoot -File -Filter log.txt | Remove-Item
Get-ChildItem -Path (Join-Path (Join-Path $PSScriptRoot api) *) -File -Exclude "index.md" | Remove-Item
