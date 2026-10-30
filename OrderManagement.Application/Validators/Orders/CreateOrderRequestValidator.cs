using FluentValidation;
using OrderManagement.Application.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Validators.Orders
{
    public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
    {
        public CreateOrderRequestValidator()
        {
            RuleFor(x => x.CustomerId)
                .NotEmpty()
                .WithMessage("Customer ID is required.");

            RuleFor(x => x.ShippingAddress)
                .NotEmpty()
                .WithMessage("Shipping address is required.")
                .MaximumLength(500);

            RuleFor(x => x.Items)
                .NotEmpty()
                .WithMessage("Order must contain at least one item.");

            RuleForEach(x => x.Items)
                .SetValidator(new CreateOrderItemRequestValidator());

            RuleFor(x => x.Items)
                .Must(items =>
                    items.Select(x => x.ProductId).Distinct().Count()
                    == items.Count)
                .When(x => x.Items != null)
                .WithMessage("Duplicate products are not allowed.");
        }
    }

    public class CreateOrderItemRequestValidator
        : AbstractValidator<CreateOrderItemRequest>
    {
        public CreateOrderItemRequestValidator()
        {
            RuleFor(x => x.ProductId)
                .NotEmpty()
                .WithMessage("Product ID is required.");

            RuleFor(x => x.Quantity)
                .GreaterThan(0)
                .WithMessage("Quantity must be greater than zero.");
        }
    }

}
