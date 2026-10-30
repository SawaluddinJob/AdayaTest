using FluentValidation;
using Microsoft.EntityFrameworkCore;
using OrderManagement.Api.Models;
using OrderManagement.Application.Exceptions;

namespace OrderManagement.Api.Middleware
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(
            RequestDelegate next,
            ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Request failed. CorrelationId: {CorrelationId}",
                    context.TraceIdentifier);

                if (context.Response.HasStarted)
                {
                    throw;
                }

                context.Response.Clear();

                var response = new ApiErrorResponse
                {
                    CorrelationId = context.TraceIdentifier
                };

                switch (ex)
                {
                    case ValidationException validationException:
                        context.Response.StatusCode = 400;
                        response.Code = "VALIDATION_ERROR";
                        response.Message = "Request validation failed.";

                        response.Errors = validationException.Errors
                            .GroupBy(x => x.PropertyName)
                            .ToDictionary(
                                x => x.Key,
                                x => x.Select(e => e.ErrorMessage).ToArray());
                        break;

                    case NotFoundException:
                        context.Response.StatusCode = 404;
                        response.Code = "NOT_FOUND";
                        response.Message = ex.Message;
                        break;

                    case ConflictException:
                        context.Response.StatusCode = 409;
                        response.Code = "CONFLICT";
                        response.Message = ex.Message;
                        break;

                    case DbUpdateConcurrencyException:
                        context.Response.StatusCode = 409;
                        response.Code = "CONCURRENCY_CONFLICT";
                        response.Message =
                            "The resource was modified by another request.";
                        break;

                    default:
                        context.Response.StatusCode = 500;
                        response.Code = "INTERNAL_SERVER_ERROR";
                        response.Message =
                            "An unexpected error occurred.";
                        break;
                }

                context.Response.ContentType = "application/json";

                await context.Response.WriteAsJsonAsync(response);
            }
        }
    }
}
