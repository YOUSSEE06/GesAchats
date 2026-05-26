# 🗄️ Schéma de la Base de Données

## 1. Informations Générales
- **SGBD** : PostgreSQL
- **ORM** : Entity Framework Core 10.0
- **Nom de la base** : `GesAchatsDb`

## 2. Tables Principales

### 👤 Utilisateurs & Rôles
- `Roles` : Définit les types d'accès (ADMIN, ACHETEUR, etc.).
- `Users` : Comptes utilisateurs avec hash de mot de passe et lien vers un rôle.

### 📦 Catalogue & Stock
- `Products` : Articles gérés (Désignation, Stock actuel, Seuil minimum).
- `Suppliers` : Coordonnées des fournisseurs.

### 📝 Cycle d'Achat
- `Quotations` : Devis reçus des fournisseurs.
- `PurchaseOrders` : Bons de commande officiels émis.
- `DeliveryNotes` : Réception des marchandises (impacte le stock).

### 💳 Finance
- `Invoices` : Factures fournisseurs liées aux réceptions.
- `Payments` : Règlements effectués pour solder les factures.

### 🕵️ Traçabilité
- `AuditLogs` : Historique des modifications système.

## 3. Diagramme Conceptuel (Simplifié)
`[Devis] -> [Commande] -> [Livraison (Stock +)] -> [Facture] -> [Paiement]`

## 4. Paramètres de Connexion
Configurés dans `appsettings.json` :
```json
"DefaultConnection": "Host=localhost;Port=5432;Database=GesAchatsDb;..."
```
