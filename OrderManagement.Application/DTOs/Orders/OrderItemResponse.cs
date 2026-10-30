using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Orders
{
    public class OrderItemResponse
    {
        public Guid ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Subtotal => Quantity * UnitPrice;
    }
}
