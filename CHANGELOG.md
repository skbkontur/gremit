# Changelog

## v3.4.x - 2021.03.10
- Add net5.0 support
- Drop netcoreapp2.x support

## v3.3.20 - 2021.02.25
- Adding MIT license file to nuget package

## v3.3.18 - 2020.12.25
- Fix bug with not passing type parameter for `Ldelem_Ref` instruction

## v3.3.7 - 2020.05.23
- Support `Localloc` operation

## v3.3.1 - 2020.04.01
- Sign GrEmit assembly with strong name

## v3.2.2 - 2019.11.21
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
