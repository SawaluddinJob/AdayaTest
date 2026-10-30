using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.IntegrationTests.Infrastructure
{
    public static class TestDatabase
    {
        public static DbContextOptions<OrderManagementDbContext> CreateOptions()
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ORDER_TEST_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=order_management_test;Username=postgres;Password=mcgdata";

            return new DbContextOptionsBuilder<OrderManagementDbContext>()
                .UseNpgsql(connectionString)
                .Options;
        }
    }
}
