using System.Collections.Generic;

namespace SimpleMem.Exceptions;

public static class ErrorCodes
{
    /// <summary>
    ///  Lookup dictionary for relevant error codes and their meanings.
    /// </summary>
    public static Dictionary<uint, string> CodeLookup { get; } = new()
    {
        { 5, "Access is denied" },
        { 6, "The handle is invalid" },
        { 8, "Not enough memory resources are available to process this command" },
        { 11, "An attempt was made to load a program with an incorrect format" },
        { 12, "The access code is invalid" },
        { 13, "The data is invalid" },
        { 14, "Not enough memory is available to complete this operation" },
        { 87, "Invalid parameter" },
        { 299, "Only part of a ReadProcessMemory or WriteProcessMemory request was completed" },
        { 487, "Attempt to access invalid address" },
        { 998, "Invalid access to memory location" }
    };
}