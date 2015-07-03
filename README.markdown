#GrEmit

GrEmit is a library containing different helpers for generating code using Reflection.Emit with the main one being GroboIL - smart wrapper over [ILGenerator](http://msdn.microsoft.com/en-us/library/system.reflection.emit.ilgenerator.aspx).

##Usage

GroboIL is a replacement for ILGenerator. Instead of calling ILGenerator.Emit(OpCode, ..), call GroboIL.OpCode(..).

###Example

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

##Advantages
Besides more beautiful interface GroboIL has some more advantages over ILGenerator:
 - GroboIL has one method for all instructions from the same family, for instance, instead of 11 instructions OpCodes.Ldelem_* there is one method GroboIL.Ldelem(Type type).
 - During code generation GroboIL builds the content of the evaluation stack and validates instructions arguments, and if something went wrong, immediately throws an Exception.
 - There is a debug ouput of the code being generated.
 - Full generics support.
 - It is possible to debug MethodBuilders.
 - Appropriate performance. For instance, a program with 500000 instructions will be validated by GroboIL in 3 seconds (I guess there is a way to break performance, but in practice such a program won't occure).

Example of debug output:

GroboIL.GetILCode()
```
        ldarg.0                                                                // [List<TZzz>]
        dup                                                                    // [List<TZzz>, List<TZzz>]
        brtrue notNull_0                                                       // [null]
        pop                                                                    // []
        ldc.i4.0                                                               // [Int32]
        newarr TZzz                                                            // [TZzz[]]
notNull_0:                                                                     // [{Object: IList, IList<TZzz>, IReadOnlyList<TZzz>}]
        ldarg.1                                                                // [{Object: IList, IList<TZzz>, IReadOnlyList<TZzz>}, Func<TZzz, Int32>]
        call Int32 Enumerable.Sum<TZzz>(IEnumerable<TZzz>, Func<TZzz, Int32>)  // [Int32]
        ret                                                                    // []
```