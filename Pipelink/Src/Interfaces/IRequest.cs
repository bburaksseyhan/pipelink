namespace Pipelink.Interfaces;

/// <summary>
/// Represents a marker interface that defines a contract for a request with a specified response type.
/// </summary>
/// <typeparam name="TResponse">The type of the response that the request is expected to produce.</typeparam>
/// <remarks>
/// This interface provides a foundational structure for implementing request-response patterns,
/// enabling a clear separation of query/command operations and handling mechanisms.
/// </remarks>
public interface IRequest<TResponse> { }
