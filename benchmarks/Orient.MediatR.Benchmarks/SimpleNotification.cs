using Orient.MediatR.Interfaces;

namespace Orient.MediatR.Benchmarks;

public record SimpleNotification(int Id) : INotification; 