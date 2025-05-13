namespace Innago.Shared.HeapService;

using JetBrains.Annotations;

/// <summary>
/// Represents a generic data container for handling the result of an operation and its associated error message, if any.
/// </summary>
/// <typeparam name="T">The type of the result object.</typeparam>
/// <param name="Result">The result of the operation. Can be null if the operation failed or no result is available.</param>
/// <param name="Error">The error message associated with the operation, if any. Can be null if the operation succeeded.</param>
[PublicAPI]
public record Payload<T>(T? Result = default, string? Error = null);