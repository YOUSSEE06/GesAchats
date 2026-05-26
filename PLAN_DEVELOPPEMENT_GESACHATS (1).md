# GesAchats — Plan de Développement Complet

**Système de Gestion des Achats & Approvisionnements**  
*Application bureautique interne multi-utilisateurs*  
**Version 2.0 | Version du plan : 1.0**

---

## 📑 Table des Matières

1. [Vue d'ensemble Exécutive](#vue-densemble-exécutive)
2. [Architecture Technologique](#architecture-technologique)
3. [Structure de la Base de Données](#structure-de-la-base-de-données)
4. [Décomposition des Modules](#décomposition-des-modules)
5. [Workflow et Cas d'Usage](#workflow-et-cas-dusage)
6. [Plan de Développement par Phase](#plan-de-développement-par-phase)
7. [Stratégie de Test et QA](#stratégie-de-test-et-qa)
8. [Déploiement et Installation](#déploiement-et-installation)
9. [Ressources et Chronologie](#ressources-et-chronologie)
10. [Risques et Mitigation](#risques-et-mitigation)
11. [Documentation Requise](#documentation-requise)

---

## Vue d'Ensemble Exécutive

### Objectif du Projet

Développer une **application bureautique centralisée** permettant à une entreprise de construction de gérer efficacement son cycle d'approvisionnement complet : de l'analyse des besoins en stock jusqu'au règlement des factures fournisseurs. Cette application unifiera des processus fragmentés et améliorera la traçabilité, la performance et la conformité.

### Périmètre

| Aspect | Détail |
|--------|--------|
| **Type d'application** | Logiciel de bureau (WPF/C#/.NET) |
| **Portée** | Réseau local (LAN) interne uniquement |
| **Utilisateurs** | 10–12 simultanés max |
| **Plateforme** | Windows 10, 11 (avec .NET 10.0 Runtime) |
| **Base de données** | SQL Server Express / LocalDB |
| **Modules couverts** | 6 étapes du workflow complet |
| **Rôles supportés** | 4 niveaux (Opérationnel → Décisionnel) |

### Livrables Principaux

- ✅ Application exécutable (.exe)
- ✅ Base de données pré-configurée
- ✅ Manuel d'installation & configuration
- ✅ Guide utilisateur par rôle
- ✅ Documentation technique (architecture, DB, API interne)
- ✅ Scripts de sauvegarde et maintenance

### Bénéfices Attendus

| Bénéfice | Impact |
|----------|--------|
| Centralisation des données | Élimination de 80% des fichiers dispersés |
| Traçabilité complète | Audit trail complet, horodatage de chaque action |
| Réduction des erreurs | Validation stricte du workflow, doublons impossibles |
| Historique des prix | Optimisation des négociations fournisseurs |
| Conformité | Respect du cycle documentaire, archivage sécurisé |

---

## Architecture Technologique

### Vue d'Ensemble Architecturale

```
┌──────────────────────────────────────────────────────────┐
│                  COUCHE PRÉSENTATION (WPF)                │
│  - MainWindow.xaml / XAML Pages                           │
│  - UserControls (Formulaires, Tableaux, Widgets)          │
│  - Styles XAML centralisés (Ressources)                   │
└────────────────────────────────────────────────────────────┘
                             ↓
┌──────────────────────────────────────────────────────────┐
│                   COUCHE MÉTIER (C#)                      │
│  - ViewModels (MVVM Pattern)                              │
│  - Services métier (Gestion workflow, validation)         │
│  - Entités métier (Devis, BC, BL, Facture, Règlement)    │
│  - Gestionnaires rôles & permissions                      │
└────────────────────────────────────────────────────────────┘
                             ↓
┌──────────────────────────────────────────────────────────┐
│                   COUCHE ACCÈS AUX DONNÉES                │
│  - Entity Framework Core / Dapper                         │
│  - Repositories pattern                                   │
│  - Migrations BD                                          │
│  - Gestion transactions ACID                              │
└────────────────────────────────────────────────────────────┘
                             ↓
┌──────────────────────────────────────────────────────────┐
│                COUCHE PERSISTANCE (SQL SERVER)            │
│  - Tables métier (Devis, BC, BL, Facture, Paiement)      │
│  - Tables de support (Fournisseurs, Utilisateurs, Logs)  │
│  - Triggers & Procédures stockées                         │
│  - Indexes & Partitionnement                              │
└────────────────────────────────────────────────────────────┘
```

### Stack Technologique Détaillé

#### Frontend
- **Framework** : WPF (Windows Presentation Foundation)
- **Langage** : C# 14.0+
- **Markup** : XAML
- **Pattern UI** : MVVM (Model-View-ViewModel)
- **Binding** : Data Binding WPF + INotifyPropertyChanged
- **Ressources** : Brushes, Styles, Templates centralisés

#### Backend
- **Plateforme** : .NET 10.0 (Long-Term Support - LTS)
- **Langage** : C# 14.0+
- **ORM** : Entity Framework Core 10+
- **Patterns** : Repository, Service Locator, Singleton
- **Validation** : FluentValidation
- **Logging** : Serilog
- **DI Container** : Microsoft.Extensions.DependencyInjection

#### Base de Données
- **SGBD** : SQL Server Express 2019/2022
- **Schéma** : Relatif complet, normalisé
- **Transactions** : ACID complètes
- **Triggers** : Validation métier, mises à jour en cascade
- **Procédures stockées** : Rapports, archivage
- **Index** : Optimisation lectures fréquentes

#### Outils & Infrastructure
- **Contrôle de version** : Git (GitHub/GitLab)
- **Package Manager** : NuGet
- **Build** : MSBuild
- **Testing** : xUnit, Moq
- **CI/CD** : GitHub Actions / Azure DevOps
- **Documentation** : Markdown, Confluence

### Prérequis Environnement .NET 10.0

#### Pour les Développeurs

**Environnement de Développement :**
- **Visual Studio 2022** (17.10+) avec workload ".NET desktop development"
- **.NET 10.0 SDK** (inclus avec Visual Studio 2022)
- **Visual Studio Code** + C# Dev Kit (alternative légère)

**Vérification Installation :**
```bash
dotnet --version       # Doit afficher 10.0.x
dotnet --list-sdks    # Vérifier présence du SDK 10.0
```

**Project File Configuration :**
```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<LangVersion>latest</LangVersion>
```

#### Pour les Machines de Production/Déploiement

**Système d'Exploitation :**
- Windows 10 21H2 (Build 19044+) ou Windows 11
- ⚠️ **Non compatible** : Windows Vista, 7, 8, 8.1
- ✅ **Recommandé** : Windows 10/11 (dernières mises à jour)

**Runtime & Dependencies :**
- **.NET 10.0 Runtime** (Téléchargement MS : https://dotnet.microsoft.com/download/dotnet/10.0)
- **Visual C++ Redistributable** 2015+ (si déploiement standalone)
- **SQL Server Native Client 11.0+** ou **ODBC Driver 17 for SQL Server**

**Installation Runtime (non-développeurs) :**
```powershell
# Télécharger depuis MS ou via déploiement interne
# Exécuter installer en tant qu'admin
# Redémarrer machine
dotnet --info  # Vérification après installation
```

**Requête Système Minimale (Production) :**
- CPU : Dual-core 2.0 GHz+
- RAM : 2 GB minimum, 4+ GB recommandé
- Disque : 500 MB pour runtime + application
- Réseau : LAN interne stable (10Mbps+)

#### Configuration SQL Server

**Version Supportée :**
- SQL Server Express 2019 SP2+ ou 2022
- LocalDB (Dev) via SQL Server Express

**Compatibility Level :**
- 150 (SQL Server 2019) ou 160 (SQL Server 2022)

**Exigences Réseau :**
- Port TCP 1433 (défaut) sur réseau LAN
- Authentification : Windows Auth ou Mixed Mode
- TLS 1.2+ pour connexions chiffrées (optionnel mais recommandé)

#### Mise à Jour Futures (.NET 10.0 LTS)

**.NET 10.0 est une version LTS (Long-Term Support) :**
- **Support critiques & sécurité** : 18 mois minimum
- **Date de fin support** : Novembre 2026
- **Upgrade path** : Si nécessaire → .NET 12 LTS (fin 2026)

**Plan Migration :**
- Évaluation moitié du support LTS (mai 2025)
- Test compatibilité .NET 12 préalables (Q4 2025)
- Upgrade planning (si requis) Q1-Q2 2026

---

## Structure de la Base de Données

### Schéma Entité-Relation (ER)

```
┌─────────────────────┐
│   UTILISATEURS      │
├─────────────────────┤
│ id (PK)             │
│ login (UNIQUE)      │
│ email               │
│ motDePasse (hash)   │
│ role (FK)           │
│ actif               │
│ dateCreation        │
│ dateModif           │
└─────────────────────┘
         ↓
┌─────────────────────┐
│      RÔLES          │
├─────────────────────┤
│ id (PK)             │
│ code (UNIQUE)       │
│ label               │
│ permissions (JSON)  │
└─────────────────────┘

┌─────────────────────┐
│   FOURNISSEURS      │
├─────────────────────┤
│ id (PK)             │
│ raisonSociale       │
│ contact             │
│ email               │
│ telephone           │
│ adresse             │
│ codePostal          │
│ ville               │
│ pays                │
│ actif               │
└─────────────────────┘

┌──────────────────────────────┐
│        ARTICLES              │
├──────────────────────────────┤
│ id (PK)                      │
│ designation                  │
│ unite (kg, m2, pcs, etc)    │
│ stockActuel                  │
│ stockMinimum                 │
│ famille (Matériaux, etc)    │
│ actif                        │
└──────────────────────────────┘

┌──────────────────────────────┐
│   HISTORIQUE_PRIX_FOURNISSEUR│
├──────────────────────────────┤
│ id (PK)                      │
│ fournisseur_id (FK)          │
│ article_id (FK)              │
│ prixUnitaireHT               │
│ prixUnitaireTTC              │
│ dateDebut                    │
│ dateFin                      │
│ quantiteMin                  │
│ delaiLivraisonJours          │
└──────────────────────────────┘

┌──────────────────────────────┐
│        DEVIS                 │
├──────────────────────────────┤
│ id (PK)                      │
│ numero (UNIQUE)              │
│ dateCreation                 │
│ fournisseur_id (FK)          │
│ article_id (FK)              │
│ designation                  │
│ quantite                     │
│ unite                        │
│ prixUnitaireHT               │
│ prixUnitaireTTC              │
│ statut (En attente, Validé) │
│ creePar_id (FK → Utilisateur)│
│ dateModif                    │
└──────────────────────────────┘

┌──────────────────────────────┐
│    BON_DE_COMMANDE           │
├──────────────────────────────┤
│ id (PK)                      │
│ numero (UNIQUE)              │
│ dateEmission                 │
│ fournisseur_id (FK)          │
│ devis_id (FK)                │
│ article_id (FK)              │
│ designation                  │
│ quantite                     │
│ unite                        │
│ prixUnitaireHT               │
│ prixUnitaireTTC              │
│ montantTotal                 │
│ statut (Brouillon, Émis, etc)│
│ creePar_id (FK)              │
│ dateModif                    │
│ dateExpectedLivraison        │
└──────────────────────────────┘

┌──────────────────────────────┐
│    BON_DE_LIVRAISON          │
├──────────────────────────────┤
│ id (PK)                      │
│ numero (UNIQUE ou Ref Fourni)│
│ dateReception                │
│ commande_id (FK)             │
│ fournisseur_id (FK)          │
│ quantiteRecue                │
│ quantiteConforme             │
│ quantiteDefectueuse          │
│ observations                 │
│ documentScanne_id (FK)       │
│ statut (Reception partielle, │
│         Complete)            │
│ receptionnePar_id (FK)       │
│ dateModif                    │
└──────────────────────────────┘

┌──────────────────────────────┐
│         FACTURE              │
├──────────────────────────────┤
│ id (PK)                      │
│ numeroFacture (UNIQUE)       │
│ dateFacture                  │
│ fournisseur_id (FK)          │
│ bonLivraison_id (FK)         │
│ montantHT                    │
│ montantTVA                   │
│ montantTTC                   │
│ statut (Enregistrée, En att.)│
│ documentScanne_id (FK)       │
│ enregistreePar_id (FK)       │
│ dateModif                    │
└──────────────────────────────┘

┌──────────────────────────────┐
│       RÈGLEMENT              │
├──────────────────────────────┤
│ id (PK)                      │
│ facture_id (FK)              │
│ modePaiement (Chèque, VT, etc)
│ datePaiement                 │
│ montantPaye                  │
│ statut (En cours, Validé)   │
│ justificatif_id (FK)         │
│ saisieePar_id (FK)           │
│ dateModif                    │
│ factureSolde (boolean)       │
└──────────────────────────────┘

┌──────────────────────────────┐
│    DOCUMENTS_ARCHIVES        │
├──────────────────────────────┤
│ id (PK)                      │
│ cheminFichier                │
│ nomOriginal                  │
│ typeMime                     │
│ taille                       │
│ hash (SHA256)                │
│ dateUpload                   │
│ uploadePar_id (FK)           │
└──────────────────────────────┘

┌──────────────────────────────┐
│      AUDIT_LOG               │
├──────────────────────────────┤
│ id (PK)                      │
│ utilisateur_id (FK)          │
│ action (CREATE, UPDATE, etc) │
│ entite (Devis, BC, Facture)  │
│ entiteId                     │
│ anciennesValeurs (JSON)      │
│ nouvellesValeurs (JSON)      │
│ dateAction                   │
│ adresseIP                    │
└──────────────────────────────┘

┌──────────────────────────────┐
│  PARAMETRES_SYSTEM           │
├──────────────────────────────┤
│ id (PK)                      │
│ cle (UNIQUE)                 │
│ valeur                       │
│ typeValeur (string, int, bool)
│ modifiePar_id (FK)           │
│ dateModif                    │
└──────────────────────────────┘
```

### Tables Détaillées

#### **Utilisateurs**
```sql
CREATE TABLE Utilisateurs (
    id INT PRIMARY KEY IDENTITY(1,1),
    login NVARCHAR(50) NOT NULL UNIQUE,
    email NVARCHAR(100) NOT NULL,
    motDePasse NVARCHAR(255) NOT NULL, -- Hash bcrypt
    role_id INT NOT NULL FOREIGN KEY REFERENCES Roles(id),
    actif BIT NOT NULL DEFAULT 1,
    dateCreation DATETIME DEFAULT GETUTCDATE(),
    dateModif DATETIME DEFAULT GETUTCDATE(),
    dateLastLogin DATETIME NULL
);
```

#### **Rôles & Permissions**
```sql
CREATE TABLE Roles (
    id INT PRIMARY KEY IDENTITY(1,1),
    code NVARCHAR(50) NOT NULL UNIQUE, -- 'MAGASINIER', 'ACHETEUR', 'COMPTABLE', 'ADMIN'
    label NVARCHAR(100),
    description NVARCHAR(500),
    permissions NVARCHAR(MAX) -- JSON format for flexibility
);

-- Exemples de permissions (JSON):
-- MAGASINIER: ['CREATE_BESOIN', 'READ_BESOIN', 'CREATE_BL', 'READ_BL']
-- ACHETEUR: ['READ_BESOIN', 'CREATE_DEVIS', 'READ_DEVIS', 'CREATE_BC', 'READ_BC']
-- COMPTABLE: ['READ_BL', 'CREATE_FACTURE', 'READ_FACTURE', 'CREATE_REGLEMENT']
-- ADMIN: ['*'] -- Toutes les permissions
```

#### **Devis**
```sql
CREATE TABLE Devis (
    id INT PRIMARY KEY IDENTITY(1,1),
    numero NVARCHAR(50) NOT NULL UNIQUE, -- Format: DEV-YYYY-####
    dateCreation DATETIME DEFAULT GETUTCDATE(),
    fournisseur_id INT NOT NULL FOREIGN KEY REFERENCES Fournisseurs(id),
    article_id INT NOT NULL FOREIGN KEY REFERENCES Articles(id),
    designation NVARCHAR(500),
    quantite DECIMAL(18,2),
    unite NVARCHAR(20),
    prixUnitaireHT DECIMAL(18,2),
    prixUnitaireTTC DECIMAL(18,2),
    montantTotalTTC DECIMAL(18,2),
    statut NVARCHAR(50) DEFAULT 'En attente', -- 'En attente', 'Validé', 'Cloturé'
    creePar_id INT FOREIGN KEY REFERENCES Utilisateurs(id),
    dateModif DATETIME DEFAULT GETUTCDATE(),
    
    INDEX idx_numero (numero),
    INDEX idx_fournisseur (fournisseur_id),
    INDEX idx_statut (statut)
);
```

#### **Bon de Commande**
```sql
CREATE TABLE BonsDeCommande (
    id INT PRIMARY KEY IDENTITY(1,1),
    numero NVARCHAR(50) NOT NULL UNIQUE, -- Format: BC-YYYY-####
    dateEmission DATETIME DEFAULT GETUTCDATE(),
    fournisseur_id INT NOT NULL FOREIGN KEY REFERENCES Fournisseurs(id),
    devis_id INT FOREIGN KEY REFERENCES Devis(id),
    article_id INT NOT NULL FOREIGN KEY REFERENCES Articles(id),
    designation NVARCHAR(500),
    quantite DECIMAL(18,2),
    unite NVARCHAR(20),
    prixUnitaireHT DECIMAL(18,2),
    prixUnitaireTTC DECIMAL(18,2),
    montantTotal DECIMAL(18,2),
    statut NVARCHAR(50) DEFAULT 'Brouillon',
    -- Statuts: Brouillon, Émis, Partiellement livré, Livré, Cloturé
    dateExpectedLivraison DATETIME,
    creePar_id INT FOREIGN KEY REFERENCES Utilisateurs(id),
    dateModif DATETIME DEFAULT GETUTCDATE(),
    observations NVARCHAR(MAX),
    
    INDEX idx_numero (numero),
    INDEX idx_fournisseur (fournisseur_id),
    INDEX idx_statut (statut),
    INDEX idx_devis (devis_id)
);
```

#### **Bon de Livraison**
```sql
CREATE TABLE BonsLivraison (
    id INT PRIMARY KEY IDENTITY(1,1),
    numero NVARCHAR(50) NOT NULL UNIQUE,
    dateReception DATETIME DEFAULT GETUTCDATE(),
    commande_id INT NOT NULL FOREIGN KEY REFERENCES BonsDeCommande(id),
    fournisseur_id INT NOT NULL FOREIGN KEY REFERENCES Fournisseurs(id),
    quantiteRecue DECIMAL(18,2),
    quantiteConforme DECIMAL(18,2),
    quantiteDefectueuse DECIMAL(18,2),
    observations NVARCHAR(MAX),
    documentScanne_id INT FOREIGN KEY REFERENCES DocumentsArchives(id),
    statut NVARCHAR(50) DEFAULT 'En attente',
    -- Statuts: En attente, Réception partielle, Réception complète
    receptionnePar_id INT FOREIGN KEY REFERENCES Utilisateurs(id),
    dateModif DATETIME DEFAULT GETUTCDATE(),
    
    INDEX idx_numero (numero),
    INDEX idx_commande (commande_id),
    INDEX idx_statut (statut)
);
```

#### **Facture**
```sql
CREATE TABLE Factures (
    id INT PRIMARY KEY IDENTITY(1,1),
    numeroFacture NVARCHAR(50) NOT NULL UNIQUE,
    dateFacture DATETIME,
    fournisseur_id INT NOT NULL FOREIGN KEY REFERENCES Fournisseurs(id),
    bonLivraison_id INT FOREIGN KEY REFERENCES BonsLivraison(id),
    montantHT DECIMAL(18,2),
    montantTVA DECIMAL(18,2),
    montantTTC DECIMAL(18,2),
    statut NVARCHAR(50) DEFAULT 'Enregistrée',
    -- Statuts: Enregistrée, En attente paiement, Partiellement régléee, Soldée
    documentScanne_id INT FOREIGN KEY REFERENCES DocumentsArchives(id),
    enregistreePar_id INT FOREIGN KEY REFERENCES Utilisateurs(id),
    dateModif DATETIME DEFAULT GETUTCDATE(),
    dateEcheance DATETIME,
    
    INDEX idx_numero (numeroFacture),
    INDEX idx_fournisseur (fournisseur_id),
    INDEX idx_statut (statut),
    INDEX idx_dateFacture (dateFacture)
);
```

#### **Règlement**
```sql
CREATE TABLE Reglements (
    id INT PRIMARY KEY IDENTITY(1,1),
    facture_id INT NOT NULL FOREIGN KEY REFERENCES Factures(id),
    modePaiement NVARCHAR(50), -- Chèque, Virement, Lettre change, Espèces
    datePaiement DATETIME NOT NULL,
    montantPaye DECIMAL(18,2),
    statut NVARCHAR(50) DEFAULT 'En cours', -- En cours, Validé, Refusé
    justificatif_id INT FOREIGN KEY REFERENCES DocumentsArchives(id),
    saisieePar_id INT FOREIGN KEY REFERENCES Utilisateurs(id),
    dateModif DATETIME DEFAULT GETUTCDATE(),
    numeroReference NVARCHAR(100), -- N° chèque, ref virement, etc
    
    INDEX idx_facture (facture_id),
    INDEX idx_datePaiement (datePaiement),
    INDEX idx_statut (statut)
);
```

#### **Audit Log**
```sql
CREATE TABLE AuditLog (
    id INT PRIMARY KEY IDENTITY(1,1),
    utilisateur_id INT FOREIGN KEY REFERENCES Utilisateurs(id),
    action NVARCHAR(50), -- CREATE, READ, UPDATE, DELETE, EXPORT
    entite NVARCHAR(100), -- Devis, BonCommande, Facture, etc
    entiteId INT,
    anciennesValeurs NVARCHAR(MAX), -- JSON serialized
    nouvellesValeurs NVARCHAR(MAX), -- JSON serialized
    dateAction DATETIME DEFAULT GETUTCDATE(),
    adresseIP NVARCHAR(50),
    
    INDEX idx_utilisateur (utilisateur_id),
    INDEX idx_dateAction (dateAction),
    INDEX idx_entite (entite),
    INDEX idx_action (action)
);
```

---

## Décomposition des Modules

### 1. **Module Authentification & Gestion Utilisateurs**

#### Fonctionnalités Clés
- Login / Logout avec identifiant + mot de passe
- Gestion des sessions (durée max, timeout inactivité)
- Changement de mot de passe
- Récupération de mot de passe (question secrète)
- Liste des utilisateurs (Admin only)
- Création/Modification/Suppression utilisateurs (Admin only)
- Attribution des rôles et permissions

#### Points Techniques
- Hashage bcrypt des mots de passe
- Token JWT pour sessions (optionnel)
- Session timeout après 30 min d'inactivité
- Blocage après 5 tentatives échouées

#### Estimé
- Développement : 5 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 2. **Module Gestion des Fournisseurs**

#### Fonctionnalités Clés
- CRUD fournisseurs (Création, lecture, modification, suppression)
- Recherche et filtrage par nom, localité, contact
- Historique des prix par fournisseur et article
- Évaluation fournisseurs (délais, qualité, prix)
- Import/Export liste fournisseurs (Excel)
- Statut actif/inactif

#### Points Techniques
- Validation coordonnées (email, téléphone)
- Triggers pour archivage avant suppression
- Historique prix versionnée (date début/fin)

#### Estimé
- Développement : 4 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 3. **Module Articles & Stock**

#### Fonctionnalités Clés
- CRUD articles (designation, unité, famille)
- Gestion du stock actuel et stock minimum
- Alertes de rupture de stock
- Consultation historique prix par article
- Catégorisation des articles
- Import/Export catalogue articles

#### Points Techniques
- Vue de stock consolidée (somme inventaire + bons en cours)
- Calcul automatique des besoins (stock minimum - stock actuel)
- Trigger mise à jour stock après livraison

#### Estimé
- Développement : 4 jours
- Tests : 2 jours

---

### 4. **Module Devis**

#### Fonctionnalités Clés
- Création de devis (auto-numérotation : DEV-YYYY-####)
- Sélection fournisseur, article, quantité, prix
- Consultation historique prix fournisseur
- Modification devis (tant que non validé)
- Génération PDF devis
- Suppression devis (si pas de BC associée)
- Statuts : En attente → Validé → Cloturé

#### Validation Métier
- Devis validé par création d'un BC
- Prix issus de l'historique ou manuel
- Quantité > 0

#### Points Techniques
- Auto-génération numéro unique (séquence BD)
- Calcul montant total (Qté × Prix TTC)
- Génération PDF avec iTextSharp / PdfSharp
- Audit de chaque modification

#### Estimé
- Développement : 6 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 5. **Module Bon de Commande**

#### Fonctionnalités Clés
- Création BC à partir d'un devis validé
- Modification des conditions (si pas encore émis)
- Saisie manuelle sans devis (cas dérogatoire)
- Émission BC (changement statut)
- Génération PDF pour impression/envoi
- Suivi de l'avancement jusqu'à livraison
- Statuts : Brouillon → Émis → Partiellement livré → Livré → Cloturé

#### Cas de Validation
- BC émis valide et clôture le Devis
- Impossible de modifier BC une fois Émis
- Dates : dateEmission < dateExpectedLivraison

#### Points Techniques
- Lien obligatoire vers Devis (sauf création manuelle)
- Génération numéro unique (BC-YYYY-####)
- PDF exportable pour envoi au fournisseur
- Notification Magasinier quand BC émis

#### Estimé
- Développement : 6 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 6. **Module Bon de Livraison**

#### Fonctionnalités Clés
- Saisie BL lors réception marchandises
- Vérification conformité Qté commandée vs reçue
- Signalement défauts/non-conformités
- Téléchargement document physique scané
- Mise à jour stock automatique
- Statuts : En attente → Réception partielle → Réception complète

#### Validation Métier
- BL validé clôture/partiellement clôture le BC
- Stock augmenté du montant conforme reçu
- Notification Comptable quand BL complet (facture attendue)

#### Points Techniques
- Lien obligatoire vers BC existant
- Gestion quantités partielles (reçues ≤ commandées)
- Upload documents scannés (PDF, PNG, JPG)
- Recalcul stock automatique

#### Estimé
- Développement : 5 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 7. **Module Facture**

#### Fonctionnalités Clés
- Enregistrement factures reçues
- Vérification conformité (montant, articles, dates)
- Lien obligatoire vers BL
- Upload document facture original
- Calcul TVA (calcul ou lecture directe)
- Suivi état paiement (Enregistrée → En attente → Partiellement réglée → Soldée)
- Génération rappels écheance

#### Validation Métier
- Facture validée clôture le BL
- Montant facture ≤ Montant BC
- Date facture cohérente

#### Points Techniques
- Lecture données facture (manuel ou OCR optionnel)
- Archivage de la facture originale (PDF)
- Cálcul TVA (fixe ou variable)
- Notification Comptable pour paiement

#### Estimé
- Développement : 5 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 8. **Module Règlement**

#### Fonctionnalités Clés
- Saisie paiements (Chèque, Virement, Lettre change, etc)
- Montants partiels possibles (plusieurs règlements pour 1 facture)
- Upload justificatif (scan chèque, confirmation virement)
- Réconciliation automatique avec factures
- Statuts paiement : En cours → Validé → Refusé
- État des paiements (En attente, Partiel, Soldé)

#### Validation Métier
- Règlement validé clôture la Facture
- Somme règlements ≤ Montant facture TTC
- Facture entièrement payée → Soldée

#### Points Techniques
- Support multiples modes de paiement
- Archivage justificatif numérisé
- Numérotation référence (n° chèque, ref virement)
- Export rapports de trésorerie

#### Estimé
- Développement : 5 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 9. **Module Tableaux de Bord & Rapports**

#### Indicateurs Clés
- Commandes en cours (nombre, montant)
- Livraisons attendues vs retardées
- Factures à régler (montant en attente)
- Fournisseurs actifs / statut
- Évolution prix fournisseur (courbes)
- Taux de conformité livraisons
- Retards moyens

#### Rapports Exportables
- Commandes par fournisseur (période)
- Factures par statut (en attente, soldées)
- Historique prix par article
- Analyse délais fournisseurs
- Bilan mensuel/trimestriel

#### Points Techniques
- Graphiques avec Chart.Js / OxyPlot
- Filtres dynamiques (date, fournisseur, statut)
- Export Excel/PDF
- Cache résultats pour performance

#### Estimé
- Développement : 7 jours
- Tests : 2 jours
- Intégration : 1 jour

---

### 10. **Module Paramètres Système & Administration**

#### Fonctionnalités
- Configuration générales (TVA, délais par défaut, etc)
- Gestion des sauvegardes BD
- Logs d'audit consultables
- Gestion des documents archivés
- Nettoyage des anciennes données (archivage)
- Restauration à partir d'une sauvegarde

#### Accès
- Admin uniquement

#### Points Techniques
- Stockage paramètres en BD (table PARAMETRES_SYSTEM)
- Interface administrateur sécurisée
- Logs d'audit consultables et exportables

#### Estimé
- Développement : 4 jours
- Tests : 1 jour

---

## Workflow et Cas d'Usage

### Workflow Principal

```
┌─────────────────────────────────────────┐
│  ÉTAPE 1: ANALYSE DES BESOINS          │
│  Acteur: Magasinier                     │
│  Doc: Besoin / Stock                    │
└─────────────────────────────────────────┘
  • Consulter stock actuel
  • Identifier articles sous stock minimum
  • Créer/Saisir liste de besoins

                ↓

┌─────────────────────────────────────────┐
│  ÉTAPE 2: PRÉPARATION DEVIS             │
│  Acteur: Responsable des Achats         │
│  Doc: Devis                             │
└─────────────────────────────────────────┘
  • Recevoir liste besoins
  • Consulter historique prix fournisseurs
  • Créer devis multi-fournisseur
  • Comparer devis

                ↓

┌─────────────────────────────────────────┐
│  ÉTAPE 3: BON DE COMMANDE               │
│  Acteur: Responsable des Achats         │
│  Doc: Bon de Commande                   │
└─────────────────────────────────────────┘
  • Sélectionner fournisseur
  • Émettre BC
  • Envoyer BC au fournisseur

                ↓

┌─────────────────────────────────────────┐
│  ÉTAPE 4: BON DE LIVRAISON              │
│  Acteur: Magasinier                     │
│  Doc: Bon de Livraison                  │
└─────────────────────────────────────────┘
  • Recevoir marchandises
  • Vérifier conformité (Qté, qualité)
  • Saisir BL + scan
  • Valider → Stock augmente

                ↓

┌─────────────────────────────────────────┐
│  ÉTAPE 5: FACTURE                       │
│  Acteur: Comptable                      │
│  Doc: Facture                           │
└─────────────────────────────────────────┘
  • Recevoir facture fournisseur
  • Vérifier conformité (montant, items)
  • Enregistrer en BD
  • Planifier paiement

                ↓

┌─────────────────────────────────────────┐
│  ÉTAPE 6: RÈGLEMENT                     │
│  Acteur: Comptable                      │
│  Doc: Règlement                         │
└─────────────────────────────────────────┘
  • Effectuer paiement
  • Enregistrer mode + montant
  • Uploader justificatif
  • Clôturer facture

                ↓

        [CYCLE TERMINÉ]
```

### Cas d'Usage Détaillés

#### UC-01 : Créer un Devis

| Aspect | Détail |
|--------|--------|
| **Acteur principal** | Responsable des Achats |
| **Préconditions** | Besoin identifié, fournisseur sélectionné |
| **Flux principal** | 1. Accéder à formulaire Devis<br>2. Saisir date, fournisseur, article<br>3. Saisir quantité, prix unitaire<br>4. Système calcule montant total<br>5. Enregistrer devis |
| **Post-condition** | Devis créé avec numéro unique, statut "En attente" |
| **Exceptions** | - Fournisseur invalide<br>- Prix négatif<br>- Quantité = 0 |
| **Estimation** | 5 minutes/devis utilisateur |

#### UC-02 : Émettre un Bon de Commande

| Aspect | Détail |
|--------|--------|
| **Acteur principal** | Responsable des Achats |
| **Préconditions** | Devis existant et validé |
| **Flux principal** | 1. Consulter dévis<br>2. Créer BC à partir du devis<br>3. Vérifier conditions<br>4. Valider BC → Statut "Émis"<br>5. Générer PDF<br>6. Envoyer fournisseur |
| **Post-condition** | BC créée avec numéro, Devis cloturé |
| **Exceptions** | - Dévis déjà cloturé<br>- Prix modifié sans raison |
| **Estimation** | 10 minutes/BC |

#### UC-03 : Réceptionner une Livraison

| Aspect | Détail |
|--------|--------|
| **Acteur principal** | Magasinier |
| **Préconditions** | BC émise existante |
| **Flux principal** | 1. Accéder module BL<br>2. Sélectionner BC<br>3. Saisir quantité reçue<br>4. Signaler défauts (si nécessaire)<br>5. Uploader scan BL fournisseur<br>6. Valider → Stock mis à jour |
| **Post-condition** | BL créée, stock augmente, BC partiellement/complètement livrée |
| **Exceptions** | - Qté reçue > Qté commandée<br>- Document scan invalide |
| **Estimation** | 15 minutes/livraison |

#### UC-04 : Enregistrer une Facture

| Aspect | Détail |
|--------|--------|
| **Acteur principal** | Comptable |
| **Préconditions** | BL complète réceptionnée |
| **Flux principal** | 1. Accéder module Facture<br>2. Sélectionner BL associée<br>3. Saisir data facture (n°, date, montant TTC)<br>4. Uploader PDF facture<br>5. Valider → BL cloturée, Facture en attente paiement |
| **Post-condition** | Facture enregistrée, comptabilité avertie |
| **Exceptions** | - Montant facture ≠ Montant BC<br>- Date facture invalide |
| **Estimation** | 10 minutes/facture |

#### UC-05 : Effectuer un Paiement

| Aspect | Détail |
|--------|--------|
| **Acteur principal** | Comptable |
| **Préconditions** | Facture enregistrée |
| **Flux principal** | 1. Accéder module Règlement<br>2. Sélectionner facture<br>3. Saisir mode paiement, date, montant<br>4. Uploader justificatif<br>5. Valider → Facture soldée (si montant = TTC) |
| **Post-condition** | Règlement enregistré, facture status update |
| **Exceptions** | - Montant paiement > Montant facture<br>- Justificatif invalide |
| **Estimation** | 5 minutes/règlement |

---

## Plan de Développement par Phase

### **PHASE 0 : INITIALISATION (1 semaine)**

#### Jalons
- ✅ Environnement de développement configuré
- ✅ Dépôt Git créé et structuré
- ✅ Modèle de base de données validé
- ✅ Outils et frameworks ajoutés au projet

#### Tâches
1. **Mise en place projet C# / WPF**
   - Créer solution Visual Studio (.NET 6)
   - Structure des dossiers : Presentation, Services, Models, Data, Utils
   - Ajouter NuGet packages : EF Core, Serilog, FluentValidation, Moq

2. **Configuration Base de Données**
   - Installation SQL Server Express 2022
   - Création BD "GesAchats"
   - Scripts création tables (voir schéma ER ci-dessus)
   - Création triggers & procédures stockées

3. **Dépôt Git & Documentation**
   - Initialiser repository (GitHub/GitLab)
   - Créer .gitignore, README.md
   - Documenter conventions code (C#, nommage, commentaires)

4. **Configuration CI/CD**
   - Pipeline build automatique
   - Exécution tests unitaires
   - Code coverage reporting

#### Résultats
- ✅ Solution VS compilable et fonctionnelle
- ✅ BD vierge prête avec schéma correct
- ✅ Pipeline CI/CD actif

---

### **PHASE 1 : FONDATIONS (2 semaines)**

#### Jalons
- ✅ Authentification opérationnelle
- ✅ Interface principale WPF + navigation
- ✅ Gestion des rôles & permissions
- ✅ Architeture MVVM en place

#### Tâches
1. **Module Authentification** (5j)
   - UC: Login / Logout
   - Service authentification (HashCode bcrypt, JWT optionnel)
   - Tests unitaires (5 cas: login OK, mauvais pwd, compte désactivé, etc)

2. **Interface Principal & Navigation** (5j)
   - MainWindow.xaml (layout sidebar + contenu)
   - ViewModels pour chaque module
   - Navigation entre pages (MVVM)
   - Styles globaux (colors, buttons, inputs)

3. **Gestion des Rôles & Permissions** (4j)
   - Service autorisation (RBAC)
   - Tests : chaque rôle accède uniquement ses modules
   - Cache permissions en mémoire (refresh à login)

4. **Tests & Intégration** (2j)
   - Tests unitaires authentification
   - Tests intégration DB
   - Tests UI basiques

#### Résultats
- ✅ Application launch, login fonctionnel
- ✅ Navigation entre modules opérationnelle
- ✅ Permissions appliquées par rôle

---

### **PHASE 2 : MODULES DE BASE (3 semaines)**

#### Jalons
- ✅ Gestion Fournisseurs + Articles
- ✅ Module Devis opérationnel
- ✅ Rapports basiques

#### Tâches
1. **Module Fournisseurs** (4j)
   - CRUD fournisseurs (create, read, update, delete)
   - Recherche & filtrage
   - Historique prix par fournisseur-article
   - Import/Export Excel
   - Tests : 10 cas (validation email, doublons, etc)

2. **Module Articles & Stock** (4j)
   - CRUD articles (designation, unité, famille)
   - Gestion stock (actuel, minimum)
   - Alerte rupture stock
   - Calcul automatique besoins (stock_min - stock_actuel)
   - Tests : 8 cas

3. **Module Devis** (5j)
   - Création devis (auto-numérotation)
   - Sélection fournisseur + historique prix
   - Modification avant validation
   - PDF generation
   - Tests : 12 cas
   - Intégration : audit log à chaque action

4. **Tableaux de Bord Basique** (3j)
   - KPI : commandes en cours, livraisons attendues, factures en attente
   - Graphiques simples (OxyPlot)
   - Filtres par date, fournisseur

#### Résultats
- ✅ 3 modules core fonctionnels
- ✅ ~800 lignes tests
- ✅ DB peuplée de données test

---

### **PHASE 3 : WORKFLOW PRINCIPAL (3 semaines)**

#### Jalons
- ✅ Bon de Commande opérationnel
- ✅ Bon de Livraison intégré
- ✅ Workflow complet : Devis → BC → BL

#### Tâches
1. **Module Bon de Commande** (5j)
   - Création BC (à partir devis ou manuelle)
   - Validation & émission
   - Modification conditionnelle
   - PDF exportable
   - Tests : 10 cas
   - Intégration : dévis cloturé automatiquement

2. **Module Bon de Livraison** (5j)
   - Saisie BL (réception marchandises)
   - Vérification conformité (Qté, qualité)
   - Upload document scné
   - Mise à jour stock automatique
   - Tests : 12 cas
   - Intégration : stock augmente, BC partiellement livrée

3. **Notification Utilisateurs** (2j)
   - Notifications internes (BC émis, BL complet attendu)
   - Email (optionnel, ultérieur)
   - Logs persistants dans BD

4. **Tests d'Intégration Workflow** (2j)
   - Scénario end-to-end : Devis → BC → BL (Qté partielle + complète)
   - Vérifications automatique des statuts
   - Audit trail complet

#### Résultats
- ✅ Workflow 1-4 complètement opérationnel
- ✅ Stock synchronisé avec livraisons
- ✅ ~1200 lignes tests

---

### **PHASE 4 : FINANCE & CLÔTURE (3 semaines)**

#### Jalons
- ✅ Module Facture opérationnel
- ✅ Module Règlement complètement intégré
- ✅ Rapports finance avancés

#### Tâches
1. **Module Facture** (5j)
   - Enregistrement factures
   - Vérification conformité (montant, items)
   - Upload document original
   - Lien obligatoire BL
   - Tests : 10 cas
   - Intégration : BL cloturée

2. **Module Règlement** (5j)
   - Saisie paiements (Chèque, Virement, LC, Espèces)
   - Montants partiels & réconciliation
   - Upload justificatif
   - Marquage facture soldée
   - Tests : 12 cas
   - Intégration : facture cloturée

3. **Rapports Finance** (3j)
   - Factures par statut (En attente, Partiellement réglée, Soldée)
   - Analyse délais paiement
   - Prévisions trésorerie
   - Export Excel mensuel

4. **Tests Workflow Complet** (2j)
   - Scénario end-to-end 1-6 (Devis → Règlement)
   - Paiements multiples par facture
   - Réconciliation automatique

#### Résultats
- ✅ Workflow 100% opérationnel
- ✅ Cycle achat complet fonctionnel
- ✅ ~1500 lignes tests

---

### **PHASE 5 : SÉCURITÉ, PERFORMANCE & POLISSAGE (2 semaines)**

#### Jalons
- ✅ Audit trail complet & traçabilité
- ✅ Performance optimisée (10-12 users)
- ✅ Sauvegardes automatiques
- ✅ Application stable & prête déploiement

#### Tâches
1. **Audit & Traçabilité** (3j)
   - Enregistrement toutes actions (CREATE, UPDATE, DELETE, READ)
   - Historique des modifications (avant/après)
   - Horodatage précis
   - Rapports d'audit consultables
   - Tests : 6 cas

2. **Performance & Optimisation** (3j)
   - Tests charge (10-12 utilisateurs simultanés)
   - Indexation BD (colonnes fréquemment consultées)
   - Caching (permissions, fournisseurs)
   - Lazy loading collections (EF Core)

3. **Sauvegardes & Restauration** (2j)
   - Backups automatiques quotidiens (BD complète)
   - Historique 30 jours de sauvegardes
   - Script restauration automatisé
   - Tests restauration (2 cas)

4. **UI Polish & Ergonomie** (2j)
   - Vérification messages d'erreur explicites
   - Confirmations avant actions dangereuses
   - Tooltips sur champs complexes
   - Dark mode optionnel

5. **Tests Final** (2j)
   - Régressions (tous modules)
   - Tests utilisateur (UAT)
   - Performance test 12 users simultanés

#### Résultats
- ✅ Application audit-proof
- ✅ Performance validée
- ✅ Backup strategy implémentée

---

### **PHASE 6 : DÉPLOIEMENT & FORMATION (1 semaine)**

#### Jalons
- ✅ Application déployée en production
- ✅ Utilisateurs formés
- ✅ Support en place

#### Tâches
1. **Préparation Installation** (2j)
   - Script installer (SQL Server + Application)
   - Guide d'installation step-by-step
   - Configuration BD (users, connexion)
   - Données de démarrage (fournisseurs, articles)

2. **Manuels Utilisateurs** (2j)
   - Guide par rôle (Magasinier, Acheteur, Comptable, Admin)
   - Vidéos tutoriels (opérations clés)
   - FAQ

3. **Formation Utilisateurs** (2j)
   - Session live (1h par groupe)
   - Q&A en directe
   - Support sur place premiers jours

4. **Go-Live** (1j)
   - Installation postes utilisateurs
   - Vérification connectivité BD
   - Premiers pas guidés
   - Support d'urgence disponible

#### Résultats
- ✅ Application opérationnelle en production
- ✅ Tous utilisateurs formés
- ✅ Support documenté

---

## Chronologie Globale

```
Phase 0 : Initialisation              1 sem  |█|
Phase 1 : Fondations                  2 sem  |██|
Phase 2 : Modules de base             3 sem  |███|
Phase 3 : Workflow principal          3 sem  |███|
Phase 4 : Finance & Clôture           3 sem  |███|
Phase 5 : Sécurité & Perf             2 sem  |██|
Phase 6 : Déploiement & Formation     1 sem  |█|
                                      ─────────
                                     15 SEMAINES (3.5 MOIS)
```

### Estimation Ressources

| Rôle | Équipe | Sem/Phase | Total | Notes |
|------|--------|-----------|-------|-------|
| **Architecte** | 1 | 0.5 | 2 | Semaines 0-1 |
| **Lead Dev (Backend)** | 1 | 15 | 15 | Toutes phases |
| **Lead Dev (WPF)** | 1 | 15 | 15 | Toutes phases |
| **Junior Dev** | 1 | 15 | 15 | Phases 2-5 |
| **QA/Tester** | 1 | 12 | 12 | Phases 1-6 |
| **BA/PM** | 0.5 | 15 | 7.5 | Toutes phases |

**Totaux Équipe:**
- **4.5 FTE** (Full-Time Equivalent)
- **~66 hommes-jours** de développement
- **~47 hommes-jours** de tests

---

## Stratégie de Test et QA

### Niveaux de Test

#### 1. Tests Unitaires
- **Framework** : xUnit
- **Mock** : Moq
- **Coverage cible** : 80%+
- **Exécution** : Automatique via CI/CD

##### Cas d'exemple :
```csharp
[Fact]
public void CreateDevis_ValidInput_ReturnsDevisId()
{
    // Arrange
    var devisService = new DevisService(_mockRepo, _logger);
    var dto = new CreateDevisDto { /* ... */ };
    
    // Act
    var result = devisService.Create(dto);
    
    // Assert
    Assert.NotNull(result);
    Assert.True(result.Id > 0);
}

[Fact]
public void CreateDevis_InvalidQuantity_ThrowsException()
{
    // Arrange
    var devisService = new DevisService(_mockRepo, _logger);
    var dto = new CreateDevisDto { Quantite = -1 };
    
    // Act & Assert
    Assert.Throws<ValidationException>(() => devisService.Create(dto));
}
```

#### 2. Tests d'Intégration
- **Cibles** : Services + BD
- **BD de test** : LocalDB avec données de test
- **Exécution** : Avant chaque merge

##### Cas d'exemple :
```csharp
[Fact]
public void Workflow_DavisToBCToBlToFacture_StatusesUpdated()
{
    // Arrange
    using var context = new GesAchatsDbContext(_testDb);
    var devis = CreateTestDevis(context);
    var bc = CreateTestBC(context, devis.Id);
    var bl = CreateTestBL(context, bc.Id);
    var facture = CreateTestFacture(context, bl.Id);
    
    // Act
    context.SaveChanges();
    var updatedDevis = context.Devis.Find(devis.Id);
    var updatedBC = context.BonsDeCommande.Find(bc.Id);
    
    // Assert
    Assert.Equal("Cloturé", updatedDevis.Statut);
    Assert.Equal("Livré", updatedBC.Statut);
}
```

#### 3. Tests Fonctionnels (Black Box)
- **Outils** : Selenium WPF Driver (optionnel) ou tests manuels
- **Cas** : UC complets (Devis → BC → BL → Facture → Règlement)
- **Couverture** : Tous modules, tous rôles

#### 4. Tests de Performance
- **Outils** : JMeter (simulation utilisateurs) ou tests manuels
- **Charge** : 12 utilisateurs simultanés
- **Métrique** : Temps réponse < 2s pour opérations courantes

#### 5. Tests de Sécurité
- **Authentification** : Login/logout, timeout session
- **Autorisation** : Accès interdit aux données hors rôle
- **Injection SQL** : Tests paramètres DB
- **OWASP Top 10** : Vérification standards

### Plan de Test Détaillé

| Module | Test Unitaire | Test Intégration | Test Fonctionnel | Couverture |
|--------|---|---|---|---|
| **Authentification** | 8 cas | 3 scénarios | 4 UC | 95% |
| **Fournisseurs** | 6 cas | 2 scénarios | 3 UC | 85% |
| **Articles & Stock** | 7 cas | 2 scénarios | 3 UC | 85% |
| **Devis** | 12 cas | 3 scénarios | 4 UC | 90% |
| **BC** | 12 cas | 3 scénarios | 4 UC | 90% |
| **BL** | 12 cas | 3 scénarios | 4 UC | 90% |
| **Facture** | 10 cas | 2 scénarios | 3 UC | 88% |
| **Règlement** | 10 cas | 2 scénarios | 3 UC | 88% |
| **Audit & Traçabilité** | 8 cas | 2 scénarios | 2 UC | 85% |
| **Rapports** | 6 cas | 2 scénarios | 3 UC | 80% |
| **Total** | **91 cas** | **24 scénarios** | **33 UC** | **~88%** |

### Critères d'Acceptation QA

| Critère | Condition |
|---------|-----------|
| **Tests Unitaires** | Coverage ≥ 80%, Tous tests passent |
| **Tests Intégration** | Tous scénarios passent, 0 données corrompues |
| **Tests Fonctionnels** | Tous UC exécutés, 0 bugs critiques |
| **Performance** | Temps réponse < 2s (12 users), DB queries optimisées |
| **Sécurité** | Audit trail complet, pas d'accès non-autorisé, password hashing ok |
| **Compatibilité** | Windows 7-11, .NET 6, SQL Server 2019/2022 |

---

## Déploiement et Installation

### Prérequis Système

| Composant | Minimal | Recommandé |
|-----------|---------|-----------|
| **OS** | Windows 7 SP1 | Windows 10/11 |
| **.NET Runtime** | .NET 6.0 Desktop | .NET 6.0 LTS |
| **RAM** | 4 GB | 8 GB |
| **Disque** | 2 GB | 5 GB |
| **SQL Server** | Express 2019 | Standard 2022 |
| **Connexion Réseau** | LAN 100 Mbps | LAN 1 Gbps |

### Architecture Déploiement

```
┌──────────────────────────────────────────────────────────┐
│                   SERVEUR (Réseau Local)                 │
├──────────────────────────────────────────────────────────┤
│  SQL Server 2019/2022 Express                            │
│  - Base de données "GesAchats"                           │
│  - Users SQL (admin, lecteur, etc)                       │
│  - Backups quotidiens en local                           │
└──────────────────────────────────────────────────────────┘
                          ↑ LAN (TCP/IP)
                          ↓
┌──────────────────────────────────────────────────────────┐
│              POSTES UTILISATEURS (Client)                │
├──────────────────────────────────────────────────────────┤
│  Windows 10/11                                           │
│  .NET 6.0 Runtime                                        │
│  GesAchats.exe (Application WPF)                         │
│  Répertoire local : C:\Program Files\GesAchats\         │
│  Données : connectées au serveur SQL                     │
└──────────────────────────────────────────────────────────┘
                       (10-12 postes)
```

### Package d'Installation

```
GesAchats-2.0-Installer.zip
├── bin/
│   ├── GesAchats.exe (application principale)
│   ├── GesAchats.dll (assemblies)
│   ├── Dependencies/ (NuGet packages)
│   └── ...
├── Database/
│   ├── Create-Schema.sql (création tables)
│   ├── Create-Triggers.sql
│   ├── Insert-InitialData.sql (fournisseurs, articles par défaut)
│   └── Seed-Data.sql (données test)
├── Config/
│   ├── App.config (paramètres connexion BD)
│   ├── Settings.json (TVA, délais, etc)
│   └── Roles-Permissions.json
├── Documentation/
│   ├── README.md
│   ├── Guide-Installation.pdf
│   ├── Guide-Utilisateur.pdf
│   ├── Admin-Guide.pdf
│   └── FAQ.pdf
├── Tools/
│   ├── Backup-Script.bat
│   ├── Restore-Script.bat
│   └── Database-Maintenance.sql
└── Installers/
    ├── .NET-6-Runtime-Setup.exe
    └── SQL-Server-Express-2022.exe (si nécessaire)
```

### Procédure d'Installation Pas-à-Pas

#### Étape 1 : Installation Base de Données

```powershell
# 1. Installer SQL Server Express 2022 (si non présent)
# - Télécharger depuis microsoft.com
# - Paramètres: Instance name "GESACHATS", mode authentification mixte
# - Activer TCP/IP dans Configuration Manager

# 2. Créer base de données
sqlcmd -S localhost\GESACHATS -i Database\Create-Schema.sql
sqlcmd -S localhost\GESACHATS -i Database\Create-Triggers.sql
sqlcmd -S localhost\GESACHATS -i Database\Insert-InitialData.sql

# 3. Vérifier
sqlcmd -S localhost\GESACHATS -Q "SELECT COUNT(*) FROM Utilisateurs"
# Output: ------
#         1  (1 admin user créé)
```

#### Étape 2 : Installation Application

```powershell
# 1. Créer répertoire
mkdir "C:\Program Files\GesAchats"

# 2. Copier fichiers
xcopy bin\* "C:\Program Files\GesAchats\" /E /I

# 3. Configurer connexion BD
# Éditer App.config:
# <connectionString>
#   Data Source=localhost\GESACHATS;
#   Initial Catalog=GesAchats;
#   Integrated Security=false;
#   User ID=gesachats_user;
#   Password=SecurePassword123!
# </connectionString>

# 4. Créer shortcut sur Bureau
# Target: C:\Program Files\GesAchats\GesAchats.exe
```

#### Étape 3 : Configuration Utilisateurs

```sql
-- Créer utilisateur SQL Server (Windows Auth ou SQL Auth)
CREATE LOGIN [EMPRESA\User1] FROM WINDOWS;
CREATE USER [EMPRESA\User1] FOR LOGIN [EMPRESA\User1];

-- OU (SQL Auth)
CREATE LOGIN [user1] WITH PASSWORD='TempPassword123!';
CREATE USER [user1] FOR LOGIN [user1];

-- Accorder droits
ALTER ROLE db_datareader ADD MEMBER [EMPRESA\User1];
ALTER ROLE db_datawriter ADD MEMBER [EMPRESA\User1];
```

#### Étape 4 : Test Connectivité

```
Lancer GesAchats.exe
→ Écran de login
→ Saisir identifiants (admin / mot_de_passe_par_défaut)
→ Vérifier connexion BD réussie
→ Accéder dashboard
```

#### Étape 5 : Sauvegarde Initiale

```powershell
# Script sauvegarde quotidienne (Scheduled Task Windows)
# Tools\Backup-Script.bat
BACKUP DATABASE [GesAchats] 
TO DISK = 'D:\Backups\GesAchats_$(date +%Y%m%d_%H%M%S).bak'
WITH COMPRESSION;

# Planifier : Task Scheduler → Quotidien 22:00
```

### Post-Installation

- ✅ Vérifier tous 12 postes se connectent
- ✅ Importer liste fournisseurs/articles (Excel)
- ✅ Tester chaque rôle (Magasinier, Acheteur, Comptable, Admin)
- ✅ Configurer TVA, délais par défaut (Paramètres Admin)

---

## Ressources et Chronologie

### Équipe Recommandée

#### 1. Architecte Système / Lead Dev Backend (1 FTE)
- **Responsabilités** :
  - Architecture globale (3-tiers)
  - Design base de données
  - Patterns & bonnes pratiques (SOLID, DI)
  - Code review & mentoring
  - Performance & sécurité

- **Compétences** :
  - C# avancé, .NET 6+
  - EF Core, SQL Server
  - Architecture logicielle
  - 8+ ans expérience

#### 2. Lead Dev WPF (1 FTE)
- **Responsabilités** :
  - Interface utilisateur (XAML/WPF)
  - MVVM pattern & data binding
  - Styles globaux
  - Performance UI
  - Tests UI

- **Compétences** :
  - WPF, XAML, C#
  - MVVM, Data binding
  - Design UI/UX (Figma/XD)
  - 5+ ans expérience

#### 3. Développeur Junior (1 FTE, à partir Phase 2)
- **Responsabilités** :
  - Développement modules simples
  - Tests unitaires
  - Documentation code
  - Support intégration

- **Compétences** :
  - C# intermédiaire
  - Premiers contacts EF Core
  - 1-2 ans expérience

#### 4. QA / Testeur (1 FTE, à partir Phase 1)
- **Responsabilités** :
  - Tests unitaires + intégration
  - Tests fonctionnels (scripting)
  - Rapports défauts (Jira/Azure DevOps)
  - Tests charge & sécurité

- **Compétences** :
  - xUnit, Moq
  - Tests manuels
  - SQL Server (queries de vérification)
  - 3+ ans expérience

#### 5. Product Owner / Business Analyst (0.5 FTE)
- **Responsabilités** :
  - Clarification requirements
  - Validation avec utilisateurs
  - Priorités & scope management
  - Documentation fonctionnelle

- **Compétences** :
  - Gestion de projet Agile
  - Communication métier/technique
  - Documentation

### Chronologie par Ressource

```
Semaine:   1  2  3  4  5  6  7  8  9 10 11 12 13 14 15

Architecte: ████░░░░░░░░░░
Lead Dev BK:████████████████████████████████░░░░
Lead Dev WPF:████████████████████████████████░░░░
Junior Dev:    ░░░░████████████████████████████░░
QA/Tester:        ██████████████████████████████░░
PO/BA:      ██████████████████████████████████░░░░
```

**Total Investissement Humain :**
- 4.5 FTE × 15 semaines = 67.5 hommes-semaines
- ~270 hommes-jours
- Coût (en salaires moyen) : ~€54k - €72k

---

## Risques et Mitigation

### Matrice Risques

| Risque | Probabilité | Impact | Mitigation | Propriétaire |
|--------|---|---|---|---|
| **Périmètre scope creep** | Haute | Moyen | Définir périmètre strict au démarrage, contrôler changements | PO/PM |
| **Performance BD (12 users)** | Moyen | Haut | Tests charge Phase 5, indexation dès le départ | Arch |
| **Délai fournisseurs données** | Moyen | Moyen | Créer données par défaut, import Excel parallèle | BA |
| **Turnover équipe dev** | Faible | Haut | Documentation code excellence, pair programming, code review | Lead Dev |
| **Intégration WPF/Backend tardive** | Moyen | Moyen | Tests d'intégration dès Phase 1, CI/CD strict | Lead Dev WPF + BK |
| **Bugs critiques en production** | Faible | Très haut | UAT extensive, hotline support 1ère semaine | QA + Arch |
| **Incompatibilité Windows 7** | Faible | Moyen | Tester systématiquement Win7/8/10/11 | QA |
| **Perte de données** | Très faible | Très haut | Backups 2 fois/jour, tests restauration mensuels | DBA/Ops |
| **Réseau LAN instable** | Faible | Moyen | Reconnexion automatique, cache local temp | Arch |

### Plan de Contingence

#### Risque 1 : Périmètre Scope Creep
- **Symptôme** : Demandes nouvelles modules après Phase 2
- **Plan B** : Créer "Phase 7 Évolutions Futures" pour exigences non-critiques
- **Processus** : Changement request → Validation impact → Intégration prochain release

#### Risque 2 : Performance BD
- **Symptôme** : Tests charge Phase 5 montrent latence > 2s
- **Plan B** : 
  - Partitionnement tables volumineuses
  - Caching multi-niveaux (Redis optionnel)
  - Migration SQL Server Standard si Express insuffisant

#### Risque 3 : Intégration WPF/Backend
- **Symptôme** : Interface affiche mal données, binding brisé
- **Plan B** : 
  - DTOs intermédiaires pour désaccouplage
  - Mock services côté UI pour tests indépendants
  - Pair programming Lead Dev WPF + Arch

#### Risque 4 : Bugs Critiques Production
- **Symptôme** : Perte données, workflow impossible après go-live
- **Plan B** : 
  - Rollback facile (script BD de retour)
  - Hotline 24h/24 J+1 après déploiement
  - UAT 2 semaines avant go-live intensif

---

## Documentation Requise

### Documentation Technique

#### 1. Architecture Document
- **Contenu** : Vue générale, patterns (3-tiers, MVVM), dépendances
- **Format** : PDF + Markdown
- **Audience** : Développeurs, architectes
- **Effort** : 3 jours

#### 2. Design Base de Données
- **Contenu** : Schéma ER complet, descriptions tables, triggers, procédures
- **Format** : Diagrammes ER + SQL scripts
- **Audience** : DBAs, développeurs backend
- **Effort** : 2 jours

#### 3. API Interne (Services)
- **Contenu** : Services disponibles, signatures méthodes, exceptions
- **Format** : Code comments + Swagger (optionnel)
- **Exemple** :
```csharp
/// <summary>
/// Crée un nouveau devis.
/// </summary>
/// <param name="dto">Données du devis</param>
/// <returns>Identifiant du devis créé</returns>
/// <exception cref="ValidationException">Si validation échoue</exception>
public int CreateDevis(CreateDevisDto dto)
```
- **Effort** : 2 jours

#### 4. Code Comments & Conventions
- **Style** : Camel case (C#), XML comments obligatoires, max 100 chars ligne
- **Exemple** :
```csharp
// ✅ BON
/// <summary>Calcule le montant total d'un devis</summary>
private decimal CalculateTotalAmount(int quantite, decimal prixUnitaire)
{
    return quantite * prixUnitaire;
}

// ❌ MAUVAIS
decimal calcul(int q, decimal p) { return q * p; }
```
- **Effort** : Continu (intégré chaque commit)

### Documentation Utilisateur

#### 1. Guide Installation
- **Audience** : Administrateur système
- **Contenu** :
  - Prérequis système détaillés
  - Étapes installation pas-à-pas
  - Troubleshooting connexion
  - Configuration réseau
- **Format** : PDF avec captures d'écran
- **Effort** : 2 jours

#### 2. Guide Utilisateur par Rôle
- **Magasinier** (15 pages) :
  - Consulter stock
  - Créer besoins
  - Réceptionner livraisons
  - Vidéos (5 min each)

- **Responsable Achats** (18 pages) :
  - Consulter besoins
  - Créer/comparer devis
  - Émettre bons de commande
  - Suivi livraisons
  - Vidéos (6 min each)

- **Comptable** (15 pages) :
  - Enregistrer factures
  - Effectuer paiements
  - Rapports finance
  - Vidéos (4 min each)

- **Admin** (10 pages) :
  - Gestion utilisateurs
  - Paramètres système
  - Sauvegardes

- **Format** : PDF + Vidéos YouTube interne
- **Effort** : 5 jours

#### 3. FAQ & Troubleshooting
- **Contenu** :
  - Questions fréquentes par rôle
  - Solutions problèmes connexion
  - Récupération de données
  - Reportage de bugs

- **Format** : Confluence wiki interne
- **Effort** : 2 jours

#### 4. Vidéos Tutoriels
- **Cas clés** :
  - Login / Logout (1 min)
  - Créer devis (3 min)
  - Émettre BC (2 min)
  - Réceptionner BL (3 min)
  - Enregistrer facture (2 min)
  - Effectuer paiement (2 min)
  - Consultation rapports (2 min)

- **Outils** : Camtasia / OBS
- **Effort** : 3 jours (scripting + enregistrement)

#### 5. Manuel Administrateur
- **Contenu** :
  - Gestion BD (backups, maintenance)
  - Gestion utilisateurs & rôles
  - Paramètres système (TVA, délais)
  - Logs & audit
  - Troubleshooting

- **Format** : PDF 25-30 pages
- **Effort** : 3 jours

### Matrice Documentation

| Document | Audience | Format | Timing | Effort |
|----------|----------|--------|--------|--------|
| Architecture | Devs | PDF + Confluence | Phase 1 | 3j |
| Schéma BD | DBAs, Devs | Diagramme ER | Phase 0 | 2j |
| API Services | Devs | Code comments | Continu | 2j |
| Guide Installation | Admin | PDF | Phase 6 | 2j |
| Guide Utilisateur | Tous users | PDF + Vidéo | Phase 6 | 5j |
| FAQ | Tous users | Wiki | Phase 6 | 2j |
| Admin Manual | Admin IT | PDF | Phase 6 | 3j |
| Conventions Code | Devs | README | Phase 0 | 1j |
| **Total** | - | - | - | **20 jours** |

---

## Points Critiques de Succès

1. ✅ **Bien définir périmètre** dès le départ (pas de scope creep)
2. ✅ **Architecture solide** (3-tiers, MVVM, SOLID) dès Phase 1
3. ✅ **Tests automatisés** au cœur (80%+ coverage)
4. ✅ **CI/CD stricte** (pas de commit sans tests verts)
5. ✅ **UAT intensif** 2 semaines avant go-live
6. ✅ **Documentation excellent** (devs + utilisateurs)
7. ✅ **Équipe stable** (éviter turnover en cours de projet)
8. ✅ **Communication régulière** stakeholders (bi-hebdo syncs)
9. ✅ **Sauvegarde stratégie** dès Phase 0
10. ✅ **Support hot-line** 1ère semaine après déploiement

---

## Conclusion

Ce plan fournit une **feuille de route techniquement complète et réaliste** pour développer GesAchats en **15 semaines (3.5 mois)** avec une équipe de **4.5 FTE**.

Le projet suit une **approche progressive et itérative**, commençant par les fondations critiques (Authentification, Architecture, BD), puis construisant étape-par-étape le workflow complet (Devis → BC → BL → Facture → Règlement).

Chaque phase a des **jalons clairs, estimations temps réalistes, critères d'acceptation définis**, et un **plan de test rigoureux** assurant qualité et robustesse.

**Go-live réaliste : Fin août/début septembre 2024** (si démarrage début juin).

---

**Document Version 1.0 | Généré le 30 avril 2026**
