@echo off
cd /d "%~dp0"
start "Digimon Skill Preview" "Builds\PortablePreview\DittochesMulti.exe" --skill-gallery -screen-fullscreen 0 -screen-width 1280 -screen-height 940 -logFile "%~dp0Builds\PortablePreview\Skills.log"
