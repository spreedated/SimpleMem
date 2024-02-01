using System;
using System.Collections.Generic;

namespace SimpleMem.Exceptions
{
    /// <summary>
    ///  Thrown when memory read operations have failed.
    ///  Intended to be used as a support tool for hard-to-debug issues.
    /// </summary>
    public class MemoryReadException : Exception
    {
        public MemoryReadException(string message) : base(message) { }

        public MemoryReadException(uint error) : base($"Error code {error} {ErrorCodes.CodeLookup.GetValueOrDefault(error)}") { }
    }
}
