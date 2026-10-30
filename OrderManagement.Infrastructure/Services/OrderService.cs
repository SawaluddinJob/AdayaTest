using Microsoft.EntityFrameworkCore;
using Npgsql;
using OrderManagement.Application.DTOs.Common;
using OrderManagement.Application.DTOs.Orders;
using OrderManagement.Application.Exceptions;
using OrderManagement.Application.Interfaces;
using OrderManagement.Domain.Entities;
using OrderManagement.Domain.Enums;
using OrderManagement.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace OrderManagement.Infrastructure.Services
{
    public class OrderService : IOrderService
    {
        private readonly OrderManagementDbContext _context;

        public OrderService(OrderManagementDbContext context)
        {
            _context = context;
        }

        public async Task<OrderResponse> CreateAsync(
            CreateOrderRequest request,
            string idempotencyKey,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(idempotencyKey))
            {
                throw new ConflictException(
                    "Idempotency-Key header is required.");
            }

            var requestHash = GenerateRequestHash(request);

            // 1. Check existing idempotency key
            var existingKey = await _context.IdempotencyKeys
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    x => x.Key == idempotencyKey,
                    cancellationToken);

            if (existingKey != null)
            {
                return await HandleExistingKey(
                    existingKey,
                    requestHash,
                    cancellationToken);
            }

            await using var transaction =
                await _context.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                // 2. Reserve idempotency key
                var idempotencyRecord = new IdempotencyKey
                {
                    Id = Guid.NewGuid(),
                    Key = idempotencyKey,
                    RequestHash = requestHash,
                    OrderId = null,
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.IdempotencyKeys.Add(idempotencyRecord);

                await _context.SaveChangesAsync(cancellationToken);

                // 3. Create Order
                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    CustomerId = request.CustomerId,
                    ShippingAddress = request.ShippingAddress,
                    Status = OrderStatus.Pending,
                    Version = 1,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                };

                // 4. Process products in sorted order
                foreach (var item in request.Items.OrderBy(x => x.ProductId))
                {
                    var product = await _context.Products
                        .AsNoTracking()
                        .FirstOrDefaultAsync(
                            x => x.Id == item.ProductId,
                            cancellationToken);

                    if (product == null)
                    {
                        throw new NotFoundException(
                            $"Product {item.ProductId} not found.");
                    }

                    // 5. Atomic stock deduction
                    var affectedRows = await _context.Products
                        .Where(x =>
                            x.Id == item.ProductId &&
                            x.StockQuantity >= item.Quantity)
                        .ExecuteUpdateAsync(
                            setters => setters.SetProperty(
                                x => x.StockQuantity,
                                x => x.StockQuantity - item.Quantity),
                            cancellationToken);

                    if (affectedRows == 0)
                    {
                        throw new ConflictException(
                            $"Insufficient stock for product: {product.Name}.");
                    }

                    // 6. Create order item
                    order.Items.Add(new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        OrderId = order.Id,
                        ProductId = product.Id,
                        Quantity = item.Quantity,
                        UnitPrice = product.Price
                    });
                }

                // 7. Save order
                _context.Orders.Add(order);

                await _context.SaveChangesAsync(cancellationToken);

                // 8. Update idempotency reservation
                idempotencyRecord.OrderId = order.Id;

                await _context.SaveChangesAsync(cancellationToken);

                // 9. Commit transaction
                await transaction.CommitAsync(cancellationToken);

                _context.ChangeTracker.Clear();

                var savedOrder = await _context.Orders
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .ThenInclude(x => x.Product)
                    .FirstAsync(
                        x => x.Id == order.Id,
                        cancellationToken);

                return MapToResponse(savedOrder);
            }
            catch (DbUpdateException ex)
                when (ex.InnerException is PostgresException pg &&
                      pg.SqlState == PostgresErrorCodes.UniqueViolation &&
                      pg.ConstraintName == "IX_IdempotencyKeys_Key")
            {
                await transaction.RollbackAsync(cancellationToken);

                // Another request won the same idempotency key.
                _context.ChangeTracker.Clear();

                var winner = await _context.IdempotencyKeys
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.Key == idempotencyKey,
                        cancellationToken);

                if (winner == null)
                {
                    throw;
                }

                return await HandleExistingKey(
                    winner,
                    requestHash,
                    cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        private async Task<OrderResponse> HandleExistingKey(
            IdempotencyKey existingKey,
            string requestHash,
            CancellationToken cancellationToken)
        {
            if (existingKey.RequestHash != requestHash)
            {
                throw new ConflictException(
                    "Idempotency key has already been used with a different request.");
            }

            if (existingKey.OrderId == null)
            {
                throw new InvalidOperationException(
                    "Idempotency reservation is incomplete.");
            }

            var order = await _context.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(
                    x => x.Id == existingKey.OrderId.Value,
                    cancellationToken);

            if (order == null)
            {
                throw new InvalidOperationException(
                    "Order associated with idempotency key was not found.");
            }

            return MapToResponse(order);
        }

        private static string GenerateRequestHash(
            CreateOrderRequest request)
        {
            var canonicalPayload = new
            {
                request.CustomerId,
                request.ShippingAddress,
                Items = request.Items
                    .OrderBy(x => x.ProductId)
                    .Select(x => new
                    {
                        x.ProductId,
                        x.Quantity
                    })
                    .ToList()
            };

            var json = JsonSerializer.Serialize(canonicalPayload);

            var hash = SHA256.HashData(
                Encoding.UTF8.GetBytes(json));

            return Convert.ToHexString(hash);
        }

        private static OrderResponse MapToResponse(Order order)
        {
            return new OrderResponse
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                ShippingAddress = order.ShippingAddress,
                Status = order.Status,
                Version = order.Version,
                CreatedAt = order.CreatedAt,
                Items = order.Items.Select(item => new OrderItemResponse
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product?.Name ?? string.Empty,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            };
        }

        public async Task<OrderResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .FirstOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

            if (order == null)
            {
                throw new NotFoundException(
                    $"Order with ID {id} was not found.");
            }

            return MapToResponse(order);
        }

        public async Task<PagedResponse<OrderResponse>> GetListAsync(OrderListQuery query, CancellationToken cancellationToken)
        {
            var ordersQuery = _context.Orders
                .AsNoTracking()
                .AsQueryable();

            if (query.Status.HasValue)
            {
                ordersQuery = ordersQuery.Where(
                    x => x.Status == query.Status.Value);
            }

            if (query.CustomerId.HasValue)
            {
                ordersQuery = ordersQuery.Where(
                    x => x.CustomerId == query.CustomerId.Value);
            }

            if (query.StartDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(
                    x => x.CreatedAt >= query.StartDate.Value);
            }

            if (query.EndDate.HasValue)
            {
                ordersQuery = ordersQuery.Where(
                    x => x.CreatedAt <= query.EndDate.Value);
            }

            var totalRecords = await ordersQuery.CountAsync(
                cancellationToken);

            var orders = await ordersQuery
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.Id)
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .Include(x => x.Items)
                .ThenInclude(x => x.Product)
                .ToListAsync(cancellationToken);

            return new PagedResponse<OrderResponse>
            {
                Data = orders.Select(MapToResponse).ToList(),
                Page = query.Page,
                PageSize = query.PageSize,
                TotalRecords = totalRecords
            };
        }
    }
}
