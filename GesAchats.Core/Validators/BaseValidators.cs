using FluentValidation;
using GesAchats.Core.Entities;

namespace GesAchats.Core.Validators;

public class SupplierValidator : AbstractValidator<Supplier>
{
    public SupplierValidator()
    {
        RuleFor(s => s.CompanyName).NotEmpty().MaximumLength(200);
        RuleFor(s => s.Email).EmailAddress().When(s => !string.IsNullOrEmpty(s.Email));
        RuleFor(s => s.Phone).MaximumLength(20).Matches(@"^[0-9+\s-]*$").WithMessage("Format de téléphone invalide");
    }
}

public class ProductValidator : AbstractValidator<Product>
{
    public ProductValidator()
    {
        RuleFor(p => p.Designation).NotEmpty().MaximumLength(200);
        RuleFor(p => p.Unit).NotEmpty().MaximumLength(20);
        RuleFor(p => p.MinimumStock).GreaterThanOrEqualTo(0);
    }
}
