# Changelog

## v3.0 - 2019.01.14
- Remove IL modification related functionality (`MethodBodyParsing` namespace) entirely since it had been broken 
  after adding .NET Core support.
- Switch tests to run on .NET Core 2.2.

## v2.3 - 2018.09.13
- Use [Nerdbank.GitVersioning](https://github.com/AArnott/Nerdbank.GitVersioning) to automate generation of assembly 
  and nuget package versions.

## v2.2 - 2018.01.01
- Support .NET Standard 2.0 (PR [#9](https://github.com/skbkontur/gremit/pull/9) 
  by [@Amartel1986](https://github.com/Amartel1986)).
- Switch to SDK-style project format and dotnet core build tooling.
