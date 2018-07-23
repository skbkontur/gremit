@echo off

set SolutionName=GrEmit

rem reset current directory to the location of this script
pushd "%~dp0"

if exist "./%GrEmit%/bin" (
    rd "./%GrEmit%/bin" /Q /S || exit /b 1
)

dotnet build --force --no-incremental --configuration Release "./%GrEmit%.sln" || exit /b 1

dotnet pack --no-build --configuration Release "./%GrEmit%.sln" || exit /b 1

pushd "./%GrEmit%/bin/Release"
rem dotnet nuget push *.nupkg -s https://nuget.org || exit /b 1
popd

pause
