path = r'c:\Users\PC\Desktop\wpf\sfe\GesAchats.WPF\MainWindow.xaml'
with open(path, 'r', encoding='utf-8') as f:
    content = f.read()

old = '        <DataTemplate DataType="{x:Type vm:SettingsViewModel}">\n            <views:SettingsView />\n        </DataTemplate>\n        <!-- Ajouter d\'autres DataTemplates ici pour les nouveaux modules -->'
new = '''        <DataTemplate DataType="{x:Type vm:SettingsViewModel}">
            <views:SettingsView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:WarehouseDashboardViewModel}">
            <views:WarehouseDashboardView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:StockAnalysisViewModel}">
            <views:StockAnalysisView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:NeedsListViewModel}">
            <views:NeedsListView />
        </DataTemplate>
        <DataTemplate DataType="{x:Type vm:DeliveryNotesViewModel}">
            <views:DeliveryNotesView />
        </DataTemplate>'''
content = content.replace(old, new)

old_menu = '''                <Separator Margin="0,5"/>
                <TextBlock Text="Opérations" Foreground="Gray" FontSize="10" Margin="10,0,0,5"/>
                
                <Button Content="Gestion des Devis" Command="{Binding NavigateToQuotationsCommand}"'''
new_menu = '''                <Separator Margin="0,5"/>
                <TextBlock Text="Magasinier" Foreground="Gray" FontSize="10" Margin="10,0,0,5"/>
                
                <Button Content="Dashboard" Command="{Binding NavigateToDashboardCommand}" 
                        Visibility="{Binding IsMagasinier, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="0,0,0,5" Padding="10,8" HorizontalContentAlignment="Left" Background="Transparent" BorderThickness="0"/>
                
                <Button Content="Analyse du Stock" Command="{Binding NavigateToDashboardCommand}" 
                        Visibility="{Binding IsMagasinier, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="0,0,0,5" Padding="10,8" HorizontalContentAlignment="Left" Background="Transparent" BorderThickness="0"/>
                
                <Button Content="Liste de Besoins" Command="{Binding NavigateToBesoinsCommand}" 
                        Visibility="{Binding IsMagasinier, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="0,0,0,5" Padding="10,8" HorizontalContentAlignment="Left" Background="Transparent" BorderThickness="0"/>
                
                <Button Content="Bons de Livraison" Command="{Binding NavigateToDeliveryNotesCommand}" 
                        Visibility="{Binding IsMagasinier, Converter={StaticResource BooleanToVisibilityConverter}}"
                        Margin="0,0,0,5" Padding="10,8" HorizontalContentAlignment="Left" Background="Transparent" BorderThickness="0"/>

                <Separator Margin="0,5"/>
                <TextBlock Text="Opérations" Foreground="Gray" FontSize="10" Margin="10,0,0,5"/>
                
                <Button Content="Gestion des Devis" Command="{Binding NavigateToQuotationsCommand}"'''
content = content.replace(old_menu, new_menu)

with open(path, 'w', encoding='utf-8') as f:
    f.write(content)

print('Done')
