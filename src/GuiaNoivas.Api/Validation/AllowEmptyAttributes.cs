using System.ComponentModel.DataAnnotations;

namespace GuiaNoivas.Api.Validation;

/// <summary>
/// Allows null or empty; validates format only when non-empty.
/// </summary>
public class AllowEmptyEmailAddressAttribute : ValidationAttribute
{
    private readonly EmailAddressAttribute _inner = new EmailAddressAttribute();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
            return _inner.IsValid(s)
                ? ValidationResult.Success
                : new ValidationResult($"{validationContext.DisplayName} deve ser um e-mail válido ou vazio.");
        }
        return new ValidationResult($"{validationContext.DisplayName} deve ser um e-mail válido ou vazio.");
    }
}

/// <summary>
/// Allows null or empty; validates format only when non-empty.
/// </summary>
public class AllowEmptyUrlAttribute : ValidationAttribute
{
    private readonly UrlAttribute _inner = new UrlAttribute();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value == null) return ValidationResult.Success;
        if (value is string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return ValidationResult.Success;
            return _inner.IsValid(s)
                ? ValidationResult.Success
                : new ValidationResult($"{validationContext.DisplayName} deve ser uma URL válida ou vazia.");
        }
        return new ValidationResult($"{validationContext.DisplayName} deve ser uma URL válida ou vazia.");
    }
}
