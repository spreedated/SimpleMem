using System;

namespace SimpleMem.Extensions
{
    /// <summary>
    /// Extensions for MultiLevelPtr
    /// </summary>
    public static class PointerExtensions
    {
        /// <summary>
        ///  Reads the value, in memory, of this <see cref="MultiLevelPtr{T}" />. This extension saves
        ///  a separate call to <see cref="Memory.ReadAddressFromMlPtr" />.
        /// </summary>
        /// <param name="mlPtr">The <see cref="MultiLevelPtr{T}" /> to read the value from</param>
        /// <param name="mem">The <see cref="Memory" /> instance in which this value lays</param>
        /// <returns></returns>
        public static T ReadValue<T>(this MultiLevelPtr mlPtr, Memory mem) where T : struct => mem.ReadValueFromMlPtr<T>(mlPtr);

        /// <summary>
        /// Writes the value in the same fashion as <see cref="Memory.WriteMemory{T}"/>
        /// </summary>
        public static int WriteValue<T>(this MultiLevelPtr mlPtr, Memory mem, T val) where T : struct
            => mem.WriteMemory(mlPtr.GetAddress(mem), val);

        /// <summary>
        /// Writes the bytes to memory at the address resolved from the MultiLevelPtr.
        /// </summary>
        /// <returns>The number of bytes written</returns>
        public static int WriteBytes(this MultiLevelPtr mlPtr, Memory mem, params byte[] bytes) =>
            mem.WriteMemory(mlPtr.GetAddress(mem), bytes);

        /// <summary>
        ///  Gets the address resolved from the given mlPtr.
        /// </summary>
        /// <param name="mlPtr">The <see cref="MultiLevelPtr" /> to read the address from</param>
        /// <param name="mem">The <see cref="Memory" /> instance in which this address lays</param>
        /// <returns>A IntPtr containing the address, if found.</returns>
        public static IntPtr GetAddress(this MultiLevelPtr mlPtr, Memory mem) => mem.ReadAddressFromMlPtr(mlPtr);
    }
}
