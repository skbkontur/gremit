using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace GrEmit.Injection
{
    public static class MethodUtil
    {
        [DllImport("kernel32.dll")]
        public static extern unsafe bool VirtualProtect(IntPtr lpAddress, uint dwSize, MEMORY_PROTECTION_CONSTANTS flNewProtect, MEMORY_PROTECTION_CONSTANTS* lpflOldProtect);

        [Flags]
        public enum MEMORY_PROTECTION_CONSTANTS
        {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
            PAGE_TARGETS_INVALID = 0x40000000,
            PAGE_TARGETS_NO_UPDATE = 0x40000000,
        }

        public static unsafe bool HookMethod(MethodBase dest, MethodBase victim)
        {
            if(compareExchange2Words == null)
                return false;
            var destAddr = GetMethodAddress(dest);

            //var genericArguments = new Type[0];

            //if (victim.DeclaringType.GetGenericArguments() != null)
            //{
            //    genericArguments = victim.DeclaringType.GetGenericArguments().ToArray();
            //}

            //if (victim.GetGenericArguments() != null)
            //{
            //    genericArguments = genericArguments.Concat(victim.GetGenericArguments()).ToArray();
            //}

            RuntimeHelpers.PrepareMethod(victim.MethodHandle);
            //RuntimeHelpers.PrepareMethod(victim.MethodHandle, victim.DeclaringType.GetGenericArguments().Concat(victim.GetGenericArguments()).Select(type => type.TypeHandle).ToArray());
            var victimAddr = GetMethodAddress(victim);

            bool canMakeRelJmp;
            int dist = 0;
            if(IntPtr.Size == 4)
            {
                canMakeRelJmp = true;
                dist = destAddr.ToInt32() - victimAddr.ToInt32() - 5;
            }
            else
            {
                var dist64 = destAddr.ToInt64() - victimAddr.ToInt64() - 5;
                canMakeRelJmp = dist64 >= int.MinValue && dist64 <= int.MaxValue;
                if(canMakeRelJmp)
                    dist = (int)dist64;
            }
            if(canMakeRelJmp)
            {
                // Make relative jump
                var bytes = BitConverter.GetBytes(dist);
                var hookCode = new byte[]
                    {
                        0xE9, bytes[0], bytes[1], bytes[2], bytes[3], // jmp dest
                        0x00,
                        0x00,
                        0x00,
                    };
                long x = BitConverter.ToInt64(hookCode, 0);

                MEMORY_PROTECTION_CONSTANTS oldProtect;
                if(!VirtualProtect(victimAddr, 8, MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE, &oldProtect))
                    return false;

                relJmpHooker(victimAddr, x);

                VirtualProtect(victimAddr, 8, oldProtect, &oldProtect);

                return true;
            }
            else
            {
                // Make absolute jump
                UIntPtr lo, hi;
                if(IntPtr.Size == 8)
                {
                    // x64
                    var bytes = BitConverter.GetBytes(destAddr.ToInt64());
                    var hookCode = new byte[16]
                        {
                            0x48, 0xB8, bytes[0], bytes[1], bytes[2], bytes[3],
                            bytes[4], bytes[5], bytes[6], bytes[7], // movabs rax, hookMethod
                            0xFF, 0xE0, // jmp rax
                            0x90, // nop
                            0x90, // nop
                            0x90, // nop
                            0x90 // nop
                        };
                    lo = new UIntPtr(BitConverter.ToUInt64(hookCode, 0));
                    hi = new UIntPtr(BitConverter.ToUInt64(hookCode, 8));
                }
                else
                {
                    // x86
                    var bytes = BitConverter.GetBytes(destAddr.ToInt32());
                    var hookCode = new byte[8]
                        {
                            0xB8, bytes[0], bytes[1], bytes[2], bytes[3], // mov eax, hookMethod
                            0xFF, 0xE0, // jmp eax
                            0x90 // nop
                        };
                    lo = new UIntPtr(BitConverter.ToUInt32(hookCode, 0));
                    hi = new UIntPtr(BitConverter.ToUInt32(hookCode, 4));
                }

                MEMORY_PROTECTION_CONSTANTS oldProtect;
                if(!VirtualProtect(victimAddr, (uint)(IntPtr.Size * 2), MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE, &oldProtect))
                    return false;

                compareExchange2Words(victimAddr, lo, hi);

                VirtualProtect(victimAddr, (uint)(IntPtr.Size * 2), oldProtect, &oldProtect);

                return true;
            }
        }

        public static Action<IntPtr, long> EmitRelJmpHooker()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(void), new[] {typeof(IntPtr), typeof(long)}, typeof(string), true);
            var il = method.GetILGenerator();
            var cycleLabel = il.DefineLabel();
            il.MarkLabel(cycleLabel);
            il.Emit(OpCodes.Ldarg_0); // stack: [ptr]
            il.Emit(OpCodes.Dup); // stack: [ptr, ptr]
            var x = il.DeclareLocal(typeof(long));
            il.Emit(OpCodes.Ldind_I8); // stack: [ptr, *ptr]
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Stloc, x); // x = *ptr; stack: [ptr, x]
            il.Emit(OpCodes.Ldc_I8, unchecked((long)0xFFFFFF0000000000));
            il.Emit(OpCodes.And); // stack: [ptr, x & 0xFFFFFF0000000000]
            il.Emit(OpCodes.Ldarg_1); // stack: [ptr, x & 0xFFFFFF0000000000, code]
            il.Emit(OpCodes.Or); // stack: [ptr, (x & 0xFFFFFF0000000000) | code]
            il.Emit(OpCodes.Ldloc, x); // stack: [ptr, (x & 0xFFFFFF0000000000) | code, x]
            var methodInfo = typeof(Interlocked).GetMethod("CompareExchange", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof(long).MakeByRefType(), typeof(long), typeof(long)}, null);
            il.EmitCall(OpCodes.Call, methodInfo, null); // stack: [Interlocked.CompareExchange(ptr, (x & 0xFFFFFF0000000000) | code, x)]
            il.Emit(OpCodes.Ldloc, x); // stack: [Interlocked.CompareExchange(ptr, (x & 0xFFFFFF0000000000) | code, x), x]
            il.Emit(OpCodes.Bne_Un_S, cycleLabel); // if(Interlocked.CompareExchange(ptr, (x & 0xFFFFFF0000000000) | code, x) != x) goto cycle; stack: []
            il.Emit(OpCodes.Ret);
            return (Action<IntPtr, long>)method.CreateDelegate(typeof(Action<IntPtr, long>));
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void CompareExchange2WordsDelegate(IntPtr location, UIntPtr lo, UIntPtr hi);

        private static unsafe CompareExchange2WordsDelegate EmitCompareExchange2Words()
        {
            byte[] code;
            if(IntPtr.Size == 4)
            {
                code = new byte[]
                    {
                        0x56, //                            push esi
                        0x53, //                            push ebx
                        0x8B, 0x74, 0x24, 0x0C, //          mov esi, dword [esp + 12]
                        0x8B, 0x5C, 0x24, 0x10, //          mov ebx, dword [esp + 16]
                        0x8B, 0x4C, 0x24, 0x14, //          mov ecx, dword [esp + 20]
                        0x8B, 0x06, //                      mov eax, dword [esi]
                        0x8B, 0x56, 0x04, //                mov edx, dword [esi + 4]
                        //                              _loop:
                        0xF0, 0x0F, 0xC7, 0x0E, //          lock cmpxchg8b qword [esi]
                        0x75, 0xFA, //                      jne _loop
                        0x5B, //                            pop ebx
                        0x5E, //                            pop esi
                        0xC2, 0x0C, 0x00 //                 ret 12
                    };
            }
            else
            {
                code = new byte[]
                    {
                        0x56, //                            push rsi
                        0x53, //                            push rbx
                        0x48, 0x89, 0xCE, //                mov rsi, rcx
                        0x48, 0x89, 0xD3, //                mov rbx, rdx
                        0x4C, 0x89, 0xC1, //                mov rcx, r8
                        0x48, 0x8B, 0x06, //                mov rax, qword [rsi]
                        0x48, 0x8B, 0x56, 0x08, //          mov rdx, qword [rsi + 8]
                        //                              _loop:
                        0xF0, 0x48, 0x0F, 0xC7, 0x0E, //    lock cmpxchg16b dqword [rsi]
                        0x75, 0xF9, //                      jne _loop
                        0x5B, //                            pop rbx
                        0x5E, //                            pop rsi
                        0xC3 //                             ret
                    };
            }
            var ptr = Marshal.AllocHGlobal(code.Length);
            MEMORY_PROTECTION_CONSTANTS oldProtect;
            if(!VirtualProtect(ptr, (uint)code.Length, MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE, &oldProtect))
                return null;
            Marshal.Copy(code, 0, ptr, code.Length);
            return (CompareExchange2WordsDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(CompareExchange2WordsDelegate));
        }

        /// <summary>
        ///     Gets the address of the method stub
        /// </summary>
        /// <param name="method">The method handle.</param>
        /// <returns></returns>
        public static IntPtr GetMethodAddress(MethodBase method)
        {
            if((method is DynamicMethod))
                return GetDynamicMethodAddress(method);

            // Prepare the method so it gets jited
            RuntimeHelpers.PrepareMethod(method.MethodHandle);

            return method.MethodHandle.GetFunctionPointer();
        }

        private static IntPtr GetDynamicMethodAddress(MethodBase method)
        {
            var handle = GetDynamicMethodRuntimeHandle(method);
            RuntimeHelpers.PrepareMethod(handle);
            return handle.GetFunctionPointer();
        }

        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            RuntimeMethodHandle handle;

            if(Environment.Version.Major == 4)
            {
                var getMethodDescriptorInfo = typeof(DynamicMethod).GetMethod("GetMethodDescriptor",
                                                                              BindingFlags.NonPublic | BindingFlags.Instance);
                handle = (RuntimeMethodHandle)getMethodDescriptorInfo.Invoke(method, null);
            }
            else
            {
                var fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
                handle = ((RuntimeMethodHandle)fieldInfo.GetValue(method));
            }

            return handle;
        }

        private static readonly CompareExchange2WordsDelegate compareExchange2Words = EmitCompareExchange2Words();
        private static readonly Action<IntPtr, long> relJmpHooker = EmitRelJmpHooker();
    }
}