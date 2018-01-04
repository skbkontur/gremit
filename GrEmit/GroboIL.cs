using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

using GrEmit.InstructionComments;
using GrEmit.InstructionParameters;
using GrEmit.Utils;

namespace GrEmit
{
    /// <summary>
    ///     Level of verification of types compatibility
    /// </summary>
    public enum TypesAssignabilityVerificationKind
    {
        /// <summary>
        ///     Performs no checks at all with regard to types assignability
        /// </summary>
        None,

        /// <summary>
        ///     Makes no difference between CLI pointer types (objects, managed pointers, unmanaged pointers).
        ///     Still performs all verifications between these low level types (u can't store int64 to int32 for intance).
        /// </summary>
        LowLevelOnly,

        /// <summary>
        ///     Performs all verifications with regard to types assignability. This is the default behaviour.
        /// </summary>
        HighLevel
    }

    // ReSharper disable InconsistentNaming
    public class GroboIL : IDisposable, IEnumerable
    {
        private GroboIL(ILGenerator il, Type returnType, Type[] parameterTypes, bool analyzeStack, ISymbolDocumentWriter symbolDocumentWriter)
        {
            this.il = il;
            this.analyzeStack = analyzeStack;
            this.symbolDocumentWriter = symbolDocumentWriter;
            methodReturnType = returnType;
            methodParameterTypes = parameterTypes;
            VerificationKind = TypesAssignabilityVerificationKind.HighLevel;
/*
            OpCodes.Localloc;
            OpCodes.Mkrefany;
            OpCodes.Refanytype;
            OpCodes.Refanyval;
            OpCodes.Rethrow;
            OpCodes.Sizeof;
*/
        }

        public GroboIL(DynamicMethod method, bool analyzeStack = true)
            : this(method.GetILGenerator(),
                   method.ReturnType,
                   ReflectionExtensions.GetParameterTypes(method),
                   analyzeStack,
                   null)
        {
        }

        public GroboIL(MethodBuilder method, bool analyzeStack = true)
            : this(method.GetILGenerator(),
                   method.ReturnType,
                   method.IsStatic
                       ? ReflectionExtensions.GetParameterTypes(method)
                       : new[] {method.ReflectedType}.Concat(ReflectionExtensions.GetParameterTypes(method)).ToArray(),
                   analyzeStack,
                   null)
        {
        }

        public GroboIL(MethodBuilder method, ISymbolDocumentWriter symbolDocumentWriter)
            : this(method.GetILGenerator(),
                   method.ReturnType,
                   method.IsStatic
                       ? ReflectionExtensions.GetParameterTypes(method)
                       : new[] {method.ReflectedType}.Concat(ReflectionExtensions.GetParameterTypes(method)).ToArray(),
                   true,
                   symbolDocumentWriter)
        {
            if(symbolDocumentWriter == null)
                throw new ArgumentNullException("symbolDocumentWriter");
        }

        public GroboIL(ConstructorBuilder constructor, bool analyzeStack = true)
            : this(constructor.GetILGenerator(),
                   typeof(void),
                   new[] {constructor.ReflectedType}.Concat(ReflectionExtensions.GetParameterTypes(constructor)).ToArray(),
                   analyzeStack,
                   null)
        {
        }

        public GroboIL(ConstructorBuilder constructor, ISymbolDocumentWriter symbolDocumentWriter)
            : this(constructor.GetILGenerator(),
                   typeof(void),
                   new[] {constructor.ReflectedType}.Concat(ReflectionExtensions.GetParameterTypes(constructor)).ToArray(),
                   true,
                   symbolDocumentWriter)
        {
            if(symbolDocumentWriter == null)
                throw new ArgumentNullException("symbolDocumentWriter");
        }

        /// <summary>
        ///     Gets or sets level of verifications with regard to types compatibility
        /// </summary>
        public TypesAssignabilityVerificationKind VerificationKind { get; set; }

        public void Seal()
        {
            if(!analyzeStack)
                return;
            var lastInstruction = ilCode.Count == 0 ? null : ilCode.GetInstruction(ilCode.Count - 1) as ILCode.ILInstruction;
            if(lastInstruction == null || (lastInstruction.OpCode != OpCodes.Ret && lastInstruction.OpCode != OpCodes.Br
                                           && lastInstruction.OpCode != OpCodes.Br_S && lastInstruction.OpCode != OpCodes.Throw && lastInstruction.OpCode != OpCodes.Jmp))
                throw new InvalidOperationException("An IL program must end with one of the following instructions: 'ret', 'br', 'br.s', 'throw', 'jmp'");
            ilCode.CheckLabels();
        }

        public void Dispose()
        {
            if(!ReflectionExtensions.IsMono)
            {
#if !NETSTANDARD2_0
                if(Marshal.GetExceptionPointers() != IntPtr.Zero)
                    return;
#endif
                if(Marshal.GetExceptionCode() != 0)
                    return;
                Seal();
            }
            if(symbolDocumentWriter != null)
            {
                var linesInfo = ilCode.GetLinesInfo();
                for(var i = 0; i < linesInfo.Value.Count; ++i)
                {
                    var instruction = (ILCode.ILInstruction)ilCode.GetInstruction(i);
                    var parameter = instruction.Parameter;
                    if(instruction.Kind == ILCode.InstructionKind.Instruction || instruction.Kind == ILCode.InstructionKind.DebugWriteLine)
                        MarkSequencePoint(il, symbolDocumentWriter, linesInfo.Value[i].Key, 0, linesInfo.Value[i].Value, 1000);
                    foreach(var prefix in instruction.Prefixes ?? new List<KeyValuePair<OpCode, ILInstructionParameter>>())
                        Emit(prefix.Key, prefix.Value);
                    switch(instruction.Kind)
                    {
                    case ILCode.InstructionKind.Instruction:
                        Emit(instruction.OpCode, parameter);
                        break;
                    case ILCode.InstructionKind.Label:
                        il.MarkLabel(((LabelILInstructionParameter)parameter).Label);
                        break;
                    case ILCode.InstructionKind.DebugWriteLine:
                        if(parameter is StringILInstructionParameter)
                            il.EmitWriteLine(((StringILInstructionParameter)parameter).Value);
                        else
                            il.EmitWriteLine(((LocalILInstructionParameter)parameter).Local);
                        break;
                    case ILCode.InstructionKind.TryStart:
                        il.BeginExceptionBlock();
                        break;
                    case ILCode.InstructionKind.Catch:
                        il.BeginCatchBlock(parameter == null ? null : ((TypeILInstructionParameter)parameter).Type);
                        break;
                    case ILCode.InstructionKind.Fault:
                        il.BeginFaultBlock();
                        break;
                    case ILCode.InstructionKind.FilteredException:
                        il.BeginExceptFilterBlock();
                        break;
                    case ILCode.InstructionKind.Finally:
                        il.BeginFinallyBlock();
                        break;
                    case ILCode.InstructionKind.TryEnd:
                        il.EndExceptionBlock();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                    }
                }
            }
        }

        public string GetILCode()
        {
            return ilCode.ToString();
        }

        /// <summary>
        ///     Declares a local variable of the specified type, optionally pinning the object referred to by the variable.
        /// </summary>
        /// <param name="localType">
        ///     A <see cref="System.Type">Type</see> object that represents the type of the local variable.
        /// </param>
        /// <param name="name">Name of the local being declared.</param>
        /// <param name="pinned">true to pin the object in memory; otherwise, false.</param>
        /// <param name="appendUniquePrefix">true if a unique prefix is to be appended.</param>
        /// <returns>
        ///     A <see cref="Local">Local</see> object that represents the local variable.
        /// </returns>
        public Local DeclareLocal(Type localType, string name, bool pinned = false, bool appendUniquePrefix = true)
        {
            var local = il.DeclareLocal(localType, pinned);
            name = string.IsNullOrEmpty(name) ? "local" : name;
            var uniqueName = !appendUniquePrefix ? name : name + "_" + localId++;
            if (symbolDocumentWriter != null)
                Local.SetLocalSymInfo(local, uniqueName);
            return new Local(local, uniqueName);
        }

        /// <summary>
        ///     Declares a local variable of the specified type, optionally pinning the object referred to by the variable.
        /// </summary>
        /// <param name="localType">
        ///     A <see cref="System.Type">Type</see> object that represents the type of the local variable.
        /// </param>
        /// <param name="pinned">true to pin the object in memory; otherwise, false.</param>
        /// <returns>
        ///     A <see cref="Local">Local</see> object that represents the local variable.
        /// </returns>
        public Local DeclareLocal(Type localType, bool pinned = false)
        {
            var local = il.DeclareLocal(localType, pinned);
            var name = "local_" + localId++;
            if(symbolDocumentWriter != null)
                Local.SetLocalSymInfo(local, name);
            return new Local(local, name);
        }

        /// <summary>
        ///     Declares a new label.
        /// </summary>
        /// <param name="name">Name of label.</param>
        /// <param name="appendUniquePrefix">true if a unique prefix is to be appended.</param>
        /// <returns>
        ///     A <see cref="Label">Label</see> object that can be used as a token for branching.
        /// </returns>
        public Label DefineLabel(string name, bool appendUniquePrefix = true)
        {
            var uniqueName = !appendUniquePrefix ? name : name + "_" + labelId++;
            return new Label(il.DefineLabel(), uniqueName);
        }

        /// <summary>
        ///     Marks the Common intermediate language (CIL) stream's current position with the given label.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to mark the CIL stream's current position with.
        /// </param>
        public void MarkLabel(Label label)
        {
            var stackIsNull = stack == null;
            if(!stackIsNull)
                ilCode.MarkLabel(label, GetComment());
            if(analyzeStack)
                MutateStack(default(OpCode), new LabelILInstructionParameter(label));
            if(stackIsNull)
                ilCode.MarkLabel(label, GetComment());
            if(symbolDocumentWriter == null)
                il.MarkLabel(label);
        }

        /// <summary>
        ///     Emits the Common intermediate language (CIL) to call System.Console.WriteLine with a string.
        /// </summary>
        /// <param name="str">The string to be printed.</param>
        public void WriteLine(string str)
        {
            ilCode.WriteLine(new StringILInstructionParameter(str), GetComment());
            if(symbolDocumentWriter == null)
                il.EmitWriteLine(str);
        }

        /// <summary>
        ///     Emits the Common intermediate language (CIL) to call System.Console.WriteLine with the given local variable.
        /// </summary>
        /// <param name="local">The local variable whose value is to be written to the console.</param>
        public void WriteLine(Local local)
        {
            ilCode.WriteLine(new LocalILInstructionParameter(local), GetComment());
            if(symbolDocumentWriter == null)
                il.EmitWriteLine(local);
        }

        /// <summary>
        ///     Marks a sequence point in the Common intermediate language (CIL) stream.
        /// </summary>
        /// <param name="document">The document for which the sequence point is being defined.</param>
        /// <param name="startLine">The line where the sequence point begins.</param>
        /// <param name="startColumn">The column in the line where the sequence point begins.</param>
        /// <param name="endLine">The line where the sequence point ends.</param>
        /// <param name="endColumn">The column in the line where the sequence point ends.</param>
        public void MarkSequencePoint(ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
        {
            MarkSequencePoint(il, document, startLine, startColumn, endLine, endColumn);
        }

        public static void MarkSequencePoint(ILGenerator il, ISymbolDocumentWriter document, int startLine, int startColumn, int endLine, int endColumn)
        {
#if NETSTANDARD2_0
            throw new NotSupportedException("Not supported for netstandard2.0");
#else
            il.MarkSequencePoint(document, startLine, startColumn, endLine, endColumn);
#endif
        }

        /// <summary>
        ///     Begins an exception block for a non-filtered exception.
        /// </summary>
        public void BeginExceptionBlock()
        {
            ilCode.BeginExceptionBlock(GetComment());
            if(symbolDocumentWriter == null)
                il.BeginExceptionBlock();
        }

        /// <summary>
        ///     Begins a catch block.
        /// </summary>
        /// <param name="exceptionType">
        ///     The <see cref="Type">Type</see> object that represents the exception.
        /// </param>
        public void BeginCatchBlock(Type exceptionType)
        {
            if(analyzeStack)
                stack = new EvaluationStack(new ESType[] {new SimpleESType(exceptionType ?? typeof(Exception))});
            ilCode.BeginCatchBlock(exceptionType == null ? null : new TypeILInstructionParameter(exceptionType), GetComment());
            if(symbolDocumentWriter == null)
                il.BeginCatchBlock(exceptionType);
        }

        /// <summary>
        ///     Begins an exception block for a filtered exception.
        /// </summary>
        public void BeginExceptFilterBlock()
        {
            if(analyzeStack)
                stack = new EvaluationStack(new ESType[] {new SimpleESType(typeof(Exception))});
            ilCode.BeginExceptFilterBlock(GetComment());
            if(symbolDocumentWriter == null)
                il.BeginExceptFilterBlock();
        }

        /// <summary>
        ///     Begins an exception fault block in the Common intermediate language (CIL) stream.
        /// </summary>
        public void BeginFaultBlock()
        {
            if(analyzeStack)
                stack = new EvaluationStack();
            ilCode.BeginFaultBlock(GetComment());
            if(symbolDocumentWriter == null)
                il.BeginFaultBlock();
        }

        /// <summary>
        ///     Begins a finally block in the Common intermediate language (CIL) instruction stream.
        /// </summary>
        public void BeginFinallyBlock()
        {
            if(analyzeStack)
                stack = new EvaluationStack();
            ilCode.BeginFinallyBlock(GetComment());
            if(symbolDocumentWriter == null)
                il.BeginFinallyBlock();
        }

        /// <summary>
        ///     Ends an exception block.
        /// </summary>
        public void EndExceptionBlock()
        {
            ilCode.EndExceptionBlock(GetComment());
            if(symbolDocumentWriter == null)
                il.EndExceptionBlock();
        }

        /// <summary>
        ///     Signals the Common Language Infrastructore (CLI) to inform the debugger that a break point has been tripped
        /// </summary>
        public void Break()
        {
            Emit(OpCodes.Break);
        }

        /// <summary>
        ///     Fills space if opcodes are patched. No meaningful operation is performed although a processing cycle can be consumed.
        /// </summary>
        public void Nop()
        {
            Emit(OpCodes.Nop);
        }

        /// <summary>
        ///     Throws the exception object currently on the evaluation stack.
        /// </summary>
        public void Throw()
        {
            Emit(OpCodes.Throw);
        }

        /// <summary>
        ///     Rethrows the current exception.
        /// </summary>
        public void Rethrow()
        {
            Emit(OpCodes.Rethrow);
        }

        /// <summary>
        ///     Implements a jump table.
        /// </summary>
        /// <param name="labels">
        ///     The array of <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Switch(params Label[] labels)
        {
            if(labels == null)
                throw new ArgumentNullException("labels");
            if(labels.Length == 0)
                throw new ArgumentException("At least one label must be specified", "labels");
            if(labels.Any(label => label == null))
                throw new ArgumentException("All labels must be specified", "labels");
            Emit(OpCodes.Switch, labels);
        }

        /// <summary>
        ///     Returns from the current method, pushing a return value (if present) from the callee's evaluation stack onto the caller's evaluation stack.
        /// </summary>
        public void Ret()
        {
            Emit(OpCodes.Ret);
            stack = null;
        }

        /// <summary>
        ///     Exits a protected region of code, unconditionally transferring control to a specific target instruction.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Leave(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Leave, label);
            stack = null;
        }

        /// <summary>
        ///     Exits current method and jumps to specified method.
        ///     <param name="method">
        ///         The <see cref="MethodInfo">Method</see> to jump to.
        ///     </param>
        /// </summary>
        public void Jmp(MethodInfo method)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            if(method.ReturnType != methodReturnType)
                throw new ArgumentException(string.Format("The return type must be of type '{0}'", methodReturnType), "method");
            var parameterTypes = method.GetParameters().Select(info => info.ParameterType).ToArray();
            if(parameterTypes.Length != methodParameterTypes.Length)
                throw new ArgumentException(string.Format("The number of arguments must be equal to {0}", methodParameterTypes.Length), "method");
            for(var i = 0; i < parameterTypes.Length; ++i)
            {
                if(parameterTypes[i] != methodParameterTypes[i])
                    throw new ArgumentException(string.Format("The argument #{0} must be of type '{1}'", i + 1, methodParameterTypes[i]), "method");
            }
            Emit(OpCodes.Jmp, method);
        }

        /// <summary>
        ///     Unconditionally transfers control to a target instruction.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Br(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Br, label);
            stack = null;
        }

        /// <summary>
        ///     Transfers control to a target instruction if value is false, a null reference, or zero.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Brfalse(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Brfalse, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction if value is true, not null, or non-zero.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Brtrue(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Brtrue, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction if the first value is less than or equal to the second value.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        /// <param name="unsigned">
        ///     True if treat values being compared as unsigned.
        ///     <para></para>
        ///     Depending on that flag either <see cref="OpCodes.Ble">Ble</see> or <see cref="OpCodes.Ble_Un">Ble_Un</see> instruction will be emitted.
        /// </param>
        public void Ble(Label label, bool unsigned)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(unsigned ? OpCodes.Ble_Un : OpCodes.Ble, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction if the first value is greater than or equal to the second value.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        /// <param name="unsigned">
        ///     True if treat values being compared as unsigned.
        ///     <para></para>
        ///     Depending on that flag either <see cref="OpCodes.Bge">Bge</see> or <see cref="OpCodes.Bge_Un">Bge_Un</see> instruction will be emitted.
        /// </param>
        public void Bge(Label label, bool unsigned)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(unsigned ? OpCodes.Bge_Un : OpCodes.Bge, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction if the first value is less than the second value.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        /// <param name="unsigned">
        ///     True if treat values being compared as unsigned.
        ///     <para></para>
        ///     Depending on that flag either <see cref="OpCodes.Blt">Blt</see> or <see cref="OpCodes.Blt_Un">Blt_Un</see> instruction will be emitted.
        /// </param>
        public void Blt(Label label, bool unsigned)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(unsigned ? OpCodes.Blt_Un : OpCodes.Blt, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction if the first value is greater than the second value.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        /// <param name="unsigned">
        ///     True if treat values being compared as unsigned.
        ///     <para></para>
        ///     Depending on that flag either <see cref="OpCodes.Bgt">Bgt</see> or <see cref="OpCodes.Bgt_Un">Bgt_Un</see> instruction will be emitted.
        /// </param>
        public void Bgt(Label label, bool unsigned)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(unsigned ? OpCodes.Bgt_Un : OpCodes.Bgt, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction when two unsigned integer values or unordered float values are not equal.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Bne_Un(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Bne_Un, label);
        }

        /// <summary>
        ///     Transfers control to a target instruction if two values are equal.
        /// </summary>
        /// <param name="label">
        ///     The <see cref="Label">Label</see> object to jump to.
        /// </param>
        public void Beq(Label label)
        {
            if(label == null)
                throw new ArgumentNullException("label");
            Emit(OpCodes.Beq, label);
        }

        /// <summary>
        ///     Compares two values. If they are equal, the integer value 1 (int32) is pushed onto the evaluation stack; otherwise 0 (int32) is pushed onto the evaluation stack.
        /// </summary>
        public void Ceq()
        {
            Emit(OpCodes.Ceq);
        }

        /// <summary>
        ///     Compares two values. If the first value is greater than the second, the integer value 1 (int32) is pushed onto the evaluation stack; otherwise 0 (int32) is pushed onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat values being compared as unsigned.
        ///     <para></para>
        ///     Depending on that flag either <see cref="OpCodes.Cgt">Cgt</see> or <see cref="OpCodes.Cgt_Un">Cgt_Un</see> instruction will be emitted.
        /// </param>
        public void Cgt(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Cgt_Un : OpCodes.Cgt);
        }

        /// <summary>
        ///     Compares two values. If the first value is less than the second, the integer value 1 (int32) is pushed onto the evaluation stack; otherwise 0 (int32) is pushed onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat values being compared as unsigned.
        ///     <para></para>
        ///     Depending on that flag either <see cref="OpCodes.Clt">Clt</see> or <see cref="OpCodes.Clt_Un">Clt_Un</see> instruction will be emitted.
        /// </param>
        public void Clt(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Clt_Un : OpCodes.Clt);
        }

        /// <summary>
        ///     Removes the value currently on top of the evaluation stack.
        /// </summary>
        public void Pop()
        {
            Emit(OpCodes.Pop);
        }

        /// <summary>
        ///     Copies the current topmost value on the evaluation stack, and then pushes the copy onto the evaluation stack.
        /// </summary>
        public void Dup()
        {
            Emit(OpCodes.Dup);
        }

        /// <summary>
        ///     Loads the address of the local variable at a specific index onto the evaluation stack.
        /// </summary>
        /// <param name="local">
        ///     The <see cref="Local">Local</see> object whose address needs to be loaded onto the evaluation stack.
        /// </param>
        public void Ldloca(Local local)
        {
            Emit(OpCodes.Ldloca, local);
        }

        /// <summary>
        ///     Loads the local variable at a specific index onto the evaluation stack.
        /// </summary>
        /// <param name="local">
        ///     The <see cref="Local">Local</see> object which needs to be loaded onto the evaluation stack.
        /// </param>
        public void Ldloc(Local local)
        {
            Emit(OpCodes.Ldloc, local);
        }

        /// <summary>
        ///     Pops the current value from the top of the evaluation stack and stores it in a the local variable list at a specified index.
        /// </summary>
        /// <param name="local">
        ///     The <see cref="Local">Local</see> object in which the value must be stored.
        /// </param>
        public void Stloc(Local local)
        {
            Emit(OpCodes.Stloc, local);
        }

        /// <summary>
        ///     Pushes a null reference (type O) onto the evaluation stack.
        /// </summary>
        public void Ldnull()
        {
            Emit(OpCodes.Ldnull, new TypeILInstructionParameter(null));
        }

        /// <summary>
        ///     Initializes each field of the value type at a specified address to a null reference or a 0 of the appropriate primitive type.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of object being initialized. Must be a value type.
        /// </param>
        public void Initobj(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Initobj, type);
        }

        /// <summary>
        ///     Copies the value type located at the address of an object (type &amp;, * or native int) to the address of the destination object (type &amp;, * or native int).
        ///     <para></para>
        ///     The parameters are: a destination address and a source address.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of objects being copied. Must be a value type.
        /// </param>
        public void Cpobj(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Cpobj, type);
        }

        /// <summary>
        ///     Loads an argument (referenced by a specified index value) onto the evaluation stack.
        /// </summary>
        /// <param name="index">
        ///     Index of the argument being pushed.
        ///     <para></para>
        ///     Depending on that index emits on of the following instructions:
        ///     <para></para>
        ///     <see cref="OpCodes.Ldarg_0">Ldarg_0</see>, <see cref="OpCodes.Ldarg_1">Ldarg_1</see>, <see cref="OpCodes.Ldarg_2">Ldarg_2</see>,
        ///     <see cref="OpCodes.Ldarg_3">Ldarg_3</see>, <see cref="OpCodes.Ldarg_S">Ldarg_S</see>, <see cref="OpCodes.Ldarg">Ldarg</see>
        /// </param>
        public void Ldarg(int index)
        {
            if(index < 0)
                throw new ArgumentOutOfRangeException("index", "Argument index cannot be less than zero");
            if(index >= methodParameterTypes.Length)
                throw new ArgumentOutOfRangeException("index", "Argument index cannot be greater than or equal to the number of parameters of the method being emitted");

            switch(index)
            {
            case 0:
                Emit(OpCodes.Ldarg_0);
                break;
            case 1:
                Emit(OpCodes.Ldarg_1);
                break;
            case 2:
                Emit(OpCodes.Ldarg_2);
                break;
            case 3:
                Emit(OpCodes.Ldarg_3);
                break;
            default:
                if(index < 256)
                    Emit(OpCodes.Ldarg_S, (byte)index);
                else
                    Emit(OpCodes.Ldarg, index);
                break;
            }
        }

        /// <summary>
        ///     Stores the value on top of the evaluation stack in the argument slot at a specified index.
        /// </summary>
        /// <param name="index">
        ///     Index of the argument to store the value in.
        ///     <para></para>
        ///     Depending on that index emits either <see cref="OpCodes.Starg_S">Starg_S</see> or <see cref="OpCodes.Starg">Starg</see> instruction.
        /// </param>
        public void Starg(int index)
        {
            if(index < 0)
                throw new ArgumentOutOfRangeException("index", "Argument index cannot be less than zero");
            if(index >= methodParameterTypes.Length)
                throw new ArgumentOutOfRangeException("index", "Argument index cannot be greater than or equal to the number of parameters of the method being emitted");
            if(index < 256)
                Emit(OpCodes.Starg_S, (byte)index);
            else
                Emit(OpCodes.Starg, index);
        }

        /// <summary>
        ///     Load an argument address onto the evaluation stack.
        /// </summary>
        /// <param name="index">
        ///     Index of the argument to load address of.
        ///     <para></para>
        ///     Depending on that index emits either <see cref="OpCodes.Ldarga_S">Ldarga_S</see> or <see cref="OpCodes.Ldarga">Ldarga</see> instruction.
        /// </param>
        public void Ldarga(int index)
        {
            if(index < 0)
                throw new ArgumentOutOfRangeException("index", "Argument index cannot be less than zero");
            if(index >= methodParameterTypes.Length)
                throw new ArgumentOutOfRangeException("index", "Argument index cannot be greater than or equal to the number of parameters of the method being emitted");
            if(index < 256)
                Emit(OpCodes.Ldarga_S, (byte)index);
            else
                Emit(OpCodes.Ldarga, index);
        }

        /// <summary>
        ///     Returns an unmanaged pointer to the argument list of the current method
        /// </summary>
        public void Arglist()
        {
            Emit(OpCodes.Arglist);
        }

        /// <summary>
        ///     Pushes a supplied value of type int32 onto the evaluation stack as an int32.
        /// </summary>
        /// <param name="value">
        ///     The value to push.
        ///     <para></para>
        ///     Depending on the value emits one of the following instructions:
        ///     <para></para>
        ///     <see cref="OpCodes.Ldc_I4_0">Ldc_I4_0</see>, <see cref="OpCodes.Ldc_I4_1">Ldc_I4_1</see>, <see cref="OpCodes.Ldc_I4_2">Ldc_I4_2</see>, <see cref="OpCodes.Ldc_I4_3">Ldc_I4_3</see>,
        ///     <see cref="OpCodes.Ldc_I4_4">Ldc_I4_4</see>, <see cref="OpCodes.Ldc_I4_5">Ldc_I4_5</see>, <see cref="OpCodes.Ldc_I4_6">Ldc_I4_6</see>, <see cref="OpCodes.Ldc_I4_7">Ldc_I4_7</see>,
        ///     <see cref="OpCodes.Ldc_I4_8">Ldc_I4_8</see>, <see cref="OpCodes.Ldc_I4_M1">Ldc_I4_M1</see>, <see cref="OpCodes.Ldc_I4_S">Ldc_I4_S</see>, <see cref="OpCodes.Ldc_I4">Ldc_I4</see>
        /// </param>
        public void Ldc_I4(int value)
        {
            switch(value)
            {
            case 0:
                Emit(OpCodes.Ldc_I4_0);
                break;
            case 1:
                Emit(OpCodes.Ldc_I4_1);
                break;
            case 2:
                Emit(OpCodes.Ldc_I4_2);
                break;
            case 3:
                Emit(OpCodes.Ldc_I4_3);
                break;
            case 4:
                Emit(OpCodes.Ldc_I4_4);
                break;
            case 5:
                Emit(OpCodes.Ldc_I4_5);
                break;
            case 6:
                Emit(OpCodes.Ldc_I4_6);
                break;
            case 7:
                Emit(OpCodes.Ldc_I4_7);
                break;
            case 8:
                Emit(OpCodes.Ldc_I4_8);
                break;
            case -1:
                Emit(OpCodes.Ldc_I4_M1);
                break;
            default:
                if(value < 128 && value >= -128)
                    Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                else
                    Emit(OpCodes.Ldc_I4, value);
                break;
            }
        }

        /// <summary>
        ///     Pushes a supplied value of type int64 onto the evaluation stack as an int64.
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Ldc_I8(long value)
        {
            Emit(OpCodes.Ldc_I8, value);
        }

        /// <summary>
        ///     Pushes a supplied value of type float32 onto the evaluation stack as type F (float).
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Ldc_R4(float value)
        {
            Emit(OpCodes.Ldc_R4, value);
        }

        /// <summary>
        ///     Pushes a supplied value of type float64 onto the evaluation stack as type F (float).
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Ldc_R8(double value)
        {
            Emit(OpCodes.Ldc_R8, value);
        }

        /// <summary>
        ///     Pushes a supplied value of type native int onto the evaluation stack.
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Ldc_IntPtr(IntPtr value)
        {
            if(IntPtr.Size == 4)
                Ldc_I4(value.ToInt32());
            else
                Ldc_I8(value.ToInt64());
            Conv<IntPtr>();
        }

        /// <summary>
        ///     Clears the specified pinned local by setting it to null
        /// </summary>
        /// <param name="local"></param>
        public void FreePinnedLocal(Local local)
        {
            if(!ReflectionExtensions.IsMono || !(local.Type.IsPointer || local.Type.IsByRef))
            {
                Ldnull();
                Stloc(local);
            }
            else
            {
                var prevVerificationKind = VerificationKind;
                VerificationKind = TypesAssignabilityVerificationKind.LowLevelOnly;
                Ldc_I4(0);
                Conv<UIntPtr>();
                Stloc(local);
                VerificationKind = prevVerificationKind;
            }
        }

        /// <summary>
        ///     Pushes the number of elements of a zero-based, one-dimensional array onto the evaluation stack.
        /// </summary>
        public void Ldlen()
        {
            Emit(OpCodes.Ldlen);
        }

        /// <summary>
        ///     Pushes an unmanaged pointer (type native int) to the native code implementing a specific method onto the evaluation stack.
        /// </summary>
        /// <param name="method">The method to load address of.</param>
        public void Ldftn(MethodInfo method)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            Emit(OpCodes.Ldftn, method);
        }

        /// <summary>
        ///     Pushes an unmanaged pointer (type native int) to the native code implementing a particular virtual method associated with a specified object onto the evaluation stack.
        /// </summary>
        /// <param name="method">The method to load address of.</param>
        public void Ldvirtftn(MethodInfo method)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            Emit(OpCodes.Ldvirtftn, method);
        }

        /// <summary>
        ///     Replaces the value of a field with a value from the evaluation stack.
        /// </summary>
        /// <param name="field">
        ///     The field to store value in.
        ///     <para></para>
        ///     Depending on whether the field is static or not emits either <see cref="OpCodes.Stsfld">Stsfld</see> or <see cref="OpCodes.Stfld">Stfld</see> respectively.
        /// </param>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Stfld(FieldInfo field, bool isVolatile = false, int? unaligned = null)
        {
            if(field == null)
                throw new ArgumentNullException("field");
            if(field.IsStatic && unaligned != null)
                throw new ArgumentException("Static fields are always aligned to the natural size", "unaligned");
            InsertPrefixes(isVolatile, unaligned);
            Emit(field.IsStatic ? OpCodes.Stsfld : OpCodes.Stfld, field);
        }

        /// <summary>
        ///     Pushes the value of a field onto the evaluation stack.
        /// </summary>
        /// <param name="field">
        ///     The field to load value of.
        ///     <para></para>
        ///     Depending on whether the field is static or not emits either <see cref="OpCodes.Ldsfld">Ldsfld</see> or <see cref="OpCodes.Ldfld">Ldfld</see> respectively.
        /// </param>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Ldfld(FieldInfo field, bool isVolatile = false, int? unaligned = null)
        {
            if(field == null)
                throw new ArgumentNullException("field");
            if(field.IsStatic && unaligned != null)
                throw new ArgumentException("Static fields are always aligned to the natural size", "unaligned");
            InsertPrefixes(isVolatile, unaligned);
            Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
        }

        /// <summary>
        ///     Pushes the address of a field onto the evaluation stack.
        /// </summary>
        /// <param name="field">
        ///     The field to load address of.
        ///     <para></para>
        ///     Depending on whether the field is static or not emits either <see cref="OpCodes.Ldsflda">Ldsflda</see> or <see cref="OpCodes.Ldflda">Ldflda</see> respectively.
        /// </param>
        public void Ldflda(FieldInfo field)
        {
            if(field == null)
                throw new ArgumentNullException("field");
            Emit(field.IsStatic ? OpCodes.Ldsflda : OpCodes.Ldflda, field);
        }

        /// <summary>
        ///     Loads the address of the array element at a specified array index onto the top of the evaluation stack as type &amp; (managed pointer).
        /// </summary>
        /// <param name="elementType">The element type of the array.</param>
        /// <param name="asReadonly">
        ///     True if the result address should be read only. Emits the <see cref="OpCodes.Readonly">Readonly</see> prefix.
        /// </param>
        public void Ldelema(Type elementType, bool asReadonly = false)
        {
            if(elementType == null)
                throw new ArgumentNullException("elementType");
            if(asReadonly)
            {
                if(analyzeStack)
                    ilCode.AppendPrefix(OpCodes.Readonly, null);
                if(symbolDocumentWriter == null)
                    il.Emit(OpCodes.Readonly);
            }
            Emit(OpCodes.Ldelema, elementType);
        }

        /// <summary>
        ///     Loads the element at a specified array index onto the top of the evaluation stack.
        /// </summary>
        /// <param name="elementType">
        ///     The element type of the array.
        ///     <para></para>
        ///     Depending on that type emits one of the following instructions:
        ///     <para></para>
        ///     <see cref="OpCodes.Ldelem_Ref">Ldelem_Ref</see>, <see cref="OpCodes.Ldelem_I">Ldelem_I</see>, <see cref="OpCodes.Ldelem_I1">Ldelem_I1</see>, <see cref="OpCodes.Ldelem_I2">Ldelem_I2</see>,
        ///     <see cref="OpCodes.Ldelem_I4">Ldelem_I4</see>, <see cref="OpCodes.Ldelem_I8">Ldelem_I8</see>, <see cref="OpCodes.Ldelem_U1">Ldelem_U1</see>, <see cref="OpCodes.Ldelem_U2">Ldelem_U2</see>,
        ///     <see cref="OpCodes.Ldelem_U4">Ldelem_U4</see>, <see cref="OpCodes.Ldelem_R4">Ldelem_R4</see>, <see cref="OpCodes.Ldelem_R8">Ldelem_R8</see>
        ///     <para></para>
        ///     If the element type is a user-defined value type emits <see cref="OpCodes.Ldelema">Ldelema</see> &amp; <see cref="OpCodes.Ldobj">Ldobj</see> instructions.
        /// </param>
        public void Ldelem(Type elementType)
        {
            if(elementType == null)
                throw new ArgumentNullException("elementType");
            if(IsStruct(elementType))
            {
                // struct
                Ldelema(elementType);
                Ldobj(elementType);
                return;
            }
            var parameter = new TypeILInstructionParameter(elementType);
            if(!elementType.IsValueType) // class
                Emit(OpCodes.Ldelem_Ref, parameter);
            else if(elementType == typeof(IntPtr) || elementType == typeof(UIntPtr))
                Emit(OpCodes.Ldelem_I, parameter);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                    Emit(OpCodes.Ldelem_I1, parameter);
                    break;
                case TypeCode.Byte:
                    Emit(OpCodes.Ldelem_U1, parameter);
                    break;
                case TypeCode.Int16:
                    Emit(OpCodes.Ldelem_I2, parameter);
                    break;
                case TypeCode.Int32:
                    Emit(OpCodes.Ldelem_I4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Ldelem_I8, parameter);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(OpCodes.Ldelem_U2, parameter);
                    break;
                case TypeCode.UInt32:
                    Emit(OpCodes.Ldelem_U4, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Ldelem_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Ldelem_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + elementType.Name + "' is not supported");
                }
            }
        }

        /// <summary>
        ///     Replaces the array element at a given index with the value on the evaluation stack.
        /// </summary>
        /// <param name="elementType">
        ///     The element type of the array.
        ///     <para></para>
        ///     Depending on that type emits one of the following instructions:
        ///     <para></para>
        ///     <see cref="OpCodes.Stelem_Ref">Stelem_Ref</see>, <see cref="OpCodes.Stelem_I">Stelem_I</see>, <see cref="OpCodes.Stelem_I1">Stelem_I1</see>, <see cref="OpCodes.Stelem_I2">Stelem_I2</see>,
        ///     <see cref="OpCodes.Stelem_I4">Stelem_I4</see>, <see cref="OpCodes.Stelem_I8">Stelem_I8</see>, <see cref="OpCodes.Stelem_R4">Stelem_R4</see>, <see cref="OpCodes.Stelem_R8">Stelem_R8</see>
        ///     <para></para>
        ///     DOES NOT WORK if the element type is a user-defined value type. In such a case emit <see cref="OpCodes.Ldelema">Ldelema</see> &amp; <see cref="OpCodes.Stobj">Stobj</see> instructions.
        /// </param>
        public void Stelem(Type elementType)
        {
            if(elementType == null)
                throw new ArgumentNullException("elementType");
            if(IsStruct(elementType))
                throw new InvalidOperationException("To store an item to an array of structs use Ldelema & Stobj instructions");

            var parameter = new TypeILInstructionParameter(elementType);
            if(!elementType.IsValueType) // class
                Emit(OpCodes.Stelem_Ref, parameter);
            else if(elementType == typeof(IntPtr) || elementType == typeof(UIntPtr))
                Emit(OpCodes.Stelem_I, parameter);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(elementType))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    Emit(OpCodes.Stelem_I1, parameter);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                case TypeCode.Int16:
                    Emit(OpCodes.Stelem_I2, parameter);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    Emit(OpCodes.Stelem_I4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Stelem_I8, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Stelem_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Stelem_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + elementType.Name + "' is not supported");
                }
            }
        }

        /// <summary>
        ///     Stores a value of a specified type at a specified address.
        /// </summary>
        /// <param name="type">
        ///     The type of a value being stored.
        ///     <para></para>
        ///     Depending on that type emits one of the following instructions:
        ///     <para></para>
        ///     <see cref="OpCodes.Stind_Ref">Stind_Ref</see>, <see cref="OpCodes.Stind_I1">Stind_I1</see>, <see cref="OpCodes.Stind_I2">Stind_I2</see>, <see cref="OpCodes.Stind_I4">Stind_I4</see>,
        ///     <see cref="OpCodes.Stind_I8">Stind_I8</see>, <see cref="OpCodes.Stind_R4">Stind_R4</see>, <see cref="OpCodes.Stind_R8">Stind_R8</see>
        ///     <para></para>
        ///     If the value is of a user-defined value type emits <see cref="OpCodes.Stobj">Stobj</see> instruction.
        /// </param>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Stind(Type type, bool isVolatile = false, int? unaligned = null)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(IsStruct(type))
            {
                Stobj(type, isVolatile, unaligned);
                return;
            }

            InsertPrefixes(isVolatile, unaligned);

            var parameter = new TypeILInstructionParameter(type);
            if(!type.IsValueType) // class
                Emit(OpCodes.Stind_Ref, parameter);
            else
            {
                // Primitive
                switch(Type.GetTypeCode(type))
                {
                case TypeCode.Boolean:
                case TypeCode.SByte:
                case TypeCode.Byte:
                    Emit(OpCodes.Stind_I1, parameter);
                    break;
                case TypeCode.Int16:
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(OpCodes.Stind_I2, parameter);
                    break;
                case TypeCode.Int32:
                case TypeCode.UInt32:
                    Emit(OpCodes.Stind_I4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Stind_I8, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Stind_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Stind_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + type.Name + "' is not supported");
                }
            }
        }

        /// <summary>
        ///     Loads a value of a specifed type onto the evaluation stack indirectly.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of a value being loaded.
        ///     <para></para>
        ///     Depending on that type emits one of the following instructions:
        ///     <para></para>
        ///     <see cref="OpCodes.Ldind_Ref">Ldind_Ref</see>, <see cref="OpCodes.Ldind_I1">Ldind_I1</see>, <see cref="OpCodes.Ldind_I2">Ldind_I2</see>, <see cref="OpCodes.Ldind_I4">Ldind_I4</see>,
        ///     <see cref="OpCodes.Ldind_I8">Ldind_I8</see>, <see cref="OpCodes.Ldind_U1">Ldind_U1</see>, <see cref="OpCodes.Ldind_U2">Ldind_U2</see>, <see cref="OpCodes.Ldind_U4">Ldind_U4</see>,
        ///     <see cref="OpCodes.Ldind_R4">Ldind_R4</see>, <see cref="OpCodes.Ldind_R8">Ldind_R8</see>
        ///     <para></para>
        ///     If the value is of a user-defined value type emits <see cref="OpCodes.Ldobj">Ldobj</see> instruction.
        /// </param>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Ldind(Type type, bool isVolatile = false, int? unaligned = null)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(IsStruct(type))
            {
                Ldobj(type, isVolatile, unaligned);
                return;
            }

            InsertPrefixes(isVolatile, unaligned);

            var parameter = new TypeILInstructionParameter(type);
            if(!type.IsValueType) // class
                Emit(OpCodes.Ldind_Ref, parameter);
            else
            {
                switch(Type.GetTypeCode(type))
                {
                case TypeCode.SByte:
                    Emit(OpCodes.Ldind_I1, parameter);
                    break;
                case TypeCode.Byte:
                case TypeCode.Boolean:
                    Emit(OpCodes.Ldind_U1, parameter);
                    break;
                case TypeCode.Int16:
                    Emit(OpCodes.Ldind_I2, parameter);
                    break;
                case TypeCode.Char:
                case TypeCode.UInt16:
                    Emit(OpCodes.Ldind_U2, parameter);
                    break;
                case TypeCode.Int32:
                    Emit(OpCodes.Ldind_I4, parameter);
                    break;
                case TypeCode.UInt32:
                    Emit(OpCodes.Ldind_U4, parameter);
                    break;
                case TypeCode.Int64:
                case TypeCode.UInt64:
                    Emit(OpCodes.Ldind_I8, parameter);
                    break;
                case TypeCode.Single:
                    Emit(OpCodes.Ldind_R4, parameter);
                    break;
                case TypeCode.Double:
                    Emit(OpCodes.Ldind_R8, parameter);
                    break;
                default:
                    throw new NotSupportedException("Type '" + type.Name + "' is not supported");
                }
            }
        }

        /// <summary>
        ///     Copies a specified number of bytes from a source address to a destination address.
        ///     <para></para>
        ///     The parameters are: a destination address, a source address and a number of bytes to copy.
        /// </summary>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Cpblk(bool isVolatile = false, int? unaligned = null)
        {
            InsertPrefixes(isVolatile, unaligned);
            Emit(OpCodes.Cpblk);
        }

        /// <summary>
        ///     Initializes a specified block of memory at a specific address to a given size and initial value.
        ///     <para></para>
        ///     The parameters are: an address, an initial value and a number of bytes.
        /// </summary>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Initblk(bool isVolatile = false, int? unaligned = null)
        {
            InsertPrefixes(isVolatile, unaligned);
            Emit(OpCodes.Initblk);
        }

        /// <summary>
        ///     Converts a metadata token of a specified type to its runtime representation, pushing it onto the evaluation stack.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> object metadata token of which is being pushed onto the evaluation stack.
        /// </param>
        public void Ldtoken(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            Emit(OpCodes.Ldtoken, type);
        }

        /// <summary>
        ///     Converts a metadata token of a specified method to its runtime representation, pushing it onto the evaluation stack.
        /// </summary>
        /// <param name="method">
        ///     The <see cref="MethodInfo">MethodInfo</see> object metadata token of which is being pushed onto the evaluation stack.
        /// </param>
        public void Ldtoken(MethodInfo method)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            Emit(OpCodes.Ldtoken, method);
        }

        /// <summary>
        ///     Converts a metadata token of a specified field to its runtime representation, pushing it onto the evaluation stack.
        /// </summary>
        /// <param name="field">
        ///     The <see cref="FieldInfo">FieldInfo</see> object metadata token of which is being pushed onto the evaluation stack.
        /// </param>
        public void Ldtoken(FieldInfo field)
        {
            if(field == null)
                throw new ArgumentNullException("field");
            Emit(OpCodes.Ldtoken, field);
        }

        /// <summary>
        ///     Attempts to cast an object passed by reference to the specified class.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> to cast an object to.
        /// </param>
        public void Castclass(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(type.IsValueType)
                throw new ArgumentException("A reference type expected", "type");
            Emit(OpCodes.Castclass, type);
        }

        /// <summary>
        ///     Tests whether an object reference (type O) is an instance of a particular class.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> to test.
        /// </param>
        public void Isinst(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            Emit(OpCodes.Isinst, type);
        }

        /// <summary>
        ///     Converts the boxed representation of a type specified in the instruction to its unboxed form.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of boxed object. Must be a value type.
        /// </param>
        public void Unbox_Any(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType && !type.IsGenericParameter)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Unbox_Any, type);
        }

        /// <summary>
        ///     Converts a value type to an object reference (type O).
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of object to box.
        /// </param>
        public void Box(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType && !type.IsGenericParameter)
                throw new ArgumentException("A value type expected", "type");
            Emit(OpCodes.Box, type);
        }

        /// <summary>
        ///     Copies a value of a specified type from the evaluation stack into a supplied memory address.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of object to be stored.
        /// </param>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Stobj(Type type, bool isVolatile = false, int? unaligned = null)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            InsertPrefixes(isVolatile, unaligned);
            Emit(OpCodes.Stobj, type);
        }

        /// <summary>
        ///     Copies the value type object pointed to by an address to the top of the evaluation stack.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of object to be loaded.
        /// </param>
        /// <param name="isVolatile">True if an address on top of the evaluation stack must be treated as volatile.</param>
        /// <param name="unaligned">The value of alignment and null if address is aligned to the natural size.</param>
        public void Ldobj(Type type, bool isVolatile = false, int? unaligned = null)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            if(!type.IsValueType)
                throw new ArgumentException("A value type expected", "type");
            InsertPrefixes(isVolatile, unaligned);
            Emit(OpCodes.Ldobj, type);
        }

        /// <summary>
        ///     Creates a new object or a new instance of a value type, pushing an object reference (type O) onto the evaluation stack.
        /// </summary>
        /// <param name="constructor">
        ///     The <see cref="ConstructorInfo">Constructor</see> to be called.
        /// </param>
        public void Newobj(ConstructorInfo constructor)
        {
            if(constructor == null)
                throw new ArgumentNullException("constructor");
            Emit(OpCodes.Newobj, constructor);
        }

        /// <summary>
        ///     Pushes an object reference to a new zero-based, one-dimensional array whose elements are of a specific type onto the evaluation stack.
        /// </summary>
        /// <param name="type">
        ///     The <see cref="Type">Type</see> of elements.
        /// </param>
        public void Newarr(Type type)
        {
            if(type == null)
                throw new ArgumentNullException("type");
            Emit(OpCodes.Newarr, type);
        }

        /// <summary>
        ///     Throws <see cref="ArithmeticException">ArithmeticException</see> if value is not a finite number.
        /// </summary>
        public void Ckfinite()
        {
            Emit(OpCodes.Ckfinite);
        }

        /// <summary>
        ///     Computes the bitwise AND of two values and pushes the result onto the evaluation stack.
        /// </summary>
        public void And()
        {
            Emit(OpCodes.And);
        }

        /// <summary>
        ///     Compute the bitwise complement of the two integer values on top of the stack and pushes the result onto the evaluation stack.
        /// </summary>
        public void Or()
        {
            Emit(OpCodes.Or);
        }

        /// <summary>
        ///     Computes the bitwise XOR of the top two values on the evaluation stack, pushing the result onto the evaluation stack.
        /// </summary>
        public void Xor()
        {
            Emit(OpCodes.Xor);
        }

        /// <summary>
        ///     Adds two values and pushes the result onto the evaluation stack.
        /// </summary>
        public void Add()
        {
            Emit(OpCodes.Add);
        }

        /// <summary>
        ///     Adds two integers, performs an overflow check, and pushes the result onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either <see cref="OpCodes.Add_Ovf">Add_Ovf</see> or <see cref="OpCodes.Add_Ovf_Un">Add_Ovf_Un</see> instruction.
        /// </param>
        public void Add_Ovf(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Add_Ovf_Un : OpCodes.Add_Ovf);
        }

        /// <summary>
        ///     Subtracts one value from another and pushes the result onto the evaluation stack.
        /// </summary>
        public void Sub()
        {
            Emit(OpCodes.Sub);
        }

        /// <summary>
        ///     Subtracts one integer value from another, performs an overflow check, and pushes the result onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either <see cref="OpCodes.Sub_Ovf">Sub_Ovf</see> or <see cref="OpCodes.Sub_Ovf_Un">Sub_Ovf_Un</see> instruction.
        /// </param>
        public void Sub_Ovf(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Sub_Ovf_Un : OpCodes.Sub_Ovf);
        }

        /// <summary>
        ///     Multiplies two values and pushes the result on the evaluation stack.
        /// </summary>
        public void Mul()
        {
            Emit(OpCodes.Mul);
        }

        /// <summary>
        ///     Multiplies two integer values, performs an overflow check, and pushes the result onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either <see cref="OpCodes.Mul_Ovf">Mul_Ovf</see> or <see cref="OpCodes.Mul_Ovf_Un">Mul_Ovf_Un</see> instruction.
        /// </param>
        public void Mul_Ovf(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Mul_Ovf_Un : OpCodes.Mul_Ovf);
        }

        /// <summary>
        ///     Divides two values and pushes the result as a floating-point (type F) or quotient (type int32) onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either <see cref="OpCodes.Div">Div</see> or <see cref="OpCodes.Div_Un">Div_Un</see> instruction.
        /// </param>
        public void Div(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Div_Un : OpCodes.Div);
        }

        /// <summary>
        ///     Divides two values and pushes the remainder onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either <see cref="OpCodes.Rem">Rem</see> or <see cref="OpCodes.Rem_Un">Rem_Un</see> instruction.
        /// </param>
        public void Rem(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Rem_Un : OpCodes.Rem);
        }

        /// <summary>
        ///     Shifts an integer value to the left (in zeroes) by a specified number of bits, pushing the result onto the evaluation stack.
        /// </summary>
        public void Shl()
        {
            Emit(OpCodes.Shl);
        }

        /// <summary>
        ///     Shifts an integer value to the right by a specified number of bits, pushing the result onto the evaluation stack.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either <see cref="OpCodes.Shr">Shr</see> or <see cref="OpCodes.Shr_Un">Shr_Un</see> instruction.
        /// </param>
        public void Shr(bool unsigned)
        {
            Emit(unsigned ? OpCodes.Shr_Un : OpCodes.Shr);
        }

        /// <summary>
        ///     Negates a value and pushes the result onto the evaluation stack.
        /// </summary>
        public void Neg()
        {
            Emit(OpCodes.Neg);
        }

        /// <summary>
        ///     Computes the bitwise complement of the integer value on top of the stack and pushes the result onto the evaluation stack as the same type.
        /// </summary>
        public void Not()
        {
            Emit(OpCodes.Not);
        }

        /// <summary>
        ///     Pushes a new object reference to a string literal stored in the metadata.
        /// </summary>
        /// <param name="value">The value to push.</param>
        public void Ldstr(string value)
        {
            if(value == null)
                throw new ArgumentNullException("value");
            Emit(OpCodes.Ldstr, value);
        }

        /// <summary>
        ///     Converts the value on top of the evaluation stack to a specified numeric type.
        /// </summary>
        /// <typeparam name="T">
        ///     The <see cref="Type">Type</see> to convert to.
        ///     <para>
        ///         Depending on that type one of the following instructions will be emitted:
        ///         <para></para>
        ///         <see cref="OpCodes.Conv_I1">Conv_I1</see>, <see cref="OpCodes.Conv_U1">Conv_U1</see>, <see cref="OpCodes.Conv_I2">Conv_I2</see>, <see cref="OpCodes.Conv_U2">Conv_U2</see>,
        ///         <see cref="OpCodes.Conv_I4">Conv_I4</see>, <see cref="OpCodes.Conv_U4">Conv_U4</see>, <see cref="OpCodes.Conv_I8">Conv_I8</see>, <see cref="OpCodes.Conv_U8">Conv_U8</see>,
        ///         <see cref="OpCodes.Conv_I">Conv_I</see>, <see cref="OpCodes.Conv_U">Conv_U</see>, <see cref="OpCodes.Conv_R4">Conv_R4</see>, <see cref="OpCodes.Conv_R8">Conv_R8</see>
        ///         <para></para>
        ///     </para>
        /// </typeparam>
        public void Conv<T>()
        {
            var type = typeof(T);
            OpCode opCode;
            if(type == typeof(IntPtr))
                opCode = OpCodes.Conv_I;
            else if(type == typeof(UIntPtr))
                opCode = OpCodes.Conv_U;
            else
            {
                switch(Type.GetTypeCode(type))
                {
                case TypeCode.SByte:
                    opCode = OpCodes.Conv_I1;
                    break;
                case TypeCode.Byte:
                    opCode = OpCodes.Conv_U1;
                    break;
                case TypeCode.Int16:
                    opCode = OpCodes.Conv_I2;
                    break;
                case TypeCode.UInt16:
                    opCode = OpCodes.Conv_U2;
                    break;
                case TypeCode.Int32:
                    opCode = OpCodes.Conv_I4;
                    break;
                case TypeCode.UInt32:
                    opCode = OpCodes.Conv_U4;
                    break;
                case TypeCode.Int64:
                    opCode = OpCodes.Conv_I8;
                    break;
                case TypeCode.UInt64:
                    opCode = OpCodes.Conv_U8;
                    break;
                case TypeCode.Single:
                    opCode = OpCodes.Conv_R4;
                    break;
                case TypeCode.Double:
                    opCode = OpCodes.Conv_R8;
                    break;
                default:
                    throw new ArgumentException(string.Format("Expected numeric type but was '{0}'", type), "type");
                }
            }
            Emit(opCode);
        }

        /// <summary>
        ///     Converts the unsigned integer value on top of the evaluation stack to float32.
        /// </summary>
        public void Conv_R_Un()
        {
            Emit(OpCodes.Conv_R_Un);
        }

        /// <summary>
        ///     Converts the signed value on top of the evaluation stack to a specified integer type throwing <see cref="OverflowException">OverflowException</see>.
        /// </summary>
        /// <param name="unsigned">
        ///     True if treat the parameters of operation as unsigned.
        ///     <para></para>
        ///     Depending in that flag emits either Conv_Ovf_* or Conv_Ovf_*_Un instruction.
        /// </param>
        /// <typeparam name="T">
        ///     The <see cref="Type">Type</see> to convert to.
        ///     <para>
        ///         Depending on that type one of the following instructions will be emitted:
        ///         <para></para>
        ///         <see cref="OpCodes.Conv_Ovf_I1">Conv_Ovf_I1</see>, <see cref="OpCodes.Conv_Ovf_U1">Conv_Ovf_U1</see>, <see cref="OpCodes.Conv_Ovf_I2">Conv_Ovf_I2</see>, <see cref="OpCodes.Conv_Ovf_U2">Conv_Ovf_U2</see>,
        ///         <see cref="OpCodes.Conv_Ovf_I4">Conv_Ovf_I4</see>, <see cref="OpCodes.Conv_Ovf_U4">Conv_Ovf_U4</see>, <see cref="OpCodes.Conv_Ovf_I8">Conv_Ovf_I8</see>, <see cref="OpCodes.Conv_Ovf_U8">Conv_Ovf_U8</see>,
        ///         <see cref="OpCodes.Conv_Ovf_I">Conv_Ovf_I</see>, <see cref="OpCodes.Conv_Ovf_U">Conv_Ovf_U</see>
        ///         <para></para>
        ///     </para>
        /// </typeparam>
        public void Conv_Ovf<T>(bool unsigned)
        {
            var type = typeof(T);
            OpCode opCode;
            if(!unsigned)
            {
                if(type == typeof(IntPtr))
                    opCode = OpCodes.Conv_Ovf_I;
                else if(type == typeof(UIntPtr))
                    opCode = OpCodes.Conv_Ovf_U;
                else
                {
                    switch(Type.GetTypeCode(type))
                    {
                    case TypeCode.SByte:
                        opCode = OpCodes.Conv_Ovf_I1;
                        break;
                    case TypeCode.Byte:
                        opCode = OpCodes.Conv_Ovf_U1;
                        break;
                    case TypeCode.Int16:
                        opCode = OpCodes.Conv_Ovf_I2;
                        break;
                    case TypeCode.UInt16:
                        opCode = OpCodes.Conv_Ovf_U2;
                        break;
                    case TypeCode.Int32:
                        opCode = OpCodes.Conv_Ovf_I4;
                        break;
                    case TypeCode.UInt32:
                        opCode = OpCodes.Conv_Ovf_U4;
                        break;
                    case TypeCode.Int64:
                        opCode = OpCodes.Conv_Ovf_I8;
                        break;
                    case TypeCode.UInt64:
                        opCode = OpCodes.Conv_Ovf_U8;
                        break;
                    default:
                        throw new ArgumentException(string.Format("Expected integer type but was '{0}'", type), "T");
                    }
                }
            }
            else
            {
                if(type == typeof(IntPtr))
                    opCode = OpCodes.Conv_Ovf_I_Un;
                else if(type == typeof(UIntPtr))
                    opCode = OpCodes.Conv_Ovf_U_Un;
                else
                {
                    switch(Type.GetTypeCode(type))
                    {
                    case TypeCode.SByte:
                        opCode = OpCodes.Conv_Ovf_I1_Un;
                        break;
                    case TypeCode.Byte:
                        opCode = OpCodes.Conv_Ovf_U1_Un;
                        break;
                    case TypeCode.Int16:
                        opCode = OpCodes.Conv_Ovf_I2_Un;
                        break;
                    case TypeCode.UInt16:
                        opCode = OpCodes.Conv_Ovf_U2_Un;
                        break;
                    case TypeCode.Int32:
                        opCode = OpCodes.Conv_Ovf_I4_Un;
                        break;
                    case TypeCode.UInt32:
                        opCode = OpCodes.Conv_Ovf_U4_Un;
                        break;
                    case TypeCode.Int64:
                        opCode = OpCodes.Conv_Ovf_I8_Un;
                        break;
                    case TypeCode.UInt64:
                        opCode = OpCodes.Conv_Ovf_U8_Un;
                        break;
                    default:
                        throw new ArgumentException(string.Format("Expected integer type but was '{0}'", type), "T");
                    }
                }
            }
            Emit(opCode);
        }

        /// <summary>
        ///     Calls the method indicated by the passed method descriptor.
        ///     <para></para>
        ///     Emits a <see cref="OpCodes.Call">Call</see> or <see cref="OpCodes.Callvirt">Callvirt</see> instruction depending on whether the method is a virtual or not.
        /// </summary>
        /// <param name="method">
        ///     The <see cref="MethodInfo">Method</see> to be called.
        /// </param>
        /// <param name="constrained">
        ///     The <see cref="Type">Type</see> of an object to constrain the method call on. Emits the <see cref="OpCodes.Constrained">Constrained</see> prefix.
        /// </param>
        /// <param name="tailcall">
        ///     True if the method call is a tail call. Emits the <see cref="OpCodes.Tailcall">Tailcall</see> prefix.
        /// </param>
        /// <param name="optionalParameterTypes">The types of the optional arguments if the method is a varargs method; otherwise, null.</param>
        /// <param name="isVirtual">Only if you sure, that method is virtual</param>
        public void Call(MethodInfo method, Type constrained = null, bool tailcall = false, Type[] optionalParameterTypes = null, bool isVirtual = false)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            var opCode = method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call;
            if(isVirtual)
                opCode = OpCodes.Callvirt;
            if(opCode == OpCodes.Callvirt)
            {
                if(constrained != null && constrained.IsValueType)
                {
                    if(analyzeStack)
                        ilCode.AppendPrefix(OpCodes.Constrained, new TypeILInstructionParameter(constrained));
                    if(symbolDocumentWriter == null)
                        il.Emit(OpCodes.Constrained, constrained);
                }
            }
            if(tailcall)
            {
                if(analyzeStack)
                    ilCode.AppendPrefix(OpCodes.Tailcall, null);
                if(symbolDocumentWriter == null)
                    il.Emit(OpCodes.Tailcall);
            }
            var parameter = new CallILInstructionParameter(method, constrained);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.EmitCall(opCode, method, optionalParameterTypes);
        }

        /// <summary>
        ///     Calls the constructor indicated by the passed constructor descriptor.
        /// </summary>
        /// <param name="constructor">
        ///     The <see cref="ConstructorInfo">Constructor</see> to be called.
        /// </param>
        public void Call(ConstructorInfo constructor)
        {
            if(constructor == null)
                throw new ArgumentNullException("constructor");
            var parameter = new ConstructorILInstructionParameter(constructor);
            var lineNumber = ilCode.Append(OpCodes.Call, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(OpCodes.Call, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(OpCodes.Call, constructor);
        }

        /// <summary>
        ///     Statically calls the method indicated by the passed method descriptor.
        /// </summary>
        /// <param name="method">
        ///     The <see cref="MethodInfo">Method</see> to be called.
        /// </param>
        /// <param name="tailcall">
        ///     True if the method call is a tail call. Emits the <see cref="OpCodes.Tailcall">Tailcall</see> prefix.
        /// </param>
        /// <param name="optionalParameterTypes">The types of the optional arguments if the method is a varargs method; otherwise, null.</param>
        public void Callnonvirt(MethodInfo method, bool tailcall = false, Type[] optionalParameterTypes = null)
        {
            if(method == null)
                throw new ArgumentNullException("method");
            if(tailcall)
            {
                if(analyzeStack)
                    ilCode.AppendPrefix(OpCodes.Tailcall, null);
                if(symbolDocumentWriter == null)
                    il.Emit(OpCodes.Tailcall);
            }
            var opCode = OpCodes.Call;
            var parameter = new MethodILInstructionParameter(method);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.EmitCall(opCode, method, optionalParameterTypes);
        }

        /// <summary>
        ///     Calls the method indicated on the evaluation stack (as a pointer to an entry point) with arguments described by a calling convention.
        /// </summary>
        /// <param name="callingConvention">The managed calling convention to be used.</param>
        /// <param name="returnType">
        ///     The <see cref="Type">Type</see> of the result.
        /// </param>
        /// <param name="parameterTypes">The types of the required arguments to the instruction.</param>
        /// <param name="tailcall">
        ///     True if the method call is a tail call. Emits the <see cref="OpCodes.Tailcall">Tailcall</see> prefix.
        /// </param>
        /// <param name="optionalParameterTypes">The types of the optional arguments for varargs calls.</param>
        public void Calli(CallingConventions callingConvention, Type returnType, Type[] parameterTypes, bool tailcall = false, Type[] optionalParameterTypes = null)
        {
            if(tailcall)
            {
                if(analyzeStack)
                    ilCode.AppendPrefix(OpCodes.Tailcall, null);
                if(symbolDocumentWriter == null)
                    il.Emit(OpCodes.Tailcall);
            }
            var parameter = new MethodByAddressILInstructionParameter(callingConvention, returnType, parameterTypes);
            var lineNumber = ilCode.Append(OpCodes.Calli, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(OpCodes.Calli, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.EmitCalli(OpCodes.Calli, callingConvention, returnType, parameterTypes, optionalParameterTypes);
        }

#if !NETSTANDARD2_0
        /// <summary>
        ///     Calls the method indicated on the evaluation stack (as a pointer to an entry point) with arguments described by a calling convention.
        /// </summary>
        /// <param name="callingConvention">The unmanaged calling convention to be used.</param>
        /// <param name="returnType">
        ///     The <see cref="Type">Type</see> of the result.
        /// </param>
        /// <param name="parameterTypes">The types of the required arguments to the instruction.</param>
        public void Calli(CallingConvention callingConvention, Type returnType, Type[] parameterTypes)
        {
            var parameter = new MethodByAddressILInstructionParameter(callingConvention, returnType, parameterTypes);
            var lineNumber = ilCode.Append(OpCodes.Calli, parameter, new EmptyILInstructionComment());
            if (analyzeStack && stack != null)
                MutateStack(OpCodes.Calli, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if (symbolDocumentWriter == null)
                il.EmitCalli(OpCodes.Calli, callingConvention, returnType, parameterTypes);
        }
#endif

        private void Emit(OpCode opCode, ILInstructionParameter parameter)
        {
            if(parameter == null)
                il.Emit(opCode);
            else if(parameter is TypeILInstructionParameter)
                il.Emit(opCode, ((TypeILInstructionParameter)parameter).Type);
            else if(parameter is ConstructorILInstructionParameter)
                il.Emit(opCode, ((ConstructorILInstructionParameter)parameter).Constructor);
            else if(parameter is FieldILInstructionParameter)
                il.Emit(opCode, ((FieldILInstructionParameter)parameter).Field);
            else if(parameter is LabelILInstructionParameter)
                il.Emit(opCode, ((LabelILInstructionParameter)parameter).Label);
            else if(parameter is LocalILInstructionParameter)
                il.Emit(opCode, ((LocalILInstructionParameter)parameter).Local);
            else if(parameter is LabelsILInstructionParameter)
                il.Emit(opCode, ((LabelsILInstructionParameter)parameter).Labels.Select(label => (System.Reflection.Emit.Label)label).ToArray());
            else if(parameter is MethodILInstructionParameter)
                il.EmitCall(opCode, ((MethodILInstructionParameter)parameter).Method, null);
            else if(parameter is StringILInstructionParameter)
                il.Emit(opCode, ((StringILInstructionParameter)parameter).Value);
            else if(parameter is MethodByAddressILInstructionParameter)
            {
                var calliParameter = (MethodByAddressILInstructionParameter)parameter;
                if(calliParameter.ManagedCallingConvention != null)
                    il.EmitCalli(opCode, calliParameter.ManagedCallingConvention.Value, calliParameter.ReturnType, calliParameter.ParameterTypes, null);
                else
#if NETSTANDARD2_0
                    throw new NotSupportedException("Unmanaged function call is not supported for netstandard2.0");
#else
                    il.EmitCalli(opCode, calliParameter.UnmanagedCallingConvention.Value, calliParameter.ReturnType, calliParameter.ParameterTypes);
#endif
            }
            else
            {
                var primitiveParameter = (PrimitiveILInstructionParameter)parameter;
                var typeCode = Type.GetTypeCode(primitiveParameter.Value.GetType());
                switch(typeCode)
                {
                case TypeCode.SByte:
                    il.Emit(opCode, (sbyte)primitiveParameter.Value);
                    break;
                case TypeCode.Byte:
                    il.Emit(opCode, (byte)primitiveParameter.Value);
                    break;
                case TypeCode.Int16:
                    il.Emit(opCode, (short)primitiveParameter.Value);
                    break;
                case TypeCode.Int32:
                    il.Emit(opCode, (int)primitiveParameter.Value);
                    break;
                case TypeCode.Int64:
                    il.Emit(opCode, (long)primitiveParameter.Value);
                    break;
                case TypeCode.Double:
                    il.Emit(opCode, (double)primitiveParameter.Value);
                    break;
                case TypeCode.Single:
                    il.Emit(opCode, (float)primitiveParameter.Value);
                    break;
                default:
                    throw new InvalidOperationException(string.Format("Type code '{0}' is not valid at this point", typeCode));
                }
            }
        }

        private void InsertPrefixes(bool isVolatile, int? unaligned)
        {
            if(isVolatile)
            {
                if(analyzeStack)
                    ilCode.AppendPrefix(OpCodes.Volatile, null);
                il.Emit(OpCodes.Volatile);
            }
            if(unaligned != null)
            {
                if(unaligned != 1 && unaligned != 2 && unaligned != 4)
                    throw new ArgumentException("Value of alignment must be 1, 2 or 4.", "unaligned");
                if(analyzeStack)
                    ilCode.AppendPrefix(OpCodes.Unaligned, new PrimitiveILInstructionParameter((byte)unaligned.Value));
                if(symbolDocumentWriter == null)
                    il.Emit(OpCodes.Unaligned, (byte)unaligned.Value);
            }
        }

        private void MutateStack(OpCode opCode, ILInstructionParameter parameter)
        {
            StackMutatorCollection.Mutate(opCode, this, parameter, ref stack);
        }

        private static bool IsStruct(Type type)
        {
            return type.IsValueType && !type.IsPrimitive && !type.IsEnum && type != typeof(IntPtr) && type != typeof(UIntPtr);
        }

        private ILInstructionComment GetComment()
        {
            return stack == null ? (ILInstructionComment)new InaccessibleCodeILInstructionComment() : new StackILInstructionComment(stack.Reverse().ToArray());
        }

        private void Emit(OpCode opCode, TypeILInstructionParameter parameter)
        {
            var lineNumber = ilCode.Append(opCode, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode);
        }

        private void Emit(OpCode opCode)
        {
            var lineNumber = ilCode.Append(opCode, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, null);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode);
        }

        private void Emit(OpCode opCode, Local local)
        {
            var parameter = new LocalILInstructionParameter(local);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, local);
        }

        private void Emit(OpCode opCode, Type type)
        {
            var parameter = new TypeILInstructionParameter(type);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, type);
        }

        private void Emit(OpCode opCode, byte value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, int value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, sbyte value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, long value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, double value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, float value)
        {
            var parameter = new PrimitiveILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, string value)
        {
            var parameter = new StringILInstructionParameter(value);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, value);
        }

        private void Emit(OpCode opCode, Label label)
        {
            var parameter = new LabelILInstructionParameter(label);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, label);
        }

        private void Emit(OpCode opCode, Label[] labels)
        {
            var parameter = new LabelsILInstructionParameter(labels);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, labels.Select(label => (System.Reflection.Emit.Label)label).ToArray());
        }

        private void Emit(OpCode opCode, FieldInfo field)
        {
            var parameter = new FieldILInstructionParameter(field);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, field);
        }

        private void Emit(OpCode opCode, MethodInfo method)
        {
            var parameter = new MethodILInstructionParameter(method);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, method);
        }

        private void Emit(OpCode opCode, ConstructorInfo constructor)
        {
            var parameter = new ConstructorILInstructionParameter(constructor);
            var lineNumber = ilCode.Append(opCode, parameter, new EmptyILInstructionComment());
            if(analyzeStack && stack != null)
                MutateStack(opCode, parameter);
            ilCode.SetComment(lineNumber, GetComment());
            if(symbolDocumentWriter == null)
                il.Emit(opCode, constructor);
        }

        public IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        internal readonly Dictionary<Label, ESType[]> labelStacks = new Dictionary<Label, ESType[]>();
        internal readonly ILCode ilCode = new ILCode();
        internal readonly Type methodReturnType;
        internal readonly Type[] methodParameterTypes;

        private int localId;
        private int labelId;

        private EvaluationStack stack = new EvaluationStack();

        private readonly ILGenerator il;
        private readonly bool analyzeStack = true;
        private readonly ISymbolDocumentWriter symbolDocumentWriter;

        public class Label
        {
            public Label(System.Reflection.Emit.Label label, string name)
            {
                this.label = label;
                this.Name = name;
            }

            public static implicit operator System.Reflection.Emit.Label(Label label)
            {
                return label.label;
            }

            public string Name { get; }

            private readonly System.Reflection.Emit.Label label;
        }

        public class Local
        {
            public Local(LocalBuilder localBuilder, string name)
            {
                this.localBuilder = localBuilder;
                this.Name = name;
            }

            public static implicit operator LocalBuilder(Local local)
            {
                return local.localBuilder;
            }

            public void SetLocalSymInfo(string name)
            {
                SetLocalSymInfo(localBuilder, name);
            }

            public string Name { get; }
            public Type Type { get { return localBuilder.LocalType; } }

            private readonly LocalBuilder localBuilder;

            public static void SetLocalSymInfo(LocalBuilder localBuilder, string name)
            {
#if NETSTANDARD2_0
                throw new NotSupportedException("Not supported for netstandard2.0");
#else
                localBuilder.SetLocalSymInfo(name);
#endif
            }
        }
    }

    // ReSharper restore InconsistentNaming
}
