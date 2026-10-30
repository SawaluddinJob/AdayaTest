using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Orders
{
    public class CreateOrderRequest
    {
        public Guid CustomerId { get; set; }

        public List<CreateOrderItemRequest> Items { get; set; } = [];

        public string ShippingAddress { get; set; } = string.Empty;
    }
}
