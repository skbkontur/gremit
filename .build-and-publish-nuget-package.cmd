dotnet build --force --no-incremental --configuration Release ./GrEmit.sln
dotnet pack --no-build --configuration Release ./GrEmit.sln
cd ./GrEmit/bin/Release
dotnet nuget push *.nupkg -s https://nuget.org
pause