# GrEmit
[![Build Status][build-status-travis]][travis]

GrEmit is a library containing different helpers for generating code using Reflection.Emit with the main one being GroboIL - a smart wrapper over [ILGenerator](http://msdn.microsoft.com/en-us/library/system.reflection.emit.ilgenerator.aspx).

## Usage

GroboIL is a replacement for ILGenerator. Instead of calling ILGenerator.Emit(OpCode, ..), one may call GroboIL.OpCode(..).

### Example

ILGenerator:
```
// .. creating DynamicMethod, MethodBuilder or ConstructorBuilder
var il = method.GetILGenerator();
il.Emit(OpCodes.Ldarg_0);
il.Emit(OpCodes.Ldc_i4_4);
il.EmitCall(OpCodes.Callvirt, someMethod, null);
il.Emit(OpCodes.Ret);
```
GroboIL:
```
// .. creating DynamicMethod, MethodBuilder or ConstructorBuilder
using(var il = new GroboIL(method))
{
    il.Ldarg(0);
    il.Ldc_I4(4);
    il.Call(someMethod);
    il.Ret();
}
```

## Advantages
Besides more beautiful interface GroboIL has some more advantages over ILGenerator:
 - GroboIL has a single method for all instructions from the same family, for instance, instead of 11 instructions OpCodes.Ldelem_* there is one method GroboIL.Ldelem(Type type).
 - During code generation GroboIL builds the content of the evaluation stack and validates instructions arguments, and if something is not OK, immediately throws an Exception.
 - There is a debug ouput of the code being generated.
 - Full generics support.
 - It is possible to debug MethodBuilders (use constructor of GroboIL with parameter [ISymbolDocumentWriter](http://msdn.microsoft.com/en-us/library/system.diagnostics.symbolstore.isymboldocumentwriter.aspx)).
 - Appropriate performance. For instance, once I had to compile a program with 500000 instructions and it was verified by GroboIL in 3 seconds (I guess there is a way to break performance, but in practice such a program won't occure).

Example of debug output:

GroboIL.GetILCode()
```
        ldarg.0              // [List<T>]
        dup                  // [List<T>, List<T>]
        brtrue notNull_0     // [null]
        pop                  // []
        ldc.i4.0             // [Int32]
        newarr T             // [T[]]
notNull_0:                   // [{Object: IList, IList<T>, IReadOnlyList<T>}]
        ldarg.1              // [{Object: IList, IList<T>, IReadOnlyList<T>}, Func<T, Int32>]
        call Int32 Enumerable.Sum<T>(IEnumerable<T>, Func<T, Int32>)
                             // [Int32]
        ret                  // []
```

[build-status-travis]: https://travis-ci.org/skbkontur/gremit.svg?branch=master

[travis]: https://travis-ci.org/skbkontur/gremit
