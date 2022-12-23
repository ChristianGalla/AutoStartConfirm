@echo off

REM Switch to the directory of the batch file
cd %~dp0

msbuild Daily.targets /property:Configuration=Release