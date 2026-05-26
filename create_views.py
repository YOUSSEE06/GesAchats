import os

views_dir = r"c:\Users\PC\Desktop\wpf\sfe\GesAchats.WPF\Views"

delivery_notes_xaml = '''<UserControl x:Class="GesAchats.WPF.Views.DeliveryNotesView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="900">

    <Grid Margin="15">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="*"/>
            <ColumnDefinition Width="250"/>
        </Grid.ColumnDefinitions>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Grid.ColumnSpan="2" Text="Reception des Marchandises - Bons de Livraison" FontSize="18" FontWeight="Bold" Margin="0,0,0,10"/>

        <Border Grid.Row="1" Grid.Column="0" Background="#F9F9F9" CornerRadius="4" Padding="15" Margin="0,0,10,0">
            <Grid>
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                    <RowDefinition Height="Auto"/>
                </Grid.RowDefinitions>
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="140"/>
                    <ColumnDefinition Width="*"/>
                </Grid.ColumnDefinitions>

                <TextBlock Grid.Row="0" Grid.Column="0" Text="Date reception *" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <DatePicker Grid.Row="0" Grid.Column="1" SelectedDate="{Binding ReceptionDate}" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="1" Grid.Column="0" Text="N BL *" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <TextBox Grid.Row="1" Grid.Column="1" Text="{Binding NumeroBl}" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="2" Grid.Column="0" Text="Fournisseur *" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <ComboBox Grid.Row="2" Grid.Column="1" ItemsSource="{Binding PurchaseOrders}" SelectedItem="{Binding SelectedPurchaseOrder}" DisplayMemberPath="Supplier.Name" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="3" Grid.Column="0" Text="BC correspondant *" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <ComboBox Grid.Row="3" Grid.Column="1" ItemsSource="{Binding PurchaseOrders}" SelectedItem="{Binding SelectedPurchaseOrder}" DisplayMemberPath="OrderNumber" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="4" Grid.Column="0" Text="Qte attendue" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <TextBlock Grid.Row="4" Grid.Column="1" Text="{Binding SelectedPurchaseOrder.Quantity}" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="5" Grid.Column="0" Text="Qte recue *" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <TextBox Grid.Row="5" Grid.Column="1" Text="{Binding QuantiteRecue}" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="6" Grid.Column="0" Text="Conformite" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <TextBlock Grid.Row="6" Grid.Column="1" Text="{Binding ConformiteText}" FontWeight="Bold" Foreground="{Binding IsConforme, Converter={StaticResource BooleanToBrushConverter}}" Margin="0,0,0,5"/>

                <TextBlock Grid.Row="7" Grid.Column="0" Text="Observations" VerticalAlignment="Center" Margin="0,0,0,5"/>
                <TextBox Grid.Row="7" Grid.Column="1" Text="{Binding Observations}" TextWrapping="Wrap" AcceptsReturn="True" Height="60" Margin="0,0,0,5"/>

                <StackPanel Grid.Row="8" Grid.Column="1" Orientation="Horizontal" Margin="0,5,0,0">
                    <TextBlock Text="{Binding FilePath}" VerticalAlignment="Center" Margin="0,0,10,0" TextTrimming="CharacterEllipsis" MaxWidth="200"/>
                    <Button Content="Parcourir" Command="{Binding BrowseFileCommand}" Padding="8,3"/>
                </StackPanel>

                <StackPanel Grid.Row="8" Grid.Column="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,15,0,0">
                    <Button Content="Annuler" Command="{Binding CancelCommand}" Padding="12,4" Margin="0,0,10,0"/>
                    <Button Content="Valider" Command="{Binding ValidateCommand}" Padding="12,4" Background="#4CAF50" Foreground="White"/>
                </StackPanel>
            </Grid>
        </Border>

        <Border Grid.Row="1" Grid.RowSpan="2" Grid.Column="1" Background="#F0F0F0" CornerRadius="4" Padding="10">
            <StackPanel>
                <TextBlock Text="HISTORIQUE" FontWeight="Bold" Margin="0,0,0,10"/>
                <ItemsControl ItemsSource="{Binding RecentNotes}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <Border Background="White" CornerRadius="3" Padding="8" Margin="0,0,0,5">
                                <StackPanel>
                                    <TextBlock Text="{Binding DeliveryNumber}" FontWeight="Bold"/>
                                    <TextBlock Text="{Binding ReceptionDate, StringFormat=dd/MM HH:mm}" FontSize="10" Foreground="Gray"/>
                                </StackPanel>
                            </Border>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
                <Separator Margin="0,10"/>
                <TextBlock Margin="0,0,0,5">
                    <Run Text="Auj: "/><Run Text="{Binding BlToday}" FontWeight="Bold"/><Run Text=" BL"/>
                </TextBlock>
                <TextBlock>
                    <Run Text="Sem: "/><Run Text="{Binding BlThisWeek}" FontWeight="Bold"/><Run Text=" BL"/>
                </TextBlock>
            </StackPanel>
        </Border>
    </Grid>
</UserControl>
'''

warehouse_dashboard_xaml = '''<UserControl x:Class="GesAchats.WPF.Views.WarehouseDashboardView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:mc="http://schemas.openxmlformats.org/markup-compatibility/2006"
             xmlns:d="http://schemas.microsoft.com/expression/blend/2008"
             mc:Ignorable="d"
             d:DesignHeight="600" d:DesignWidth="800">

    <Grid Margin="15">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <TextBlock Text="Dashboard - Espace Magasinier" FontSize="20" FontWeight="Bold" Margin="0,0,0,15"/>

        <!-- KPI -->
        <UniformGrid Grid.Row="1" Columns="5" Margin="0,0,0,15">
            <Border Background="#E3F2FD" CornerRadius="6" Padding="15" Margin="0,0,10,0">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="Stock" FontSize="11" Foreground="#1976D2" HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding TotalStockItems}" FontSize="24" FontWeight="Bold" Foreground="#1976D2" HorizontalAlignment="Center"/>
                    <TextBlock Text="articles" FontSize="10" Foreground="Gray" HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
            <Border Background="#FFEBEE" CornerRadius="6" Padding="15" Margin="0,0,10,0">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="Ruptures" FontSize="11" Foreground="#D32F2F" HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding RupturesCount}" FontSize="24" FontWeight="Bold" Foreground="#D32F2F" HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
            <Border Background="#FFF8E1" CornerRadius="6" Padding="15" Margin="0,0,10,0">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="Sous min" FontSize="11" Foreground="#F57C00" HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding SousMinCount}" FontSize="24" FontWeight="Bold" Foreground="#F57C00" HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
            <Border Background="#E8F5E9" CornerRadius="6" Padding="15" Margin="0,0,10,0">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="Besoins attente" FontSize="11" Foreground="#388E3C" HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding PendingNeedsCount}" FontSize="24" FontWeight="Bold" Foreground="#388E3C" HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
            <Border Background="#F3E5F5" CornerRadius="6" Padding="15">
                <StackPanel HorizontalAlignment="Center">
                    <TextBlock Text="Receptions sem" FontSize="11" Foreground="#7B1FA2" HorizontalAlignment="Center"/>
                    <TextBlock Text="{Binding BlThisWeekCount}" FontSize="24" FontWeight="Bold" Foreground="#7B1FA2" HorizontalAlignment="Center"/>
                </StackPanel>
            </Border>
        </UniformGrid>

        <!-- Alertes -->
        <Border Grid.Row="2" Background="#FFF3E0" CornerRadius="4" Padding="15" Margin="0,0,0,15">
            <StackPanel>
                <TextBlock Text="ALERTES" FontWeight="Bold" Margin="0,0,0,8"/>
                <ItemsControl ItemsSource="{Binding Alerts}">
                    <ItemsControl.ItemTemplate>
                        <DataTemplate>
                            <TextBlock Text="{Binding Message}" Foreground="{Binding Color}" Margin="0,0,0,3"/>
                        </DataTemplate>
                    </ItemsControl.ItemTemplate>
                </ItemsControl>
            </StackPanel>
        </Border>

        <!-- Acces rapides -->
        <StackPanel Grid.Row="3" Orientation="Horizontal" HorizontalAlignment="Center">
            <Button Content="Consulter stock" Command="{Binding GoToStockCommand}" Padding="15,8" Margin="0,0,10,0" Background="#2196F3" Foreground="White"/>
            <Button Content="Creer besoin" Command="{Binding GoToNeedsCommand}" Padding="15,8" Margin="0,0,10,0" Background="#FF9800" Foreground="White"/>
            <Button Content="Voir commandes" Command="{Binding GoToOrdersCommand}" Padding="15,8" Margin="0,0,10,0" Background="#4CAF50" Foreground="White"/>
            <Button Content="Enregistrer livraison" Command="{Binding GoToDeliveryCommand}" Padding="15,8" Background="#9C27B0" Foreground="White"/>
        </StackPanel>
    </Grid>
</UserControl>
'''

files = {
    "DeliveryNotesView.xaml": delivery_notes_xaml,
    "WarehouseDashboardView.xaml": warehouse_dashboard_xaml,
}

for name, content in files.items():
    path = os.path.join(views_dir, name)
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Created {path}")

# Create code-behind files
codebehinds = {
    "DeliveryNotesView.xaml.cs": "using System.Windows.Controls;\n\nnamespace GesAchats.WPF.Views;\n\npublic partial class DeliveryNotesView : UserControl\n{\n    public DeliveryNotesView()\n    {\n        InitializeComponent();\n    }\n}\n",
    "WarehouseDashboardView.xaml.cs": "using System.Windows.Controls;\n\nnamespace GesAchats.WPF.Views;\n\npublic partial class WarehouseDashboardView : UserControl\n{\n    public WarehouseDashboardView()\n    {\n        InitializeComponent();\n    }\n}\n",
}

for name, content in codebehinds.items():
    path = os.path.join(views_dir, name)
    with open(path, "w", encoding="utf-8") as f:
        f.write(content)
    print(f"Created {path}")

print("Done")
