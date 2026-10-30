using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Interfaces;
using OrderManagement.Application.DTOs.Common;

namespace OrderManagement.Api.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IValidator<CreateOrderRequest> _validator;
        private readonly IValidator<OrderListQuery> _listValidator;

        public OrdersController(IOrderService orderService, IValidator<CreateOrderRequest> validator,
            IValidator<OrderListQuery> listValidator)
        {
            _orderService = orderService;
            _validator = validator;
            _listValidator = listValidator;
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

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
        {
            var result = await _orderService.GetByIdAsync(
                id,
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Order retrieved successfully.",
                data = result
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetList([FromQuery] OrderListQuery query, CancellationToken cancellationToken)
        {
            await _listValidator.ValidateAndThrowAsync(
                query,
                cancellationToken);

            var result = await _orderService.GetListAsync(
                query,
                cancellationToken);

            return Ok(new
            {
                success = true,
                message = "Orders retrieved successfully.",
                data = result
            });
        }

        [HttpPut("{id:guid}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken cancellationToken)
        {
            var result = await _orderService.UpdateStatusAsync(
                id,
                request,
                cancellationToken);

            return Ok(result);
        }

        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelOrderRequest request, CancellationToken cancellationToken)
        {
            var result = await _orderService.CancelAsync(
                id,
                request,
                cancellationToken);

            return Ok(result);
        }
    }
}
