# 🚀 PROMPT 2 (FINAL) - ARCHITECTURE COMPLÈTE MAGASINIER + AJOUT PRODUITS

## 📌 CONTEXTE & CLARIFICATIONS

**Application** : GesAchats v2.0  
**Framework** : C# WPF (Windows Presentation Foundation)  
**Client** : Entreprise de construction de bâtiments  
**Rôle** : Technicien / Magasinier (Gestion des stocks et approvisionnements)  
**Status** : Le projet a été nettoyé - recommençons from scratch

---

## 🎯 LE MAGASINIER EN 60 SECONDES

Le Magasinier est responsable de :

1. **📊 ANALYSER** le stock en temps réel
   - Voir quels articles sont sous le minimum
   - Identifier les ruptures critiques
   
2. **📝 PLANIFIER** les commandes
   - Créer une liste de besoins (articles + quantités)
   - **NOUVEAU : Ajouter de nouveaux produits dans la liste**
   - Transmettre cette liste au Responsable Achats
   
3. **📦 CONSULTER** les Bons de Commande
   - Voir ce que le Resp. Achats a commandé (lecture seule)
   
4. **🚚 ENREGISTRER** les livraisons
   - Saisir les bons de livraison à la réception
   - Upload les fichiers PDF/images du BL
   
5. **📈 SUIVRE** l'activité
   - Dashboard avec KPI et alertes

---

## 📄 LES 5 PAGES À CRÉER

### ╔═ PAGE 1 : ANALYSE DU STOCK

**Chemin** : `Views/StockAnalysisView.xaml` + `.xaml.cs`

**Objectif** : Tableau de bord du stock avec alertes visuelles

**Éléments** :

```
┌────────────────────────────────────────────────────┐
│ Espace Magasinier / Stocks    [Actualiser] [Aide]  │
├────────────────────────────────────────────────────┤
│                                                     │
│  [🔍 Recherche]  [État: Tous ▼]                   │
│                                                     │
│  ┌─────────────────────────────────────────────┐  │
│  │ N°  │ Désignation  │ Stock │ Min │ Unité │  │  │
│  ├─────────────────────────────────────────────┤  │
│  │ 1   │ Graviers 40mm│  5    │ 20  │ m3   │🔴│  │
│  │ 2   │ Ciment 35kg  │ 45    │ 50  │ sac  │🟡│  │
│  │ 3   │ Acier HA8    │ 150   │ 100 │ bar  │🟢│  │
│  │ 4   │ Nouveau*     │  0    │ 10  │ m3   │⚪│  │
│  └─────────────────────────────────────────────┘  │
│                       ↑ Produits avec stock = 0    │
│                                                     │
│  [➕ Créer un Besoin]                             │
│                                                     │
└────────────────────────────────────────────────────┘

* Produits ajoutés par le Magasinier (pas encore commandés)
```

**Colonnes du tableau** :
- N° Article
- Désignation
- Stock Actuel
- Seuil Minimum
- Unité
- **État** (avec code couleur)
  - 🟢 VERT = Normal (Stock >= Seuil)
  - 🟡 JAUNE = Sous minimum (Stock < Seuil)
  - 🔴 ROUGE = Rupture critique (Stock = 0 ou très bas)
  - ⚪ BLANC/GRIS = Produit nouveau (Stock = 0, ajouté par Magasinier)
- Consommation 7 jours

**Fonctionnalités** :
- 🔄 Bouton "Actualiser" → Rafraîchit les données
- ➕ Bouton "Créer un Besoin" → Navigue à Page 2
- 🔍 Recherche + Filtre par état
- 📊 Résumé en bas : "Total: X | Ruptures: Y | Sous min: Z | Nouveaux produits: W"

**Données sources** : `BDD - Table Articles`

---

### ╔═ PAGE 2 : LISTE DES BESOINS + AJOUT PRODUITS 🆕

**Chemin** : `Views/NeedsListView.xaml` + `.xaml.cs`

**Objectif** : Le Magasinier crée une liste de commande ET peut ajouter de nouveaux produits

**Flux** :
```
Magasinier crée une liste + ajoute produits → Transmet au Resp. Achats → Resp. Achats crée les BC
```

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Créer une Liste de Besoins d'Approvisionnement   │
├──────────────────────────────────────────────────┤
│                                                   │
│  [🔍 Recherche article...]                       │
│                   [➕ Ajouter un produit] 🆕    │
│                                                   │
│  ┌──────────────────────────────────────┐        │
│  │☑ │ Désignation │ Stock │ Qty Cmd │  │        │
│  ├──────────────────────────────────────┤        │
│  │☐ │ Graviers 40 │  5    │ [50]    │  │        │
│  │☑ │ Ciment 35kg │ 45    │ [10]    │  │        │
│  │☑ │ Sable fin*  │  0    │ [100]   │  │ 🆕   │
│  │☐ │ Acier HA8   │ 150   │         │  │        │
│  └──────────────────────────────────────┘        │
│             ↑ Produits marqués avec *             │
│                                                   │
│  ┌─ RÉCAPITULATIF ──────────────┐                │
│  │ Articles sélectionnés: 3      │                │
│  │ Quantité totale: 160 unités   │                │
│  │ Produits nouveaux: 1          │ 🆕          │
│  │                               │                │
│  │ • Ciment 35kg: 10 sacs        │                │
│  │ • Graviers 40: 50 m³          │                │
│  │ • Sable fin*: 100 m³ (NOUVEAU)│ 🆕          │
│  └───────────────────────────────┘                │
│                                                   │
│  [Annuler]  [Transmettre au Resp. Achats] ✓     │
│                                                   │
└──────────────────────────────────────────────────┘
```

**Tableau de sélection** :
- ☑️ Checkbox (sélection)
- Désignation
- Stock Actuel
- Seuil Min (si produit nouveau, voir formulaire d'ajout)
- **Quantité à Commander** (champ texte - saisi en direct)
- Indicateur si produit nouveau (*)

**NOUVEAU : Formulaire d'ajout de produit** :

Quand on clique sur "➕ Ajouter un produit", une **POPUP/Dialog** s'affiche :

```
┌────────────────────────────────────────┐
│ Ajouter un nouveau produit             │
├────────────────────────────────────────┤
│                                         │
│ Désignation *:                         │
│ [________________________]              │
│                                         │
│ Unité *:                               │
│ [Sélect ▼]  (m³, sac, barre, kg, etc) │
│                                         │
│ Stock Initial *:                       │
│ [0]  (Sera toujours 0 pour nouveau)    │
│                                         │
│ Seuil Minimum *:                       │
│ [________________]                     │
│                                         │
│ ℹ️ Info: Le stock initial sera 0       │
│    jusqu'à la première livraison       │
│                                         │
│ [Annuler]  [Ajouter et Sélectionner] ✓ │
│                                         │
└────────────────────────────────────────┘
```

**Champs d'ajout de produit** :
- **Désignation** (TextBox) *obligatoire* - Nom du produit
- **Unité** (ComboBox) *obligatoire* - m³, sac, barre, kg, l, etc.
- **Stock Initial** (TextBox, **LECTURE SEULE**) = 0 toujours
- **Seuil Minimum** (TextBox) *obligatoire* - Quantité minimale à maintenir

**Panneau récapitulatif** (à droite) :
- Nombre d'articles sélectionnés
- Quantité totale à commander
- **Nombre de produits nouveaux** 🆕
- Liste détaillée des sélections (avec marquage * pour nouveaux)
- Date/heure de création

**Fonctionnalités** :
- Recherche pour filtrer les articles existants
- Bouton "➕ Ajouter un produit" → Ouvre dialog d'ajout 🆕
- Après ajout du produit → Il s'ajoute au tableau ET devient sélectionné ✓ 🆕
- Saisie directe de quantité dans le tableau
- Calcul automatique du récapitulatif (avec comptage des nouveaux produits)
- Bouton "Transmettre" → Archive + Notification au Resp. Achats (avec liste des nouveaux produits)
- Bouton "Annuler" → Retour Page 1

**Données sources** : `BDD - Table Articles`  
**Données créées** : 
- `BDD - Table Articles` (nouveau produit) 🆕
- `BDD - Table BesoinsApprov + BesoinDetails`

---

### ╔═ PAGE 3 : BONS DE COMMANDE (CONSULTATION)

**Chemin** : `Views/PurchaseOrdersView.xaml` + `.xaml.cs`

**Objectif** : Afficher les BC créés par le Resp. Achats (LECTURE SEULE)

⚠️ **Le Magasinier NE PEUT PAS MODIFIER cette page**

**Éléments** :

```
┌─────────────────────────────────────────────────┐
│ Bons de Commande (Lecture seule)                │
├─────────────────────────────────────────────────┤
│                                                  │
│ [Statut: Tous ▼]  [Fournisseur: Tous ▼]        │
│ [Du: 01/01] [Au: 31/12]  [Appliquer]           │
│                                                  │
│ ┌────────────────────────────────────────────┐ │
│ │Date│ N°BC    │ Fournisseur │ Désignation  │ │
│ ├────────────────────────────────────────────┤ │
│ │10/1│BC-24-001│ Société Mat.│ Graviers 40mm│ │
│ │09/1│BC-24-002│ Cimenterie  │ Ciment 35kg  │ │
│ └────────────────────────────────────────────┘ │
│      (Double-clic pour détails)                 │
│                                                  │
│ [Actualiser]                                    │
│                                                  │
└─────────────────────────────────────────────────┘
```

**Colonnes du tableau** :
- DATE
- N°BC (Bon de Commande)
- FOURNISSEUR
- DÉSIGNATION
- QUANTITÉ
- UNITÉ
- PU (HT) - Prix Unitaire Hors Taxe
- PU (TTC) - Prix Unitaire TTC
- TOTAL
- 📎 FICHIER (lien pour télécharger PDF/IMG)
- STATUT (code couleur)
  - 🟢 Livré
  - 🔵 Émis
  - 🟡 Partiellement livré
  - ⚪ Brouillon

**Filtres** :
- Par statut (Tous / Brouillon / Émis / Partiellement livré / Livré)
- Par fournisseur
- Par date (plage De/À)

**Fonctionnalités** :
- Double-clic → Affiche popup détails complets
- Bouton "Télécharger" → Récupère le PDF/IMG
- Résumé : "Total BC: X | Montant: YYY€"
- Bouton "Actualiser"

**Données sources** : `BDD - Table BonsCommande`

---

### ╔═ PAGE 4 : BONS DE LIVRAISON (SAISIE)

**Chemin** : `Views/DeliveryNotesView.xaml` + `.xaml.cs`

**Objectif** : Enregistrer les marchandises à la réception

**Éléments** :

```
┌──────────────────────────────────────────────┐
│ Réception des Marchandises - Bons de Livraison
├──────────────────────────────────────────────┤
│                                               │
│ FORMULAIRE                      │ HISTORIQUE  │
│ ─────────────────────────────   │ ─────────   │
│                                 │             │
│ Date réception *:               │ BL-24-005   │
│ [📅 10/01/2024]                 │ 10/01 14:30 │
│                                 │             │
│ N° BL *:                        │ BL-24-004   │
│ [_______________]               │ 09/01 09:15 │
│                                 │             │
│ Fournisseur *:                  │ Résumé      │
│ [Sélect. ▼]                     │ ─────────   │
│                                 │ Auj: 2 BL   │
│ BC correspondant *:             │ Sem: 8 BL   │
│ [Sélect. ▼]                     │             │
│                                 │             │
│ Quantité attendue: 50 m³        │             │
│ Quantité reçue: [50]            │             │
│ ✓ Conforme                      │             │
│                                 │             │
│ Observations:                   │             │
│ [_________________________]      │             │
│                                 │             │
│ PDF/IMG du BL *:                │             │
│ [📎 Aucun fichier]  [📁 Parco] │             │
│                                 │             │
│ [✓ Valider]  [Annuler]         │             │
│                                 │             │
└──────────────────────────────────────────────┘
```

**Formulaire de saisie** :
- ⏰ **Date de réception** (DatePicker) *obligatoire*
- 📋 **N°BL** (TextBox) *obligatoire*
- 🏢 **Fournisseur** (ComboBox) *obligatoire*
- 🔗 **BC correspondant** (ComboBox) *obligatoire*
- 📦 **Quantité attendue** (affichée automatiquement)
- 📥 **Quantité reçue** (TextBox à remplir)
- ⚠️ **Conformité** (affichée auto: "✓ Conforme" ou "⚠️ Écart")
- 📝 **Observations** (TextBox multi-ligne)
- 📎 **Fichier PDF/IMG du BL** (upload) *obligatoire*
- 🖼️ **Aperçu du fichier** (affichage miniature)

**Panneau latéral (Historique)** :
- Derniers 5 BL reçus (clickable)
- Résumé : "Aujourd'hui: X BL" / "Cette semaine: Y BL"

**Fonctionnalités** :
- Bouton "📁 Parcourir" → Sélectionne PDF/IMG
- Aperçu automatique si image
- Bouton "✓ Valider" → Sauvegarde en BDD + Notification au Comptable
- Bouton "Annuler" → Retour
- Validation : Tous les champs obligatoires vérifiés
- Message de confirmation après enregistrement

**Données sources** : `BDD - Table BonsCommande (lien)`  
**Données créées** : `BDD - Table BonsLivraison`

---

### ╔═ PAGE 5 : DASHBOARD MAGASINIER

**Chemin** : `Views/WarehouseDashboardView.xaml` + `.xaml.cs`

**Objectif** : Vue synthétique avec KPI et alertes

**Éléments** :

```
┌──────────────────────────────────────────────┐
│ Dashboard - Espace Magasinier               │
├──────────────────────────────────────────────┤
│                                              │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│ │ 📦Stock  │ │ ⚠️Rupture│ │ 🟡Sous.M │    │
│ │  315 u.  │ │    2     │ │    5     │    │
│ └──────────┘ └──────────┘ └──────────┘    │
│                                              │
│ ┌──────────┐ ┌──────────┐ ┌──────────┐    │
│ │ ⏳Besoin  │ │ 📥Recept │ │ ✨Nouveaux│   │
│ │ attente  │ │ cet. sem │ │ produits │    │
│ │    3     │ │    8     │ │    2     │    │
│ └──────────┘ └──────────┘ └──────────┘    │
│                                              │
│ ┌────────────────────────────────────────┐ │
│ │ ALERTES                                │ │
│ │ 🔴 Graviers 40mm - RUPTURE CRITIQUE   │ │
│ │ 🟡 Ciment 35kg - Sous le minimum      │ │
│ │ ✨ 2 nouveaux produits en attente    │ │ 🆕
│ └────────────────────────────────────────┘ │
│                                              │
│ [Consulter stock] [Créer besoin]           │
│ [Voir commandes]  [Enregistrer livraison]  │
│                                              │
└──────────────────────────────────────────────┘
```

**Cartes KPI** :
- 📦 Stock total (nombre unités)
- ⚠️ Articles en rupture (nombre)
- 🟡 Articles sous minimum (nombre)
- ⏳ Demandes en attente de traitement
- 📥 Bons de livraison reçus cette semaine
- **✨ Nouveaux produits ajoutés** (nombre) 🆕

**Alertes** (dynamiques) :
- Ruptures critiques (🔴 Rouge)
- Sous minimum (🟡 Orange)
- Demandes non traitées (🔵 Bleu)
- **Nouveaux produits en attente** (✨ Doré/Violet) 🆕

**Boutons d'accès rapide** :
- "Consulter le Stock" → Page 1
- "Créer une Demande" → Page 2
- "Voir les Commandes" → Page 3
- "Enregistrer Livraison" → Page 4

**Données sources** : `BDD - Toutes les tables`

---

## 🗄️ SCHÉMA BASE DE DONNÉES (SQL)

```sql
-- 1. TABLE ARTICLES (Données maître)
CREATE TABLE Articles (
    ArticleId INT PRIMARY KEY AUTO_INCREMENT,
    Designation NVARCHAR(255) NOT NULL,
    StockActuel INT DEFAULT 0,
    SeuilMin INT DEFAULT 10,
    Unite NVARCHAR(50), -- m3, sac, barre, etc.
    Consommation7j INT DEFAULT 0,
    IsNew BIT DEFAULT 0, -- 1 si produit ajouté par Magasinier (Stock=0), 0 sinon 🆕
    DateCreation DATETIME DEFAULT GETDATE(),
    CreePar NVARCHAR(255), -- Nom du Magasinier si IsNew=1 🆕
    DateMaj DATETIME DEFAULT GETDATE()
);

-- 2. TABLE BESOINS APPROV (Créé par Magasinier)
CREATE TABLE BesoinsApprov (
    BesoinId INT PRIMARY KEY AUTO_INCREMENT,
    DateCreation DATETIME DEFAULT GETDATE(),
    Statut NVARCHAR(50) DEFAULT 'En attente', -- En attente / Transmis / En cours / Complété
    CreePar NVARCHAR(255), -- Nom du Magasinier
    NombreProduitNouveau INT DEFAULT 0, -- Nombre de produits nouveaux dans ce besoin 🆕
    DateTransmission DATETIME NULL
);

-- 3. TABLE BESOIN DETAILS (Articles dans le besoin)
CREATE TABLE BesoinDetails (
    DetailId INT PRIMARY KEY AUTO_INCREMENT,
    BesoinId INT NOT NULL,
    ArticleId INT NOT NULL,
    QuantiteCommande INT NOT NULL,
    FOREIGN KEY (BesoinId) REFERENCES BesoinsApprov(BesoinId),
    FOREIGN KEY (ArticleId) REFERENCES Articles(ArticleId)
);

-- 4. TABLE BONS COMMANDE (Créé par Resp. Achats)
CREATE TABLE BonsCommande (
    BcId INT PRIMARY KEY AUTO_INCREMENT,
    NumeroBc NVARCHAR(50) UNIQUE NOT NULL, -- BC-2024-001
    Date DATETIME DEFAULT GETDATE(),
    Fournisseur NVARCHAR(255) NOT NULL,
    Designation NVARCHAR(255) NOT NULL,
    Quantite INT NOT NULL,
    Unite NVARCHAR(50),
    PuHt DECIMAL(10, 2), -- Prix Unitaire HT
    PuTtc DECIMAL(10, 2), -- Prix Unitaire TTC
    Total DECIMAL(12, 2), -- Quantité × PuTtc
    Statut NVARCHAR(50) DEFAULT 'Brouillon', -- Brouillon/Émis/Partiellement livré/Livré
    FichierPdf NVARCHAR(MAX), -- Chemin du fichier ou blob
    CreePar NVARCHAR(255),
    DateMaj DATETIME DEFAULT GETDATE()
);

-- 5. TABLE BONS LIVRAISON (Créé par Magasinier)
CREATE TABLE BonsLivraison (
    BlId INT PRIMARY KEY AUTO_INCREMENT,
    NumeroBl NVARCHAR(50) NOT NULL,
    DateReception DATETIME NOT NULL,
    Fournisseur NVARCHAR(255) NOT NULL,
    BcId INT NOT NULL, -- Lien au BC correspondant
    QuantiteRecue INT NOT NULL,
    Observations NVARCHAR(MAX),
    FichierPdf NVARCHAR(MAX), -- Chemin ou blob du PDF/IMG
    Statut NVARCHAR(50) DEFAULT 'En attente', -- En attente/Validé/Rejeté
    DateCreation DATETIME DEFAULT GETDATE(),
    CreePar NVARCHAR(255), -- Nom du Magasinier
    FOREIGN KEY (BcId) REFERENCES BonsCommande(BcId)
);

-- 6. INDEX POUR PERFORMANCE
CREATE INDEX idx_Articles_Stock ON Articles(StockActuel);
CREATE INDEX idx_Articles_IsNew ON Articles(IsNew); -- 🆕 Pour rapidement récupérer les nouveaux produits
CREATE INDEX idx_BesoinsApprov_Statut ON BesoinsApprov(Statut);
CREATE INDEX idx_BonsCommande_Statut ON BonsCommande(Statut);
CREATE INDEX idx_BonsCommande_Fournisseur ON BonsCommande(Fournisseur);
CREATE INDEX idx_BonsLivraison_BcId ON BonsLivraison(BcId);
```

---

## 🏗️ STRUCTURE DU PROJET C# WPF

```
GesAchats/
├── Views/
│   ├── StockAnalysisView.xaml
│   ├── StockAnalysisView.xaml.cs
│   ├── NeedsListView.xaml (MODIFIÉ - Ajout dialog produit) 🆕
│   ├── NeedsListView.xaml.cs (MODIFIÉ - Logique ajout produit) 🆕
│   ├── AddProductDialog.xaml (NOUVEAU) 🆕
│   ├── AddProductDialog.xaml.cs (NOUVEAU) 🆕
│   ├── PurchaseOrdersView.xaml
│   ├── PurchaseOrdersView.xaml.cs
│   ├── DeliveryNotesView.xaml
│   ├── DeliveryNotesView.xaml.cs
│   ├── WarehouseDashboardView.xaml
│   └── WarehouseDashboardView.xaml.cs
│
├── ViewModels/
│   ├── StockAnalysisViewModel.cs (MODIFIÉ - Indicateur nouveaux produits) 🆕
│   ├── NeedsListViewModel.cs (MODIFIÉ - Logique ajout produit) 🆕
│   ├── AddProductViewModel.cs (NOUVEAU) 🆕
│   ├── PurchaseOrdersViewModel.cs
│   ├── DeliveryNotesViewModel.cs
│   └── WarehouseDashboardViewModel.cs (MODIFIÉ - KPI nouveaux produits) 🆕
│
├── Models/
│   ├── Article.cs (MODIFIÉ - Ajouter propriétés IsNew, CreePar) 🆕
│   ├── BesoinApprov.cs (MODIFIÉ - Ajouter NombreProduitNouveau) 🆕
│   ├── BesoinDetail.cs
│   ├── BonCommande.cs
│   └── BonLivraison.cs
│
├── Services/
│   ├── DatabaseService.cs (MODIFIÉ - Méthode pour ajouter produit) 🆕
│   ├── FileUploadService.cs
│   ├── NotificationService.cs
│   └── NavigationService.cs
│
├── Helpers/
│   ├── ViewModelBase.cs
│   ├── RelayCommand.cs
│   └── ColorConverter.cs (MODIFIÉ - Couleur pour nouveaux produits) 🆕
│
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs
```

---

## 🎨 STYLE & DESIGN

**Couleurs** :
- 🟢 Normal: #4CAF50
- 🟡 Sous min: #FFC107
- 🔴 Rupture: #F44336
- 🔵 Info: #2196F3
- ✨ **Nouveaux produits: #9C27B0 (Violet/Doré)** 🆕

**Police** : Segoe UI 11pt (normal), 12pt (titre)

**Icônes** : Utiliser Unicode/Emoji + FontAwesome (optionnel)

**Responsive** : Min 1200x800

---

## ✅ CHECKLIST DE DÉVELOPPEMENT

### PHASE 1 - MODELS & DATABASE
- [ ] Créer classes Models (Article, BesoinApprov, etc.)
- [ ] **MODIFIER Article.cs : Ajouter IsNew, CreePar** 🆕
- [ ] **MODIFIER BesoinApprov.cs : Ajouter NombreProduitNouveau** 🆕
- [ ] Créer tables BDD (SQL)
- [ ] Créer DatabaseService (CRUD pour chaque table)
- [ ] **Ajouter méthode DatabaseService.AddNewArticle()** 🆕

### PHASE 2 - PAGE 1 (Stock Analysis)
- [ ] StockAnalysisView.xaml
- [ ] StockAnalysisView.xaml.cs
- [ ] StockAnalysisViewModel.cs
- [ ] **MODIFIER ColorConverter pour couleur nouveaux produits** 🆕
- [ ] Tests: Affiche tableau, filtres, nav vers Page 2

### PHASE 3 - PAGE 2 (Needs List) + AJOUT PRODUIT 🆕
- [ ] NeedsListView.xaml (MODIFIÉ - Bouton "Ajouter produit")
- [ ] NeedsListView.xaml.cs (MODIFIÉ - Gestion dialog)
- [ ] **AddProductDialog.xaml (NOUVEAU)** 🆕
- [ ] **AddProductDialog.xaml.cs (NOUVEAU)** 🆕
- [ ] **AddProductViewModel.cs (NOUVEAU)** 🆕
- [ ] NeedsListViewModel.cs (MODIFIÉ - Logique ajout)
- [ ] Tests: Sélection, saisie quantité, ajout produit, transmission

### PHASE 4 - PAGE 3 (Purchase Orders)
- [ ] PurchaseOrdersView.xaml
- [ ] PurchaseOrdersView.xaml.cs
- [ ] PurchaseOrdersViewModel.cs
- [ ] Tests: Affiche BC, filtres, téléchargement fichier

### PHASE 5 - PAGE 4 (Delivery Notes)
- [ ] DeliveryNotesView.xaml
- [ ] DeliveryNotesView.xaml.cs
- [ ] DeliveryNotesViewModel.cs
- [ ] FileUploadService.cs
- [ ] Tests: Saisie form, upload fichier, validation

### PHASE 6 - PAGE 5 (Dashboard) 🆕
- [ ] WarehouseDashboardView.xaml (MODIFIÉ - KPI nouveaux produits)
- [ ] WarehouseDashboardView.xaml.cs (MODIFIÉ)
- [ ] WarehouseDashboardViewModel.cs (MODIFIÉ)
- [ ] Tests: Affiche KPI, alertes, nouveaux produits, accès rapides

### PHASE 7 - INTÉGRATION & POLISH
- [ ] Navigation MainWindow
- [ ] Gestion erreurs globale
- [ ] Validation données
- [ ] Messages utilisateur (succès/erreur)
- [ ] Tests complets

---

## 🚀 QUICK START

1. **Créer les models** dans `Models/`
2. **Créer les tables SQL** (avec colonnes IsNew, CreePar)
3. **Créer DatabaseService** dans `Services/`
4. **Créer ViewModels** dans `ViewModels/`
5. **Créer Views** dans `Views/`
6. **Créer AddProductDialog + ViewModel** 🆕
7. **Intégrer MainWindow** (navigation)
8. **Tester** chaque page isolément
9. **Valider** le flux complet (avec ajout produit)

---

## ⚠️ RÈGLES MÉTIER STRICTES

✅ Le Magasinier PEUT :
- Consulter le stock
- Créer une liste de besoins
- **Ajouter de nouveaux produits dans la liste des besoins** 🆕
- Transmettre au Resp. Achats
- Consulter les BC (lecture seule)
- Enregistrer les BL
- Voir son dashboard

❌ Le Magasinier NE PEUT PAS :
- Modifier les produits existants
- Modifier/créer des BC
- Modifier les articles maître (sauf ajout nouveau)
- Valider les factures
- Effectuer les paiements

---

## 🆕 DÉTAIL AJOUT PRODUIT

### **Workflow d'ajout** :

```
1. Magasinier clique "➕ Ajouter un produit"
   ↓
2. Dialog AddProductDialog s'affiche
   ↓
3. Magasinier saisit :
   - Désignation (obligatoire)
   - Unité (obligatoire)
   - Stock Initial = 0 (lecture seule)
   - Seuil Min (obligatoire)
   ↓
4. Magasinier clique "Ajouter et Sélectionner"
   ↓
5. Produit inséré en BDD avec :
   - ArticleId (auto-généré)
   - Designation (saisi)
   - StockActuel = 0
   - SeuilMin (saisi)
   - Unite (saisi)
   - IsNew = 1 (NOUVEAU PRODUIT) ✨
   - CreePar = Nom du Magasinier
   - DateCreation = Maintenant
   ↓
6. Produit s'ajoute au tableau NeedsListView
   - Marqué avec * (NOUVEAU)
   - Automatiquement sélectionné (checkbox=true)
   - Prêt pour saisie de quantité
   ↓
7. Récapitulatif s'actualise :
   - Nombre articles sélectionnés +1
   - Nombre produits nouveaux +1
   ↓
8. Magasinier saisit la quantité à commander
   ↓
9. Bouton "Transmettre" → Archive la liste avec indication "2 produits nouveaux"
   ↓
10. Le Responsable Achats reçoit la liste avec ces nouveaux produits
    Il peut les commander ou les refuser selon son jugement
```

### **Affichage en Page 1 (Analyse du Stock)** :

Après transmission, si le Responsable Achats a créé un BC :
- Le produit nouveau **disparaît du marquage ✨**
- Il s'affiche comme un produit normal
- Son stock reste à 0 jusqu'à la première livraison

Si le Responsable Achats **ne crée PAS de BC** :
- Le produit reste avec stock = 0
- Il apparaît encore dans la liste (avec fond gris/blanc spécial)
- État = ⚪ (BLANC = Produit nouveau en attente de commande)

---

**STATUS** : ✅ Prêt pour développement immédiat avec ajout produit

**TEMPS ESTIMÉ** : 45-70 heures (6-8 jours)

**LIVRABLES** : Code WPF complet + BDD + Tests + Ajout Produit
