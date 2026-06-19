Tu es un agent de développement autonome travaillant directement sur mon application desktop **GesAchats**.

## Stack technique

* WPF .NET ;
* architecture MVVM ;
* CommunityToolkit.Mvvm ;
* PostgreSQL ;
* Entity Framework Core ;
* Repository et Unit of Work ;
* injection de dépendances ;
* Serilog.

## Problème à résoudre

Dans l’espace Admin, la page **Situation du Fournisseur** s’ouvre correctement, mais elle n’affiche aucune donnée réelle.

État actuel :

* le nom du fournisseur sélectionné n’apparaît pas dans le titre ;
* `Total des Commandes` affiche `0` ;
* `Total des Bons de Livraison` affiche `0` ;
* `Total des Factures` affiche `0` ;
* `Total des Règlements` affiche `0,00 MAD` ;
* `Solde à Payer` affiche `0,00 MAD` ;
* le tableau détaillé reste vide.

Les données existent pourtant dans PostgreSQL et s’affichent correctement dans les pages suivantes :

* `Suivi Fournisseurs` ;
* `Gestion Bons de Commande` ;
* `Bons de Livraison` ;
* `Factures Fournisseurs` ;
* `Suivi Règlements`.

## Mission

Diagnostiquer la cause exacte, corriger la connexion de la page `Situation du Fournisseur` aux données réelles, compiler toute la solution et tester la navigation complète.

Ne modifie pas le design actuel de la page, sauf si une correction de binding XAML est indispensable.

---

# 1. Règle principale

Ne commence pas par créer de nouvelles classes ou de nouvelles requêtes.

Commence par analyser les pages fonctionnelles et réutilise leur architecture réelle :

* ViewModels ;
* services ;
* interfaces ;
* repositories ;
* Unit of Work ;
* DTO ;
* entités ;
* relations EF Core ;
* système de navigation ;
* injection de dépendances ;
* converters ;
* styles ;
* méthodes de calcul.

N’invente aucun nom de classe, aucune propriété et aucune relation.

Utilise uniquement les noms réellement présents dans le projet.

---

# 2. Analyse des pages fonctionnelles

Inspecte les fichiers liés aux pages suivantes :

1. `Suivi Fournisseurs`
2. `Gestion Bons de Commande`
3. `Bons de Livraison`
4. `Factures Fournisseurs`
5. `Suivi Règlements`
6. `Situation du Fournisseur`

Pour chaque page, identifie :

* le fichier XAML ;
* le ViewModel ;
* les commandes MVVM ;
* le service injecté ;
* l’interface du service ;
* les repositories utilisés ;
* les DTO utilisés ;
* les entités EF Core utilisées ;
* la méthode de chargement ;
* la requête EF Core ;
* l’enregistrement dans l’injection de dépendances ;
* la manière dont le `DataContext` est affecté ;
* la manière dont la navigation est réalisée.

Produis une cartographie réelle, par exemple :

```text
Vue
→ ViewModel
→ Interface de service
→ Service
→ Unit of Work ou Repository
→ DbContext
→ PostgreSQL
```

Utilise les véritables noms trouvés dans le code.

---

# 3. Diagnostiquer la navigation

Analyse précisément le bouton :

```text
Voir la situation
```

Vérifie :

* son `Command` ;
* son `CommandParameter` ;
* le type du paramètre envoyé ;
* la valeur réelle envoyée ;
* la propriété représentant la clé du fournisseur ;
* la méthode exécutée ;
* la page ou le ViewModel de destination.

Place un breakpoint ou un log au début de la commande.

Vérifie que l’identifiant envoyé :

* n’est pas `0` ;
* n’est pas `null` ;
* correspond au fournisseur de la ligne sélectionnée ;
* utilise le même type que la clé primaire réelle ;
* arrive dans `SituationFournisseurViewModel`.

Corrige le bouton si, par exemple :

* il transmet le mauvais identifiant ;
* il transmet l’objet complet alors que la commande attend un identifiant ;
* le `CommandParameter` utilise un mauvais binding ;
* la navigation perd le paramètre ;
* le ViewModel de destination est recréé sans son paramètre.

Ne crée pas un nouveau système de navigation.

Ne crée pas une nouvelle fenêtre.

---

# 4. Vérifier l’initialisation du ViewModel

Analyse le cycle de vie réel de `SituationFournisseurViewModel`.

Vérifie que :

1. le ViewModel est créé par l’injection de dépendances ;
2. ses services sont correctement injectés ;
3. l’identifiant du fournisseur lui est transmis ;
4. sa méthode de chargement est réellement exécutée ;
5. la méthode asynchrone est attendue avec `await` ;
6. les exceptions ne sont pas ignorées ;
7. `IsLoading` revient toujours à `false`.

Réutilise l’interface d’initialisation déjà présente dans le projet, par exemple :

```text
INavigationAware
IAsyncInitializable
IParameterReceiver
```

Ne crée une nouvelle convention que si aucune convention de navigation avec paramètres n’existe.

La logique attendue doit être équivalente à :

```csharp
public async Task InitializeAsync(int fournisseurId)
{
    FournisseurId = fournisseurId;
    await LoadSituationAsync();
}
```

Adapte le type et les noms à la structure réelle.

---

# 5. Vérifier le DataContext

Vérifie que la page utilise exactement l’instance du ViewModel créée par l’injection de dépendances.

Recherche notamment une création manuelle telle que :

```xml
<UserControl.DataContext>
    <local:SituationFournisseurViewModel/>
</UserControl.DataContext>
```

ou :

```csharp
DataContext = new SituationFournisseurViewModel();
```

Si le ViewModel possède des dépendances injectées, supprime toute création manuelle incorrecte et utilise l’instance fournie par le système de navigation.

Vérifie également :

* l’enregistrement de la vue ;
* l’enregistrement du ViewModel ;
* l’enregistrement du service de situation ;
* les durées de vie `Transient`, `Scoped` ou `Singleton` ;
* l’utilisation du même `DbContext` que les pages fonctionnelles.

---

# 6. Analyser les relations réelles

Inspecte les entités et configurations EF Core relatives à :

* Fournisseur ;
* Bon de commande ;
* Ligne de bon de commande ;
* Bon de livraison ;
* Ligne de bon de livraison ;
* Facture ;
* Ligne de facture ;
* Règlement ;
* Produit ou Article.

Inspecte :

* les clés primaires ;
* les clés étrangères ;
* les propriétés de navigation ;
* `OnModelCreating` ;
* les classes `IEntityTypeConfiguration<T>` ;
* `HasOne` ;
* `WithMany` ;
* `WithOne` ;
* les relations optionnelles ;
* les noms réels des propriétés.

Détermine la chaîne réelle permettant de retrouver les données d’un fournisseur.

Exemple uniquement indicatif :

```text
Fournisseur
→ BonsCommande
→ BonLivraison
→ Facture
→ Règlements
```

N’utilise pas cette chaîne sans vérifier les vraies relations du projet.

---

# 7. Comparer avec Gestion Bons de Commande

Analyse la page fonctionnelle `Gestion Bons de Commande`.

Identifie comment elle récupère :

* le numéro du bon ;
* le fournisseur ;
* les lignes de commande ;
* les articles ;
* le prix unitaire ;
* la quantité ;
* le total de ligne ;
* le total de commande.

Réutilise la même source de données et les mêmes propriétés.

Pour la situation fournisseur, la requête doit filtrer directement dans PostgreSQL selon l’identifiant du fournisseur.

Ne charge pas toutes les commandes pour ensuite utiliser :

```csharp
.Where(...)
```

sur une collection déjà chargée en mémoire.

Le filtre doit être inclus dans la requête EF Core.

---

# 8. Comparer avec Bons de Livraison

Analyse la page fonctionnelle `Bons de Livraison`.

Identifie :

* le numéro du BL ;
* la relation avec le bon de commande ;
* les lignes de livraison ;
* les articles ;
* la quantité commandée ;
* la quantité livrée ;
* l’écart ;
* l’état.

Réutilise les vraies relations existantes.

Ne relie pas les documents par leurs références textuelles si une clé étrangère existe.

---

# 9. Comparer avec Factures Fournisseurs

Analyse la page `Factures Fournisseurs`.

Identifie :

* le numéro de facture ;
* la relation avec le fournisseur ;
* la relation avec le bon de livraison ou le bon de commande ;
* les lignes de facture ;
* le montant HT de chaque ligne ;
* le montant de TVA de chaque ligne ;
* le montant TTC de chaque ligne ;
* le total TTC officiel de la facture.

Réutilise la même logique que cette page.

Ne recalcule une valeur que si elle n’est pas enregistrée dans la base.

Si une valeur officielle existe dans l’entité, utilise-la en priorité.

---

# 10. Comparer avec Suivi Règlements

Analyse la page `Suivi Règlements`.

Identifie :

* la relation entre règlement et facture ;
* la date ;
* le mode ;
* la référence ;
* le montant ;
* le total réglé ;
* le reste à payer ;
* le statut ;
* les enums ;
* les converters ;
* les méthodes de calcul.

Réutilise les règles déjà présentes.

Ne crée pas une deuxième logique de statut incompatible avec cette page.

---

# 11. Vérifier les requêtes EF Core

La page doit récupérer uniquement la situation du fournisseur sélectionné.

Utilise :

```csharp
AsNoTracking()
```

pour les données en lecture seule.

Évite :

* les requêtes dans des boucles ;
* les appels `GetAllAsync()` suivis d’un filtrage en mémoire ;
* le lazy loading implicite ;
* le problème N+1 ;
* plusieurs appels inutiles pour chaque commande.

Utilise selon la structure réelle :

* une projection EF Core ;
* des `Include` et `ThenInclude` raisonnables ;
* ou un nombre limité de requêtes groupées.

Si les services existants ne proposent pas le filtrage nécessaire, ajoute des méthodes spécialisées sans supprimer les méthodes utilisées par les autres pages.

Exemples uniquement indicatifs :

```csharp
GetBonsCommandeByFournisseurIdAsync(...)
GetBonsLivraisonByFournisseurIdAsync(...)
GetFacturesByFournisseurIdAsync(...)
GetReglementsByFournisseurIdAsync(...)
```

Respecte les conventions réelles du projet.

---

# 12. Service de situation

Créer ou corriger une seule méthode centrale permettant de récupérer la situation complète.

Exemple indicatif :

```csharp
Task<SituationFournisseurDto?> GetSituationFournisseurAsync(
    int fournisseurId,
    CancellationToken cancellationToken = default
);
```

Cette méthode doit retourner :

* l’identifiant du fournisseur ;
* son nom ;
* le nombre de commandes ;
* le nombre de bons de livraison ;
* le nombre de factures ;
* le montant total des règlements ;
* le solde total à payer ;
* les blocs du tableau détaillé.

Elle doit construire un bloc par bon de commande contenant :

```text
Bon de commande
+ lignes commandées
+ bon de livraison associé
+ lignes livrées
+ facture associée
+ lignes facturées
+ règlements associés
```

Ne mets aucune logique métier dans le code-behind.

---

# 13. Vérifier les propriétés observables

Vérifie que les propriétés du ViewModel notifient correctement l’interface.

Avec CommunityToolkit.Mvvm, utilise les mécanismes déjà présents, par exemple :

```csharp
[ObservableProperty]
private string nomFournisseur = string.Empty;

[ObservableProperty]
private int totalCommandes;

[ObservableProperty]
private decimal totalReglements;
```

Pour la collection du tableau, utilise une collection notifiable ou affecte une nouvelle collection à une propriété observable :

```csharp
ObservableCollection<SituationOperationDto>
```

ou le type équivalent utilisé dans le projet.

Ne charge pas les données dans une simple propriété privée non notifiée.

---

# 14. Vérifier les bindings XAML

Compare chaque binding XAML aux propriétés réelles du ViewModel.

Vérifie :

* le titre ;
* les cinq cartes ;
* l’`ItemsSource` du tableau ;
* les propriétés des lignes ;
* les `DataTemplate` ;
* les `RelativeSource` ;
* les converters ;
* les conditions de visibilité.

Le titre doit afficher :

```text
Situation du Fournisseur – [Nom du fournisseur]
```

Les cartes doivent être liées aux vraies propriétés numériques.

Exemples indicatifs :

```xml
Text="{Binding TotalCommandes}"
Text="{Binding TotalBonsLivraison}"
Text="{Binding TotalFactures}"
Text="{Binding TotalReglements, StringFormat={}{0:N2} MAD}"
Text="{Binding SoldeAPayer, StringFormat={}{0:N2} MAD}"
```

Le tableau doit être lié à la vraie collection :

```xml
ItemsSource="{Binding Operations}"
```

Vérifie particulièrement le changement de `DataContext` à l’intérieur des `ItemsControl` et des `DataTemplate`.

Un binding incorrect dans un template peut afficher les en-têtes tout en laissant toutes les lignes vides.

---

# 15. Vérifier les états de visibilité

Analyse les propriétés telles que :

```text
IsLoading
HasData
HasOperations
HasError
IsEmpty
```

et leurs converters.

Vérifie qu’elles ne cachent pas les données après le chargement.

Les règles attendues sont :

```text
Loading visible si IsLoading = true

Tableau visible si :
IsLoading = false
ET HasError = false
ET Operations.Count > 0

Message aucune donnée visible si :
IsLoading = false
ET HasError = false
ET Operations.Count = 0

Message erreur visible si :
HasError = true
```

Ne montre pas automatiquement les cartes à zéro lorsqu’une erreur technique s’est produite.

---

# 16. Logs Serilog de diagnostic

Ajoute des logs permettant de suivre tout le chargement.

Journalise au minimum :

```text
FournisseurId reçu par la commande
Navigation vers Situation Fournisseur
Initialisation du ViewModel
Début du chargement
Fournisseur trouvé ou introuvable
Nombre de commandes trouvé
Nombre de bons de livraison trouvé
Nombre de factures trouvé
Nombre de règlements trouvé
Nombre de blocs construits
Valeur du total des règlements
Valeur du solde à payer
Fin du chargement
Exception éventuelle
```

Exemple indicatif :

```csharp
_logger.Information(
    "Chargement de la situation du fournisseur {FournisseurId}",
    fournisseurId
);
```

En cas d’erreur :

```csharp
_logger.Error(
    ex,
    "Erreur pendant le chargement de la situation du fournisseur {FournisseurId}",
    fournisseurId
);
```

Ne masque pas une exception en remplaçant simplement toutes les valeurs par zéro.

---

# 17. Vérifier directement la base de données

Utilise la même configuration PostgreSQL que les autres pages.

Vérifie :

* la chaîne de connexion ;
* le `DbContext` injecté ;
* l’environnement chargé ;
* `appsettings.json` ;
* `appsettings.local.json` s’il existe ;
* le projet de démarrage ;
* les migrations ;
* la base réellement interrogée.

Pour un fournisseur connu possédant des opérations, vérifie directement dans la base :

* son identifiant ;
* ses commandes ;
* ses livraisons ;
* ses factures ;
* ses règlements.

Compare les résultats de la base avec ceux retournés par le service.

Ne modifie pas le schéma PostgreSQL sauf si une incohérence réelle et démontrée l’exige.

---

# 18. Calculs obligatoires

Utilise les données réelles.

```text
Total des Commandes
= nombre de bons de commande du fournisseur
```

```text
Total des Bons de Livraison
= nombre de bons de livraison liés à ses commandes
```

```text
Total des Factures
= nombre de factures liées au fournisseur
```

```text
Total des Règlements
= somme des montants de tous ses règlements
```

```text
Solde à Payer
= somme des montants TTC des factures
- somme des règlements
```

Pour chaque facture :

```text
Total réglé
= somme de ses règlements
```

```text
Reste à payer
= total TTC de la facture - total réglé
```

Applique une tolérance raisonnable pour éviter les petites valeurs négatives dues aux arrondis.

---

# 19. Méthode de travail obligatoire

Travaille dans cet ordre :

## Phase A — Inspection

* localiser tous les fichiers concernés ;
* comprendre les relations ;
* comprendre la navigation ;
* comprendre les pages fonctionnelles ;
* identifier la cause probable.

## Phase B — Diagnostic instrumenté

* ajouter les logs nécessaires ;
* exécuter l’application ;
* cliquer sur `Voir la situation` ;
* relever l’identifiant reçu ;
* vérifier la requête ;
* vérifier le résultat du service ;
* vérifier le ViewModel ;
* vérifier les bindings.

## Phase C — Correction minimale

* corriger uniquement les éléments responsables ;
* réutiliser les services existants ;
* conserver le design ;
* ne pas casser les autres pages.

## Phase D — Compilation

Exécuter :

```bash
dotnet clean
dotnet restore
dotnet build
```

Utiliser le fichier `.sln` ou le projet de démarrage réel.

Corriger toutes les erreurs de compilation.

## Phase E — Tests fonctionnels

Tester la navigation et les données avec plusieurs cas.

---

# 20. Tests obligatoires

Effectue au minimum les tests suivants :

### Test 1 — Fournisseur avec plusieurs opérations

Vérifier :

* le nom ;
* les cinq cartes ;
* les commandes ;
* les livraisons ;
* les factures ;
* les règlements ;
* les totaux.

### Test 2 — Fournisseur différent

Vérifier que les données changent et qu’aucune donnée du fournisseur précédent ne reste affichée.

### Test 3 — Fournisseur sans commande

Afficher :

```text
Aucune opération disponible pour ce fournisseur.
```

### Test 4 — Commande sans bon de livraison

La commande doit rester affichée et les cellules BL, facture et règlement doivent afficher `—` ou rester vides selon le design.

### Test 5 — Bon de livraison sans facture

Afficher la commande et la livraison sans provoquer d’exception.

### Test 6 — Facture sans règlement

Vérifier :

```text
Total réglé = 0
Reste à payer = Total facture
Statut = En attente
```

### Test 7 — Facture avec plusieurs règlements

Vérifier la somme, le reste à payer et le statut.

### Test 8 — Navigation Retour

Vérifier :

```text
Suivi Fournisseurs
→ Voir la situation
→ Situation du Fournisseur
→ Retour
→ Suivi Fournisseurs
```

### Test 9 — Fournisseur supprimé entre-temps

Afficher :

```text
Le fournisseur demandé est introuvable.
```

sans faire planter l’application.

### Test 10 — Erreur technique

Afficher :

```text
Impossible de charger la situation du fournisseur.
```

et enregistrer l’exception complète dans Serilog.

---

# 21. Interdictions

Ne pas :

* utiliser des données fictives ;
* coder les valeurs en dur ;
* remplacer artificiellement les zéros ;
* modifier le design global ;
* remplacer le tableau actuel ;
* créer une nouvelle fenêtre ;
* créer un deuxième système de navigation ;
* créer un second `DbContext` ;
* créer une nouvelle chaîne de connexion ;
* ajouter de la logique métier dans le code-behind ;
* faire des requêtes synchrones bloquantes ;
* faire une requête dans une boucle ;
* charger toutes les tables puis filtrer en mémoire ;
* inventer une relation ;
* modifier inutilement PostgreSQL ;
* supprimer une méthode utilisée par une page fonctionnelle ;
* déclarer le problème résolu uniquement parce que le projet compile.

---

# 22. Critères d’acceptation

Le travail est terminé uniquement si :

1. le véritable identifiant du fournisseur est transmis ;
2. le ViewModel est correctement initialisé ;
3. les données viennent de PostgreSQL ;
4. le nom du fournisseur apparaît ;
5. les cinq cartes affichent des valeurs réelles ;
6. le tableau affiche les opérations réelles ;
7. les données correspondent aux autres pages fonctionnelles ;
8. changer de fournisseur change correctement les résultats ;
9. aucune donnée fictive n’est utilisée ;
10. les autres pages fonctionnent toujours ;
11. la solution compile sans erreur ;
12. les logs confirment le chargement correct.

---

# 23. Rapport final obligatoire

À la fin, fournis :

1. la cause exacte du problème ;
2. la valeur de l’identifiant reçue pendant le test ;
3. les fichiers analysés ;
4. les fichiers modifiés ;
5. les fichiers créés ;
6. les pages fonctionnelles utilisées comme référence ;
7. les vrais noms des entités et propriétés ;
8. les relations EF Core utilisées ;
9. les services et repositories réutilisés ;
10. la requête ou projection EF Core finale ;
11. les corrections de navigation ;
12. les corrections de DataContext ;
13. les corrections de bindings ;
14. les logs ajoutés ;
15. le résultat de `dotnet build` ;
16. les tests exécutés et leurs résultats ;
17. les éventuels problèmes encore présents.

Ne réponds pas uniquement avec des recommandations.

Inspecte les fichiers, exécute le projet ou les tests disponibles, applique les corrections, compile la solution et rapporte les résultats réels.
