@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Open_Faithful_Gallery.ps1" -Model agumon -Demo
if errorlevel 1 pause
