using System;

namespace SimpleMem.Extensions
{
    /// <summary>
    /// Extensions for Memory
    /// </summary>
    public static class MemoryExtensions
    {
        /// <summary>
        /// Extension that serves as a wrapper for mem.ModuleBaseAddress + offset.
        /// Useful for storing lots of static offsets that derive directly from
        /// the ModuleBaseAddress (these are not the same as pointers, although they are similar).
        /// </summary>
        /// <param name="mem">The memory instance in which the base address exists</param>
        /// <param name="offset">Any address</param>
        /// <returns></returns>
        public static IntPtr StaticOffset(this Memory mem, int offset) => mem.ModuleBaseAddress + offset;
        /// <inheritdoc cref="StaticOffset(SimpleMem.Memory,int)"/> 
        public static IntPtr StaticOffset(this Memory mem, long offset) => new((long)mem.ModuleBaseAddress + offset);
    }
}
