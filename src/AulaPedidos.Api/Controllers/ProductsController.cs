using System.ComponentModel.DataAnnotations;
using AulaPedidos.Application.Common;
using AulaPedidos.Application.Messaging;
using AulaPedidos.Application.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AulaPedidos.Api.Controllers;
[ApiController]
[Authorize]
[Route("api/v1/products")]
public sealed class ProductsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<Page<ProductDto>>(200)]
    public async Task<IActionResult> List([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListProductsQuery(new PageRequest(page, pageSize)), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ProductDto>(200)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetProductQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
    [HttpPost]
    [ProducesResponseType<ProductDto>(201)]
    [Authorize(Policy = "catalog.write")]
    public async Task<IActionResult> Create(ProductInput input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new CreateProductCommand(input.Sku, input.Name, input.Price), cancellationToken);
        return result.IsSuccess ? CreatedAtAction(nameof(Get), new { id = result.Value!.Id }, result.Value) : this.ToProblem(result.Error!);
    }
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ProductDto>(200)]
    [Authorize(Policy = "catalog.write")]
    public async Task<IActionResult> Update(Guid id, UpdateProductInput input, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateProductCommand(id, input.Sku, input.Name, input.Price, input.Version), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : this.ToProblem(result.Error!);
    }
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(204)]
    [Authorize(Policy = "catalog.write")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid version, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new DeleteProductCommand(id, version), cancellationToken);
        return result.IsSuccess ? NoContent() : this.ToProblem(result.Error!);
    }
}
public sealed record ProductInput([Required, StringLength(32, MinimumLength = 3)] string Sku,
    [Required, StringLength(120, MinimumLength = 3)] string Name,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price);
public sealed record UpdateProductInput([Required, StringLength(32, MinimumLength = 3)] string Sku,
    [Required, StringLength(120, MinimumLength = 3)] string Name,
    [Range(typeof(decimal), "0.01", "1000000")] decimal Price, Guid Version);
