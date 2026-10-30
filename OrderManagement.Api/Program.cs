using Microsoft.EntityFrameworkCore;
using OrderManagement.Infrastructure.Data;
using FluentValidation;
using OrderManagement.Application.Validators.Orders;
using OrderManagement.Application.Interfaces;
using OrderManagement.Infrastructure.Services;
using OrderManagement.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Serilog
builder.Host.UseSerilog((context, services, configuration) =>
    configuration
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .WriteTo.Console()
);

// 2. Register FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<
    CreateOrderRequestValidator>();

// 3. Register Database Context
builder.Services.AddDbContext<OrderManagementDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// 4. Register Application Services
builder.Services.AddScoped<IOrderService, OrderService>();

// 5. Register Controllers
builder.Services.AddControllers();

// 6. Register API Explorer and Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// 7. Build Application
var app = builder.Build();

// 8. Configure HTTP Request Logging
app.UseSerilogRequestLogging();

// 9. Configure Custom Middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// 10. Configure Swagger
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// 11. Configure HTTPS
app.UseHttpsRedirection();

// 12. Configure Authorization
app.UseAuthorization();

// 13. Map Controllers
app.MapControllers();

// 14. Run Application
app.Run();