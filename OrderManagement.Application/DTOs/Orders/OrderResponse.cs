using OrderManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Orders
{
    public class OrderResponse
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }

        public string ShippingAddress { get; set; } = string.Empty;

        public OrderStatus Status { get; set; }

        public int Version { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public decimal TotalAmount => Items.Sum(x => x.Subtotal);

        public List<OrderItemResponse> Items { get; set; } = [];
    }
}
