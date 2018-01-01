//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2015 Jb Evain
// Copyright (c) 2008 - 2011 Novell, Inc.
// Copyright (c) 2016 Igor Chevdar
//
// Licensed under the MIT/X11 license.
//

using System;
using System.Collections.Generic;
using System.Reflection;

namespace GrEmit.MethodBodyParsing
{
    public class MaxStackSizeCalculator
    {
        public MaxStackSizeCalculator(MethodBody body, Func<MetadataToken, object> tokenResolver)
        {
            this.body = body;
            this.tokenResolver = tokenResolver;
        }

        public int ComputeMaxStack()
        {
            var stack_size = 0;
            var max_stack = 0;
            Dictionary<Instruction, int> stack_sizes = null;

            if(body.HasExceptionHandlers)
                ComputeExceptionHandlerStackSize(ref stack_sizes);

            foreach(var instruction in body.Instructions)
                ComputeStackSize(instruction, ref stack_sizes, ref stack_size, ref max_stack);

            return max_stack;
        }

        private void ComputeExceptionHandlerStackSize(ref Dictionary<Instruction, int> stack_sizes)
        {
            foreach(var handler in body.ExceptionHandlers)
            {
                switch(handler.HandlerType)
                {
                case ExceptionHandlerType.Catch:
                    AddExceptionStackSize(handler.HandlerStart, ref stack_sizes);
                    break;
                case ExceptionHandlerType.Filter:
                    AddExceptionStackSize(handler.FilterStart, ref stack_sizes);
                    AddExceptionStackSize(handler.HandlerStart, ref stack_sizes);
                    break;
                }
            }
        }

        private static void AddExceptionStackSize(Instruction handler_start, ref Dictionary<Instruction, int> stack_sizes)
        {
            if(handler_start == null)
                return;

            if(stack_sizes == null)
                stack_sizes = new Dictionary<Instruction, int>();

            stack_sizes[handler_start] = 1;
        }

        private void ComputeStackSize(Instruction instruction, ref Dictionary<Instruction, int> stack_sizes, ref int stack_size, ref int max_stack)
        {
            int computed_size;
            if(stack_sizes != null && stack_sizes.TryGetValue(instruction, out computed_size))
                stack_size = computed_size;

            max_stack = Math.Max(max_stack, stack_size);
            ComputeStackDelta(instruction, ref stack_size);
            max_stack = Math.Max(max_stack, stack_size);

            CopyBranchStackSize(instruction, ref stack_sizes, stack_size);
            ComputeStackSize(instruction, ref stack_size);
        }

        private static void CopyBranchStackSize(Instruction instruction, ref Dictionary<Instruction, int> stack_sizes, int stack_size)
        {
            if(stack_size == 0)
                return;

            switch(instruction.OpCode.OperandType)
            {
            case OperandType.ShortInlineBrTarget:
            case OperandType.InlineBrTarget:
                CopyBranchStackSize(ref stack_sizes, (Instruction)instruction.Operand, stack_size);
                break;
            case OperandType.InlineSwitch:
                var targets = (Instruction[])instruction.Operand;
                for(int i = 0; i < targets.Length; i++)
                    CopyBranchStackSize(ref stack_sizes, targets[i], stack_size);
                break;
            }
        }

        private static void CopyBranchStackSize(ref Dictionary<Instruction, int> stack_sizes, Instruction target, int stack_size)
        {
            if(stack_sizes == null)
                stack_sizes = new Dictionary<Instruction, int>();

            int branch_stack_size = stack_size;

            int computed_size;
            if(stack_sizes.TryGetValue(target, out computed_size))
                branch_stack_size = Math.Max(branch_stack_size, computed_size);

            stack_sizes[target] = branch_stack_size;
        }

        private static void ComputeStackSize(Instruction instruction, ref int stack_size)
        {
            switch(instruction.OpCode.FlowControl)
            {
            case FlowControl.Branch:
            case FlowControl.Break:
            case FlowControl.Throw:
            case FlowControl.Return:
                stack_size = 0;
                break;
            }
        }

        private static MethodSignature GetMethodSignature(MethodBase method)
        {
            return new MethodSignature
                {
                    hasThis = method.CallingConvention.HasFlag(CallingConventions.HasThis)
                              && !method.CallingConvention.HasFlag(CallingConventions.ExplicitThis),
                    parametersCount = method.GetParameters().Length,
                    hasReturnType = method is MethodInfo && ((MethodInfo)method).ReturnType != typeof(void)
                };
        }

        private MethodSignature GetMethodSignature(MetadataToken token, OpCode opCode)
        {
            if(opCode.Code != Code.Calli)
                return GetMethodSignature((MethodBase)tokenResolver(token));
            var signature = (byte[])tokenResolver(token);
            var parsedSignature = new SignatureReader(signature).ReadAndParseMethodSignature();
            return new MethodSignature
                {
                    hasThis = parsedSignature.HasThis && !parsedSignature.ExplicitThis,
                    parametersCount = parsedSignature.ParamCount,
                    hasReturnType = parsedSignature.HasReturnType
                };
        }

        private MethodSignature GetMethodSignature(Instruction instruction)
        {
            if(instruction.Operand is MetadataToken)
                return GetMethodSignature((MetadataToken)instruction.Operand, instruction.OpCode);
            return GetMethodSignature((MethodBase)instruction.Operand);
        }

        private void ComputeStackDelta(Instruction instruction, ref int stack_size)
        {
            switch(instruction.OpCode.FlowControl)
            {
            case FlowControl.Call:
                {
                    var methodSignature = GetMethodSignature(instruction);

                    // pop 'this' argument
                    if(methodSignature.hasThis && instruction.OpCode.Code != Code.Newobj)
                        stack_size--;
                    // pop normal arguments
                    stack_size -= methodSignature.parametersCount;
                    // pop function pointer
                    if(instruction.OpCode.Code == Code.Calli)
                        stack_size--;
                    // push return value
                    if(methodSignature.hasReturnType || instruction.OpCode.Code == Code.Newobj)
                        stack_size++;
                    break;
                }
            default:
                ComputePopDelta(instruction.OpCode.StackBehaviourPop, ref stack_size);
                ComputePushDelta(instruction.OpCode.StackBehaviourPush, ref stack_size);
                break;
            }
        }

        private static void ComputePopDelta(StackBehaviour pop_behavior, ref int stack_size)
        {
            switch(pop_behavior)
            {
            case StackBehaviour.Popi:
            case StackBehaviour.Popref:
            case StackBehaviour.Pop1:
                stack_size--;
                break;
            case StackBehaviour.Pop1_pop1:
            case StackBehaviour.Popi_pop1:
            case StackBehaviour.Popi_popi:
            case StackBehaviour.Popi_popi8:
            case StackBehaviour.Popi_popr4:
            case StackBehaviour.Popi_popr8:
            case StackBehaviour.Popref_pop1:
            case StackBehaviour.Popref_popi:
                stack_size -= 2;
                break;
            case StackBehaviour.Popi_popi_popi:
            case StackBehaviour.Popref_popi_popi:
            case StackBehaviour.Popref_popi_popi8:
            case StackBehaviour.Popref_popi_popr4:
            case StackBehaviour.Popref_popi_popr8:
            case StackBehaviour.Popref_popi_popref:
                stack_size -= 3;
                break;
            case StackBehaviour.PopAll:
                stack_size = 0;
                break;
            }
        }

        private static void ComputePushDelta(StackBehaviour push_behaviour, ref int stack_size)
        {
            switch(push_behaviour)
            {
            case StackBehaviour.Push1:
            case StackBehaviour.Pushi:
            case StackBehaviour.Pushi8:
            case StackBehaviour.Pushr4:
            case StackBehaviour.Pushr8:
            case StackBehaviour.Pushref:
                stack_size++;
                break;
            case StackBehaviour.Push1_push1:
                stack_size += 2;
                break;
            }
        }

        private readonly MethodBody body;
        private readonly Func<MetadataToken, object> tokenResolver;

        private class MethodSignature
        {
            public bool hasThis;
            public int parametersCount;
            public bool hasReturnType;
        }
    }
}