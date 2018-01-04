@echo off
dotnet pack --configuration Release ./GrEmit.sln
cd ./Gremit/bin/Release
dotnet nuget push *.nupkg -s https://nuget.org