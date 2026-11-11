namespace Orient.MediatR.Interfaces;

/// <summary>
/// Migration bridge for code written against Pipelink 2.x. Resolves to the same instance as <see cref="IMediator"/>.
/// </summary>
[Obsolete("IPipelink was renamed to IMediator in Orient.MediatR. This bridge will be removed in the next major version.")]
public interface IPipelink : IMediator;
