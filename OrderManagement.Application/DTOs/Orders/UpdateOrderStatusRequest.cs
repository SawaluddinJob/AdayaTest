using OrderManagement.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.DTOs.Orders
{
    public class UpdateOrderStatusRequest
    {
        public OrderStatus Status { get; set; }

        public int Version { get; set; }
    }
}
