#pragma warning disable S101

namespace SimpleMem.Models
{
    internal struct MEMORY_BASIC_INFORMATION
    {
        internal ulong BaseAddress;
        internal ulong AllocationBase;
        internal uint AllocationProtect;
        internal uint __alignment1;
        internal ulong RegionSize;
        internal uint State;
        internal uint Protect;
        internal uint Type;
        internal uint __alignment2;
    }
}
