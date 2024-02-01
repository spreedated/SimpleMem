using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleMem
{
    /// <summary>
    ///  Class for conveniently definining multi-level pointers.
    ///  Multi-level pointers are used for obtaining values even
    ///  after restarts of programs (unlike a single,
    ///  non-static memory address).
    ///  <example>
    ///   Consider a game called "MyGame" with a module of "MyGame.exe".
    ///   "MyGame.exe" is given a memory address that changes with each
    ///   launch of the program, but all of the values desired (such as
    ///   gold, points, exp, etc.) lay the same distance away in memory
    ///   from the module. Say the offset for gold is 0xC and exp is 0xD.
    ///   A MultiLevelPtr can be created from the "base address" of the module
    ///   with the offsets being 0xC for gold and 0xD for exp. Assuming the
    ///   base address and offsets are correct, the desired values will
    ///   always be returned.
    ///  </example>
    /// </summary>
    public class MultiLevelPtr
    {
        /// <summary>
        ///  Base address of the pointer chain
        /// </summary>
        public IntPtr Base { get; set; }
        /// <summary>
        ///  Optional list of offsets containing pointer offsets (from the provided base).
        /// </summary>
        public List<IntPtr> Offsets { get; set; } = [];

        #region Constructor
        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        /// </summary>
        /// <param name="lpBaseAddress">The address the pointer starts from. This is almost always the ModuleBaseAddress.</param>
        /// <param name="offsets">The offsets needed to decipher the chain</param>
        public MultiLevelPtr(IntPtr lpBaseAddress, params IntPtr[] offsets)
        {
            if (offsets.Length == 0)
            {
                this.Base = lpBaseAddress;
            }
            else
            {
                this.Base = lpBaseAddress;
                this.Offsets = [.. offsets];
            }
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        /// </summary>
        /// <param name="lpBaseAddress">The address the pointer starts from. This is almost always the ModuleBaseAddress.</param>
        /// <param name="offsets">The offsets needed to decipher the chain</param>
        public MultiLevelPtr(IntPtr lpBaseAddress, params int[] offsets)
        {
            this.Base = lpBaseAddress;

            if (offsets.Length == 0)
            {
                return;
            }

            this.Offsets = ConvertInts(offsets);
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        ///  Creates a multi level pointer from an existing one, then adds
        ///  the provided offsets to the old baseMlPtr's offsets list.
        /// </summary>
        /// <param name="baseMlPtr">The previous MultiLevelPtr to base this one from</param>
        /// <param name="offsets">Collection of offsets to append to the old base offsets</param>
        public MultiLevelPtr(MultiLevelPtr baseMlPtr, params int[] offsets)
        {
            this.Base = baseMlPtr.Base;

            if (baseMlPtr.Offsets.Count != 0)
            {
                foreach (var offset in baseMlPtr.Offsets)
                {
                    this.Offsets.Add(offset);
                }
            }

            if (offsets.Length == 0)
            {
                return;
            }

            foreach (int offset in offsets)
            {
                this.Offsets.Add(new IntPtr(offset));
            }
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        ///  Creates a multi level pointer from an existing one, then adds
        ///  the provided offsets to the old baseMlPtr's offsets list.
        /// </summary>
        /// <param name="baseMlPtr">The previous MultiLevelPtr to base this one from</param>
        /// <param name="offsets">Collection of offsets to append to the old base offsets</param>
        public MultiLevelPtr(MultiLevelPtr baseMlPtr, params long[] offsets)
        {
            this.Base = baseMlPtr.Base;

            if (baseMlPtr.Offsets.Count != 0)
            {
                foreach (var offset in baseMlPtr.Offsets)
                {
                    this.Offsets.Add(offset);
                }
            }

            if (offsets.Length == 0)
            {
                return;
            }

            foreach (long offset in offsets)
            {
                this.Offsets.Add(new IntPtr(offset));
            }
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        /// </summary>
        /// <param name="lpBaseAddress">The address the pointer starts from. This is almost always the ModuleBaseAddress.</param>
        /// <param name="offsets">The offsets needed to decipher the chain</param>
        public MultiLevelPtr(long lpBaseAddress, params int[] offsets)
        {
            this.Base = new IntPtr(lpBaseAddress);

            if (offsets.Length == 0)
            {
                return;
            }

            this.Offsets = ConvertInts(offsets);
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        /// </summary>
        /// <param name="pointers">A chain of pointers to resolve</param>
        public MultiLevelPtr(IntPtr[] pointers)
        {
            this.Base = pointers[0];
            this.Offsets = new(pointers[1..]);
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        /// </summary>
        /// <param name="pointers">A chain of pointers to resolve</param>
        public MultiLevelPtr(int[] pointers)
        {
            this.Base = new IntPtr(pointers[0]);
            this.Offsets = ConvertInts(pointers[1..]);
        }

        /// <summary>
        ///  <inheritdoc cref="MultiLevelPtr" />
        /// </summary>
        /// <param name="pointers">A chain of pointers to resolve</param>
        public MultiLevelPtr(long[] pointers)
        {
            this.Base = new IntPtr(pointers[0]);
            this.Offsets = ConvertLongs(pointers[1..]);
        }
        #endregion

        private static List<IntPtr> ConvertInts(int[] ints)
        {
            List<IntPtr> n = new(ints.Length);
            foreach (int i in ints)
            {
                n.Add(new IntPtr(i));
            }

            return n;
        }

        private static List<IntPtr> ConvertLongs(long[] ints)
        {
            List<IntPtr> n = new(ints.Length);
            foreach (long i in ints)
            {
                n.Add(new IntPtr(i));
            }

            return n;
        }

        public override string ToString()
        {
            StringBuilder sb = new($"MultiLevelPtr(Base={this.Base:X}, Offsets=[");
            foreach (nint offset in this.Offsets)
            {
                sb.Append($"{offset:X}, ");
            }
            sb.Remove(sb.Length - 2, 2);
            sb.Append("])");
            return sb.ToString();
        }
    }
}