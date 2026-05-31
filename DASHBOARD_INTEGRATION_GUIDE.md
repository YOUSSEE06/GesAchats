# 📊 Dashboard Acheteur - Guide d'Intégration Complet

## 📋 Vue d'ensemble

Ce package contient tous les fichiers nécessaires pour créer un dashboard professionnel pour le rôle **Acheteur** dans l'application NexMart. Le dashboard affiche :

- **6 KPIs** avec indicateurs de progression
- **3 graphiques** (Évolution des achats, Dépenses par fournisseur, Répartition des commandes)
- **2 tables d'analyse** (Analyse des prix, Dernières opérations)
- **4 alertes** (Stock faible, Devis en retard, etc.)

## 📦 Fichiers Fournis

### 1. **AcheteurDashboardViewModel.cs**
- Logique complète du dashboard
- Chargement des KPIs et données
- Gestion des graphiques et alertes
- Patterns MVVM + async/await
- **Location**: `ViewModels/Acheteur/AcheteurDashboardViewModel.cs`

### 2. **AcheteurDashboardPage.xaml**
- Interface UI complète
- Cartes KPI avec Material Design
- Graphiques et tables
- Alertes et points d'attention
- **Location**: `Views/Acheteur/AcheteurDashboardPage.xaml`

### 3. **AcheteurDashboardPage.xaml.cs**
- Code-behind simple
- Initialisation de la View
- **Location**: `Views/Acheteur/AcheteurDashboardPage.xaml.cs`

### 4. **App.xaml**
- Configuration MaterialDesignThemes
- Styles globaux
- Couleurs personnalisées
- **Location**: `App.xaml` (à la racine du projet)

### 5. **ServiceInterfaces.cs**
- Interfaces des services métier
- DTOs/Models pour le dashboard
- **Location**: `Core/Services/ServiceInterfaces.cs`

### 6. **DashboardDependencyConfiguration.cs**
- Configuration de l'injection de dépendances
- Enregistrement des services et ViewModels
- **Location**: `WPF/DashboardDependencyConfiguration.cs`

## 🚀 Étapes d'Intégration

### Étape 1 : Installer MaterialDesignThemes

```bash
# Via NuGet Package Manager Console
Install-Package MaterialDesignThemes -Version 4.9.0

# Via .NET CLI
dotnet add package MaterialDesignThemes
```

**Version recommandée**: 4.9.0+ (compatible .NET 10.0)

### Étape 2 : Copier les fichiers

```
GesAchats.WPF/
├── ViewModels/
│   └── Acheteur/
│       └── AcheteurDashboardViewModel.cs
├── Views/
│   └── Acheteur/
│       ├── AcheteurDashboardPage.xaml
│       └── AcheteurDashboardPage.xaml.cs
├── App.xaml
├── DashboardDependencyConfiguration.cs
└── Core/
    └── Services/
        └── ServiceInterfaces.cs
```

### Étape 3 : Configurer App.xaml

Remplacer votre `App.xaml` actuel par le fichier fourni, ou merger les `ResourceDictionary` :

```xml
<ResourceDictionary.MergedDictionaries>
    <!-- Material Design: Thème de base -->
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml"/>
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Defaults.xaml"/>
    
    <!-- Couleurs Material Design -->
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Primary/MaterialDesignColor.Blue.xaml"/>
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignColors;component/Themes/Recommended/Accent/MaterialDesignColor.Amber.xaml"/>
</ResourceDictionary.MergedDictionaries>
```

### Étape 4 : Implémenter les Services

Créer les implémentations des services définis dans `ServiceInterfaces.cs` :

```csharp
public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IUnitOfWork _unitOfWork;
    
    public PurchaseOrderService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    public async Task<int> GetPendingPurchaseOrdersCountAsync(DateTime startDate, DateTime endDate)
    {
        // Implémenter la logique
    }
    
    // ... implémenter les autres méthodes
}
```

### Étape 5 : Configurer l'Injection de Dépendances

Dans votre `App.xaml.cs`, ajouter la configuration:

```csharp
public partial class App : Application
{
    private IServiceProvider _serviceProvider;
    
    protected override void OnStartup(StartupEventArgs e)
    {
        var services = new ServiceCollection();
        
        // Services existants
        ConfigureServices(services);
        
        // Dashboard services
        services.AddDashboardServices();
        
        _serviceProvider = services.BuildServiceProvider();
        base.OnStartup(e);
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Vos services existants
    }
}
```

### Étape 6 : Connecter la View au ViewModel

Dans `AcheteurDashboardPage.xaml.cs` :

```csharp
public partial class AcheteurDashboardPage : UserControl
{
    public AcheteurDashboardPage(AcheteurDashboardViewModel viewModel)
    {
        InitializeComponent();
        this.DataContext = viewModel;
    }
}
```

### Étape 7 : Ajouter la View au Shell/Navigation

Dans votre shell ou système de navigation:

```xaml
<!-- Dans le menu ou la navigation -->
<Button Content="Tableau de bord" 
        Command="{Binding NavigateCommand}" 
        CommandParameter="AcheteurDashboard"/>
```

## ⚙️ Configuration des Services Métier

### Services à implémenter

#### IPurchaseOrderService
```csharp
- GetPendingPurchaseOrdersCountAsync()
- GetPurchaseOrderCountByStatusAsync()
- GetMonthlyPurchaseAmountAsync()
- GetRecentPurchaseOrdersAsync()
```

#### INeedsService
```csharp
- GetPendingNeedsCountAsync()
```

#### ISupplierService
```csharp
- GetActiveSupplierCountAsync()
- GetTopSuppliersByExpenseAsync()
```

#### IStockService
```csharp
- GetTrackedProductsCountAsync()
- GetLowStockProductsCountAsync()
- GetProductPriceAnalysisAsync()
```

#### IInvoiceService
```csharp
- GetTotalInvoiceAmountAsync()
- GetInvoiceCountAsync()
```

## 📊 Personnalisation

### Modifier les couleurs

Dans `App.xaml`, modifier les brushes:

```xml
<SolidColorBrush x:Key="PrimaryColor" Color="#2196F3"/>
<SolidColorBrush x:Key="SuccessColor" Color="#4CAF50"/>
```

### Ajouter des graphiques

Utiliser `LiveChartsCore` ou `OxyPlot` (déjà installés):

```xaml
<lvc:CartesianChart Series="{Binding MySeries}"/>
```

### Modifier les KPIs

Ajouter ou modifier les cartes dans `AcheteurDashboardPage.xaml` et ajouter les propriétés correspondantes dans le ViewModel.

## 🔧 Dépannage

### Problème: "Cannot resolve service"
**Solution**: Vérifier que tous les services sont enregistrés dans `DependencyInjection` et que les interfaces sont implémentées.

### Problème: "MaterialDesignThemes not found"
**Solution**: Installer le package NuGet: `Install-Package MaterialDesignThemes`

### Problème: "Binding fails"
**Solution**: 
1. Vérifier que le ViewModel est assigné au DataContext
2. Vérifier les noms des propriétés
3. Vérifier que le ViewModel hérite de `BaseViewModel` avec `INotifyPropertyChanged`

### Problème: "No data in charts"
**Solution**: 
1. Vérifier que les services retournent les bonnes données
2. Vérifier que le chargement est asynchrone
3. Ajouter du logging pour debuguer

## 📈 Ajouter des Métriques Personnalisées

Pour ajouter une nouvelle métrique:

1. **Ajouter une propriété au ViewModel**:
```csharp
private int _maMetrique;
public int MaMetrique
{
    get => _maMetrique;
    set => SetProperty(ref _maMetrique, value);
}
```

2. **Charger les données**:
```csharp
private async Task LoadMaMetrique()
{
    MaMetrique = await _service.GetMaMetriqueAsync();
}
```

3. **Ajouter la carte KPI**:
```xaml
<Border>
    <StackPanel Padding="16">
        <TextBlock Text="Ma Métrique"/>
        <TextBlock Text="{Binding MaMetrique}" FontSize="28" FontWeight="Bold"/>
    </StackPanel>
</Border>
```

## 📚 Resources

- **Material Design**: https://material.io/design
- **MaterialDesignThemes**: https://github.com/MaterialDesignInXAML/MaterialDesignInXamlToolkit
- **LiveChartsCore**: https://livecharts.dev/
- **WPF MVVM**: https://learn.microsoft.com/en-us/dotnet/desktop/wpf/

## ✅ Checklist d'Intégration

- [ ] MaterialDesignThemes installé via NuGet
- [ ] Fichiers copiés aux bons emplacements
- [ ] App.xaml configuré
- [ ] Services métier implémentés
- [ ] Injection de dépendances configurée
- [ ] View connectée au ViewModel
- [ ] Navigation intégrée
- [ ] Données chargées correctement
- [ ] Styles appliqués correctement
- [ ] Graphiques affichés
- [ ] Alertes fonctionnelles

## 🎯 Prochaines Étapes

1. **Tester le dashboard** avec des données réelles
2. **Personnaliser les couleurs** selon votre brand
3. **Ajouter des filtres** pour les périodes
4. **Intégrer l'export PDF** (QuestPDF est déjà installé)
5. **Ajouter des permissions** selon les rôles

## 📞 Support

Si vous avez des questions ou des problèmes:

1. Vérifier les logs (Serilog)
2. Consulter la documentation Material Design
3. Vérifier les bindings WPF
4. Ajouter du debugging dans le ViewModel
