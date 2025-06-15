@echo off
REM Run tests with coverage using PowerShell script
powershell -ExecutionPolicy Bypass -File "%~dp0run-tests-with-coverage.ps1" %*
