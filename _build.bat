@echo off

set buildToolsPath=%ProgramFiles(x86)%\MSBuild
set msbuild="%buildToolsPath%\14.0\bin\amd64\MSBuild.exe"
set sln=GrEmit\GrEmit.sln
SET VCTargetsPath=%buildToolsPath%\Microsoft.Cpp\v4.0\V140

%msbuild% /v:q /t:Rebuild /p:Configuration=Release /nodeReuse:false /maxcpucount %sln%