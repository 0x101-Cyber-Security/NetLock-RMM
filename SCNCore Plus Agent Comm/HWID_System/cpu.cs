using System;
using System.Runtime.InteropServices;

namespace _x101.HWID_System
{
    [System.Reflection.ObfuscationAttribute(Feature = "Virtualization", Exclude = false)]
    internal static class CpuId
    {
        [DllImport("user32", EntryPoint = "CallWindowProcW", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr CallWindowProcW(byte[] bytes, IntPtr hWnd, int msg, byte[] wParam, IntPtr lParam);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VirtualProtect(byte[] lpAddress, IntPtr dwSize, int flNewProtect, out int lpflOldProtect);

        private const int PAGE_EXECUTE_READWRITE = 0x40;

        public static string GetCpuId()
        {
            var serialNumber = new byte[8];
            return ExecuteCode(ref serialNumber) ?
                $"{BitConverter.ToUInt32(serialNumber, 4):X8}{BitConverter.ToUInt32(serialNumber, 0):X8}"
                : "ND";
        }

        private static bool ExecuteCode(ref byte[] result)
        {
            bool is64Bit = IntPtr.Size == 8;
            byte[] code = is64Bit ? Get64BitCode() : Get32BitCode();

            if (!VirtualProtect(code, (IntPtr)code.Length, PAGE_EXECUTE_READWRITE, out _))
                throw new InvalidOperationException("Failed to change memory protection.");

            return CallWindowProcW(code, IntPtr.Zero, 0, result, (IntPtr)result.Length) != IntPtr.Zero;
        }

        private static byte[] Get64BitCode() => new byte[]
        {
        0x53,                   // push rbx
        0x48, 0xc7, 0xc0, 0x01, 0x00, 0x00, 0x00, // mov rax, 0x1
        0x0f, 0xa2,             // cpuid
        0x41, 0x89, 0x00,       // mov [r8], eax
        0x41, 0x89, 0x50, 0x04, // mov [r8+0x4], edx
        0x5b,                   // pop rbx
        0xc3                    // ret
        };

        private static byte[] Get32BitCode() => new byte[]
        {
        0x55,                   // push ebp
        0x89, 0xe5,             // mov ebp, esp
        0x57,                   // push edi
        0x8b, 0x7d, 0x10,       // mov edi, [ebp+0x10]
        0x6a, 0x01,             // push 0x1
        0x58,                   // pop eax
        0x53,                   // push ebx
        0x0f, 0xa2,             // cpuid
        0x89, 0x07,             // mov [edi], eax
        0x89, 0x57, 0x04,       // mov [edi+0x4], edx
        0x5b,                   // pop ebx
        0x5f,                   // pop edi
        0x89, 0xec,             // mov esp, ebp
        0x5d,                   // pop ebp
        0xc2, 0x10, 0x00        // ret 0x10
        };
    }
}