using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using OrderManagement.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.IntegrationTests.Orders
{
    public class OrderConcurrencyTests
    {
        [Fact]
        public async Task CreateOrder_WhenTwoRequestsOrderSameStock_OnlyOneShouldSucceed()
        {
            var options = TestDatabase.CreateOptions();

            await using (var setupContext = new OrderManagementDbContext(options))
            {
                await setupContext.Database.EnsureDeletedAsync();

                // await setupContext.Database.EnsureCreatedAsync();

                await setupContext.Database.MigrateAsync();

                setupContext.Products.Add(new Product
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Product Test",
                    StockQuantity = 15,
                    Price = 100000
                });

                await setupContext.SaveChangesAsync();
            }

            var productId =
                Guid.Parse("11111111-1111-1111-1111-111111111111");

            var customerId =
                Guid.Parse("22222222-2222-2222-2222-222222222222");

            var request = new CreateOrderRequest
            {
                CustomerId = customerId,
                ShippingAddress = "Jakarta",
                Items = new List<CreateOrderItemRequest>
            {
                new()
                {
                    ProductId = productId,
                    Quantity = 10
                }
            }
            };

            var taskA = CreateOrderAsync(
                options,
                request,
                "CONCURRENT-ORDER-A");

            var taskB = CreateOrderAsync(
                options,
                request,
                "CONCURRENT-ORDER-B");

            var results = await Task.WhenAll(taskA, taskB);

            Assert.Equal(1, results.Count(x => x == "SUCCESS"));
            Assert.Equal(1, results.Count(x => x == "CONFLICT"));

            await using var verifyContext =
                new OrderManagementDbContext(options);

            var product = await verifyContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == productId);

            var orderCount = await verifyContext.Orders.CountAsync();

            Assert.Equal(5, product.StockQuantity);
            Assert.Equal(1, orderCount);
        }

        private static async Task<string> CreateOrderAsync(
            DbContextOptions<OrderManagementDbContext> options,
            CreateOrderRequest request,
            string idempotencyKey)
        {
            try
            {
                await using var context =
                    new OrderManagementDbContext(options);

                var service = new OrderService(context);

                await service.CreateAsync(
                    request,
                    idempotencyKey,
                    CancellationToken.None);

                return "SUCCESS";
            }
            catch (ConflictException)
            {
                return "CONFLICT";
            }
        }
    }
}
