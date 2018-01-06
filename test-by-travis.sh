#!/bin/bash

set -ev

if [ ${DOTNETCORE} -eq 1 ]
then
  dotnet restore ./GrEmit.sln --verbosity m
  dotnet build --configuration Release --framework netstandard2.0 ./GrEmit/GrEmit.csproj
  dotnet build --configuration Release --framework netcoreapp2.0 ./GrEmit.Tests/GrEmit.Tests.csproj
  dotnet test --no-build --configuration Release --framework netcoreapp2.0 ./GrEmit.Tests/GrEmit.Tests.csproj
else
  nuget install NUnit.ConsoleRunner -Version 3.7.0 -OutputDirectory testrunner
  xbuild /p:Configuration=Release ./GrEmit.sln
  mono ./testrunner/NUnit.ConsoleRunner.3.7.0/tools/nunit3-console.exe ./GrEmit.Tests/bin/Release/GrEmit.Tests.dll
fi

