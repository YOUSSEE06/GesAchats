using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace GesAchats.WPF.Converters;

public class EmailValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        string? email = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            return ValidationResult.ValidResult;
        }

        bool isValid = Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!isValid)
        {
            return new ValidationResult(false, "Format email invalide");
        }
        return ValidationResult.ValidResult;
    }
}
