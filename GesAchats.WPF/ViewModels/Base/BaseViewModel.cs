using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GesAchats.WPF.ViewModels.Base;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(storage, value))
            return false;

        storage = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected static string CalculateTrendText(int totalCurrent, int yesterdayCount)
    {
        // Percentage displayed under the card = yesterday added records divided by current total
        if (totalCurrent <= 0)
            return "+0%";

        double percentage = (yesterdayCount * 100.0) / totalCurrent;
        return $"+{percentage:0.#}%";
    }

    protected static string CalculateTrendText(decimal totalCurrent, decimal yesterdayCount)
    {
        // Percentage displayed under the card = yesterday added records divided by current total
        if (totalCurrent <= 0)
            return "+0%";

        double percentage = (double)((yesterdayCount * 100m) / totalCurrent);
        return $"+{percentage:0.#}%";
    }

    // Formule standard du Dashboard principal : ((current - previous) / previous) * 100
    protected static double CalculateVariation(double current, double previous)
    {
        if (previous == 0)
            return current > 0 ? 100 : 0;
        return Math.Round(((current - previous) / previous) * 100, 1);
    }

    protected static double CalculateVariation(int current, int previous)
    {
        return CalculateVariation((double)current, (double)previous);
    }

    protected static double CalculateVariation(decimal current, decimal previous)
    {
        return CalculateVariation((double)current, (double)previous);
    }

    // Texte + couleur de tendance KPI (même rendu que les KpiCard du Dashboard)
    protected static string FormatTrend(double percentage, out bool isPositive)
    {
        if (percentage > 0)
        {
            isPositive = true;
            return $"+{percentage:F1}%";
        }
        if (percentage < 0)
        {
            isPositive = false;
            return $"{percentage:F1}%";
        }
        isPositive = true;
        return "0%";
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    private string _title = string.Empty;
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }
}
