#pragma warning disable SYSLIB1045

using SimpleMem.Exceptions;
using SimpleMem.Models;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using static SimpleMem.HelperFunctions;

namespace SimpleMem
{
    /// <summary>
    ///  Class for cross-architecture memory manipulation.
    /// </summary>
    public class Memory
    {
        private readonly string _moduleName;

        /// <summary>
        ///  The user-defined desired access level for which the process was opened under.
        /// </summary>
        public long ProcessAccessLevel { get; }
        /// <summary>
        ///  The current process
        /// </summary>
        public Process Process { get; }
        /// <summary>
        ///  Pointer to the handle of the opened process in memory.
        /// </summary>
        public IntPtr ProcessHandle { get; }
        /// <summary>
        ///  The size of pointers for this process.
        /// </summary>
        public int PtrSize { get; } = IntPtr.Size;
        /// <summary>
        ///  Module assosciated with the provided moduleName, or the process's MainModule by default.
        /// </summary>
        public ProcessModule Module { get; }
        /// <summary>
        ///  Base address of the module
        /// </summary>
        public IntPtr ModuleBaseAddress { get; }
        /// <summary>
        ///  ModuleBaseAddress in hex form. Casting ModuleBaseAddress .ToString() directly returns a base-10 number.
        /// </summary>
        public string ModuleBaseAddressHex => this.ModuleBaseAddress.ToString("X");

        #region Constructor
        /// <summary>
        ///  Opens a handle to the given processName at the provided moduleName.
        ///  For example, if I have a process named "FTLGame" and my desired base address
        ///  is located at "FTLGame.exe" + 0xABCD..., the processName would be "FTLGame" and
        ///  the moduleName would be "FTLGame.exe".
        /// </summary>
        /// <param name="processName">
        ///  The name of the process. Use <see cref="GetProcessList" />
        ///  and find your process name if unsure. That value can be passed in as this parameter.
        /// </param>
        /// <param name="moduleName">
        ///  The name of the module to use as the base pointer. This defaults to your
        ///  application's executable if left null.
        /// </param>
        /// <param name="accessLevel">
        ///  The desired access level.
        ///  The minimum required for reading is AccessLevel.READ and the minimum required
        ///  for writing is AccessLevel.WRITE | AccessLevel.OPERATION.
        ///  AccessLevel.ALL_ACCESS gives full read-write access to the process.
        /// </param>
        public Memory(string processName, string moduleName = null, long accessLevel = ACCESS_LEVEL.PROCESS_ALL_ACCESS)
        {
            this.Process = GetProcess(processName);
            this.ProcessAccessLevel = accessLevel;
            this.ProcessHandle = OpenProcess((int)this.ProcessAccessLevel, false, this.Process.Id);

            _moduleName = moduleName ?? this.Process.MainModule?.ModuleName ?? "<module not found>";
            if (moduleName == null)
            {
                this.Module = this.Process.MainModule ?? throw new InvalidOperationException("Process has no main module.");
            }
            else
            {
                this.Module = this.GetModule(this.Process);
            }

            this.ModuleBaseAddress = this.Module.BaseAddress;
        }
        #endregion

        private ProcessModule GetModule(Process proc)
        {
            var module = proc.Modules
                             .Cast<ProcessModule>()
                             .SingleOrDefault(module => string.Equals(module.ModuleName, _moduleName, StringComparison.OrdinalIgnoreCase));

            return module ?? throw new InvalidOperationException($"Could not retrieve the module {_moduleName}.");
        }

        /// <summary>
        ///  Array of Byte pattern scan. Allows scanning for an exact array of bytes with wildcard support.
        ///  Note: Partial wildcards are not supported and will be converted into full wildcards. This has a
        ///  small possibility of resulting in more matches than desired. (e.g. AB ?1 turns into AB ??)
        /// </summary>
        /// <param name="pattern">
        ///  The pattern of bytes to look for. Bytes are separated by spaces.
        ///  Wildcards (?? symbols) are supported.
        /// </param>
        /// <param name="once">Whether to abort the scan after the first match.</param>
        /// <example>
        ///  <code>
        ///  var addresses = AoBScan("03 AD FF ?? ?? ?? 4D");
        ///  // Returns a list of addresses found (if any) matching the pattern.
        /// </code>
        /// </example>
        /// <exception cref="MemoryReadException">Thrown if marked as once but no matches found.</exception>
        /// <returns></returns>
        private List<IntPtr> AoBScan(string pattern, bool once = false)
        {
            // Ensure capitalization
            pattern = pattern.ToUpper();
            // Get min & max addresses

            GetSystemInfo(out var sysInfo);

            var procMinAddress = sysInfo.minimumApplicationAddress;
            var procMaxAddress = sysInfo.maximumApplicationAddress;

            Int64 procMinAddressL = (long)procMinAddress;
            Int64 procMaxAddressL = (long)procMaxAddress;

            int[] intBytes = transformBytes(pattern);

            var ret = new List<IntPtr>();
            while (procMinAddressL < procMaxAddressL)
            {
                // 48 = sizeof(MEMORY_BASIC_INFORMATION)
                _ = VirtualQueryEx(this.ProcessHandle, procMinAddress, out var memBasicInfo, 48);

                int CHUNK_SZ;
                if (memBasicInfo.RegionSize > int.MaxValue)
                {
                    CHUNK_SZ = int.MaxValue / 2;
                }
                else
                {
                    CHUNK_SZ = Math.Min(int.MaxValue / 2, (int)memBasicInfo.RegionSize);
                }

                // Check to see if chunk is accessible
                if (memBasicInfo.Protect is MEM_PROTECT.PAGE_EXECUTE_READWRITE or MEM_PROTECT.PAGE_EXECUTE_READ
                        or MEM_PROTECT.PAGE_READONLY or MEM_PROTECT.PAGE_READWRITE &&
                    memBasicInfo.State == MEM_ALLOC.MEM_COMMIT)
                {
                    var shared = ArrayPool<byte>.Shared;
                    byte[] buffer = shared.Rent(CHUNK_SZ);

                    unsafe
                    {
                        fixed (byte* bp = buffer)
                        {
                            HelperFunctions.ReadProcessMemory(this.ProcessHandle, new IntPtr((long)memBasicInfo.BaseAddress), bp, CHUNK_SZ, out int _);
                        }
                    }

                    var results = new List<IntPtr>();

                    for (long i = 0; i < buffer.Length; i++)
                    {
                        for (int j = 0; j < intBytes.Length; j++)
                        {
                            if ((i + j) >= buffer.Length)
                            {
                                break;
                            }

                            if (intBytes[j] != -1 && intBytes[j] != buffer[i + j])
                            {
                                break;
                            }

                            if (j == (intBytes.Length - 1))
                            {
                                var result = new IntPtr(i + (long)memBasicInfo.BaseAddress);
                                results.Add(result);

                                if (once && results.Count != 0)
                                {
                                    return results;
                                }
                            }
                        }
                    }

                    ret.AddRange(results);
                    shared.Return(buffer);
                }

                procMinAddressL += CHUNK_SZ;
                procMinAddress = new IntPtr(procMinAddressL);
            }

            if (once && ret.Count == 0)
            {
                throw new MemoryReadException("No match found for byte scan.");
            }

            return ret;

            // Helper method
            Int32[] transformBytes(string signature)
            {
                string[] bytes = signature.Split(' ');
                Int32[] ints = new int[bytes.Length];

                var regexes = new Regex[]
                {
                new(@"\?[0-9]"),
                new(@"[0-9]\?")
                };

                for (int i = 0; i < ints.Length; i++)
                {
                    if (bytes[i] == "??" || regexes.Any(x => x.IsMatch(bytes[i])))
                    {
                        ints[i] = -1;
                    }
                    else
                    {
                        ints[i] = Int32.Parse(bytes[i], NumberStyles.HexNumber);
                    }
                }

                return ints;
            }
        }

        /// <summary>
        ///  Writes the given byte array directly to memory at lpBaseAddress
        /// </summary>
        /// <param name="lpBaseAddress">The address to overwrite data at</param>
        /// <param name="bytes">The bytes to write</param>
        /// <param name="isProtected">
        ///  Whether the memory access is processed by a VirtualProtectEx call.
        ///  This can resolve some memory access violation errors at a significant speed cost - use sparingly.
        /// </param>
        /// <returns>Number of bytes written</returns>
        private int WriteMemory(IntPtr lpBaseAddress, byte[] bytes, bool isProtected = false)
        {
            unsafe
            {
                fixed (byte* bp = bytes)
                {
                    return WriteProcessMemory(this.ProcessHandle, lpBaseAddress, bp, bytes.Length, isProtected);
                }
            }
        }

        /// <summary>
        ///  Overwrites the value at lpBaseAddress with the provided value.
        /// </summary>
        /// <param name="lpBaseAddress">The address in memory to write to</param>
        /// <param name="value">The value to write</param>
        /// <returns>The number of bytes written</returns>
        public int WriteMemory<T>(IntPtr lpBaseAddress, T value) where T : struct
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
            MemoryMarshal.Write(buffer, in value);

            unsafe
            {
                fixed (byte* bp = buffer)
                {
                    return WriteProcessMemory(this.ProcessHandle, lpBaseAddress, bp, buffer.Length);
                }
            }
        }

        /// <inheritdoc cref="WriteMemory{T}" />
        public int WriteMemory(IntPtr lpBaseAddress, params byte[] bytes) => this.WriteMemory(lpBaseAddress, bytes, false);

        /// <summary>
        ///  Writes a string to the given address. If the length of value is longer than
        ///  the maximum length of the string supported by the process at lpBaseAddress,
        ///  the contents of value will overflow into subsequent addresses and will be
        ///  truncated by <see cref="ReadString" />.
        /// </summary>
        /// <example>
        ///  <code>
        ///  IntPtr addr = new IntPtr(0xabcd);
        ///  var mem = new Memory("MyGame");
        ///  mem.WriteString(addr, "abc"); // Overwrites value at addr with "abc\0".
        ///  mem.WriteString(addr, "abc", false); // Overwrites first 3 chars at addr with "abc". Other characters are left untouched.
        ///  </code>
        /// </example>
        /// <param name="lpBaseAddress">The address in memory to write to</param>
        /// <param name="value">The string to write</param>
        /// <param name="isNullTerminated">
        ///  Whether the written string should be null terminated.
        ///  If false, the value written will overwrite chars beginning from lpBaseAddress through
        ///  the end of value. See the example for more info.
        /// </param>
        /// <param name="encoding">The encoding to write the string in. UTF-8 by default.</param>
        /// <returns>Number of bytes written</returns>
        public int WriteMemory(IntPtr lpBaseAddress, string value, bool isNullTerminated = true, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            if (isNullTerminated)
            {
                value += "\0";
            }

            Span<byte> buffer = encoding.GetBytes(value);
            unsafe
            {
                fixed (byte* bp = buffer)
                {
                    return WriteProcessMemory(this.ProcessHandle, lpBaseAddress, bp, buffer.Length);
                }
            }
        }

        /// <inheritdoc cref="WriteMemory{T}" />
        /// This overload can possibly resolve some memory access violation errors at a significant speed cost.
        /// Use sparingly.
        public int WriteMemoryProtected(IntPtr lpBaseAddress, params byte[] bytes) => this.WriteMemory(lpBaseAddress, bytes, true);

        /// <summary>
        ///  Reads memory of the desired type from lpBaseAddress.
        /// </summary>
        /// <param name="lpBaseAddress">The address to read from</param>
        /// <param name="isProtected">
        ///  Whether the memory access is processed by a VirtualProtectEx call.
        ///  This can resolve some memory access violation errors at a significant speed cost - use sparingly.
        /// </param>
        /// <typeparam name="T">The type of data to read</typeparam>
        /// <returns>The value read from lpBaseAddress</returns>
        /// <exception cref="InvalidOperationException">Type specified could not be read</exception>
        public T ReadMemory<T>(IntPtr lpBaseAddress, bool isProtected = false) where T : struct
        {
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<T>()];
            unsafe
            {
                fixed (byte* bp = buffer)
                {
                    ReadProcessMemory(this.ProcessHandle, lpBaseAddress, bp, buffer.Length, isProtected);
                }
            }

            if (MemoryMarshal.TryRead(buffer, out T res))
            {
                return res;
            }

            throw new InvalidOperationException($"Failed to read memory in the form of {typeof(T)}.");
        }

        /// <summary>
        ///  Read memory from lpBaseAddress into the given buffer. The amount of
        ///  memory read (in bytes) equates to the length of the buffer.
        /// </summary>
        /// <param name="lpBaseAddress">The address to read from</param>
        /// <param name="buffer">The buffer to fill with read data</param>
        /// <param name="isProtected">
        ///  Whether the memory access is processed by a VirtualProtectEx call.
        ///  This can resolve some memory access violation errors at a significant speed cost - use sparingly.
        /// </param>
        /// <returns>
        ///  Number of bytes read
        /// </returns>
        public int ReadMemory(IntPtr lpBaseAddress, ReadOnlySpan<byte> buffer, bool isProtected = false)
        {
            unsafe
            {
                fixed (byte* bp = buffer)
                {
                    return ReadProcessMemory(this.ProcessHandle, lpBaseAddress, bp, buffer.Length, isProtected);
                }
            }
            // BUG: the memory needs to be paged and looped through if sizeBytes is too large as RPM has a size limit.
        }

        /// <summary>
        ///  Read memory from lpBaseAddress into the given buffer. The amount of
        ///  memory read (in bytes) equates to the length of the buffer.
        /// </summary>
        /// <param name="lpBaseAddress">The address to read from</param>
        /// <param name="buffer">The buffer to fill with read data</param>
        /// <param name="isProtected">
        ///  Whether the memory access is processed by a VirtualProtectEx call.
        ///  This can resolve some memory access violation errors at a significant speed cost - use sparingly.
        /// </param>
        /// <returns>
        ///  Number of bytes read
        /// </returns>
        public int ReadMemory(IntPtr lpBaseAddress, ReadOnlyMemory<byte> buffer, bool isProtected = false)
        {
            unsafe
            {
                fixed (byte* bp = buffer.Span)
                {
                    return ReadProcessMemory(this.ProcessHandle, lpBaseAddress, bp, buffer.Length, isProtected);
                }
            }
            // BUG: the memory needs to be paged and looped through if sizeBytes is too large as RPM has a size limit.
        }

        /// <summary>
        ///  Reads the specified number of bytes from lpBaseAddress in the specified encoding.
        ///  The string is expected to start at the provided address. The user is not expected to know
        ///  the length of the string obtained.
        /// </summary>
        /// <param name="lpBaseAddress">The address to read from</param>
        /// <param name="size">The maximum size of the string in bytes</param>
        /// <param name="isNullTerminated">
        ///  Whether or not to return the first null-terminated string found.
        ///  If false, returns a string containing the first (size) bytes beginning from lpBaseAddress.
        ///  For example, if false and size is 255, returns a string with 255 bytes of character data
        ///  beginning from lpBaseAddress.
        /// </param>
        /// <param name="encoding">The encoding to process the read string through. UTF-8 by default.</param>
        /// <returns>The first string read from lpBaseAddress in UTF-8 encoding, unless otherwise specified.</returns>
        /// <exception cref="InvalidOperationException">A string could not be read from the address.</exception>
        public string ReadString(IntPtr lpBaseAddress, int size = 255, bool isNullTerminated = true, Encoding encoding = null)
        {
            encoding ??= Encoding.UTF8;

            ReadOnlySpan<byte> buffer = stackalloc byte[size];
            if (this.ReadMemory(lpBaseAddress, buffer) > 0)
            {
                return isNullTerminated ? encoding.GetString(buffer).Split('\0')[0] : encoding.GetString(buffer)[..size];
            }

            throw new InvalidOperationException("String could not be read.");
        }

        /// <summary>
        ///  Array of Byte pattern scan. Allows scanning for an exact array of bytes with wildcard support.
        ///  Note: Partial wildcards are not supported and will be converted into full wildcards. This has a
        ///  small possibility of resulting in more matches than desired. (e.g. AB ?1 turns into AB ??).
        ///  This overload of AoBScan returns the first address found. A MemoryReadException is thrown if
        ///  no matches are found for the byte scan. This is faster than getting a complete AoBScan and then
        ///  filtering that list.
        /// </summary>
        /// <param name="pattern">
        ///  The pattern of bytes to look for. Bytes are separated by spaces.
        ///  Wildcards (?? symbols) are supported.
        /// </param>
        /// <example>
        ///  <code>
        ///  var addresses = AoBScan("03 AD FF ?? ?? ?? 4D");
        ///  // Returns a list of addresses found (if any) matching the pattern.
        /// </code>
        /// </example>
        /// <exception cref="MemoryReadException">Thrown if marked as once but no matches found.</exception>
        /// <returns>The first match found in the byte scan.</returns>
        public IntPtr AoBScanFirst(string pattern) => this.AoBScan(pattern, true).FirstOrDefault();

        /// <summary>
        ///  Resolves the address from a MultiLevelPtr.
        ///  <returns>
        ///   The memory address that results from the end of the pointer
        ///   Call ReadMemory on this address to retrieve the value located
        ///   at this address.
        ///  </returns>
        ///  <example>
        ///   <code>
        /// var mem = new MemoryModule("MyApplication");
        /// // Assuming the resulting value at this offset is an Int32.
        /// int[] myItemOffsets = { 0xABCD, 0xA, 0xB, 0xC };
        /// int myItemAddress = mem.GetAddressFromMlPtr(new MultiLevelPtr(mem.ModuleBaseAddress, myItemOffsets));
        /// var value = mem.ReadMemory&lt;int&gt;(myItemAddress);
        /// </code>
        ///  </example>
        /// </summary>
        public IntPtr ReadAddressFromMlPtr(MultiLevelPtr mlPtr)
        {
            var baseRead = new IntPtr((long)this.ModuleBaseAddress + (long)mlPtr.Base);

            // Read whatever value is located at the baseAddress. This is our new address.
            long res;
            bool readLong;

            if ((long)baseRead > int.MaxValue)
            {
                res = this.ReadMemory<long>(baseRead);
                readLong = true;
            }
            else
            {
                res = this.ReadMemory<int>(baseRead);
                readLong = false;
            }

            if (mlPtr.Offsets.Count == 0)
            {
                return new IntPtr(res);
            }

            foreach (var offset in mlPtr.Offsets)
            {
                var nextAddress = new IntPtr(res + (long)offset);
                if (offset == mlPtr.Offsets[^1])
                {
                    // Return address of item we're interested in.
                    // Returning a ReadMemory here would result in the value of the item.
                    return nextAddress;
                }

                // Keep looking for address
                if (readLong)
                {
                    res = this.ReadMemory<long>(nextAddress);
                }
                else
                {
                    res = this.ReadMemory<int>(nextAddress);
                }
            }

            return new IntPtr(res);
        }

        /// <summary>
        ///  Resolves the value from the address found from mlPtr
        /// </summary>
        /// <param name="mlPtr">The MultiLevelPtr to read from</param>
        /// <typeparam name="T">The type of data to read from the address resolved from the MultiLevelPtr.</typeparam>
        /// <returns>Value found from the resolved MultiLevelPtr</returns>
        public T ReadValueFromMlPtr<T>(MultiLevelPtr mlPtr) where T : struct => this.ReadMemory<T>(this.ReadAddressFromMlPtr(mlPtr));

        /// <summary>
        ///  Reads a string from the address found from mlPtr.
        ///  <inheritdoc cref="ReadString" />
        /// </summary>
        /// <param name="mlPtr">The MultiLevelPtr to read from</param>
        /// <param name="size">The maximum size of the string in bytes</param>
        /// <param name="isNullTerminated">
        ///  Whether or not to return the first null-terminated string found.
        ///  If false, returns a string containing the first (size) bytes beginning from lpBaseAddress.
        ///  For example, if false and size is 255, returns a string with 255 bytes of character data
        ///  beginning from lpBaseAddress.
        /// </param>
        /// <param name="encoding">The encoding to process the read string through. UTF-8 by default.</param>
        /// <returns>The first string read from lpBaseAddress in UTF-8 encoding, unless otherwise specified.</returns>
        public string ReadStringFromMlPtr(MultiLevelPtr mlPtr, int size = 255, bool isNullTerminated = true, Encoding encoding = null) => this.ReadString(this.ReadAddressFromMlPtr(mlPtr), size, isNullTerminated, encoding);
    }
}