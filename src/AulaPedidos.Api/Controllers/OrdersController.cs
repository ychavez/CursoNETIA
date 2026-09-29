using System.ComponentModel.DataAnnotations;
using AulaPedidos.Api.Operations;
using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Application.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaPedidos.Api.Controllers;
[ApiController]
[Route("api/v1/orders")]
[Authorize(Policy = "orders.read")]
public sealed class OrdersController(IMediator mediator, ILogger<OrdersController> logger) : ControllerBase
{
    private string CustomerId => User.FindFirst("sub")!.Value;
    [HttpGet]
    [ProducesResponseType<Page<OrderDto>>(200)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListOrdersQuery(CustomerId, new PageRequest(page, pageSize)), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<OrderDto>(200)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetOrderQuery(id, CustomerId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
    [HttpPost]
    [ProducesResponseType<OrderDto>(201)]
    [Authorize(Policy = "orders.write")]
    public async Task<IActionResult> Create(CreateOrderInput input, CancellationToken cancellationToken)
    {
        using var activity = CourseTelemetry.ActivitySource.StartActivity("orders.create");
        var result = await mediator.Send(new CreateOrderCommand(CustomerId, input.Items!), cancellationToken);
        if (!result.IsSuccess) return this.ToProblem(result.Error!);
        CourseTelemetry.OrdersCreated.Add(1);
        logger.LogInformation("Pedido {OrderId} creado con {ItemCount} partidas", result.Value!.Id, result.Value.Items.Count);
        return CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value);
    }
    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType<OrderDto>(200)]
    [Authorize(Policy = "orders.write")]
    public async Task<IActionResult> Cancel(Guid id, CancelOrderInput input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CancelOrderCommand(id, CustomerId, input.Version), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
}
public sealed record CreateOrderInput([Required, MinLength(1), MaxLength(50)] OrderLineInput[]? Items);
public sealed record CancelOrderInput(Guid Version);
