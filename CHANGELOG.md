# Changelog

## v3.2.x - 2019.11.21
- Use [SourceLink](https://github.com/dotnet/sourcelink) to help ReSharper decompiler show actual code.

## v3.2.1 - 2019.09.16
- Support for Mono 6.0

## v3.1.1 - 2019.02.26
- Target .NET Core 2.1 and 2.2 versions.
- `EmitCalli` method with native calling convention is now accessible for clients targeting .NET Core 2.1 or later.
- Switch tests to run on .NET Core 2.0, 2.1 and 2.2.

## v3.0.9 - 2019.01.14
- Remove IL modification related functionality (`MethodBodyParsing` namespace) entirely since it had been broken 
  after adding .NET Core support.
- Switch tests to run on .NET Core 2.2.

## v2.3.1 - 2018.09.13
- Use [Nerdbank.GitVersioning](https://github.com/AArnott/Nerdbank.GitVersioning) to automate generation of assembly 
  and nuget package versions.

## v2.2.0 - 2018.01.01
- Support .NET Standard 2.0 (PR [#9](https://github.com/skbkontur/gremit/pull/9) 
  by [@Amartel1986](https://github.com/Amartel1986)).
- Switch to SDK-style project format and dotnet core build tooling.
