# 🚀 PROMPT 4 - ARCHITECTURE COMPLÈTE COMPTABLE

## 📌 CONTEXTE & CLARIFICATIONS

**Application** : GesAchats v2.0  
**Framework** : C# WPF (Windows Presentation Foundation)  
**Client** : Entreprise de construction de bâtiments  
**Rôle** : Comptable (Gestion des factures fournisseurs et des règlements)  
**Status** : Développement du module Comptable

---

## 🎯 LE COMPTABLE EN 60 SECONDES

Le Comptable est responsable de :

1. **🧾 ENREGISTRER** les factures fournisseurs
   - Saisir les informations de la facture (date, fournisseur, numéro, total TTC)
   - Associer la facture au bon de commande et au bon de livraison correspondants
   - Gérer les statuts des factures (En attente / Vérifiée / Payée)

2. **✅ VÉRIFIER** la conformité des documents
   - Comparer la facture avec le Bon de Commande (BC)
   - Comparer la facture avec le Bon de Livraison (BL)
   - Détecter les écarts (prix, quantités, articles)
   - Valider ou rejeter la conformité

3. **💳 SAISIR** les règlements fournisseurs
   - Enregistrer le mode de paiement (virement, chèque, espèces)
   - Préciser la date et le montant du paiement
   - Associer le règlement à la facture correspondante
   - Ajouter une preuve de paiement (image ou PDF)

4. **📄 GÉNÉRER** les reçus de paiement PDF
   - Créer un reçu professionnel en PDF
   - Inclure toutes les informations du règlement
   - Archiver les documents générés

5. **📊 SUIVRE** l'état des paiements
   - Consulter les factures impayées ou partiellement payées
   - Voir l'historique des règlements par fournisseur
   - Analyser les soldes et les encours

---

## 📄 LES 5 PAGES À CRÉER

### ╔═ PAGE 1 : LISTE DES FACTURES FOURNISSEURS

**Chemin** : `Views/InvoiceListView.xaml` + `.xaml.cs`

**Objectif** : Afficher toutes les factures fournisseurs avec leur statut de conformité et de paiement

**Éléments** :

```
┌────────────────────────────────────────────────────────────────┐
│ Espace Comptable - Factures Fournisseurs                      │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  [➕ Nouvelle Facture]  [🔄 Actualiser]  [📊 Statistiques]   │
│                                                                 │
│  Filtres :                                                      │
│  Fournisseur: [Tous ▼]  Statut: [Tous ▼]  Période: [──/──]  │
│                                                                 │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │N°Fact.│Fournisseur  │Date    │TTC     │Conformité│Statut │  │
│  ├──────────────────────────────────────────────────────────┤  │
│  │F-001  │Soc. Mat.    │10/01   │1634€   │✅ OK     │Payée  │  │
│  │F-002  │Cimenterie   │11/01   │980€    │⚠️ Écart  │Attente│  │
│  │F-003  │Aciers Mod.  │12/01   │2100€   │✅ OK     │Vérif. │  │
│  │F-004  │Fournisseur X│13/01   │750€    │❓ Non vérf│Attente│  │
│  └──────────────────────────────────────────────────────────┘  │
│                                                                 │
│  [Voir détails]  [Vérifier conformité]  [Enregistrer paiement]│
│                                                                 │
│  Total en attente: 3 factures | Montant: 3 830€               │
└────────────────────────────────────────────────────────────────┘
```

**Colonnes du tableau** :
- N° FACTURE (identifiant unique ex: F-2024-001)
- FOURNISSEUR (nom du fournisseur)
- DATE FACTURE (date d'émission)
- N° BC (référence bon de commande lié)
- N° BL (référence bon de livraison lié)
- TOTAL TTC (montant total toutes taxes comprises)
- CONFORMITÉ (✅ OK / ⚠️ Écart / ❓ Non vérifiée)
- STATUT (En attente / Vérifiée / Partiellement payée / Payée)
- ACTION (Voir / Vérifier / Payer)

**Fonctionnalités** :
- ➕ Bouton "Nouvelle Facture" → Navigue à Page 2 (formulaire saisie)
- 🔄 Bouton "Actualiser" → Rafraîchit la liste
- 🔍 Filtres par fournisseur, statut et période
- 👁️ Bouton "Voir détails" → Affiche popup détaillé
- ✅ Bouton "Vérifier conformité" → Navigue à Page 3
- 💳 Bouton "Enregistrer paiement" → Navigue à Page 4
- 📊 Barre de résumé en bas (total en attente, montants)

**Données sources** : `BDD - Table Factures + Fournisseurs + BonsCommande + BonsLivraison`

---

### ╔═ PAGE 2 : SAISIE D'UNE FACTURE FOURNISSEUR

**Chemin** : `Views/InvoiceFormView.xaml` + `.xaml.cs`

**Objectif** : Enregistrer une nouvelle facture fournisseur et l'associer aux documents existants

**Éléments** :

```
┌────────────────────────────────────────────────────────────────┐
│ Nouvelle Facture Fournisseur                                   │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──── INFORMATIONS FACTURE ─────────────────────────────────┐ │
│  │                                                            │ │
│  │  N° Facture Fournisseur : [FAC-2024-0123         ]       │ │
│  │  Fournisseur            : [Société Matériaux SA ▼]       │ │
│  │  Date Facture           : [10/01/2024            ]       │ │
│  │  Date Réception         : [11/01/2024            ]       │ │
│  │  Date Échéance          : [10/02/2024            ]       │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── ASSOCIATION DOCUMENTS ────────────────────────────────┐ │
│  │                                                            │ │
│  │  Bon de Commande lié : [BC-2024-001 ▼]                   │ │
│  │  Bon de Livraison lié: [BL-2024-001 ▼]                   │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── MONTANTS ─────────────────────────────────────────────┐ │
│  │                                                            │ │
│  │  Montant HT   : [     1 362,50 €]                        │ │
│  │  Taux TVA     : [    20 %      ]                         │ │
│  │  Montant TVA  : [       272,50 €] (calculé auto)         │ │
│  │  Montant TTC  : [     1 635,00 €]                        │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── ARTICLES FACTURÉS ────────────────────────────────────┐ │
│  │  Article         │ Qté │ PU HT  │ Total HT │ TVA %│      │ │
│  │  Graviers 40mm   │  50 │ 25,50€ │ 1 275,00€│ 20%  │      │ │
│  │  Ciment 35kg     │  10 │  8,75€ │    87,50€│ 20%  │      │ │
│  │                  [➕ Ajouter article]                      │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Observations : [_________________________________________]    │
│                                                                 │
│  [💾 Enregistrer]   [✅ Enregistrer & Vérifier]   [Annuler]   │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

**Formulaire de saisie** :
- **N° Facture Fournisseur** (TextBox) - Numéro figurant sur la facture reçue
- **Fournisseur** (ComboBox) - Sélection depuis la liste Fournisseurs
- **Date Facture** (DatePicker) - Date d'émission par le fournisseur
- **Date Réception** (DatePicker) - Date de réception par l'entreprise
- **Date Échéance** (DatePicker) - Date limite de paiement
- **BC lié** (ComboBox) - Sélection du Bon de Commande associé (filtré par fournisseur)
- **BL lié** (ComboBox) - Sélection du Bon de Livraison associé (filtré par BC)
- **Montant HT** (TextBox numérique)
- **Taux TVA** (ComboBox : 0%, 7%, 10%, 20%)
- **Montant TVA** (calculé automatiquement, lecture seule)
- **Montant TTC** (calculé automatiquement, lecture seule)
- **Articles facturés** (DataGrid éditable avec colonnes : Article, Quantité, PU HT, Total HT, TVA%)
- **Observations** (TextBox multiligne)

**Fonctionnalités** :
- Sélection fournisseur → filtre automatique les BC disponibles
- Sélection BC → filtre automatique les BL disponibles + pré-remplit les articles
- Calcul automatique TVA et TTC en temps réel
- "Enregistrer" → Sauvegarde avec statut "En attente"
- "Enregistrer & Vérifier" → Sauvegarde puis ouvre Page 3 directement

**Données sources** :
- `BDD - Table Fournisseurs`
- `BDD - Table BonsCommande`
- `BDD - Table BonsLivraison`

**Données créées** :
- `BDD - Table Factures`
- `BDD - Table FactureDetails`

---

### ╔═ PAGE 3 : VÉRIFICATION DE CONFORMITÉ

**Chemin** : `Views/ConformityCheckView.xaml` + `.xaml.cs`

**Objectif** : Comparer la facture avec le BC et le BL pour détecter les écarts

**Éléments** :

```
┌────────────────────────────────────────────────────────────────┐
│ Vérification de Conformité - Facture F-2024-001               │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Fournisseur : Société Matériaux SA                           │
│  BC associé  : BC-2024-001  │  BL associé : BL-2024-001      │
│                                                                 │
│  ┌──── COMPARAISON ARTICLE PAR ARTICLE ──────────────────────┐ │
│  │                                                            │ │
│  │ Article      │ BC Qté│BC PU  │BL Qté │Fact.Qté│Fact.PU│🔍│ │
│  ├──────────────┼───────┼───────┼───────┼────────┼───────┼──┤ │
│  │Graviers 40mm │   50  │25,50€ │   48  │   50   │25,50€ │✅│ │
│  │Ciment 35kg   │   10  │ 8,50€ │   10  │   10   │ 8,75€ │⚠️│ │
│  │Acier HA8     │   20  │12,00€ │    0  │    0   │   -   │ℹ️│ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── RÉSUMÉ DES ÉCARTS ────────────────────────────────────┐ │
│  │                                                            │ │
│  │  ⚠️ ÉCART PRIX : Ciment 35kg                              │ │
│  │     BC : 8,50€/u  →  Facture : 8,75€/u  (+0,25€)        │ │
│  │     Impact : +2,50€ sur 10 unités                        │ │
│  │                                                            │ │
│  │  ℹ️ NON LIVRÉ : Acier HA8 (commandé, non livré)           │ │
│  │     Quantité BC : 20 barres  →  BL : 0 barre             │ │
│  │                                                            │ │
│  │  ✅ OK : Graviers 40mm (quantité et prix conformes)       │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── COMPARAISON MONTANTS GLOBAUX ─────────────────────────┐ │
│  │                                                            │ │
│  │            │   BC       │  BL     │  Facture  │  Écart   │ │
│  │  Total HT  │ 1 635,00€  │ 1 222€  │ 1 362,50€ │ -272,50€ │ │
│  │  TVA (20%) │   327,00€  │  244€   │   272,50€ │  -54,50€ │ │
│  │  Total TTC │ 1 962,00€  │ 1 466€  │ 1 635,00€ │ -327,00€ │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Décision du comptable :                                       │
│  ○ ✅ Valider malgré les écarts (ajouter justification)       │
│  ○ ❌ Rejeter la facture (contacter fournisseur)              │
│  ○ ✏️ Modifier la facture et revérifier                       │
│                                                                 │
│  Justification : [_______________________________________]     │
│                                                                 │
│  [✅ Confirmer décision]   [🖨️ Imprimer rapport]   [Retour]   │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

**Tableau de comparaison** :
- Par article, affiche en colonnes :
  - ARTICLE (nom)
  - BC Quantité / BC Prix Unitaire
  - BL Quantité livrée
  - Facture Quantité / Facture Prix Unitaire
  - STATUT (✅ OK / ⚠️ Écart prix / ⚠️ Écart quantité / ❌ Non conforme / ℹ️ Non livré)

**Résumé des écarts** :
- Liste textuelle de chaque écart détecté avec impact financier
- Comparaison des totaux HT/TVA/TTC entre BC, BL et Facture

**Logique de conformité** :
- ✅ OK : Quantité et prix identiques entre BC et Facture, article livré dans BL
- ⚠️ Écart prix : Prix facturé différent du prix commandé (tolérance configurable ex : ±2%)
- ⚠️ Écart quantité : Quantité facturée différente de la quantité commandée/livrée
- ❌ Non conforme : Article facturé absent du BC
- ℹ️ Non livré : Article commandé mais BL = 0 (pas encore livré)

**Fonctionnalités** :
- Calcul automatique des écarts à l'ouverture
- Mise en surbrillance des lignes problématiques (rouge/orange)
- Décision du comptable avec justification obligatoire si validation malgré écarts
- Génération d'un rapport de conformité imprimable
- Mise à jour du statut facture → "Vérifiée" ou "Rejetée"

**Données sources** :
- `BDD - Table Factures + FactureDetails`
- `BDD - Table BonsCommande + BcDetails`
- `BDD - Table BonsLivraison + BlDetails`

**Données mises à jour** :
- `BDD - Table Factures (Statut, Conformite, JustificationConformite)`

---

### ╔═ PAGE 4 : ENREGISTREMENT D'UN RÈGLEMENT

**Chemin** : `Views/PaymentFormView.xaml` + `.xaml.cs`

**Objectif** : Saisir un règlement pour une facture et uploader la preuve de paiement

**Éléments** :

```
┌────────────────────────────────────────────────────────────────┐
│ Enregistrer un Règlement                                       │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌──── FACTURE ASSOCIÉE ─────────────────────────────────────┐ │
│  │                                                            │ │
│  │  Facture N°  : F-2024-001                                │ │
│  │  Fournisseur : Société Matériaux SA                      │ │
│  │  Total TTC   : 1 635,00€                                 │ │
│  │  Déjà payé   :     0,00€                                 │ │
│  │  Reste à payer: 1 635,00€                                │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── DÉTAILS DU RÈGLEMENT ─────────────────────────────────┐ │
│  │                                                            │ │
│  │  Mode de paiement : ● Virement  ○ Chèque  ○ Espèces     │ │
│  │                                                            │ │
│  │  Date de paiement : [15/01/2024          ]               │ │
│  │  Montant réglé    : [          1 635,00€ ]               │ │
│  │  Référence        : [VIR-2024-00456      ]               │ │
│  │  (N° chèque, réf. virement, etc.)                        │ │
│  │                                                            │ │
│  │  Banque/Compte    : [Compte principal ▼  ]               │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── PREUVE DE PAIEMENT ───────────────────────────────────┐ │
│  │                                                            │ │
│  │  📎 Fichier : [Aucun fichier sélectionné    ] [Parcourir]│ │
│  │                                                            │ │
│  │  Formats acceptés : PDF, JPG, PNG, TIFF (max 10 Mo)     │ │
│  │                                                            │ │
│  │  ┌──────────────────────────────────────────┐            │ │
│  │  │                                          │            │ │
│  │  │   [Aperçu de la preuve de paiement]      │            │ │
│  │  │                                          │            │ │
│  │  └──────────────────────────────────────────┘            │ │
│  │                                                            │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Observations : [_________________________________________]    │
│                                                                 │
│  [💾 Enregistrer le règlement]  [📄 Générer reçu PDF]  [Annuler]│
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

**Formulaire de règlement** :
- **Facture associée** (pré-rempli, lecture seule) - Affiche les infos clés
- **Mode de paiement** (RadioButton) : Virement / Chèque / Espèces
- **Date de paiement** (DatePicker)
- **Montant réglé** (TextBox numérique - peut être partiel)
- **Référence** (TextBox) - N° chèque, référence virement, etc.
- **Banque/Compte** (ComboBox) - Compte bancaire utilisé
- **Preuve de paiement** (FileUpload + aperçu) - Image ou PDF
- **Observations** (TextBox multiligne)

**Fonctionnalités** :
- Sélection fichier → Aperçu immédiat (image : miniature, PDF : première page)
- Validation : montant ≤ reste à payer
- Paiement partiel possible → statut "Partiellement payée"
- Paiement total → statut "Payée"
- "Enregistrer" → Sauvegarde règlement + copie fichier preuve dans dossier archive
- "Générer reçu PDF" → Ouvre aperçu PDF et propose téléchargement/impression

**Upload fichier** :
- Chemin de stockage : `Documents/Preuves/{AnnéeMois}/{FournisseurId}/`
- Nom fichier : `RECU_{FactureId}_{Timestamp}.{extension}`
- Copie locale conservée + chemin stocké en BDD

**Données sources** :
- `BDD - Table Factures`
- `BDD - Table Fournisseurs`

**Données créées** :
- `BDD - Table Reglements`

---

### ╔═ PAGE 5 : SUIVI DES RÈGLEMENTS & HISTORIQUE

**Chemin** : `Views/PaymentHistoryView.xaml` + `.xaml.cs`

**Objectif** : Consulter l'historique des paiements, les factures impayées et les soldes fournisseurs

**Éléments** :

```
┌────────────────────────────────────────────────────────────────┐
│ Suivi des Règlements                                          │
├────────────────────────────────────────────────────────────────┤
│                                                                 │
│  ┌── TABLEAU DE BORD ────────────────────────────────────────┐ │
│  │  💰 Total payé ce mois  : 12 450,00€                     │ │
│  │  ⏳ Factures en attente :  3 (montant : 3 830,00€)       │ │
│  │  🔴 Factures en retard  :  1 (échéance dépassée)         │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  Filtres :                                                      │
│  Fournisseur: [Tous ▼]  Mode: [Tous ▼]  Période: [01/2024]  │
│                                                                 │
│  ┌──── HISTORIQUE DES RÈGLEMENTS ────────────────────────────┐ │
│  │ Date  │Fournisseur │Facture│Montant  │Mode    │Réf.      │ │
│  ├───────┼────────────┼───────┼─────────┼────────┼──────────┤ │
│  │15/01  │Soc. Mat.   │F-001  │1 635,00€│Virement│VIR-0456  │ │
│  │13/01  │Cimenterie  │F-003  │  490,00€│Chèque  │CHQ-1234  │ │
│  │12/01  │Aciers Mod. │F-002  │  980,00€│Espèces │-         │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  [👁️ Voir preuve]  [📄 Voir reçu PDF]  [📊 Export Excel]     │
│                                                                 │
│  ┌──── FACTURES IMPAYÉES ────────────────────────────────────┐ │
│  │ N°Fact │Fournisseur │Échéance │Montant  │Retard  │       │ │
│  ├────────┼────────────┼─────────┼─────────┼────────┼───────┤ │
│  │F-004   │Fourn. X    │10/01    │  750,00€│🔴 5j   │[Payer]│ │
│  │F-003   │Cimenterie  │20/01    │  490,00€│🟡 -5j  │[Payer]│ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌──── SOLDES PAR FOURNISSEUR ───────────────────────────────┐ │
│  │ Fournisseur        │ Facturé  │ Payé     │ Solde dû      │ │
│  ├────────────────────┼──────────┼──────────┼───────────────┤ │
│  │ Société Matériaux  │ 3 270,00€│ 3 270,00€│       0,00€  │ │
│  │ Cimenterie du Nord │   980,00€│   490,00€│     490,00€  │ │
│  │ Aciers Modernes    │ 2 100,00€│ 2 100,00€│       0,00€  │ │
│  └────────────────────────────────────────────────────────┘ │
│                                                                 │
└────────────────────────────────────────────────────────────────┘
```

**Sections** :
- **Tableau de bord** : KPIs du mois (total payé, factures en attente, retards)
- **Historique des règlements** : Tous les paiements effectués avec filtres
- **Factures impayées** : Factures non réglées avec indicateur de retard
- **Soldes par fournisseur** : Récapitulatif montants facturés / payés / dus

**Fonctionnalités** :
- 👁️ "Voir preuve" → Ouvre le fichier preuve de paiement (image/PDF)
- 📄 "Voir reçu PDF" → Ré-ouvre le reçu de paiement généré
- 📊 "Export Excel" → Exporte l'historique filtré en fichier .xlsx
- [Payer] sur une facture impayée → Navigue à Page 4 avec facture pré-sélectionnée
- Indicateur retard : 🔴 Retard > 0j / 🟡 Échéance dans les 7 jours

**Données sources** :
- `BDD - Table Factures + Fournisseurs + Reglements`

---

## 🗄️ BASE DE DONNÉES - TABLES SQL SERVER

```sql
-- 1. TABLE FACTURES FOURNISSEURS
CREATE TABLE Factures (
    FactureId       INT PRIMARY KEY IDENTITY(1,1),
    NumeroFacture   NVARCHAR(50) UNIQUE NOT NULL,   -- ex: FAC-2024-001
    NumeroFactureFournisseur NVARCHAR(100),          -- N° figurant sur la facture reçue
    FournisseurId   INT NOT NULL,
    BcId            INT NULL,                        -- Bon de Commande associé
    BlId            INT NULL,                        -- Bon de Livraison associé
    DateFacture     DATETIME NOT NULL,               -- Date d'émission par le fournisseur
    DateReception   DATETIME DEFAULT GETDATE(),      -- Date de réception
    DateEcheance    DATETIME NULL,                   -- Date limite de paiement
    MontantHt       DECIMAL(10, 2) NOT NULL,
    TauxTva         DECIMAL(5, 2) DEFAULT 20.00,
    MontantTva      DECIMAL(10, 2) NOT NULL,
    MontantTtc      DECIMAL(10, 2) NOT NULL,
    Statut          NVARCHAR(50) DEFAULT 'EnAttente', -- EnAttente/Verifiee/PartiellementPayee/Payee/Rejetee
    Conformite      NVARCHAR(50) DEFAULT 'NonVerifiee', -- NonVerifiee/Conforme/EcartMineur/NonConforme
    JustificationConformite NVARCHAR(MAX) NULL,
    Observations    NVARCHAR(MAX) NULL,
    CreePar         NVARCHAR(255),
    DateCreation    DATETIME DEFAULT GETDATE(),
    DateMaj         DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (FournisseurId) REFERENCES Fournisseurs(FournisseurId),
    FOREIGN KEY (BcId)          REFERENCES BonsCommande(BcId),
    FOREIGN KEY (BlId)          REFERENCES BonsLivraison(BlId)
);

-- 2. TABLE DETAILS FACTURE (Articles facturés)
CREATE TABLE FactureDetails (
    DetailId      INT PRIMARY KEY IDENTITY(1,1),
    FactureId     INT NOT NULL,
    ArticleId     INT NOT NULL,
    Quantite      DECIMAL(10, 3) NOT NULL,
    PuHt          DECIMAL(10, 2) NOT NULL,           -- Prix Unitaire HT facturé
    TotalHt       DECIMAL(10, 2) NOT NULL,
    TauxTva       DECIMAL(5, 2) DEFAULT 20.00,
    TotalTtc      DECIMAL(10, 2) NOT NULL,
    FOREIGN KEY (FactureId)  REFERENCES Factures(FactureId),
    FOREIGN KEY (ArticleId)  REFERENCES Articles(ArticleId)
);

-- 3. TABLE RÈGLEMENTS (Paiements)
CREATE TABLE Reglements (
    ReglementId   INT PRIMARY KEY IDENTITY(1,1),
    NumeroReglement NVARCHAR(50) UNIQUE NOT NULL,    -- ex: REG-2024-001
    FactureId     INT NOT NULL,
    FournisseurId INT NOT NULL,
    DatePaiement  DATETIME NOT NULL,
    Montant       DECIMAL(10, 2) NOT NULL,
    ModePaiement  NVARCHAR(50) NOT NULL,             -- Virement/Cheque/Especes
    Reference     NVARCHAR(255) NULL,                -- N° chèque, réf. virement
    Banque        NVARCHAR(100) NULL,
    FichierPreuve NVARCHAR(MAX) NULL,                -- Chemin fichier preuve
    TypeFichier   NVARCHAR(20) NULL,                 -- pdf/jpg/png/tiff
    FichierRecu   NVARCHAR(MAX) NULL,                -- Chemin reçu PDF généré
    Observations  NVARCHAR(MAX) NULL,
    CreePar       NVARCHAR(255),
    DateCreation  DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (FactureId)     REFERENCES Factures(FactureId),
    FOREIGN KEY (FournisseurId) REFERENCES Fournisseurs(FournisseurId)
);

-- 4. TABLE BONS LIVRAISON (Nouvelle table requise)
CREATE TABLE BonsLivraison (
    BlId          INT PRIMARY KEY IDENTITY(1,1),
    NumeroBl      NVARCHAR(50) UNIQUE NOT NULL,      -- BL-2024-001
    BcId          INT NULL,                           -- BC associé
    FournisseurId INT NOT NULL,
    DateLivraison DATETIME NOT NULL,
    Statut        NVARCHAR(50) DEFAULT 'Recu',        -- EnAttente/Recu/Partiel/Incomplet
    Observations  NVARCHAR(MAX) NULL,
    ReceptionneePar NVARCHAR(255),
    DateCreation  DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (BcId)          REFERENCES BonsCommande(BcId),
    FOREIGN KEY (FournisseurId) REFERENCES Fournisseurs(FournisseurId)
);

-- 5. TABLE DÉTAILS BL (Articles livrés)
CREATE TABLE BlDetails (
    DetailId      INT PRIMARY KEY IDENTITY(1,1),
    BlId          INT NOT NULL,
    ArticleId     INT NOT NULL,
    QuantiteCommandee DECIMAL(10, 3) NOT NULL,
    QuantiteLivree    DECIMAL(10, 3) NOT NULL,
    FOREIGN KEY (BlId)       REFERENCES BonsLivraison(BlId),
    FOREIGN KEY (ArticleId)  REFERENCES Articles(ArticleId)
);

-- 6. INDEX POUR PERFORMANCE
CREATE INDEX idx_Factures_FournisseurId   ON Factures(FournisseurId);
CREATE INDEX idx_Factures_BcId            ON Factures(BcId);
CREATE INDEX idx_Factures_Statut          ON Factures(Statut);
CREATE INDEX idx_Factures_DateEcheance    ON Factures(DateEcheance);
CREATE INDEX idx_Reglements_FactureId     ON Reglements(FactureId);
CREATE INDEX idx_Reglements_FournisseurId ON Reglements(FournisseurId);
CREATE INDEX idx_Reglements_DatePaiement  ON Reglements(DatePaiement);
CREATE INDEX idx_BonsLivraison_BcId       ON BonsLivraison(BcId);
```

---

## 🏗️ STRUCTURE DU PROJET C# WPF

```
GesAchats/
├── Views/
│   ├── InvoiceListView.xaml                   -- Page 1 : Liste factures
│   ├── InvoiceListView.xaml.cs
│   ├── InvoiceFormView.xaml                   -- Page 2 : Saisie facture
│   ├── InvoiceFormView.xaml.cs
│   ├── ConformityCheckView.xaml               -- Page 3 : Vérification conformité
│   ├── ConformityCheckView.xaml.cs
│   ├── PaymentFormView.xaml                   -- Page 4 : Enregistrement règlement
│   ├── PaymentFormView.xaml.cs
│   ├── PaymentHistoryView.xaml                -- Page 5 : Suivi & historique
│   └── PaymentHistoryView.xaml.cs
│
├── ViewModels/
│   ├── InvoiceListViewModel.cs
│   ├── InvoiceFormViewModel.cs
│   ├── ConformityCheckViewModel.cs
│   ├── PaymentFormViewModel.cs
│   └── PaymentHistoryViewModel.cs
│
├── Models/
│   ├── Facture.cs                             -- NOUVEAU
│   ├── FactureDetail.cs                       -- NOUVEAU
│   ├── Reglement.cs                           -- NOUVEAU
│   ├── BonLivraison.cs                        -- NOUVEAU
│   ├── BlDetail.cs                            -- NOUVEAU
│   └── ConformityResult.cs                   -- NOUVEAU (modèle d'écart)
│
├── Services/
│   ├── DatabaseService.cs                     -- MODIFIÉ (ajout CRUD Factures/Reglements/BL)
│   ├── ConformityService.cs                   -- NOUVEAU (logique vérification BC/BL/Facture)
│   ├── PaymentReceiptService.cs               -- NOUVEAU (génération reçu PDF)
│   ├── FileStorageService.cs                  -- NOUVEAU (upload/archivage preuves)
│   └── ExportService.cs                       -- NOUVEAU (export Excel historique)
│
├── Helpers/
│   ├── ViewModelBase.cs
│   ├── RelayCommand.cs
│   ├── CurrencyConverter.cs
│   └── FileTypeHelper.cs                      -- NOUVEAU (validation types fichiers)
│
├── Documents/                                 -- Dossier de stockage local
│   ├── Preuves/                               -- Preuves de paiement uploadées
│   │   └── {AnnéeMois}/{FournisseurId}/
│   └── Recus/                                 -- Reçus PDF générés
│       └── {AnnéeMois}/
│
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs
```

---

## 🎨 STYLE & DESIGN

**Couleurs** :
- 🟢 Payée / Conforme    : #4CAF50
- 🔵 Vérifiée            : #2196F3
- 🟡 En attente          : #FFC107
- 🟠 Partiellement payée : #FF9800
- 🔴 Rejetée / Retard    : #F44336
- ⚪ Non vérifiée        : #9E9E9E

**Police** : Segoe UI 11pt (normal), 12pt (titre)

**Icônes** : Unicode/Emoji + FontAwesome

**Responsive** : Min 1280x800

---

## ✅ CHECKLIST DE DÉVELOPPEMENT

### PHASE 1 - MODELS & DATABASE
- [ ] Créer classes Models (Facture, FactureDetail, Reglement, BonLivraison, BlDetail, ConformityResult)
- [ ] Créer tables BDD SQL Server (Factures, FactureDetails, Reglements, BonsLivraison, BlDetails)
- [ ] Créer index de performance
- [ ] Étendre DatabaseService (CRUD Factures, Reglements, BonsLivraison)

### PHASE 2 - PAGE 1 (Liste Factures)
- [ ] InvoiceListView.xaml
- [ ] InvoiceListView.xaml.cs
- [ ] InvoiceListViewModel.cs
- [ ] Tests : Affiche liste, filtres, statuts colorés

### PHASE 3 - PAGE 2 (Saisie Facture)
- [ ] InvoiceFormView.xaml
- [ ] InvoiceFormView.xaml.cs
- [ ] InvoiceFormViewModel.cs
- [ ] Tests : Saisie facture, pré-remplissage articles depuis BC, calcul TVA automatique

### PHASE 4 - PAGE 3 (Vérification Conformité)
- [ ] ConformityCheckView.xaml
- [ ] ConformityCheckView.xaml.cs
- [ ] ConformityCheckViewModel.cs
- [ ] ConformityService.cs (algorithme comparaison BC/BL/Facture)
- [ ] Tests : Détection écarts, validation, rapport conformité

### PHASE 5 - PAGE 4 (Enregistrement Règlement)
- [ ] PaymentFormView.xaml
- [ ] PaymentFormView.xaml.cs
- [ ] PaymentFormViewModel.cs
- [ ] FileStorageService.cs (upload + archivage preuve)
- [ ] PaymentReceiptService.cs (génération reçu PDF via wkhtmltopdf)
- [ ] Tests : Upload fichier, aperçu, génération PDF reçu

### PHASE 6 - PAGE 5 (Suivi & Historique)
- [ ] PaymentHistoryView.xaml
- [ ] PaymentHistoryView.xaml.cs
- [ ] PaymentHistoryViewModel.cs
- [ ] ExportService.cs (export Excel)
- [ ] Tests : Filtres, KPIs, export, ouverture preuves/reçus

### PHASE 7 - INTÉGRATION & POLISH
- [ ] Navigation MainWindow (ajout menu Comptable)
- [ ] Gestion des erreurs (exceptions, messages utilisateur)
- [ ] Validation des données (formulaires)
- [ ] Tests d'intégration complets (flux facture → vérification → paiement)

---

## 🚀 QUICK START

1. Créer les Models (Facture, Reglement, BonLivraison...)
2. Créer les tables SQL Server + index
3. Étendre DatabaseService (CRUD)
4. Créer ConformityService (logique métier)
5. Créer les 5 Pages WPF + ViewModels
6. Créer FileStorageService + PaymentReceiptService (PDF)
7. Intégrer dans MainWindow (menu Comptable)
8. Tester chaque page individuellement
9. Tester le flux complet : Facture → Conformité → Règlement → Reçu

---

## ⚠️ RÈGLES MÉTIER STRICTES

✅ Le Comptable PEUT :
- Saisir et modifier les factures fournisseurs
- Vérifier la conformité BC / BL / Facture
- Valider ou rejeter une facture (avec justification)
- Enregistrer les règlements fournisseurs
- Uploader les preuves de paiement
- Générer les reçus de paiement PDF
- Consulter l'historique des paiements et les soldes fournisseurs
- Exporter les données en Excel

❌ Le Comptable NE PEUT PAS :
- Créer ou modifier les Bons de Commande (c'est le Responsable d'Achat)
- Créer ou modifier les Bons de Livraison (c'est le Magasinier)
- Modifier les articles ou le catalogue produits
- Approuver les commandes (c'est le Directeur)
- Modifier les prix des devis

---

## 🔗 FLUX MÉTIER COMPLET

```
Magasinier          Resp. Achat          Comptable
    │                    │                    │
    │ Crée BL            │                    │
    │ (Bon Livraison)    │                    │
    │                    │ Crée BC            │
    │                    │ (Bon Commande)     │
    │                    │                    │
    │                    │                    │ ← Reçoit facture fournisseur
    │                    │                    │
    │                    │                    │ Saisit la facture (Page 2)
    │                    │                    │ Associe BC + BL
    │                    │                    │
    │                    │                    │ Vérifie conformité (Page 3)
    │                    │                    │ BC ↔ BL ↔ Facture
    │                    │                    │
    │                    │                    │ Enregistre règlement (Page 4)
    │                    │                    │ Upload preuve de paiement
    │                    │                    │ Génère reçu PDF
    │                    │                    │
    │                    │                    │ Suivi & archivage (Page 5)
```

---

**STATUS** : ✅ Prêt pour développement immédiat

**TEMPS ESTIMÉ** : 50-70 heures (6-9 jours)

**LIVRABLES** : Code WPF complet + BDD SQL Server + Services (Conformité, PDF, Upload, Export)
