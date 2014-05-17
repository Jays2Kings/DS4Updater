@echo off
@echo Attempting to replace updater, please wait...
@ping -n 4 127.0.0.1 > nul
"C:\Users\Jonathan\Documents\Projects\Updater2\Updater2\bin\Debug\Updater.exe"
@DEL "%~f0"
