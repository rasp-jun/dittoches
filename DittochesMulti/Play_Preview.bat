@echo off
cd /d "%~dp0"
start "Dittoches 3D Preview" "Builds\FaithfulPreview\DittochesMulti.exe" -screen-fullscreen 0 -screen-width 1440 -screen-height 810 -logFile "%~dp0Builds\FaithfulPreview\Player.log"
