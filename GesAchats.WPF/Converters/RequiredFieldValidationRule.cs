using System.Globalization;
using System.Windows.Controls;

namespace GesAchats.WPF.Converters;

public class RequiredFieldValidationRule : ValidationRule
{
    public override ValidationResult Validate(object value, CultureInfo cultureInfo)
    {
        string? text = value?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ValidationResult(false, "Ce champ est obligatoire");
        }
        return ValidationResult.ValidResult;
    }
}
