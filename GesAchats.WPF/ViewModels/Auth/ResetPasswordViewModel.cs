using CommunityToolkit.Mvvm.Input;
using GesAchats.Core.Interfaces;
using GesAchats.WPF.ViewModels.Base;
using Serilog;
using System;
using System.Text.RegularExpressions;
using System.Windows;

namespace GesAchats.WPF.ViewModels.Auth;

public enum ResetPasswordStep
{
    Email,
    Code,
    NewPassword
}

public class ResetPasswordViewModel : BaseViewModel
{
    private readonly IEmailVerificationService _emailVerificationService;

    private string _email = string.Empty;
    public string Email
    {
        get => _email;
        set
        {
            if (SetProperty(ref _email, value))
            {
                SendCodeCommand.NotifyCanExecuteChanged();
                Log.Information("Email changed: {Email}", value);
            }
        }
    }

    private string _verificationCode = string.Empty;
    public string VerificationCode
    {
        get => _verificationCode;
        set
        {
            if (SetProperty(ref _verificationCode, value))
            {
                VerifyCodeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private string _newPassword = string.Empty;
    public string NewPassword
    {
        get => _newPassword;
        set
        {
            if (SetProperty(ref _newPassword, value))
            {
                CalculatePasswordStrength();
                ResetPasswordCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private string _confirmPassword = string.Empty;
    public string ConfirmPassword
    {
        get => _confirmPassword;
        set
        {
            if (SetProperty(ref _confirmPassword, value))
            {
                DoPasswordsMatch = NewPassword == ConfirmPassword;
                ResetPasswordCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private int _passwordScore;
    public int PasswordScore
    {
        get => _passwordScore;
        set => SetProperty(ref _passwordScore, value);
    }

    private string _passwordStrengthLabel = "Faible";
    public string PasswordStrengthLabel
    {
        get => _passwordStrengthLabel;
        set => SetProperty(ref _passwordStrengthLabel, value);
    }

    private bool _hasMinLength;
    public bool HasMinLength
    {
        get => _hasMinLength;
        set => SetProperty(ref _hasMinLength, value);
    }

    private bool _hasLowercase;
    public bool HasLowercase
    {
        get => _hasLowercase;
        set => SetProperty(ref _hasLowercase, value);
    }

    private bool _hasUppercase;
    public bool HasUppercase
    {
        get => _hasUppercase;
        set => SetProperty(ref _hasUppercase, value);
    }

    private bool _hasDigit;
    public bool HasDigit
    {
        get => _hasDigit;
        set => SetProperty(ref _hasDigit, value);
    }

    private bool _hasSpecialCharacter;
    public bool HasSpecialCharacter
    {
        get => _hasSpecialCharacter;
        set => SetProperty(ref _hasSpecialCharacter, value);
    }

    private bool _isPasswordStrong;
    public bool IsPasswordStrong
    {
        get => _isPasswordStrong;
        set => SetProperty(ref _isPasswordStrong, value);
    }

    private bool _doPasswordsMatch;
    public bool DoPasswordsMatch
    {
        get => _doPasswordsMatch;
        set => SetProperty(ref _doPasswordsMatch, value);
    }

    private bool _isCodeVerified = false;
    public bool IsCodeVerified
    {
        get => _isCodeVerified;
        set => SetProperty(ref _isCodeVerified, value);
    }

    private string _message = string.Empty;
    public string Message
    {
        get => _message;
        set => SetProperty(ref _message, value);
    }

    private bool _isSuccess;
    public bool IsSuccess
    {
        get => _isSuccess;
        set => SetProperty(ref _isSuccess, value);
    }

    private ResetPasswordStep _currentStep = ResetPasswordStep.Email;
    public ResetPasswordStep CurrentStep
    {
        get => _currentStep;
        set
        {
            if (SetProperty(ref _currentStep, value))
            {
                OnPropertyChanged(nameof(IsEmailStepVisible));
                OnPropertyChanged(nameof(IsCodeStepVisible));
                OnPropertyChanged(nameof(IsPasswordStepVisible));
                SendCodeCommand.NotifyCanExecuteChanged();
                VerifyCodeCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public bool IsEmailStepVisible => CurrentStep == ResetPasswordStep.Email;
    public bool IsCodeStepVisible => CurrentStep == ResetPasswordStep.Code;
    public bool IsPasswordStepVisible => CurrentStep == ResetPasswordStep.NewPassword;

    // Expose commands
    public CommunityToolkit.Mvvm.Input.IRelayCommand SendCodeCommand { get; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand VerifyCodeCommand { get; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand ResetPasswordCommand { get; }
    public CommunityToolkit.Mvvm.Input.IRelayCommand GoBackCommand { get; }

    public ResetPasswordViewModel(IEmailVerificationService emailVerificationService)
    {
        _emailVerificationService = emailVerificationService;
        Title = "Réinitialiser le mot de passe";

        SendCodeCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(SendCodeAsync, CanSendCode);
        VerifyCodeCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(VerifyCodeAsync, CanVerifyCode);
        ResetPasswordCommand = new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(ResetPasswordAsync, CanResetPassword);
        GoBackCommand = new CommunityToolkit.Mvvm.Input.RelayCommand(GoBack);
    }

    // Override IsBusy to notify commands
    public new bool IsBusy
    {
        get => base.IsBusy;
        set
        {
            if (base.IsBusy != value)
            {
                base.IsBusy = value;
                SendCodeCommand.NotifyCanExecuteChanged();
                VerifyCodeCommand.NotifyCanExecuteChanged();
                ResetPasswordCommand.NotifyCanExecuteChanged();
            }
        }
    }

    private void CalculatePasswordStrength()
    {
        var password = NewPassword;
        HasMinLength = password.Length >= 8;
        HasLowercase = Regex.IsMatch(password, @"[a-z]");
        HasUppercase = Regex.IsMatch(password, @"[A-Z]");
        HasDigit = Regex.IsMatch(password, @"[0-9]");
        HasSpecialCharacter = Regex.IsMatch(password, @"[^\w\d]");

        PasswordScore = 0;
        if (HasMinLength) PasswordScore++;
        if (HasLowercase) PasswordScore++;
        if (HasUppercase) PasswordScore++;
        if (HasDigit) PasswordScore++;
        if (HasSpecialCharacter) PasswordScore++;

        PasswordStrengthLabel = PasswordScore switch
        {
            0 or 1 => "Faible",
            2 or 3 => "Moyen",
            4 => "Bon",
            5 => "Fort",
            _ => "Faible"
        };

        IsPasswordStrong = PasswordScore == 5;
        DoPasswordsMatch = NewPassword == ConfirmPassword;
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
            return emailRegex.IsMatch(email);
        }
        catch
        {
            return false;
        }
    }

    private async Task SendCodeAsync()
    {
        Log.Information("SendCodeCommand executing");
        IsBusy = true;
        IsSuccess = false;
        Message = string.Empty;

        try
        {
            var (success, message) = await _emailVerificationService.SendVerificationCodeAsync(Email);

            if (success)
            {
                IsSuccess = true;
                Message = message;
                CurrentStep = ResetPasswordStep.Code;
            }
            else
            {
                Message = message;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error sending verification code to {Email}", Email);
            Message = "Une erreur est survenue. Veuillez réessayer.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanSendCode()
    {
        var canExecute = !IsBusy 
            && CurrentStep == ResetPasswordStep.Email 
            && !string.IsNullOrWhiteSpace(Email) 
            && IsValidEmail(Email);
        
        Log.Information("CanSendCode: {CanExecute}", canExecute);
        return canExecute;
    }

    private async Task VerifyCodeAsync()
    {
        Log.Information("VerifyCodeCommand executing");
        IsBusy = true;
        IsSuccess = false;
        Message = string.Empty;

        try
        {
            var (success, message) = await _emailVerificationService.VerifyCodeOnlyAsync(Email, VerificationCode);

            if (success)
            {
                IsSuccess = true;
                Message = message;
                IsCodeVerified = true;
                CurrentStep = ResetPasswordStep.NewPassword;
            }
            else
            {
                Message = message;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error verifying code for {Email}", Email);
            Message = "Une erreur est survenue lors de la vérification du code.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanVerifyCode()
    {
        return !IsBusy
            && CurrentStep == ResetPasswordStep.Code
            && !string.IsNullOrWhiteSpace(Email)
            && !string.IsNullOrWhiteSpace(VerificationCode)
            && VerificationCode.Length == 6
            && VerificationCode.All(char.IsDigit);
    }

    private async Task ResetPasswordAsync()
    {
        Log.Information("ResetPasswordCommand executing");
        IsBusy = true;
        IsSuccess = false;
        Message = string.Empty;

        try
        {
            if (NewPassword != ConfirmPassword)
            {
                Message = "Les mots de passe ne correspondent pas.";
                return;
            }

            var (success, message) = await _emailVerificationService.ResetPasswordAsync(Email, VerificationCode, NewPassword);

            if (success)
            {
                IsSuccess = true;
                Message = message;
                await Task.Delay(2000);
                GoBack();
            }
            else
            {
                Message = message;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resetting password for {Email}", Email);
            Message = "Une erreur est survenue. Veuillez réessayer.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanResetPassword() => PasswordScore == 5 && DoPasswordsMatch && IsCodeVerified && !IsBusy;

    private void GoBack()
    {
        foreach (Window window in Application.Current.Windows)
        {
            if (window is Views.Auth.ResetPasswordWindow resetWindow)
            {
                resetWindow.Close();
                break;
            }
        }
    }
}
