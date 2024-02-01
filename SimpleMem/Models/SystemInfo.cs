#pragma warning disable S101

using System;

namespace SimpleMem.Models
{
    internal struct SYSTEM_INFO
    {
        internal ushort processorArchitecture;
        internal ushort reserved;
        internal uint pageSize;
        internal IntPtr minimumApplicationAddress;
        internal IntPtr maximumApplicationAddress;
        internal IntPtr activeProcessorMask;
        internal uint numberOfProcessors;
        internal uint processorType;
        internal uint allocationGranularity;
        internal ushort processorLevel;
        internal ushort processorRevision;
    }
}
