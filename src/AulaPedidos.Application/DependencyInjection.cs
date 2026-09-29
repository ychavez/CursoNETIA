using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Application.Orders;
using AulaPedidos.Application.Products;
using Microsoft.Extensions.DependencyInjection;

namespace AulaPedidos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IMediator, Mediator>();
        services.AddScoped<IRequestHandler<CreateProductCommand, Result<ProductDto>>, CreateProductHandler>();
        services.AddScoped<IRequestHandler<UpdateProductCommand, Result<ProductDto>>, UpdateProductHandler>();
        services.AddScoped<IRequestHandler<DeleteProductCommand, Result<Unit>>, DeleteProductHandler>();
        services.AddScoped<IRequestHandler<GetProductQuery, Result<ProductDto>>, GetProductHandler>();
        services.AddScoped<IRequestHandler<ListProductsQuery, Result<Page<ProductDto>>>, ListProductsHandler>();
        services.AddScoped<IRequestHandler<CreateOrderCommand, Result<OrderDto>>, CreateOrderHandler>();
        services.AddScoped<IRequestHandler<CancelOrderCommand, Result<OrderDto>>, CancelOrderHandler>();
        services.AddScoped<IRequestHandler<GetOrderQuery, Result<OrderDto>>, GetOrderHandler>();
        services.AddScoped<IRequestHandler<ListOrdersQuery, Result<Page<OrderDto>>>, ListOrdersHandler>();
        return services;
    }
}
