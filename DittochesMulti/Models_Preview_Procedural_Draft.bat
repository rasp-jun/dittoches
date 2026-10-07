@echo off
cd /d "%~dp0"
start "Dittoches 3D Model Preview" "Builds\PortablePreview\DittochesMulti.exe" --model-gallery -screen-fullscreen 0 -screen-width 1280 -screen-height 900 -logFile "Builds\PortablePreview\Models.log"
