@echo off

set SolutionName=GrEmit

rem reset current directory to the location of this script
pushd "%~dp0"

if exist "./%SolutionName%/bin" (
    rd "./%SolutionName%/bin" /Q /S || exit /b 1
)

dotnet build --force --no-incremental --configuration Release "./%SolutionName%.sln" || exit /b 1

dotnet pack --no-build --configuration Release "./%SolutionName%.sln" || exit /b 1

pushd "./%SolutionName%/bin/Release"
dotnet nuget push *.nupkg -s https://nuget.org || exit /b 1
popd

pause