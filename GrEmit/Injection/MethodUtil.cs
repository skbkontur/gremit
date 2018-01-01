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

        public static unsafe bool HookMethod(MethodBase victim, MethodBase dest, out Action unhook)
        {
            var destAddr = GetMethodAddress(dest);
            unhook = null;

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

                long oldCode;
                if(!PlantRelJmpHook(victimAddr, x, out oldCode))
                    return false;

                unhook = () =>
                    {
                        long tmp;
                        PlantRelJmpHook(victimAddr, oldCode, out tmp);
                    };

                return true;
            }
            else
            {
                // todo: set unhook delegate
                if(compareExchange2Words == null)
                    return false;
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

        private static unsafe bool PlantRelJmpHook(IntPtr victimAddr, long newCode, out long oldCode)
        {
            MEMORY_PROTECTION_CONSTANTS oldProtect;
            if(!VirtualProtect(victimAddr, 8, MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE, &oldProtect))
            {
                oldCode = 0;
                return false;
            }

            oldCode = relJmpHooker(victimAddr, newCode);

            VirtualProtect(victimAddr, 8, oldProtect, &oldProtect);
            return true;
        }

        private static Func<IntPtr, long, long> EmitRelJmpHooker()
        {
            var method = new DynamicMethod(Guid.NewGuid().ToString(), typeof(long), new[] {typeof(IntPtr), typeof(long)}, typeof(string), true);
            using(var il = new GroboIL(method))
            {
                il.VerificationKind = TypesAssignabilityVerificationKind.LowLevelOnly;
                var cycleLabel = il.DefineLabel("cycle");
                il.MarkLabel(cycleLabel);
                il.Ldarg(0); // stack: [ptr]
                il.Dup(); // stack: [ptr, ptr]
                var x = il.DeclareLocal(typeof(long));
                il.Ldind(typeof(long)); // stack: [ptr, *ptr]
                il.Dup();
                il.Stloc(x); // x = *ptr; stack: [ptr, newCode]
                il.Ldc_I8(unchecked((long)0xFFFFFF0000000000));
                il.And(); // stack: [ptr, x & 0xFFFFFF0000000000]
                il.Ldarg(1); // stack: [ptr, x & 0xFFFFFF0000000000, code]
                il.Or(); // stack: [ptr, (x & 0xFFFFFF0000000000) | code]
                il.Ldloc(x); // stack: [ptr, (x & 0xFFFFFF0000000000) | code, newCode]
                var methodInfo = typeof(Interlocked).GetMethod("CompareExchange", BindingFlags.Static | BindingFlags.Public, null, new[] {typeof(long).MakeByRefType(), typeof(long), typeof(long)}, null);
                il.Call(methodInfo); // stack: [Interlocked.CompareExchange(ptr, (x & 0xFFFFFF0000000000) | code, newCode)]
                il.Ldloc(x); // stack: [Interlocked.CompareExchange(ptr, (x & 0xFFFFFF0000000000) | code, newCode), newCode]
                il.Bne_Un(cycleLabel); // if(Interlocked.CompareExchange(ptr, (x & 0xFFFFFF0000000000) | code, newCode) != newCode) goto cycle; stack: []
                il.Ldloc(x);
                il.Ret();
            }
            return (Func<IntPtr, long, long>)method.CreateDelegate(typeof(Func<IntPtr, long, long>));
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
            if(method is DynamicMethod)
                return GetDynamicMethodAddress(method);
            if(method.GetType() == rtDynamicMethodType)
                return GetDynamicMethodAddress(m_ownerExtractor(method));

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
        private static readonly Func<IntPtr, long, long> relJmpHooker = EmitRelJmpHooker();
        private static readonly Type rtDynamicMethodType = typeof(DynamicMethod).GetNestedType("RTDynamicMethod", BindingFlags.NonPublic);
        private static readonly Func<MethodBase, DynamicMethod> m_ownerExtractor = FieldsExtractor.GetExtractor<MethodBase, DynamicMethod>(rtDynamicMethodType.GetField("m_owner", BindingFlags.NonPublic | BindingFlags.Instance));
    }
}