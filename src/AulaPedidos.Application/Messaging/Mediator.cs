using Microsoft.Extensions.DependencyInjection;

namespace AulaPedidos.Application.Messaging;

// Mediador didáctico: un request tiene exactamente un handler. No es un bus de integración.
internal sealed class Mediator(IServiceProvider services) : IMediator
{
    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        var handler = services.GetRequiredService(handlerType);
        return ((dynamic)handler).Handle((dynamic)request, cancellationToken);
    }
}
