using System;
using System.Collections.Generic;

namespace SimpleMem.Exceptions
{
    /// <summary>
    ///  Thrown when memory write operations have failed.
    ///  Intended to be used as a support tool for hard-to-debug issues.
    /// </summary>
    public class MemoryWriteException : Exception
    {
        public MemoryWriteException(uint error) : base($"Error code {error} {ErrorCodes.CodeLookup.GetValueOrDefault(error)}") { }
    }
}
