using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Controls;

namespace GesAchats.WPF.Converters;

public class PhoneValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        string? phone = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(phone))
        {
            return ValidationResult.ValidResult;
        }

        bool isValid = Regex.IsMatch(phone, @"^\+?[0-9\s]+$");
        if (!isValid)
        {
            return new ValidationResult(false, "Uniquement des chiffres et + au début");
        }
        return ValidationResult.ValidResult;
    }
}
