@echo off
cd /d "%~dp0"
start "Dittoches Preview" "Builds\PortablePreview\DittochesMulti.exe" -screen-fullscreen 0 -screen-width 1440 -screen-height 810 -logFile "%~dp0Builds\PortablePreview\Player.log"
