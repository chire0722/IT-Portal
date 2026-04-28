REM Project comment: Run-App.cmd contains Windows command automation for the IT Form project.

@echo off
powershell -ExecutionPolicy Bypass -File "%~dp0Run-App.ps1" %*
