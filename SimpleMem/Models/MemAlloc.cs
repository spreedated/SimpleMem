#pragma warning disable S101

namespace SimpleMem.Models
{
    public struct MEM_ALLOC
    {
        public const int MEM_COMMIT = 0x00001000;
        public const int MEM_RESERVE = 0x00002000;
        public const int MEM_RESET = 0x00080000;
        public const int MEM_RESET_UNDO = 0x1000000;
        public const int MEM_LARGE_PAGES = 0x20000000;
        public const int MEM_PHYSICAL = 0x00400000;
        public const int MEM_TOP_DOWN = 0x00100000;
        public const int MEM_COMMIT_RESERVE = MEM_COMMIT | MEM_RESERVE;
    }
}
