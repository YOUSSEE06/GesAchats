# 🏛️ Architecture Technique - GesAchats v2.0

## 1. Vue d'Ensemble
GesAchats est une application bureautique basée sur une architecture **3-tiers** découplée, utilisant les dernières technologies Microsoft .NET.

## 2. Couches de l'Application

### 📱 Couche Présentation (GesAchats.WPF)
- **Framework** : WPF (Windows Presentation Foundation)
- **Pattern** : MVVM (Model-View-ViewModel)
- **Navigation** : Système de navigation dynamique par `ContentControl` et `DataTemplates`.
- **Injection de Dépendances** : Utilisation de `Microsoft.Extensions.DependencyInjection`.

### ⚙️ Couche Métier (GesAchats.Core)
- **Entités** : Objets POCO représentant le domaine métier.
- **Interfaces** : Définition des contrats pour les services et repositories.
- **Services** : Logique métier (Authentification, Stock, Dashboard).
- **Validation** : Implémentation via `FluentValidation`.

### 🛡️ Couche Administration & Système
- **Gestion des Utilisateurs** : CRUD complet des comptes.
- **Gestion des Rôles** : Configuration des libellés et descriptions.
- **Audit Logs** : Traçabilité complète des actions.
- **Paramètres** : Configuration globale du système (TVA, Délais).

### 💾 Couche Accès aux Données (GesAchats.Data)
- **ORM** : Entity Framework Core 10.0 pour PostgreSQL.
- **Patterns** : Repository Pattern et Unit of Work pour la gestion des transactions.
- **Migrations** : Gestion de l'évolution du schéma via EF Core Migrations.

## 3. Flux de Données
1. L'utilisateur interagit avec la **Vue** (XAML).
2. La Vue déclenche une commande dans le **ViewModel**.
3. Le ViewModel appelle un **Service** métier.
4. Le Service utilise l'**Unit of Work** pour lire ou écrire dans la base de données via un **Repository**.
5. Les changements sont persistés dans **PostgreSQL**.

## 4. Sécurité
- Mots de passe hashés (BCrypt recommandé).
- Gestion des rôles (Admin, Acheteur, Magasinier, Comptable) intégrée au schéma.
- Audit Trail : Chaque action critique est enregistrée dans la table `AuditLogs`.
