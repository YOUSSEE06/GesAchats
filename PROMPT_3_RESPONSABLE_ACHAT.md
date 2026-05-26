# 🚀 PROMPT 3 - ARCHITECTURE COMPLÈTE RESPONSABLE D'ACHAT

## 📌 CONTEXTE & CLARIFICATIONS

**Application** : GesAchats v2.0  
**Framework** : C# WPF (Windows Presentation Foundation)  
**Client** : Entreprise de construction de bâtiments  
**Rôle** : Responsable des Achats (Gestion des commandes et fournisseurs)  
**Status** : Développement du module Responsable d'Achat

---

## 🎯 LE RESPONSABLE D'ACHAT EN 60 SECONDES

Le Responsable d'Achat est responsable de :

1. **📋 RECEVOIR & ANALYSER** les listes de besoins du Magasinier
   - Consulter les besoins transmis
   - Analyser les quantités demandées
   - Historique des besoins précédents

2. **💰 CONSULTER** l'historique des prix
   - Voir les prix par produit et par fournisseur
   - Comparer les tarifs historiques
   - Analyser les tendances de prix

3. **📧 PRÉPARER & COMPARER** les devis fournisseurs
   - Créer des demandes de devis (RFQ - Request For Quote)
   - Consulter les devis reçus
   - Comparer les devis côte à côte
   - Analyser les délais et conditions

4. **🏆 SÉLECTIONNER** le fournisseur le plus adapté
   - Évaluer qualité + prix + délai
   - Choisir le meilleur fournisseur
   - Justifier le choix

5. **📝 CRÉER** les Bons de Commande
   - Émettre les BC aux fournisseurs sélectionnés
   - Ajouter les détails (prix, quantité, délai)
   - Générer et envoyer les documents
   - Suivre l'état des commandes

---

## 📄 LES 6 PAGES À CRÉER

### ╔═ PAGE 1 : LISTE DES BESOINS REÇUS

**Chemin** : `Views/ReceivedNeedsView.xaml` + `.xaml.cs`

**Objectif** : Afficher les listes de besoins transmises par le Magasinier

**Éléments** :

```
┌────────────────────────────────────────────────────┐
│ Espace Responsable Achats - Besoins Reçus         │
├────────────────────────────────────────────────────┤
│                                                     │
│  [🔄 Actualiser]  [📊 Statistiques]  [Aide]      │
│                                                     │
│  ┌────────────────────────────────────────────┐  │
│  │ Date │ N°Besoin │ Créé par │ Articles │   │  │
│  ├────────────────────────────────────────────┤  │
│  │10/01 │ BES-001  │Jean Stock│    5 (1 ✨)  │  │
│  │09/01 │ BES-002  │Jean Stock│    3        │  │
│  │08/01 │ BES-003  │Jean Stock│    2        │  │
│  └────────────────────────────────────────────┘  │
│                                                     │
│  [Voir détails]  [Créer devis]                    │
│                                                     │
└────────────────────────────────────────────────────┘
```

**Colonnes du tableau** :
- DATE (date de création de la liste)
- N°BESOIN (identifiant unique)
- CRÉÉ PAR (nom du Magasinier)
- ARTICLES (nombre d'articles)
- **✨ NOUVEAUX PRODUITS** (nombre de produits nouveaux) 🆕
- STATUT (En attente / En cours / Traité)
- ACTION (Voir détails / Créer devis)

**Fonctionnalités** :
- 🔄 Bouton "Actualiser" → Rafraîchit la liste
- 👁️ Bouton "Voir détails" → Affiche popup avec articles et quantités
- 📊 Bouton "Statistiques" → Affiche stats sur les besoins
- ➕ Bouton "Créer devis" → Navigue à Page 2

**Données sources** : `BDD - Table BesoinsApprov + BesoinDetails`

---

### ╔═ PAGE 2 : CRÉATION & GESTION DES DEVIS

**Chemin** : `Views/QuotesManagementView.xaml` + `.xaml.cs`

**Objectif** : Créer et gérer les demandes de devis aux fournisseurs

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Gestion des Devis                                │
├──────────────────────────────────────────────────┤
│                                                   │
│ ┌──── CRÉER UN DEVIS ────────────────────────┐  │
│ │                                              │  │
│ │ Besoin sélectionné: BES-001 (5 articles)   │  │
│ │ Date: [10/01/2024]                         │  │
│ │                                              │  │
│ │ Articles à devis:                          │  │
│ │ ┌──────────────────────────────────────┐  │  │
│ │ │☑ │ Graviers 40m│ 50 | m3 │          │  │  │
│ │ │☑ │ Ciment 35kg │ 10 | sac│          │  │  │
│ │ │☐ │ Acier HA8   │ 20 | bar│          │  │  │
│ │ └──────────────────────────────────────┘  │  │
│ │                                              │  │
│ │ Fournisseurs à qui demander un devis:     │  │
│ │ ☑ Société Matériaux SA                     │  │
│ │ ☑ Cimenterie du Nord                       │  │
│ │ ☑ Aciers Modernes                          │  │
│ │ ☑ Fournisseur Local                        │  │
│ │                                              │  │
│ │ [Créer et Envoyer Devis]                   │  │
│ │                                              │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
│ ┌──── DEVIS EXISTANTS ───────────────────────┐  │
│ │ Besoin │ N°Devis │ Fournisseur │ Date │   │  │
│ │ BES-001│ DEV-001 │ Soc. Mat.   │ 10/01│   │  │
│ │ BES-001│ DEV-002 │ Cimenterie  │ 10/01│   │  │
│ │ BES-002│ DEV-003 │ Aciers Mod. │ 09/01│   │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
│ [Voir détails]  [Comparer]  [Supprimer]        │
│                                                   │
└──────────────────────────────────────────────────┘
```

**Formulaire de création** :
- **Besoin sélectionné** (dropdown) - Sélectionner une liste de besoins
- **Date** (DatePicker) - Date de création du devis
- **Articles à devis** (checkbox) - Sélectionner les articles du besoin
- **Fournisseurs** (checkbox list) - Sélectionner les fournisseurs
- Bouton "Créer et Envoyer Devis"

**Tableau des devis existants** :
- N°DEVIS (identifiant unique)
- BESOIN (référence au besoin)
- FOURNISSEUR
- DATE (date création)
- STATUT (Envoyé / Réponse reçue / Comparé / Rejeté)
- ACTION (Voir détails / Comparer / Supprimer)

**Fonctionnalités** :
- Sélectionner un besoin → Affiche articles
- Sélectionner articles et fournisseurs
- Créer et envoyer devis → Génère documents PDF (à imprimer/envoyer)
- Tableau récapitulatif des devis
- Double-clic → Affiche détails (articles, prix proposés, délai, conditions)
- Bouton "Comparer" → Navigue à Page 3

**Données sources** : 
- `BDD - Table BesoinsApprov + BesoinDetails`
- `BDD - Table Fournisseurs` (à créer)

**Données créées** : 
- `BDD - Table Devis`

---

### ╔═ PAGE 3 : COMPARAISON DES DEVIS

**Chemin** : `Views/QuoteComparisonView.xaml` + `.xaml.cs`

**Objectif** : Comparer les devis côte à côte et sélectionner le meilleur

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Comparaison des Devis                            │
├──────────────────────────────────────────────────┤
│                                                   │
│ Besoin: BES-001 (5 articles)                     │
│ Critères de comparaison:                         │
│ ☑ Prix Total  ☑ Délai  ☑ Qualité  ☑ Conditions│
│                                                   │
│ ┌─ ARTICLE: Graviers 40mm (Qty: 50 m³) ──────┐│
│ │                                              ││
│ │ Fournisseur │ PU HT  │ Total HT │ Délai │  ││
│ ├─────────────┼────────┼──────────┼──────┤  ││
│ │ Soc. Mat.   │ 25.50€ │1275.00€  │ 3j   │🥇││
│ │ Cimenterie  │ 26.00€ │1300.00€  │ 5j   │  ││
│ │ Aciers Mod. │ 27.00€ │1350.00€  │ 2j   │  ││
│ └─────────────┴────────┴──────────┴──────┘  ││
│                                              │
│ ┌─ ARTICLE: Ciment 35kg (Qty: 10 sacs) ─────┐│
│ │                                              ││
│ │ Fournisseur │ PU HT  │ Total HT │ Délai │  ││
│ ├─────────────┼────────┼──────────┼──────┤  ││
│ │ Soc. Mat.   │ 8.50€  │ 85.00€   │ 3j   │  ││
│ │ Cimenterie  │ 8.00€  │ 80.00€   │ 5j   │🥇││
│ │ Aciers Mod. │ 8.75€  │ 87.50€   │ 2j   │  ││
│ └─────────────┴────────┴──────────┴──────┘  ││
│                                              │
│ ┌─ RÉSUMÉ GLOBAL ───────────────────────────┐│
│ │                                              ││
│ │ Fournisseur │ Total TTC │ Délai moy │ Score││
│ ├─────────────┼───────────┼──────────┼──────┤ │
│ │ Soc. Mat.   │ 1634.50€  │ 3j       │ 8/10 │ │
│ │ Cimenterie  │ 1596.50€  │ 5j       │ 7/10 │ │
│ │ Aciers Mod. │ 1710.00€  │ 2j       │ 6/10 │ │
│ └─────────────┴───────────┴──────────┴──────┘ │
│                                              │
│ Fournisseur recommandé: Société Matériaux   │
│ Justification: Meilleur prix + délai raisonnable│
│                                              │
│ [Créer BC pour ce fournisseur] ✓             │
│ [Voir autres options]  [Retour]             │
│                                              │
└──────────────────────────────────────────────┘
```

**Tableau de comparaison** :
- Par article, afficher tous les fournisseurs avec :
  - PU HT (Prix Unitaire Hors Taxe)
  - TOTAL HT (Quantité × PU HT)
  - DÉLAI (délai de livraison)
  - QUALITÉ (notation - étoiles)
  - CONDITIONS (paiement, retour, etc.)

**Résumé global** :
- Comparaison complète par fournisseur
- TOTAL TTC (prix final)
- DÉLAI MOYEN
- SCORE (calcul automatique basé sur critères)

**Fonctionnalités** :
- Sélectionner critères de comparaison
- Filtrer par fournisseur ou article
- Tri par prix, délai, score
- Recommandation automatique du meilleur fournisseur
- Bouton "Créer BC" → Navigue à Page 4
- Bouton "Voir autres options" → Pour consulter tous les devis

**Données sources** : 
- `BDD - Table Devis`
- `BDD - Table Fournisseurs`

---

### ╔═ PAGE 4 : HISTORIQUE DES ACHATS

**Chemin** : `Views/PurchaseHistoryView.xaml` + `.xaml.cs`

**Objectif** : Consulter l'historique d'achat par produit, puis détails par produit/fournisseur

**Architecture en 2 niveaux** :
1. **Niveau 1** : Liste des produits avec bouton d'accès
2. **Niveau 2** : Historique détaillé d'un produit (toutes les commandes)

---

## 📊 **NIVEAU 1 : LISTE DES PRODUITS**

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Historique des Achats - Produits                 │
├──────────────────────────────────────────────────┤
│                                                   │
│ [🔍 Recherche produit...]  [Tri: A-Z ▼]         │
│                                                   │
│ ┌────────────────────────────────────────────┐  │
│ │ Article         │ Nb Achats │ Fournisseurs │ │
│ ├────────────────────────────────────────────┤  │
│ │ Graviers 40mm   │    12     │      3       │  │
│ │ [👁️ Voir historique]                       │  │
│ │                                              │  │
│ │ Ciment 35kg     │     8     │      2       │  │
│ │ [👁️ Voir historique]                       │  │
│ │                                              │  │
│ │ Acier HA8       │    15     │      4       │  │
│ │ [👁️ Voir historique]                       │  │
│ │                                              │  │
│ │ Sable fin       │     5     │      1       │  │
│ │ [👁️ Voir historique]                       │  │
│ │                                              │  │
│ └────────────────────────────────────────────┘  │
│                                                   │
│ Affichage: 4 produits | Total achats: 40        │
│                                                   │
└──────────────────────────────────────────────────┘
```

**Tableau des produits** :
- **ARTICLE** (Désignation du produit)
- **NB ACHATS** (nombre total d'achats pour ce produit)
- **FOURNISSEURS** (nombre de fournisseurs différents)
- **ACTION** (Bouton "👁️ Voir historique")

**Filtres & Tri** :
- 🔍 Recherche par désignation
- 📊 Tri (A-Z / Z-A / Par nombre d'achats / Par nombre de fournisseurs)
- 📅 Filtre par date (derniers 3 mois, 6 mois, 1 an, tout)

**Fonctionnalités** :
- Affichage synthétique et claire
- Navigation rapide vers détails
- Résumé en bas (nombre produits, total achats)

---

## 📈 **NIVEAU 2 : HISTORIQUE D'UN PRODUIT**

**Quand on clique "👁️ Voir historique", une nouvelle fenêtre/page s'affiche** :

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Historique d'Achat: Graviers 40mm                │
├──────────────────────────────────────────────────┤
│                                                   │
│ ← Retour  |  Total achats: 12  |  Fournisseurs: 3│
│                                                   │
│ [Filtre Fournisseur: Tous ▼] [Date: Tout ▼]    │
│                                                   │
│ ┌────────────────────────────────────────────┐  │
│ │ Date  │ N°BC    │ Fournisseur    │ PU HT  │  │
│ ├────────────────────────────────────────────┤  │
│ │10/01  │ BC-2024-001 │ Soc. Mat. │25.50€  │  │
│ │05/01  │ BC-2024-002 │ Cimenterie│26.00€  │  │
│ │01/01  │ BC-2024-003 │ Soc. Mat. │24.80€  │  │
│ │15/12  │ BC-2024-004 │ Aciers Mod│27.00€  │  │
│ │10/12  │ BC-2024-005 │ Soc. Mat. │25.30€  │  │
│ │05/12  │ BC-2024-006 │ Cimenterie│26.50€  │  │
│ │... (autres achats)                        │  │
│ └────────────────────────────────────────────┘  │
│                                                   │
│ ┌─ ANALYSE PAR FOURNISSEUR ──────────────────┐ │
│ │                                              │ │
│ │ Société Matériaux SA:                      │ │
│ │   • Nombre d'achats: 5                     │ │
│ │   • Prix moyen: 25.18€                     │ │
│ │   • Prix min: 24.80€ | Prix max: 25.50€   │ │
│ │   • Tendance: Stable                       │ │
│ │                                              │ │
│ │ Cimenterie du Nord:                        │ │
│ │   • Nombre d'achats: 4                     │ │
│ │   • Prix moyen: 26.12€                     │ │
│ │   • Prix min: 26.00€ | Prix max: 26.50€   │ │
│ │   • Tendance: ↑ Légère hausse              │ │
│ │                                              │ │
│ │ Aciers Modernes:                           │ │
│ │   • Nombre d'achats: 3                     │ │
│ │   • Prix moyen: 27.33€                     │ │
│ │   • Prix min: 27.00€ | Prix max: 27.75€   │ │
│ │   • Tendance: ↑ Hausse                     │ │
│ │                                              │ │
│ └─────────────────────────────────────────────┘ │
│                                                   │
│ 🏆 RECOMMANDATION:                              │
│    Meilleur fournisseur: Société Matériaux     │
│    (Prix le plus bas + stable)                 │
│                                                   │
│ [Graphique prix] [Exporter CSV] [Imprimer]    │
│                                                   │
└──────────────────────────────────────────────────┘
```

**En-tête** :
- Nom du produit
- Nombre total d'achats
- Nombre de fournisseurs différents
- Bouton "← Retour" (revenir à la liste)

**Tableau historique** :
- **DATE** (date de la commande)
- **N°BC** (Bon de Commande - clickable)
- **FOURNISSEUR** (nom du fournisseur)
- **QUANTITÉ** (quantité commandée)
- **UNITÉ**
- **PU HT** (Prix Unitaire Hors Taxe)
- **TOTAL HT** (Quantité × PU HT)
- **DÉLAI** (délai de livraison)
- **ACTIONS** (Voir BC / Contacter fournisseur)

**Filtres** :
- Par fournisseur (dropdown) - pour voir l'historique avec un seul fournisseur
- Par date (dropdown) - derniers 3/6/12 mois ou tout

**Section Analyse par Fournisseur** :
Pour chaque fournisseur ayant fourni ce produit :
- Nombre d'achats
- Prix moyen
- Prix minimum (avec date)
- Prix maximum (avec date)
- Tendance prix (stable / hausse / baisse)
- **Dernier prix connu**

**Recommandation** :
- Affiche le meilleur fournisseur basé sur :
  - Prix le plus bas
  - Stabilité des prix
  - Historique d'achats

**Graphique** :
- Courbe des prix par fournisseur (couleurs différentes)
- Axe X : Dates
- Axe Y : Prix

**Fonctionnalités** :
- Filtres pour chercher rapidement
- Double-clic sur une ligne BC → Affiche détails du BC
- Bouton "Contacter fournisseur" → Compose email
- Analyse automatique par fournisseur
- Graphique interactif
- Export CSV (pour Excel)
- Impression

**Données sources** : 
- `BDD - Table BonsCommande`
- `BDD - Table BcDetails`
- `BDD - Table Fournisseurs`
- `BDD - Table Articles`

---

### ╔═ PAGE 5 : CRÉATION DES BONS DE COMMANDE

**Chemin** : `Views/CreatePurchaseOrderView.xaml` + `.xaml.cs`

**Objectif** : Créer et émettre les Bons de Commande

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Créer un Bon de Commande                         │
├──────────────────────────────────────────────────┤
│                                                   │
│ ┌─ INFORMATIONS BC ─────────────────────────┐  │
│ │                                              │  │
│ │ N°BC (Auto-généré): BC-2024-001            │  │
│ │ Date BC: [10/01/2024]                      │  │
│ │ Fournisseur *: [Société Matériaux] ▼       │  │
│ │ Besoin d'origine: BES-001                  │  │
│ │                                              │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
│ ┌─ ARTICLES ────────────────────────────────┐  │
│ │                                              │  │
│ │ Désignation │ Qty │ PU HT │ PU TTC │Total │ │
│ ├──────────────────────────────────────────┤  │
│ │ Graviers 40m│ 50  │ 25.50 │ 30.60  │1530 │  │
│ │ Ciment 35kg │ 10  │ 8.50  │ 10.20  │ 102 │  │
│ │ Acier HA8   │ 20  │ 4.50  │ 5.40   │ 108 │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
│ ┌─ RÉSUMÉ FINANCIER ───────────────────────┐  │
│ │                                              │  │
│ │ Total HT: 1212.50€                         │  │
│ │ Montant TVA (20%): 242.50€                 │  │
│ │ Total TTC: 1455.00€                        │  │
│ │                                              │  │
│ │ Conditions de paiement:                    │  │
│ │ [30 jours ▼]  (Net 30)                    │  │
│ │                                              │  │
│ │ Délai de livraison demandé:                │  │
│ │ [3 jours ▼]                               │  │
│ │                                              │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
│ ┌─ OBSERVATIONS ────────────────────────────┐  │
│ │ [Livraison sur le chantier principal...]  │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
│ ✓ Aperçu BC (PDF)                             │  │
│                                                   │
│ [Brouillon] [Émettre et Envoyer] [Annuler]    │  │
│                                                   │
└──────────────────────────────────────────────────┘
```

**Formulaire de création** :
- **N°BC** (auto-généré, lecture seule) - BC-2024-001
- **Date BC** (DatePicker) - Date d'émission
- **Fournisseur** (dropdown) *obligatoire* - Sélectionner le fournisseur
- **Besoin d'origine** (lecture seule) - Référence au besoin

**Tableau des articles** :
- DÉSIGNATION
- QUANTITÉ
- PU HT (Prix Unitaire Hors Taxe)
- PU TTC (Prix Unitaire Toutes Taxes Comprises)
- TOTAL (Qty × PU TTC)
- Actions (Modifier / Supprimer)

**Résumé financier** :
- Total HT
- Montant TVA
- Total TTC
- Conditions de paiement (dropdown)
- Délai de livraison (dropdown)

**Observations** (TextBox) :
- Notes libres (livraison, instructions spéciales, etc.)

**Fonctionnalités** :
- Auto-génération du N°BC
- Calcul automatique des montants
- Aperçu PDF du BC avant envoi
- Bouton "Brouillon" → Sauvegarde sans émettre
- Bouton "Émettre et Envoyer" → Crée le BC, génère PDF, notification au fournisseur
- Bouton "Annuler" → Retour

**Données créées** : 
- `BDD - Table BonsCommande`

---

### ╔═ PAGE 6 : SUIVI DES COMMANDES

**Chemin** : `Views/OrderTrackingView.xaml` + `.xaml.cs`

**Objectif** : Suivre l'état des commandes et les livraisons

**Éléments** :

```
┌──────────────────────────────────────────────────┐
│ Suivi des Commandes                              │
├──────────────────────────────────────────────────┤
│                                                   │
│ [Statut: Tous ▼] [Fournisseur: Tous ▼]          │
│ [Date: Cette semaine ▼]                         │
│                                                   │
│ ┌────────────────────────────────────────────┐  │
│ │ N°BC │ Date │ Fournisseur │ Total │ État  │  │
│ ├────────────────────────────────────────────┤  │
│ │BC-001│10/01 │ Soc. Mat.   │1530€ │ 🟢Émis│  │
│ │BC-002│09/01 │ Cimenterie  │ 102€ │ 🟡Pdt.│  │
│ │BC-003│08/01 │ Aciers Mod. │ 108€ │ 🟡Pdt.│  │
│ │BC-004│05/01 │ Soc. Mat.   │1200€ │ 🟢Livré│ │
│ └────────────────────────────────────────────┘  │
│                                                   │
│ ┌─ DÉTAILS BC-001 ──────────────────────────┐  │
│ │                                              │  │
│ │ Statut: Émis (envoyé le 10/01)             │  │
│ │                                              │  │
│ │ Timeline:                                  │  │
│ │ ✓ BC créé (10/01)                          │  │
│ │ ✓ BC envoyé (10/01)                        │  │
│ │ → En attente de réponse fournisseur...     │  │
│ │ □ Livraison prévue (13/01)                 │  │
│ │ □ Facture reçue                            │  │
│ │ □ Paiement effectué                        │  │
│ │                                              │  │
│ │ Fournisseur: Société Matériaux SA          │  │
│ │ Contact: contact@societemat.com            │  │
│ │ Total: 1530.00€                            │  │
│ │                                              │  │
│ │ [Contacter fournisseur] [Voir BL reçus]   │  │
│ │                                              │  │
│ └──────────────────────────────────────────┘  │
│                                                   │
└──────────────────────────────────────────────────┘
```

**Filtres** :
- Statut (Tous / Brouillon / Émis / Partiellement livré / Livré / Annulé)
- Fournisseur
- Période (Cette semaine / Ce mois / 3 derniers mois / Personnalisé)

**Tableau de suivi** :
- N°BC
- DATE (date création)
- FOURNISSEUR
- TOTAL TTC
- ÉTAT (avec code couleur)
  - ⚪ Brouillon
  - 🔵 Émis
  - 🟡 Partiellement livré
  - 🟢 Livré
  - ⛔ Annulé

**Section détails** :
- Timeline/progression du BC
- Étapes complétées (checkmark)
- Étapes restantes (à venir)
- Informations fournisseur (contact)
- Actions (Contacter / Voir BL / Annuler)

**Fonctionnalités** :
- Filtres pour chercher rapidement
- Double-clic → Affiche détails
- Timeline visuelle
- Bouton "Contacter fournisseur" → Compose email
- Bouton "Voir BL reçus" → Affiche bons de livraison liés
- Export liste (CSV/PDF)

**Données sources** : 
- `BDD - Table BonsCommande`
- `BDD - Table BonsLivraison` (lien)

---

## 🗄️ SCHÉMA BASE DE DONNÉES (SQL)

```sql
-- 1. TABLE FOURNISSEURS (Données maître)
CREATE TABLE Fournisseurs (
    FournisseurId INT PRIMARY KEY AUTO_INCREMENT,
    Nom NVARCHAR(255) NOT NULL,
    Contact NVARCHAR(255),
    Email NVARCHAR(255),
    Telephone NVARCHAR(20),
    Adresse NVARCHAR(500),
    Ville NVARCHAR(100),
    CodePostal NVARCHAR(10),
    Pays NVARCHAR(100),
    ConditionsPaiement NVARCHAR(100), -- Net 30, Net 60, Immédiat, etc.
    DelaiMoyenLivraison INT, -- en jours
    Notation DECIMAL(3, 1), -- de 0 à 5 (étoiles)
    Actif BIT DEFAULT 1,
    DateCreation DATETIME DEFAULT GETDATE(),
    DateMaj DATETIME DEFAULT GETDATE()
);

-- 2. TABLE DEVIS (Demandes de prix)
CREATE TABLE Devis (
    DevisId INT PRIMARY KEY AUTO_INCREMENT,
    NumeroDevis NVARCHAR(50) UNIQUE NOT NULL, -- DEV-2024-001
    Date DATETIME DEFAULT GETDATE(),
    BesoinId INT NOT NULL, -- Référence au besoin
    FournisseurId INT NOT NULL,
    Statut NVARCHAR(50) DEFAULT 'Envoyé', -- Envoyé / Réponse reçue / Accepté / Rejeté
    DateReponse DATETIME NULL,
    Observations NVARCHAR(MAX),
    CreePar NVARCHAR(255),
    FOREIGN KEY (BesoinId) REFERENCES BesoinsApprov(BesoinId),
    FOREIGN KEY (FournisseurId) REFERENCES Fournisseurs(FournisseurId)
);

-- 3. TABLE DEVIS DETAILS (Articles dans le devis)
CREATE TABLE DevisDetails (
    DetailId INT PRIMARY KEY AUTO_INCREMENT,
    DevisId INT NOT NULL,
    ArticleId INT NOT NULL,
    Quantite INT NOT NULL,
    PuHt DECIMAL(10, 2), -- Prix Unitaire Hors Taxe devisé
    PuTtc DECIMAL(10, 2), -- Prix Unitaire TTC devisé
    DelaiLivraison INT, -- en jours
    FOREIGN KEY (DevisId) REFERENCES Devis(DevisId),
    FOREIGN KEY (ArticleId) REFERENCES Articles(ArticleId)
);

-- 4. TABLE BONS COMMANDE (Modifiée)
CREATE TABLE BonsCommande (
    BcId INT PRIMARY KEY AUTO_INCREMENT,
    NumeroBc NVARCHAR(50) UNIQUE NOT NULL, -- BC-2024-001
    Date DATETIME DEFAULT GETDATE(),
    FournisseurId INT NOT NULL, -- Clé étrangère vers Fournisseurs
    BesoinId INT, -- Référence au besoin d'origine
    DevisId INT, -- Référence au devis sélectionné
    Statut NVARCHAR(50) DEFAULT 'Brouillon', -- Brouillon/Émis/Partiellement livré/Livré
    ConditionsPaiement NVARCHAR(100),
    DelaiLivraisonDemande INT, -- en jours
    Observations NVARCHAR(MAX),
    FichierPdf NVARCHAR(MAX),
    CreePar NVARCHAR(255),
    DateMaj DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (FournisseurId) REFERENCES Fournisseurs(FournisseurId),
    FOREIGN KEY (BesoinId) REFERENCES BesoinsApprov(BesoinId),
    FOREIGN KEY (DevisId) REFERENCES Devis(DevisId)
);

-- 5. TABLE BC DETAILS (Articles commandés)
CREATE TABLE BcDetails (
    DetailId INT PRIMARY KEY AUTO_INCREMENT,
    BcId INT NOT NULL,
    ArticleId INT NOT NULL,
    Quantite INT NOT NULL,
    PuHt DECIMAL(10, 2), -- Prix Unitaire Hors Taxe commandé
    PuTtc DECIMAL(10, 2), -- Prix Unitaire TTC commandé
    FOREIGN KEY (BcId) REFERENCES BonsCommande(BcId),
    FOREIGN KEY (ArticleId) REFERENCES Articles(ArticleId)
);

-- 6. INDEX POUR PERFORMANCE
CREATE INDEX idx_Devis_BesoinId ON Devis(BesoinId);
CREATE INDEX idx_Devis_FournisseurId ON Devis(FournisseurId);
CREATE INDEX idx_Devis_Statut ON Devis(Statut);
CREATE INDEX idx_BonsCommande_FournisseurId ON BonsCommande(FournisseurId);
CREATE INDEX idx_BonsCommande_BesoinId ON BonsCommande(BesoinId);
CREATE INDEX idx_BonsCommande_DevisId ON BonsCommande(DevisId);
CREATE INDEX idx_BonsCommande_Statut ON BonsCommande(Statut);
CREATE INDEX idx_Fournisseurs_Actif ON Fournisseurs(Actif);
```

---

## 🏗️ STRUCTURE DU PROJET C# WPF

```
GesAchats/
├── Views/
│   ├── ReceivedNeedsView.xaml
│   ├── ReceivedNeedsView.xaml.cs
│   ├── QuotesManagementView.xaml
│   ├── QuotesManagementView.xaml.cs
│   ├── QuoteComparisonView.xaml
│   ├── QuoteComparisonView.xaml.cs
│   ├── PurchaseHistoryView.xaml (RENOMMÉ de PriceHistoryView) 🔄
│   ├── PurchaseHistoryView.xaml.cs (RENOMMÉ) 🔄
│   ├── ProductPurchaseHistoryWindow.xaml (NOUVEAU - Dialog) 🆕
│   ├── ProductPurchaseHistoryWindow.xaml.cs (NOUVEAU) 🆕
│   ├── CreatePurchaseOrderView.xaml
│   ├── CreatePurchaseOrderView.xaml.cs
│   ├── OrderTrackingView.xaml
│   └── OrderTrackingView.xaml.cs
│
├── ViewModels/
│   ├── ReceivedNeedsViewModel.cs
│   ├── QuotesManagementViewModel.cs
│   ├── QuoteComparisonViewModel.cs
│   ├── PurchaseHistoryViewModel.cs (RENOMMÉ de PriceHistoryViewModel) 🔄
│   ├── ProductPurchaseHistoryViewModel.cs (NOUVEAU) 🆕
│   ├── CreatePurchaseOrderViewModel.cs
│   └── OrderTrackingViewModel.cs
│
├── Models/
│   ├── Fournisseur.cs (NOUVEAU)
│   ├── Devis.cs (NOUVEAU)
│   ├── DevisDetail.cs (NOUVEAU)
│   ├── BonCommande.cs (MODIFIÉ)
│   └── BcDetail.cs (NOUVEAU)
│
├── Services/
│   ├── DatabaseService.cs (MODIFIÉ - Ajout CRUD Devis/Fournisseurs)
│   ├── PdfGeneratorService.cs (NOUVEAU - Génération PDF BC)
│   ├── EmailService.cs (NOUVEAU - Envoi devis/BC par email)
│   ├── ComparativeAnalysisService.cs (NOUVEAU - Comparaison devis)
│   └── PriceAnalysisService.cs (NOUVEAU - Analyse historique prix)
│
├── Helpers/
│   ├── ViewModelBase.cs
│   ├── RelayCommand.cs
│   └── CurrencyConverter.cs (NOUVEAU - Format monétaire)
│
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── App.xaml
└── App.xaml.cs
```

---

## 🎨 STYLE & DESIGN

**Couleurs** :
- 🔵 Info: #2196F3
- 🟢 Validé/Livré: #4CAF50
- 🟡 En attente: #FFC107
- 🔴 Erreur: #F44336
- ⚪ Brouillon: #9E9E9E

**Police** : Segoe UI 11pt (normal), 12pt (titre)

**Icônes** : Unicode/Emoji + FontAwesome

**Responsive** : Min 1200x800

---

## ✅ CHECKLIST DE DÉVELOPPEMENT

### PHASE 1 - MODELS & DATABASE
- [ ] Créer classes Models (Fournisseur, Devis, BcDetail, etc.)
- [ ] Créer tables BDD (SQL)
- [ ] Modifier BonsCommande (ajouter FK vers Fournisseur, Devis)
- [ ] Créer DatabaseService (CRUD)

### PHASE 2 - PAGE 1 (Besoins Reçus)
- [ ] ReceivedNeedsView.xaml
- [ ] ReceivedNeedsView.xaml.cs
- [ ] ReceivedNeedsViewModel.cs
- [ ] Tests: Affiche besoins, voir détails

### PHASE 3 - PAGE 2 (Gestion Devis)
- [ ] QuotesManagementView.xaml
- [ ] QuotesManagementView.xaml.cs
- [ ] QuotesManagementViewModel.cs
- [ ] PdfGeneratorService.cs (génération PDF devis)
- [ ] EmailService.cs (envoi devis)
- [ ] Tests: Créer devis, sélectionner articles/fournisseurs

### PHASE 4 - PAGE 3 (Comparaison Devis)
- [ ] QuoteComparisonView.xaml
- [ ] QuoteComparisonView.xaml.cs
- [ ] QuoteComparisonViewModel.cs
- [ ] ComparativeAnalysisService.cs (scoring)
- [ ] Tests: Comparer devis, score automatique

### PHASE 5 - PAGE 4 (Historique Prix)
- [ ] PriceHistoryView.xaml
- [ ] PriceHistoryView.xaml.cs
- [ ] PriceHistoryViewModel.cs
- [ ] PriceAnalysisService.cs (tendance, stats)
- [ ] Tests: Filtres, graphiques, export

### PHASE 6 - PAGE 5 (Création BC)
- [ ] CreatePurchaseOrderView.xaml
- [ ] CreatePurchaseOrderView.xaml.cs
- [ ] CreatePurchaseOrderViewModel.cs
- [ ] Intégration PdfGeneratorService
- [ ] Tests: Création BC, calculs, aperçu PDF

### PHASE 7 - PAGE 6 (Suivi Commandes)
- [ ] OrderTrackingView.xaml
- [ ] OrderTrackingView.xaml.cs
- [ ] OrderTrackingViewModel.cs
- [ ] Tests: Filtres, timeline, actions

### PHASE 8 - INTÉGRATION & POLISH
- [ ] Navigation MainWindow
- [ ] Gestion erreurs
- [ ] Validation données
- [ ] Messages utilisateur
- [ ] Tests complets

---

## 🚀 QUICK START

1. Créer les models
2. Créer les tables SQL
3. Créer DatabaseService (CRUD)
4. Créer ViewModels
5. Créer les 6 Pages
6. Créer Services (PDF, Email, Analyse)
7. Intégrer MainWindow
8. Tester chaque page
9. Valider le flux complet

---

## ⚠️ RÈGLES MÉTIER STRICTES

✅ Le Responsable Achats PEUT :
- Consulter les besoins du Magasinier
- Créer des demandes de devis
- Comparer les devis
- Consulter l'historique des prix
- Créer les Bons de Commande
- Suivre les commandes
- Contacter les fournisseurs

❌ Le Responsable Achats NE PEUT PAS :
- Modifier les articles
- Recevoir les marchandises (c'est le Magasinier)
- Valider les factures (c'est le Comptable)
- Effectuer les paiements

---

**STATUS** : ✅ Prêt pour développement immédiat

**TEMPS ESTIMÉ** : 60-80 heures (7-10 jours)

**LIVRABLES** : Code WPF complet + BDD + Tests + Services (PDF, Email, Analyse)
