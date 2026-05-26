# CONTENU DES INTERFACES PAR RÔLE
## Système de Gestion des Achats & Approvisionnements

---

## Introduction

Ce document détaille les **interfaces spécifiques à chaque rôle utilisateur**. Chaque interface comprend les formulaires, tableaux de bord et listes nécessaires pour accomplir les tâches associées au rôle.

Le système compte **4 rôles principaux** :
1. **Technicien / Magasinier** - Gestion des stocks et réception
2. **Responsable des Achats** - Gestion des commandes et devis
3. **Comptable** - Gestion financière et paiements
4. **Directeur Administratif** - Supervision et configuration

---

## 1. TECHNICIEN / MAGASINIER

### Responsabilités
- Analyse des stocks et identification des besoins
- Établissement de la liste des besoins
- Réception des marchandises
- Saisie des bons de livraison
- Confirmation de la conformité quantités

### Interfaces principales

#### 1.1 Analyse des Stocks & Besoins
**Description** : Consultation du stock actuel, identification des articles manquants, création de la liste des besoins

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Liste des articles en stock (désignation, quantité actuelle, seuil minimum)<br/>• Formulaire de création de besoins (quantités à commander)<br/>• Bouton "Transmettre au responsable des achats"<br/>• Historique des besoins précédents |
| **Actions** | Consulter stocks • Ajouter/modifier besoins • Transmettre besoins • Voir historique |
| **Accès données** | Lecture: tous les stocks<br/>Écriture: liste des besoins uniquement |

#### 1.2 Réception & Bon de Livraison
**Description** : Saisie et enregistrement des bons de livraison lors de la réception physique des marchandises

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Formulaire de réception (date, référence BC, fournisseur, désignation)<br/>• Champ de quantité reçue vs quantité commandée (validation)<br/>• Upload du scan du bon de livraison (PDF/image)<br/>• Bouton validation "Confirmer réception"<br/>• Liste des bons de livraison en attente |
| **Actions** | Créer BL • Télécharger document • Valider quantités • Transmettre au comptable |
| **Accès données** | Lecture: bons de commande actifs<br/>Écriture: bons de livraison uniquement |

#### 1.3 Tableau de Suivi des Commandes
**Description** : Vue d'ensemble de toutes les commandes en cours et de leur statut

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Tableau avec colonnes: Date, Numéro BC, Fournisseur, Désignation, Quantité, Statut (Émis/Partiellement livré/Livré)<br/>• Filtres: par statut, par fournisseur, par date<br/>• Détail au clic: affichage complet du bon de commande |
| **Actions** | Consulter • Filtrer • Détailler |
| **Accès données** | Lecture: tous les bons de commande |

---

## 2. RESPONSABLE DES ACHATS

### Responsabilités
- Gestion des relations fournisseurs
- Création et gestion des devis
- Émission des bons de commande
- Suivi de l'avancement des commandes
- Relance des fournisseurs

### Interfaces principales

#### 2.1 Réception des Besoins & Analyse
**Description** : Consultation et analyse des listes de besoins transmises par le Technicien

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Liste des besoins en attente (date, technicien, articles demandés, quantités)<br/>• Détail du besoin: historique du stock, justification<br/>• Bouton "Consulter l'historique des prix"<br/>• Marquer comme traité |
| **Actions** | Consulter besoins • Analyser • Consulter historique prix |
| **Accès données** | Lecture: liste des besoins, historique prix |

#### 2.2 Gestion des Devis Fournisseurs
**Description** : Création, comparaison et gestion des devis fournisseurs

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Formulaire de création de devis (fournisseur, articles, quantités, prix unitaires)<br/>• Tableau de comparaison multi-fournisseur (prix, délais, conditions)<br/>• Historique des prix par fournisseur et par article<br/>• Analyse comparative (meilleur prix, meilleur délai, meilleur rapport qualité-prix) |
| **Actions** | Créer devis • Comparer devis • Consulter historique • Valider devis |
| **Accès données** | Lecture: liste besoins, historique fournisseurs et prix<br/>Écriture: devis |

#### 2.3 Création et Émission des Bons de Commande
**Description** : Formalisation des commandes auprès du fournisseur sélectionné

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Formulaire BC: date, numéro auto-généré, fournisseur, articles<br/>• Champs: désignation, quantité, unité, prix HT, prix TTC, total<br/>• **Calcul automatique**: montant total, taxes<br/>• Bouton "Émettre commande" (génère numéro, enregistre date)<br/>• Aperçu avant émission (PDF)<br/>• Sélection du devis à valider |
| **Actions** | Créer BC • Calculs auto • Générer PDF • Émettre • Valider devis correspondant |
| **Accès données** | Lecture: devis, besoins<br/>Écriture: bons de commande |

#### 2.4 Suivi des Commandes & Relances
**Description** : Suivi de l'avancement des commandes jusqu'à livraison et gestion des relances

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Tableau des commandes en cours (BC, fournisseur, date émission, délai prévu, statut)<br/>• Filtres: par statut, par fournisseur, par délai (à la date/en retard)<br/>• Détail au clic: historique, communications avec fournisseur<br/>• Bouton "Relancer fournisseur" (génère courrier/email)<br/>• Notifications des délais dépassés |
| **Actions** | Consulter • Filtrer • Relancer • Marquer comme livré |
| **Accès données** | Lecture: tous les bons de commande et réceptions |

---

## 3. COMPTABLE

### Responsabilités
- Enregistrement des factures fournisseurs
- Vérification de la conformité (facture vs commande vs livraison)
- Enregistrement des paiements
- Suivi financier
- Gestion des justificatifs

### Interfaces principales

#### 3.1 Enregistrement des Factures
**Description** : Saisie et enregistrement des factures fournisseurs reçues

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Formulaire facture: date facture, numéro facture, fournisseur, montant TTC<br/>• **Lien automatique avec BL** (vérification de conformité)<br/>• **Lien avec BC** (validation montant TTC vs total BC)<br/>• Upload du scan facture (PDF/image)<br/>• Vérification: matching facture-BL-BC<br/>• Bouton validation "Enregistrer facture"<br/>• Liste des factures en attente de paiement |
| **Actions** | Créer facture • Lier BL • Vérifier conformité • Enregistrer • Voir historique |
| **Accès données** | Lecture: BL, BC, historique factures<br/>Écriture: factures |

#### 3.2 Gestion des Paiements
**Description** : Enregistrement des paiements effectués en règlement des factures

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Formulaire paiement: mode (chèque, virement, lettre d'échange, espèces), date, montant<br/>• **Lien automatique avec facture** (validation montant restant dû)<br/>• Upload preuve de paiement (scan chèque, confirmation virement, etc.)<br/>• Saisie du fournisseur (auto-complète)<br/>• Suivi de l'état: en attente, partiel, soldé<br/>• Calcul montant restant dû après paiement partiel |
| **Actions** | Créer paiement • Télécharger preuve • Valider • Marquer comme soldé |
| **Accès données** | Lecture: factures, historique paiements<br/>Écriture: paiements |

#### 3.3 Tableau de Suivi Financier
**Description** : Vue d'ensemble de l'état financier des achats et de la trésorerie

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Tableau: numéro facture, fournisseur, montant, date facture, date paiement, statut (enregistrée/en attente/partiellement réglée/soldée)<br/>• Filtres: par statut, par fournisseur, par période, par plage de montant<br/>• Tri: par montant, par date d'échéance<br/>• Total par statut (montants, nombre)<br/>• **Graphique**: montant dû vs montant payé (tendance)<br/>• **Alertes**: factures en retard de paiement |
| **Actions** | Consulter • Filtrer • Trier • Exporter données • Voir alertes |
| **Accès données** | Lecture: tous les factures et paiements |

#### 3.4 Pièces Justificatives & Archives
**Description** : Stockage et consultation centralisée de tous les documents justificatifs

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Liste documents: factures, bons livraison, preuves paiement, bons commande<br/>• Recherche par type, fournisseur, période, numéro document<br/>• Affichage intégré (PDF/image) ou téléchargement<br/>• Horodatage: date d'upload, utilisateur<br/>• Lien avec document comptable (facture, paiement) |
| **Actions** | Consulter • Rechercher • Télécharger • Afficher |
| **Accès données** | Lecture: tous les documents scannés |

---

## 4. DIRECTEUR ADMINISTRATIF

### Responsabilités
- Supervision globale du système
- Approbation des opérations sensibles
- Gestion des utilisateurs et permissions
- Configuration des paramètres système
- Génération de rapports et analyses

### Interfaces principales

#### 4.1 Tableau de Bord Directeur
**Description** : Vue complète et synthétique de tous les indicateurs clés du système

| Aspect | Détails |
|--------|---------|
| **Contenu** | • **Indicateurs clés**: nombre total de bons de commande, montant total acheté (période), fournisseurs actifs<br/>• **Commandes en cours**: statut, délais, tonnages/quantités<br/>• **Factures**: montant dû, montant en retard, trésorerie dépensée<br/>• **Graphiques**: évolution des achats (mensuel), répartition par fournisseur, coût moyen par article<br/>• **Alertes**: délais dépassés, factures en retard paiement, écarts de quantités<br/>• **Notifications**: actions en attente d'approbation |
| **Actions** | Consulter indicateurs • Détailler • Exporter rapports |
| **Accès données** | Lecture: tous les données du système |

#### 4.2 Validation des Opérations Sensibles
**Description** : Interface de validation et d'approbation des opérations critiques

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Liste des opérations en attente approbation (dépassement budget, fournisseur nouveau, commande importante)<br/>• Détail complet de l'opération (documents, justifications)<br/>• Commentaires/notes précédentes<br/>• Boutons: "Approuver", "Refuser avec motif"<br/>• **Historique** des approbations/refus<br/>• **Notifications** des demandes |
| **Actions** | Consulter • Approuver • Refuser • Commenter |
| **Accès données** | Lecture: tous les documents<br/>Écriture: statuts approbation |

#### 4.3 Gestion des Utilisateurs & Permissions
**Description** : Gestion des comptes, attribution des rôles, configuration des accès

| Aspect | Détails |
|--------|---------|
| **Contenu** | • Liste utilisateurs: nom, email, rôle, statut (actif/inactif), date création<br/>• Formulaire création utilisateur (nom, email, rôle, mot de passe temporaire)<br/>• Édition utilisateur: modification rôle, désactivation<br/>• Réinitialisation mot de passe<br/>• **Historique accès** (qui, quand, depuis quelle machine)<br/>• **Configuration permissions** par rôle |
| **Actions** | Créer utilisateur • Modifier rôle • Réinitialiser MDP • Désactiver • Consulter historique |
| **Accès données** | Lecture/Écriture: tous les comptes utilisateurs et permissions |

#### 4.4 Configuration Système & Paramètres Généraux
**Description** : Configuration des paramètres globaux et des règles métier du système

| Aspect | Détails |
|--------|---------|
| **Contenu** | • **Paramètres entreprise**: nom, adresse, contact administratif<br/>• **Paramètres achats**: délai standard de paiement, fournisseurs standards, montants limites de commande<br/>• **Paramètres système**: URL serveur, fréquence sauvegarde, politique archivage<br/>• **Gestion des fournisseurs**: ajout/modification/suppression, seuils d'alerte<br/>• **Configuration des statuts** documents et workflow<br/>• **Sauvegarde/restauration** données<br/>• **Logs système** (authentification, modifications, erreurs) |
| **Actions** | Configurer paramètres • Gérer fournisseurs • Sauvegarder • Consulter logs |
| **Accès données** | Lecture/Écriture: configuration complète |

#### 4.5 Rapports & Analyses Avancées
**Description** : Génération de rapports détaillés et analyses approfondies

| Aspect | Détails |
|--------|---------|
| **Contenu** | • **Rapports personnalisables**: par période, par fournisseur, par article<br/>• **Export données**: CSV, Excel, PDF<br/>• **Analyses**: coût par article, économies réalisées, variations prix, performances fournisseurs<br/>• **Comparaisons**: budgets vs réalisé, délais prévus vs délais réels<br/>• **Tableaux statistiques**: nombre commandes, montants moyens, etc.<br/>• **Visualisations**: graphiques, tableaux croisés |
| **Actions** | Générer rapport • Filtrer paramètres • Exporter • Programmer rapports périodiques |
| **Accès données** | Lecture: tous les données |

---

## RÉSUMÉ : Accès aux Données par Rôle

| Rôle | Lecture | Écriture | Approbation |
|------|---------|----------|------------|
| **Technicien/Magasinier** | Stocks, BC, BL | Besoins, Bons de livraison | — |
| **Responsable Achats** | Besoins, Prix, Devis | Devis, BC | Validation devis |
| **Comptable** | BC, BL, Factures, Paiements | Factures, Paiements | — |
| **Directeur Admin** | Tout | Tout + Système | Opérations critiques |

---

## Récapitulatif des Interfaces Totales

### Par rôle
- **Technicien / Magasinier**: 3 interfaces
- **Responsable des Achats**: 4 interfaces  
- **Comptable**: 4 interfaces
- **Directeur Administratif**: 5 interfaces

**Total: 16 interfaces à développer**

### Types d'interfaces
- **Formulaires de saisie**: 8 (besoins, devis, BC, BL, facture, paiement, utilisateur, configuration)
- **Tableaux/Listes**: 6 (suivi commandes, suivi financier, opérations, utilisateurs, rapports)
- **Tableaux de bord**: 2 (technicien/gestionnaire, directeur)
- **Modules**: 1 (gestion archive/documents)

---

## Points clés à retenir

### Architecture générale
1. **Authentification**: Chaque utilisateur a un rôle qui détermine ses accès
2. **Séparation des données**: Chaque rôle ne voit que ce qu'il doit voir
3. **Workflow documentaire**: Les documents passent d'un rôle à l'autre en cascade
4. **Traçabilité**: Toutes les actions sont enregistrées avec horodatage

### Flux de données transversaux
- **Technicien** → crée besoins → **Achats**
- **Achats** → crée BC → **Technicien** reçoit marchandises
- **Technicien** → crée BL → **Comptable**
- **Comptable** → enregistre facture et paiement → **Directeur** supervise

### Validations importantes
- Facture doit correspondre à BL et BC
- Montants doivent correspondre (prix, taxes)
- Quantités reçues doivent correspondre à quantités commandées
- Paiements doivent correspondre à factures
- Toutes les étapes doivent être validées avant de passer à la suivante

---

**Document généré**: Guide complet pour la création des interfaces du système de gestion des achats
