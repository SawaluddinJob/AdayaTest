using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Orders
{
    public class CreateOrderItemRequest
    {
        public Guid ProductId { get; set; }

        public int Quantity { get; set; }
    }
}
