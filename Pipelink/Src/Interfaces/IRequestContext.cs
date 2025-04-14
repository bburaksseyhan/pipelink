namespace Pipelink.Interfaces;

public interface IRequestContext
{
    object Request { get; set; }
    IServiceProvider ServiceProvider { get; set; }
    CancellationToken CancellationToken { get; set; }
} 