#pragma warning disable S101

namespace SimpleMem.Models
{
    public struct MEM_PROTECT
    {
        public const int PAGE_EXECUTE = 0x10;
        public const int PAGE_EXECUTE_READ = 0x20;
        public const int PAGE_EXECUTE_READWRITE = 0x40;
        public const int PAGE_EXECUTE_WRITECOPY = 0x80;
        public const int PAGE_NO_ACCESS = 0x01;
        public const int PAGE_READONLY = 0x02;
        public const int PAGE_READWRITE = 0x04;
        public const int PAGE_WRITECOPY = 0x08;
        public const int PAGE_TARGETS_INVALID = 0x40000000;
        public const int PAGE_TARGETS_NO_UPDATGE = 0x40000000;

        // Can only be used in special cases, see MSDN
        // https://docs.microsoft.com/en-us/windows/win32/memory/memory-protection-constants
        public const int PAGE_GUARD = 0x100;
        public const int PAGE_NOCACHE = 0x200;
        public const int PAGE_WRITECOMBINE = 0x400;
    }
}
