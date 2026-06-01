# Guide d'intégration - Contrôle Diagramme des Bons de Commande

## 📋 Fichiers fournis

1. **BarChartStyle.xaml** - Dictionnaire de ressources avec tous les styles
2. **PurchaseOrderChartControl.xaml** - Contrôle WPF (XAML)
3. **PurchaseOrderChartControl.xaml.cs** - Code-behind C#
4. **PurchaseOrderChartViewModel.cs** - ViewModel (MVVM)

---

## ✅ Étapes d'intégration

### 1. Copier les fichiers dans ton projet

```
GesAchats/
├── Resources/
│   └── BarChartStyle.xaml          ← Copier ici
├── Views/
│   └── Components/
│       └── PurchaseOrderChartControl.xaml      ← Copier ici
│       └── PurchaseOrderChartControl.xaml.cs   ← Copier ici
└── ViewModels/
    └── PurchaseOrderChartViewModel.cs          ← Copier ici
```

### 2. Ajouter le style au App.xaml (ou ton ResourceDictionary principal)

```xml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <!-- Autres styles... -->
            <ResourceDictionary Source="Resources/BarChartStyle.xaml"/>
        </ResourceDictionary.MergedDictionaries>
    </ResourceDictionary>
</Application.Resources>
```

### 3. Utiliser le contrôle dans une page

Exemple : `Views/Admin/DashboardPage.xaml`

```xml
<Window x:Class="GesAchats.Views.AdminWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:components="clr-namespace:GesAchats.Views.Components">

    <Grid>
        <!-- Ton contenu -->
        
        <components:PurchaseOrderChartControl 
            DataContext="{Binding PurchaseOrderChartViewModel}"
            ChartTitle="Bons de commande (7 derniers jours)"
            ChartData="{Binding ChartData}"/>
    </Grid>
</Window>
```

### 4. Configurer le ViewModel dans ton MainViewModel ou AdminViewModel

```csharp
public class AdminViewModel : ViewModelBase
{
    private PurchaseOrderChartViewModel _purchaseOrderChartViewModel;

    public PurchaseOrderChartViewModel PurchaseOrderChartViewModel
    {
        get => _purchaseOrderChartViewModel;
        set => SetProperty(ref _purchaseOrderChartViewModel, value);
    }

    public AdminViewModel()
    {
        PurchaseOrderChartViewModel = new PurchaseOrderChartViewModel();
    }
}
```

---

## 🎨 Personnalisation

### Changer les couleurs

Edit `BarChartStyle.xaml` :

```xml
<SolidColorBrush x:Key="BarPending" Color="#FF9D5C"/>    ← Orange
<SolidColorBrush x:Key="BarValidated" Color="#1ED760"/>  ← Vert
```

### Ajouter une 3ème catégorie

Dans `PurchaseOrderChartViewModel.cs`, ajoute dans `LoadChartData()` :

```csharp
ChartData.Add(new ChartBarData
{
    Label = "BC en retard",
    Value = 5,
    BarHeight = CalculateBarHeight(5, 50, 280),
    BarColor = new SolidColorBrush(Color.FromArgb(255, 255, 80, 80)) // Rouge
});
```

### Adapter la hauteur max (axe Y)

Change `maxValue` dans `LoadChartData()` :

```csharp
int maxValue = 100; // Au lieu de 50
```

Et mets à jour les labels sur le côté gauche du XAML.

---

## 📚 Dépendances

**✅ Zéro dépendance externe** - Utilise uniquement :
- WPF standard (`System.Windows`)
- Binding MVVM
- ObservableCollection

Pas besoin de Syncfusion, Live Charts, ou autre.

---

## 🔧 Fonctionnalités avancées

### Connecter une API/BDD

Dans `PurchaseOrderChartViewModel.cs`, replace le code exemple :

```csharp
private async void LoadChartData()
{
    try
    {
        // Appel async à ta BDD/API
        var data = await _purchaseOrderService.GetLastSevenDaysDataAsync();
        
        ChartData = new ObservableCollection<ChartBarData>(
            data.Select(d => new ChartBarData
            {
                Label = d.StatusName,
                Value = d.Count,
                BarHeight = CalculateBarHeight(d.Count, 50, 280),
                BarColor = GetColorForStatus(d.Status)
            })
        );
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Erreur: {ex.Message}");
    }
}

private Brush GetColorForStatus(PurchaseOrderStatus status)
{
    return status switch
    {
        PurchaseOrderStatus.Pending => new SolidColorBrush(Color.FromArgb(255, 255, 157, 92)),
        PurchaseOrderStatus.Validated => new SolidColorBrush(Color.FromArgb(255, 30, 215, 96)),
        PurchaseOrderStatus.Cancelled => new SolidColorBrush(Color.FromArgb(255, 200, 200, 200)),
        _ => Brushes.Gray
    };
}
```

### Animation au chargement

Dans `PurchaseOrderChartControl.xaml`, ajoute une animation sur les barres :

```xml
<Rectangle Height="{Binding BarHeight}" Fill="{Binding BarColor}" ...>
    <Rectangle.Triggers>
        <EventTrigger RoutedEvent="Loaded">
            <BeginStoryboard>
                <Storyboard>
                    <DoubleAnimation 
                        Storyboard.TargetProperty="Height"
                        From="0" 
                        To="{Binding BarHeight}"
                        Duration="0:0:0.6"
                        EasingFunction="EaseOut"/>
                </Storyboard>
            </BeginStoryboard>
        </EventTrigger>
    </Rectangle.Triggers>
</Rectangle>
```

---

## 🐛 Troubleshooting

### **Le diagramme ne s'affiche pas**
- Vérifie que `BarChartStyle.xaml` est dans le chemin correct
- Check les namespaces dans les fichiers XAML

### **Les barres ne sont pas proportionnelles**
- Vérifie `CalculateBarHeight()` - elle doit être : `(value / maxValue) * chartHeight`
- Ajuste `maxValue` et `chartHeight` selon tes données

### **Les couleurs sont bizarres**
- Vérifie le format des couleurs : `#AARRGGBB` (avec alpha)
- Exemples :
  - `#FF9D5C` = Orange opaque
  - `#801ED760` = Vert semi-transparent (50% opacity)

---

## 📖 Résumé

- **Pas de dépendances** ✅
- **MVVM pattern** ✅
- **Réutilisable** ✅
- **Personnalisable** ✅
- **Performant** ✅

Besoin de modifs? Dis-moi! 🚀
