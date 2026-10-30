using OrderManagement.Application.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponse> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken);
    }
}
