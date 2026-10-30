using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Interfaces;

namespace OrderManagement.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IValidator<CreateOrderRequest> _validator;

        public OrdersController(IOrderService orderService, IValidator<CreateOrderRequest> validator)
        {
            _orderService = orderService;
            _validator = validator;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request, [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
            CancellationToken cancellationToken)
        {
            await _validator.ValidateAndThrowAsync(
                request,
                cancellationToken);

            var result = await _orderService.CreateAsync(
                request,
                idempotencyKey ?? string.Empty,
                cancellationToken);

            return StatusCode(
                StatusCodes.Status201Created,
                new
                {
                    success = true,
                    message = "Order created successfully.",
                    data = result
                });
        }
    }
}
