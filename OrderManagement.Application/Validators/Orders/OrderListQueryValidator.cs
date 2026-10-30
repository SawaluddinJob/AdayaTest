using FluentValidation;
using OrderManagement.Application.DTOs.Orders;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderManagement.Application.Validators.Orders
{
    public class OrderListQueryValidator : AbstractValidator<OrderListQuery>
    {
        public OrderListQueryValidator()
        {
            RuleFor(x => x.Page)
                .GreaterThanOrEqualTo(1)
                .WithMessage("Page must be greater than or equal to 1.");

            RuleFor(x => x.PageSize)
                .InclusiveBetween(1, 100)
                .WithMessage("PageSize must be between 1 and 100.");

            RuleFor(x => x.StartDate)
                .LessThanOrEqualTo(x => x.EndDate)
                .When(x => x.StartDate.HasValue && x.EndDate.HasValue)
                .WithMessage("StartDate cannot be greater than EndDate.");

            RuleFor(x => x.CustomerId)
                .NotEqual(Guid.Empty)
                .When(x => x.CustomerId.HasValue)
                .WithMessage("CustomerId cannot be empty.");
        }
    }
}
