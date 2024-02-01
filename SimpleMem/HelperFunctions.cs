#pragma warning disable SYSLIB1054

using SimpleMem.Exceptions;
using SimpleMem.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SimpleMem
{
    internal static class HelperFunctions
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualAllocEx(IntPtr hProcess,
            IntPtr lpAddress, int dwSize, int flAllocationType, int flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualQueryEx(IntPtr hProcess,
            IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern int VirtualProtectEx(IntPtr hProcess,
            IntPtr lpAddress, int dwSize, int fLNewProtect, out int lpflOldProtect);

        [DllImport("kernel32.dll")]
        internal static extern UInt32 GetLastError();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte* lpBuffer, int dwSize, out int lpNumberOfBytesWritten);

        /// <summary>
        ///  Wrapper for kernel32.dll WriteProcessMemory. Calls VirtualProtectEx() on memory to
        ///  allow writing to protected memory.
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="lpBaseAddress"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="dwSize"></param>
        /// <param name="isProtected">
        ///  Whether the memory address space to write to has been
        ///  vetted by a VirtualProtectEx call
        /// </param>
        /// <returns>Number of bytes read</returns>
        /// <exception cref="MemoryWriteException">Thrown if the memory failed to be written</exception>
        internal unsafe static int WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte* lpBuffer, int dwSize,
            bool isProtected = false)
        {
            int lpflOldProtect = default;
            if (isProtected)
            {
                int code = VirtualProtectEx(hProcess, lpBaseAddress, dwSize,
                    MEM_PROTECT.PAGE_EXECUTE_READWRITE, out lpflOldProtect);

                if (code == 0)
                {
                    throw new MemoryWriteException(GetLastError());
                }

                if (lpflOldProtect == 0)
                {
                    throw new MemoryWriteException(GetLastError());
                }
            }

            if (!WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, out int lpNumberOfBytesWritten))
            {
                throw new MemoryWriteException(GetLastError());
            }

            if (isProtected)
            {
                // Error reporting is ignored - if a permission error exists it would be raised at the first call.
#pragma warning disable CA1806
                VirtualProtectEx(hProcess, lpBaseAddress, dwSize, lpflOldProtect, out int _);
#pragma warning restore CA1806
            }

            if (lpNumberOfBytesWritten == 0)
            {
                throw new MemoryWriteException(GetLastError());
            }

            return lpNumberOfBytesWritten;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte* lpBuffer, int dwSize, out int lpBytesRead);

        /// <summary>
        ///  Wrapper for kernel32.dll ReadProcessMemory
        /// </summary>
        /// <param name="hProcess"></param>
        /// <param name="lpBaseAddress"></param>
        /// <param name="lpBuffer"></param>
        /// <param name="dwSize"></param>
        /// <param name="isProtected">Whether the memory access is processed by a VirtualProtectEx call</param>
        /// <returns>Number of bytes read</returns>
        internal unsafe static int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte* lpBuffer, int dwSize,
            bool isProtected = false)
        {
            int lpflOldProtect = default;
            if (isProtected)
            {
                int code = VirtualProtectEx(hProcess, lpBaseAddress, dwSize,
                    MEM_PROTECT.PAGE_EXECUTE_READ, out lpflOldProtect);

                if (code == 0)
                {
                    throw new MemoryReadException(GetLastError());
                }

                if (lpflOldProtect == (int)IntPtr.Zero)
                {
                    throw new MemoryReadException(GetLastError());
                }
            }

            if (!ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, out int lpNumberOfBytesRead))
            {
                throw new MemoryReadException(GetLastError());
            }

            if (isProtected)
            {
                // Error reporting is ignored - if a permission error exists it would be raised at the first call.
#pragma warning disable CA1806
                VirtualProtectEx(hProcess, lpBaseAddress, dwSize, lpflOldProtect, out int _);
#pragma warning restore CA1806
            }

            if (lpNumberOfBytesRead == 0)
            {
                throw new MemoryReadException(GetLastError());
            }

            return lpNumberOfBytesRead;
        }

        /// <summary>
        ///  Gets the process (if possible) based on the class's processName.
        /// </summary>
        /// <exception cref="DllNotFoundException">Thrown if the process is not found.</exception>
        /// <returns></returns>
        internal static Process GetProcess(string processName)
        {
            while (true)
            {
                try
                {
                    var proc = Process.GetProcessesByName(processName)[0];
                    Console.WriteLine($"Process {processName} found!");

                    return proc;
                }
                catch (IndexOutOfRangeException)
                {
                    throw new DllNotFoundException($"Process {processName} not found.");
                }
            }
        }

        /// <summary>
        ///  Returns a list of all currently running executables' process names (regardless of architecture).
        ///  The names of these processes can be used as the processName <see cref="Memory" /> constructor argument.
        /// </summary>
        public static List<string> GetProcessList()
        {
            var ls = new List<string>();
            var processCollection = Process.GetProcesses();
            foreach (var p in processCollection)
            {
                ls.Add(p.ProcessName);
            }

            return ls;
        }
    }
}
