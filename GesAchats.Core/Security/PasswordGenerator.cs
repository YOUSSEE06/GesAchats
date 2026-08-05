namespace GesAchats.Core.Security;

/// <summary>
/// Générateur de mots de passe temporaires répondant aux exigences
/// de sécurité (majuscule, minuscule, chiffre et caractère spécial).
/// </summary>
public static class PasswordGenerator
{
    public static string Generate(int length = 10)
    {
        const string lowerChars = "abcdefghijklmnopqrstuvwxyz";
        const string upperChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        const string numberChars = "0123456789";
        const string specialChars = "!@#$%^&*()_+-=[]{}|;:,.<>?";

        if (length < 4)
        {
            length = 4;
        }

        var random = new Random();
        var password = new List<char>
        {
            upperChars[random.Next(upperChars.Length)],
            lowerChars[random.Next(lowerChars.Length)],
            numberChars[random.Next(numberChars.Length)],
            specialChars[random.Next(specialChars.Length)]
        };

        var allChars = lowerChars + upperChars + numberChars + specialChars;
        for (int i = 4; i < length; i++)
        {
            password.Add(allChars[random.Next(allChars.Length)]);
        }

        return new string(password.OrderBy(c => random.Next()).ToArray());
    }
}