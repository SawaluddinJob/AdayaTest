using Microsoft.EntityFrameworkCore;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Exceptions;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;
using OrderManagement.Infrastructure.Services;
using OrderManagement.IntegrationTests.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.IntegrationTests.Orders
{
    [Collection("PostgreSQL Integration Tests")]
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

        private static readonly Guid TestProductId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        private static readonly Guid TestCustomerId =
            Guid.Parse("22222222-2222-2222-2222-222222222222");

        private static async Task ResetDatabaseAsync(
            DbContextOptions<OrderManagementDbContext> options)
        {
            await using var context = new OrderManagementDbContext(options);

            await context.Database.EnsureDeletedAsync();
            await context.Database.MigrateAsync();

            context.Products.Add(new Product
            {
                Id = TestProductId,
                Name = "Product Test",
                StockQuantity = 15,
                Price = 100000
            });

            await context.SaveChangesAsync();
        }

        private static CreateOrderRequest CreateTestRequest()
        {
            return new CreateOrderRequest
            {
                CustomerId = TestCustomerId,
                ShippingAddress = "Jakarta",
                Items = new List<CreateOrderItemRequest>
        {
            new()
            {
                ProductId = TestProductId,
                Quantity = 10
            }
        }
            };
        }

        private static async Task<OrderResponse> CreateSeedOrderAsync(
            DbContextOptions<OrderManagementDbContext> options,
            string idempotencyKey)
        {
            await using var context = new OrderManagementDbContext(options);

            var service = new OrderService(context);

            return await service.CreateAsync(
                CreateTestRequest(),
                idempotencyKey,
                CancellationToken.None);
        }

        [Fact]
        public async Task CreateOrder_WithSameIdempotencyKeyConcurrently_ShouldCreateOnlyOneOrder()
        {
            var options = TestDatabase.CreateOptions();

            await ResetDatabaseAsync(options);

            var request = CreateTestRequest();

            //var taskA = CreateOrderAsync(
            //    options,
            //    request,
            //    "SAME-KEY-001");

            //var taskB = CreateOrderAsync(
            //    options,
            //    request,
            //    "SAME-KEY-001");

            var taskA = CreateOrderWithResponseAsync(
                options,
                request,
                "SAME-KEY-001");

            var taskB = CreateOrderWithResponseAsync(
                options,
                request,
                "SAME-KEY-001");

            var results = await Task.WhenAll(taskA, taskB);

            Assert.Equal(results[0].Id, results[1].Id);

            await using var context = new OrderManagementDbContext(options);

            var orderCount = await context.Orders.CountAsync();

            var product = await context.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == TestProductId);

            Assert.Equal(1, orderCount);
            Assert.Equal(5, product.StockQuantity);
        }

        private static async Task<OrderResponse> CreateOrderWithResponseAsync(
            DbContextOptions<OrderManagementDbContext> options,
            CreateOrderRequest request,
            string idempotencyKey)
        {
            await using var context = new OrderManagementDbContext(options);

            var service = new OrderService(context);

            return await service.CreateAsync(
                request,
                idempotencyKey,
                CancellationToken.None);
        }

        [Fact]
        public async Task CancelOrder_Concurrently_ShouldRestoreStockOnlyOnce()
        {
            // Arrange
            var options = TestDatabase.CreateOptions();

            await ResetDatabaseAsync(options);

            var order = await CreateSeedOrderAsync(
                options,
                "CANCEL-RACE-001");

            // Act
            var taskA = TryCancelAsync(options, order.Id, order.Version);
            var taskB = TryCancelAsync(options, order.Id, order.Version);

            var results = await Task.WhenAll(taskA, taskB);

            // Assert
            Assert.Equal(1, results.Count(x => x == "SUCCESS"));
            Assert.Equal(1, results.Count(x => x == "CONFLICT"));

            await using var context = new OrderManagementDbContext(options);

            var product = await context.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == TestProductId);

            var savedOrder = await context.Orders
                .AsNoTracking()
                .FirstAsync(x => x.Id == order.Id);

            Assert.Equal(15, product.StockQuantity);
            Assert.Equal(OrderStatus.Cancelled, savedOrder.Status);
        }

        private static async Task<string> TryCancelAsync(
            DbContextOptions<OrderManagementDbContext> options,
            Guid orderId,
            int version)
        {
            try
            {
                await using var context = new OrderManagementDbContext(options);

                var service = new OrderService(context);

                await service.CancelAsync(
                    orderId,
                    new CancelOrderRequest { Version = version },
                    CancellationToken.None);

                return "SUCCESS";
            }
            catch (ConflictException)
            {
                return "CONFLICT";
            }
        }

        [Fact]
        public async Task UpdateStatusAndCancel_Concurrently_OnlyOneShouldSucceed()
        {
            // Arrange
            var options = TestDatabase.CreateOptions();

            await ResetDatabaseAsync(options);

            var order = await CreateSeedOrderAsync(
                options,
                "STATUS-RACE-001");

            // Ubah Pending menjadi Confirmed.
            await using (var context = new OrderManagementDbContext(options))
            {
                var service = new OrderService(context);

                await service.UpdateStatusAsync(
                    order.Id,
                    new UpdateOrderStatusRequest
                    {
                        Status = OrderStatus.Confirmed,
                        Version = order.Version
                    },
                    CancellationToken.None);
            }

            // Version sekarang menjadi 2.
            const int currentVersion = 2;

            // Act
            var taskA = TryUpdateStatusAsync(
                options,
                order.Id,
                OrderStatus.Shipped,
                currentVersion);

            var taskB = TryCancelAsync(
                options,
                order.Id,
                currentVersion);

            var results = await Task.WhenAll(taskA, taskB);

            // Assert
            Assert.Equal(1, results.Count(x => x == "SUCCESS"));
            Assert.Equal(1, results.Count(x => x == "CONFLICT"));

            await using var verifyContext =
                new OrderManagementDbContext(options);

            var savedOrder = await verifyContext.Orders
                .AsNoTracking()
                .FirstAsync(x => x.Id == order.Id);

            var product = await verifyContext.Products
                .AsNoTracking()
                .FirstAsync(x => x.Id == TestProductId);

            Assert.True(
                savedOrder.Status == OrderStatus.Shipped ||
                savedOrder.Status == OrderStatus.Cancelled);

            if (savedOrder.Status == OrderStatus.Shipped)
            {
                Assert.Equal(5, product.StockQuantity);
            }
            else
            {
                Assert.Equal(15, product.StockQuantity);
            }
        }

        private static async Task<string> TryUpdateStatusAsync(
            DbContextOptions<OrderManagementDbContext> options,
            Guid orderId,
            OrderStatus status,
            int version)
        {
            try
            {
                await using var context = new OrderManagementDbContext(options);

                var service = new OrderService(context);

                await service.UpdateStatusAsync(
                    orderId,
                    new UpdateOrderStatusRequest
                    {
                        Status = status,
                        Version = version
                    },
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
