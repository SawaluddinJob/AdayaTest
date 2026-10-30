using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Domain.Entities
{
    public class IdempotencyKey
    {
        public Guid Id { get; set; }

        public string Key { get; set; } = string.Empty;

        public string RequestHash { get; set; } = string.Empty;

        public Guid? OrderId { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public Order? Order { get; set; }
    }
}
