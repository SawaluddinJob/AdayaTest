using OrderManagement.Application.DTOs.Common;
using OrderManagement.Application.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Interfaces
{
    public interface IOrderService
    {
        // Task<OrderResponse> CreateAsync(CreateOrderRequest request, string idempotencyKey, CancellationToken cancellationToken);

        Task<OrderResponse> CreateAsync(
            CreateOrderRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken);

        Task<OrderResponse> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken);

        Task<PagedResponse<OrderResponse>> GetListAsync(
            OrderListQuery query,
            CancellationToken cancellationToken);

        Task<OrderResponse> UpdateStatusAsync(
            Guid id,
            UpdateOrderStatusRequest request,
            CancellationToken cancellationToken);

        Task<OrderResponse> CancelAsync(
            Guid id,
            CancelOrderRequest request,
            CancellationToken cancellationToken);
    }
}
