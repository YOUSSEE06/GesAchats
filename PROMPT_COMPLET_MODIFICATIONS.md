# 📝 PROMPT COMPLET - MODIFICATIONS ESPACE MAGASINIER

---

## 🔴 PRIORITÉ 1 - CRÉATION PAGE "GESTION DES BESOINS"

### **Créer une nouvelle page/formulaire complet pour la gestion des besoins**

Tu dois créer une interface complète pour la gestion des besoins de réapprovisionnement. Cette page sera composée de deux parties principales : un formulaire de création et un tableau de liste.

#### **PARTIE 1 : FORMULAIRE DE CRÉATION (Section "Nouveau Besoin")**

Structure du formulaire :

```html
Titre : "Nouveau Besoin"

1. CHAMP : Article (Dropdown - obligatoire)
   - Label: "Article*"
   - Type: Select/Dropdown
   - Source de données: Table Articles
   - Affiche: [Désignation de l'article]
   - À la sélection, affiche automatiquement:
     * Stock Actuel: [valeur lue-seul]
     * Stock Minimum: [valeur lue-seul]
     * Écart: [calculé = Stock Min - Stock Actuel]
     * Dernier achat: [date lue-seul]

2. CHAMP : Quantité à Commander (Nombre - obligatoire)
   - Label: "Quantité à Commander*"
   - Type: Nombre (décimal)
   - Unité: Afficher l'unité de l'article sélectionné
   - Info additionnelle: 
     * "💡 Suggestion: XXX unités (pour 45 jours de couverture)"
     * Calcul: (Stock Min - Stock Actuel) + (Consommation moy * 45 jours)

3. CHAMP : Raison du Besoin (Dropdown - obligatoire)
   - Label: "Raison du Besoin*"
   - Options:
     * Réappro régulière (défaut)
     * Urgence
     * Rupture imminente
     * Stock critique
     * Projet spécifique

4. CHAMP : Justification (Texte long - obligatoire)
   - Label: "Justification*"
   - Type: Textarea (4 lignes)
   - Placeholder: "Ex: Chantier X démarre lundi, nécessite 200 sacs de ciment..."
   - Obligatoire: Oui (au moins 10 caractères)

5. CHAMP : Priorité (Dropdown - obligatoire)
   - Label: "Priorité*"
   - Options:
     * 🟢 Basse (moins de 15 jours)
     * 🟡 Normale (7-15 jours)
     * 🔴 Haute (moins de 7 jours)
   - Défaut: Normale

6. CHAMP : Date d'Urgence (Date picker - obligatoire)
   - Label: "Date d'Urgence Requise*"
   - Type: Date picker
   - Format: JJ/MM/AAAA
   - Validation: >= Aujourd'hui
   - Affiche aussi: "Jours restants: X"

7. CHAMP : Délai Requis (Nombre - obligatoire)
   - Label: "Délai de Livraison Requis (jours)*"
   - Type: Nombre entier (1-30)
   - Unité: "jours"
   - Exemple: "3 jours"

8. CHAMP : Notes Additionnelles (Texte - optionnel)
   - Label: "Notes Additionnelles"
   - Type: Textarea (3 lignes)
   - Placeholder: "Notes libres..."

BOUTONS D'ACTION (au bas du formulaire):
- [🔄 Réinitialiser] - Vide tous les champs
- [💾 Enregistrer en Brouillon] - Sauvegarde avec état "Brouillon"
- [✅ Valider & Enregistrer] - Sauvegarde avec état "Validé"
- [❌ Annuler] - Ferme le formulaire sans sauvegarder

VALIDATION:
- Tous les champs * sont obligatoires
- Message d'erreur si champ manquant ou invalide
- Confirmation avant d'enregistrer
- Message de succès après enregistrement
```

---

#### **PARTIE 2 : TABLEAU "MES BESOINS"**

Structure du tableau :

```html
Titre : "Mes Besoins"

FILTRES & RECHERCHE (Au-dessus du tableau):
- [🔍 Rechercher article...] (input texte)
- [État ▼ Tous/Brouillon/Validé/Transmis/Rejeté]
- [Priorité ▼ Tous/Basse/Normale/Haute]
- [Tri: ▼ Date DESC / Priorité DESC / État]
- [📥 Importer] (optionnel)
- [📤 Exporter Excel] (optionnel)

COLONNES DU TABLEAU:
1. "N°" (numéro auto-incrémenté, lien clickable)
2. "Date Création" (format: JJ/MM/AAAA HH:MM)
3. "Article" (nom de l'article)
4. "Quantité" (nombre + unité)
5. "Raison" (texte court: "Réappro", "Urgence", etc.)
6. "Priorité" (badge coloré: 🟢/🟡/🔴)
7. "État" (badge avec couleur):
   - 🟡 Brouillon (jaune/gris)
   - ✅ Validé (vert clair)
   - 📤 Transmis (bleu)
   - ❌ Rejeté (rouge)
   - 🔄 Relancé (orange)
8. "Actions" (icônes/boutons):
   - Si état "Brouillon" :
     * ✏️ Modifier
     * 🗑️ Supprimer
     * ✅ Valider
   - Si état "Validé" :
     * 📤 Transmettre
     * ✏️ Modifier
   - Si état "Transmis" :
     * 👁️ Voir détails (lecture seule)
   - Si état "Rejeté" :
     * 👁️ Voir raison
     * 🔄 Relancer

DONNÉES AFFICHÉES POUR CHAQUE LIGNE:
┌─────┬──────────┬──────────────────┬─────────┬──────────┬──────────┬─────────────┬────────────────┐
│ N°  │ Date     │ Article          │ Qté     │ Raison   │ Priorité │ État        │ Actions        │
├─────┼──────────┼──────────────────┼─────────┼──────────┼──────────┼─────────────┼────────────────┤
│ #1  │30/04/26  │ Ciment Sac 35kg  │ 150 sac │ Urgence  │ 🔴 Haute │ ✅ Validé   │ 📤 ✏️ 🗑️      │
│ #2  │29/04/26  │ Graviers 40mm    │ 80 m3   │ Réappro  │ 🟡 Norm  │ 📤 Transmis │ 👁️            │
│ #3  │28/04/26  │ Acier HA8        │ 5 T     │ Rupture  │ 🔴 Haute │ ❌ Rejeté   │ 👁️ 🔄        │
│ #4  │27/04/26  │ Tuiles           │ 500 p   │ Réappro  │ 🟢 Basse │ 🟡 Brouill  │ ✏️ 🗑️ ✅     │
└─────┴──────────┴──────────────────┴─────────┴──────────┴──────────┴─────────────┴────────────────┘

COULEURS & STYLES:
- État "Brouillon": Fond gris clair, texte gris foncé
- État "Validé": Fond vert clair, texte vert foncé
- État "Transmis": Fond bleu clair, texte bleu foncé
- État "Rejeté": Fond rouge clair, texte rouge foncé

PAGINATION:
- Afficher 10 lignes par page
- Liens pagination en bas

STATISTIQUES (optionnel, au-dessus du tableau):
- Nombre total de besoins: X
- Besoins en brouillon: X
- Besoins validés: X
- Besoins transmis: X
```

---

#### **PARTIE 3 : ACTIONS DÉTAILLÉES**

Quand l'utilisateur clique sur une action :

**Action "Modifier" (état Brouillon seulement)**
- Ouvre le formulaire en mode édition
- Pré-remplit les champs avec les données existantes
- Permet modification de tous les champs
- Boutons: [💾 Enregistrer les modifications] [❌ Annuler]

**Action "Supprimer" (état Brouillon seulement)**
- Affiche popup de confirmation: "Êtes-vous sûr de vouloir supprimer ce besoin ?"
- Si oui: Supprime le besoin et actualise le tableau
- Si non: Ferme le popup

**Action "Valider" (état Brouillon → Validé)**
- Affiche popup de confirmation avec résumé du besoin
- Valide tous les champs obligatoires
- Change l'état à "Validé"
- Message de succès: "Besoin #X validé avec succès"

**Action "Transmettre" (état Validé → Transmis)**
- Affiche popup de confirmation: 
  "Êtes-vous sûr de vouloir transmettre ce besoin au Responsable Achats ?"
  "Vous ne pourrez plus le modifier après transmission."
- Envoie une notification au Responsable Achats
- Change l'état à "Transmis"
- Verrouille tous les champs (lecture seule)
- Message de succès: "Besoin #X transmis au Responsable Achats"

**Action "Voir détails" (tous états)**
- Ouvre une popup/modal avec:
  * Tous les champs du besoin (lecture seule)
  * État actuel
  * Date de création
  * Date de dernière modification
  * Commentaires du Responsable Achats (si rejeté)
  * Bouton fermer

**Action "Relancer" (état Rejeté)**
- Réouvre le formulaire en mode édition
- Affiche le motif du rejet en haut
- Permet modifier les champs
- Boutons: [📤 Relancer] [❌ Annuler]

---

#### **INTÉGRATION BASE DE DONNÉES**

Table "Besoins" (structure):

```sql
CREATE TABLE Besoins (
    id SERIAL PRIMARY KEY,
    numero_besoin VARCHAR(50) UNIQUE AUTO_INCREMENT, -- #1, #2, #3...
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    date_modification TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    article_id INTEGER NOT NULL REFERENCES Articles(id),
    quantite DECIMAL(10, 2) NOT NULL,
    unite VARCHAR(50),
    raison VARCHAR(100) NOT NULL, -- 'Réappro régulière', 'Urgence', 'Rupture', etc.
    justification TEXT NOT NULL,
    priorite VARCHAR(20) NOT NULL, -- 'Basse', 'Normale', 'Haute'
    date_urgence DATE NOT NULL,
    delai_requis INTEGER NOT NULL, -- en jours
    notes TEXT, -- optionnel
    statut VARCHAR(50) DEFAULT 'Brouillon', -- 'Brouillon', 'Validé', 'Transmis', 'Rejeté', 'Relancé'
    utilisateur_id INTEGER NOT NULL REFERENCES Utilisateurs(id),
    date_transmission TIMESTAMP, -- NULL jusqu'à transmission
    motif_rejet TEXT, -- si rejeté
    date_rejet TIMESTAMP -- si rejeté
);
```

---

#### **WORKFLOW & RÈGLES**

```
État initial: Brouillon
  ↓
[Modifier / Supprimer / Valider]
  ↓
État: Validé
  ↓
[Transmettre au Responsable Achats]
  ↓
État: Transmis (verrouillé)
  ↓
[Le Responsable Achats crée Devis & BC]
  ↓
Si le Responsable rejette:
État: Rejeté
  ↓
[Magasinier peut Relancer avec modifications]
  ↓
Retour à état: Validé
  ↓
Nouveau: Transmis
```

---

#### **NOTIFICATIONS & MESSAGES**

```
Lors de la création:
✅ "Besoin #X créé en brouillon"

Lors de la validation:
✅ "Besoin #X validé avec succès"

Lors de la transmission:
✅ "Besoin #X transmis au Responsable Achats"
📧 Email au Responsable: "Nouveau besoin à traiter: Ciment 35kg (150 sacs)"

Lors du rejet (Responsable Achats):
⚠️ "Besoin #X rejeté"
📧 Email au Magasinier: "Motif: Quantité insuffisante pour ce délai"

Lors de la relance:
✅ "Besoin #X relancé"
```

---

---

## 🟡 PRIORITÉ 2 - AMÉLIORATIONS PAGES EXISTANTES

### **1. AMÉLIORER PAGE DASHBOARD**

Ajoute les éléments suivants :

#### **Élément 1 : Graphique "Évolution du Stock (7 derniers jours)"**

```html
Titre: "Évolution du Stock (7 jours)"
Type: Graphique courbe/ligne
X-axis: Jours (J-7, J-6, ... Aujourd'hui)
Y-axis: Quantité totale en stock

Courbe: Ligne continue montrant la baisse/hausse du stock
Code couleur:
- Vert: Ligne stable ou en hausse
- Orange: Ligne en baisse modérée
- Rouge: Ligne en baisse rapide

Exemple de données:
J-7: 450 unités
J-6: 420 unités
J-5: 390 unités
J-4: 380 unités
J-3: 340 unités
J-2: 310 unités
Aujourd'hui: 280 unités

Affiche aussi:
- Tendance: "↓ Baisse de 37% en 7 jours"
- Vitesse: "Consommation moyenne: 25 unités/jour"
```

#### **Élément 2 : Section "Dernières Actions" (Timeline)**

```html
Titre: "Dernières Actions"

Format Timeline:
30/04/26 14:45 │ ✅ VALIDATION      │ Besoin #4     │ Alice Achats
               │ Validation du besoin de Ciment

30/04/26 13:20 │ 📦 RÉCEPTION       │ BL #25        │ Alice Achats
               │ Réception marchandise (200 sacs)

30/04/26 10:15 │ 📤 TRANSMISSION    │ Besoin #3     │ Alice Achats
               │ Transmission au Responsable Achats

29/04/26 16:30 │ ✏️ MODIFICATION    │ Besoin #2     │ Alice Achats
               │ Modification quantité (100→150)

29/04/26 14:00 │ 📝 CRÉATION        │ Besoin #1     │ Alice Achats
               │ Création besoin de Ciment 35kg

Limite: Afficher les 5 dernières actions
```

#### **Élément 3 : Boutons "Créer un Besoin" sur articles critiques**

```html
Sur la section "Articles en Rupture Critique":

Ajouter une colonne "Actions" avec:
- Bouton [➕ Créer Besoin] 
  Au clic: Ouvre le formulaire "Nouveau Besoin" 
  Pré-remplit le champ "Article" avec l'article sélectionné
  Focus sur "Quantité à Commander"
```

#### **Élément 4 : Indicateurs de Tendance sur KPI**

```html
Modifier les cartes KPI pour afficher:

Carte "Stock Total":
  315.00 ↓
  Unités en stock
  (↓ Baisse de 12% vs hier)

Carte "Demandes en attente":
  2 ↑
  Besoins à traiter
  (↑ Hausse vs hier)

Carte "Ruptures Critiques":
  1 →
  Articles à 0 stock
  (→ Stable)

Légende:
↑ En hausse (vert)
↓ En baisse (rouge)
→ Stable (gris)
```

---

### **2. AMÉLIORER PAGE "ANALYSE DU STOCK"**

Ajoute les colonnes manquantes au tableau :

#### **Nouvelles colonnes à ajouter**

```html
Tableau actuel:
| Article | Qté Actuelle | Seuil Min | État | [Nouveau] Actions |

Tableau amélioré:
| Article | Qté Actuelle | Seuil Min | État | Jours rupture | Dernier achat | Conso moy/jour | Actions |

DÉTAILS DES NOUVELLES COLONNES:

1. Colonne "Jours avant rupture"
   Calcul: Stock Actuel / Consommation moyenne par jour
   Affichage:
   - Nombre de jours restants estimés
   - Code couleur:
     * 🔴 Rouge: < 3 jours (critique)
     * 🟡 Orange: 3-7 jours (alerte)
     * 🟢 Vert: > 7 jours (normal)
   Exemple: "3.5 jours", "0.5 jours", "45 jours"

2. Colonne "Dernier achat"
   - Date du dernier réapprovisionnement (JJ/MM/AAAA)
   - Affiche aussi: "il y a X jours"
   Exemple: "02/04/2026 (28 jours)", "28/03/2026 (il y a 2j)"

3. Colonne "Consommation moy/jour"
   Calcul: Moyenne calculée sur 30 derniers jours
   Affichage: Nombre + unité
   Exemple: "25 sacs/jour", "3.5 m³/jour", "0.2 T/jour"

4. Colonne "Actions" (améliorée)
   Boutons:
   - [➕ Créer Besoin] - Ouvre formulaire pré-rempli
   - [📊 Voir historique] - Ouvre graphe consommation article
   - [👁️ Détails] - Popup avec infos complètes

Exemple de ligne complète:
┌──────────────────┬──────────┬────────┬───────┬──────────────┬────────────────┬─────────────────┬────────────────┐
│ Article          │ Actuelle │ Min    │ État  │ Jours rupture│ Dernier achat  │ Conso moy/jour  │ Actions        │
├──────────────────┼──────────┼────────┼───────┼──────────────┼────────────────┼─────────────────┼────────────────┤
│ Ciment 35kg      │ 10 sacs  │ 50     │🔴 BAS│ 3.5 jours    │ 02/04 (28j)    │ 2.8 sacs/jour   │ ➕ 📊 👁️     │
│ Graviers 40mm    │ 0 m3     │ 20 m3  │🔴CRIT│ 0 jours      │ 28/03 (32j)    │ 5.7 m³/jour     │ ➕ 📊 👁️     │
│ Sable fin        │ 100 m3   │ 30 m3  │🟢BON │ 120 jours    │ 15/03 (45j)    │ 0.8 m³/jour     │ — 📊 👁️      │
│ Acier HA8        │ 5 T      │ 100 T  │🔴BAS │ 5 jours      │ 25/03 (35j)    │ 1.0 T/jour      │ ➕ 📊 👁️     │
│ Tuiles           │ 200 p.   │ 100 p. │🟢BON │ 30 jours     │ 20/03 (40j)    │ 6.6 p./jour     │ — 📊 👁️      │
└──────────────────┴──────────┴────────┴───────┴──────────────┴────────────────┴─────────────────┴────────────────┘
```

#### **Fonctionnalités additionnelles**

```html
1. Statistiques améliorées (au-dessus du tableau):
   - Articles en stock: 150 (inclus/exclu rupture)
   - Articles en rupture: 3
   - Articles critiques: 12
   - Stock total valeur: 125 000€ (HT)
   - Consommation moyenne/jour: 45 unités
   - Taux de rupture/mois: 2.3%

2. Filtre additionnel:
   [État ▼ Tous/Bas/Critique/Rupture/Normal]
   [Tri ▼ Article/Stock actuel/Jours rupture/Dernier achat]

3. Graphique de consommation par article (optionnel):
   Cliquer sur une ligne → affiche popup avec courbe de consommation 30 jours
```

---

### **3. COMPLÉTER PAGE "RÉCEPTIONS & BL"**

La page "Réceptions & BL" est actuellement vide. À remplir :

#### **PARTIE 1 : FORMULAIRE "CRÉER UN BON DE LIVRAISON"**

```html
Titre: "Nouveau Bon de Livraison"

1. CHAMP : Bon de Commande Associé (Dropdown - obligatoire)
   - Label: "Bon de Commande Associé*"
   - Type: Dropdown (pré-filtré sur BC non encore reçues)
   - Affiche: "BC-2026-001 - Ciment 35kg (100 sacs)"
   - À la sélection, pré-remplit automatiquement:
     * Fournisseur: [lue-seul]
     * Article: [lue-seul]
     * Quantité commandée: [lue-seul]
     * Description: [lue-seul]

2. CHAMP : Date de Réception (Date - obligatoire)
   - Label: "Date de Réception*"
   - Type: Date picker
   - Format: JJ/MM/AAAA
   - Défaut: Aujourd'hui
   - Validation: <= Aujourd'hui (pas date future)

3. CHAMP : Numéro BL (Texte - obligatoire)
   - Label: "Numéro BL (du fournisseur)*"
   - Type: Texte
   - Placeholder: "Ex: 2026-4521 ou BL-2026-001"
   - Source: Scan du BL physique

4. CHAMP : Quantité Reçue (Nombre - obligatoire)
   - Label: "Quantité Reçue*"
   - Type: Nombre (décimal)
   - Validation automatique:
     * Si = Quantité commandée: ✅ "Conforme"
     * Si < Quantité commandée: ⚠️ "Partiel (manque X)"
     * Si > Quantité commandée: ⚠️ "Surplus (+ X)"
   - Affiche aussi: "Quantité commandée: XXX"

5. CHAMP : État de Réception (Dropdown - obligatoire)
   - Label: "État de Réception*"
   - Options:
     * ✅ Réception Complète (Quantité = Cmd)
     * 🟡 Réception Partielle (Quantité < Cmd)
     * ❌ Défectueuse (Quantité OK mais qualité mauvaise)
   - Défaut: Réception Complète (si quantités égales)

6. CHAMP : Observations (Texte - optionnel)
   - Label: "Observations"
   - Type: Textarea (4 lignes)
   - Placeholder: "Ex: Rayures sur 3 sacs, mais article OK"
   - Utile pour signaler dégâts mineur

7. CHAMP : Document Scanné (File upload - obligatoire)
   - Label: "Scan du Bon de Livraison*"
   - Type: File upload (PDF, JPG, PNG)
   - Format: <= 5MB
   - Affiche aperçu miniature après upload

8. CHAMP : Photos d'Inspection (File upload - optionnel)
   - Label: "Photos d'Inspection"
   - Type: Multiple file upload
   - Format: JPG, PNG (max 5 photos)
   - Utile pour documenter dégâts ou particularités

BOUTONS D'ACTION:
- [🔄 Réinitialiser]
- [💾 Enregistrer BL]
- [❌ Annuler]

VALIDATION:
- Champs * obligatoires
- Confirmation avant enregistrement
```

#### **PARTIE 2 : TABLEAU "MES BONS DE LIVRAISON"**

```html
Titre: "Mes Bons de Livraison"

FILTRES:
- [🔍 Rechercher BL...]
- [État ▼ Tous/Complet/Partiel/Défectueux]
- [Tri ▼ Date DESC / BC / Fournisseur]

COLONNES:
1. "N° BL" (numéro, clickable pour détails)
2. "Date Réception" (JJ/MM/AAAA)
3. "N° BC Associée" (numéro, clickable)
4. "Fournisseur" (nom)
5. "Article" (désignation)
6. "Qté Reçue" (nombre + unité)
7. "Qté Commandée" (nombre + unité)
8. "Conformité" (badge):
   - ✅ Conforme (quantité = commande)
   - 🟡 Partielle (quantité < commande)
   - ❌ Défectueuse (qualité mauvaise)
9. "État" (badge):
   - 📝 Enregistrée (créée mais stock pas mis à jour)
   - ✅ Validée (stock mis à jour)
10. "Actions":
    - 👁️ Voir détails
    - ✅ Valider (si état Enregistrée)
    - 🗑️ Supprimer (si erreur)

EXEMPLE:
┌──────┬──────────────┬─────────┬──────────────────┬──────────────────┬──────────┬──────────┬────────────┬──────────┬──────────────┐
│ N° BL│ Date Réc.    │ N° BC   │ Fournisseur      │ Article          │ Reçue    │ Commandée│ Conformité │ État     │ Actions      │
├──────┼──────────────┼─────────┼──────────────────┼──────────────────┼──────────┼──────────┼────────────┼──────────┼──────────────┤
│BL-25 │ 30/04/2026   │BC-001   │BatiConstruction  │ Ciment 35kg      │200 sacs  │ 200 sacs │ ✅ Conforme│ ✅ Validée│ 👁️ 🗑️     │
│BL-24 │ 28/04/2026   │BC-002   │Quincaillerie Pro │ Tuiles           │499 p.    │ 500 p.   │ 🟡 Partiel │ 📝 Enreg. │ 👁️ ✅ 🗑️  │
│BL-23 │ 25/04/2026   │BC-003   │BatiConstruction  │ Graviers 40mm    │ 80 m3    │ 100 m3   │ 🟡 Partiel │ 📝 Enreg. │ 👁️ ✅ 🗑️  │
│BL-22 │ 22/04/2026   │BC-004   │ElecDirect        │ Acier HA8        │ 10 T     │ 10 T     │ ✅ Conforme│ ✅ Validée│ 👁️ 🗑️     │
└──────┴──────────────┴─────────┴──────────────────┴──────────────────┴──────────┴──────────┴────────────┴──────────┴──────────────┘

PAGINATION: 10 lignes/page
```

#### **PARTIE 3 : ACTION "VOIR DÉTAILS"**

```html
Popup/Modal "Détails Bon de Livraison":

Affiche tous les champs du BL (lecture seule):
- N° BL: BL-25
- Date réception: 30/04/2026
- N° BC Associée: BC-001
- Fournisseur: BatiConstruction SA
- Article: Ciment 35kg
- Quantité commandée: 200 sacs
- Quantité reçue: 200 sacs
- État réception: Réception Complète ✅
- Observations: Aucune
- Document scanné: [📎 BL_BC-001.pdf] (clickable)
- Photos: [📷 photo_1.jpg] [📷 photo_2.jpg]
- État: Validée
- Date validation: 30/04/2026 14:30
- Mise à jour stock: +200 sacs au stock "Ciment 35kg"

Boutons: [📄 Télécharger PDF] [🖨️ Imprimer] [❌ Fermer]
```

#### **INTÉGRATION BASE DE DONNÉES**

```sql
CREATE TABLE Bons_Livraison (
    id SERIAL PRIMARY KEY,
    numero_bl VARCHAR(50) UNIQUE,
    date_reception DATE NOT NULL,
    bc_id INTEGER NOT NULL REFERENCES Bons_Commande(id),
    fournisseur_id INTEGER NOT NULL REFERENCES Fournisseurs(id),
    quantite_commandee DECIMAL(10, 2) NOT NULL,
    quantite_livree DECIMAL(10, 2) NOT NULL,
    etat_reception VARCHAR(50) NOT NULL, -- 'Complète', 'Partielle', 'Défectueuse'
    observations TEXT,
    document_scanne BYTEA, -- Fichier PDF/Image
    photos BYTEA[], -- Array de photos
    conformite VARCHAR(50), -- 'Conforme', 'Partielle', 'Défectueuse'
    statut VARCHAR(50) DEFAULT 'Enregistrée', -- 'Enregistrée', 'Validée'
    utilisateur_id INTEGER NOT NULL REFERENCES Utilisateurs(id),
    date_creation TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    date_validation TIMESTAMP
);
```

#### **MISE À JOUR AUTOMATIQUE DU STOCK**

```
Quand un BL est VALIDÉ:

1. Récupère l'article depuis le BL
2. Récupère le stock actuel de l'article
3. Ajoute la quantité livrée au stock
4. Crée une trace de la mise à jour dans table Tracabilité
5. Met à jour la date de "Dernier achat" de l'article

Exemple:
Stock avant: 85 sacs de Ciment
+ Livraison: 200 sacs
= Stock après: 285 sacs

Cette mise à jour doit être IMMÉDIATE et AUTOMATIQUE
```

---

### **4. CRÉER PAGE "HISTORIQUE / AUDIT"**

Nouvelle page pour la traçabilité complète.

#### **STRUCTURE**

```html
Titre: "Historique & Audit"

FILTRES (Au-dessus):
- [🔍 Rechercher action/utilisateur...]
- [Type ▼ Tous/Création/Modification/Validation/Transmission/Réception]
- [Utilisateur ▼ Tous/Alice Achats/Bob Resp. Achats/...]
- [Période ▼ Derniers 7j/30j/3 mois/Custom dates]
- [Document ▼ Tous/Besoins/BL/Commandes/Stock]

AFFICHAGE: Timeline verticale

FORMAT:
30/04/2026 14:45:32 │ 📝 CRÉATION │ Besoin #8 │ Alice Achats │ Ciment 35kg
                    │ Création d'un besoin pour Ciment (150 sacs, priorité Haute)

30/04/2026 15:20:10 │ ✏️ MODIF    │ Besoin #8 │ Alice Achats │ Quantité mise à jour
                    │ Modification: Quantité 100 → 150 sacs

30/04/2026 16:05:45 │ ✅ VALID    │ Besoin #8 │ Alice Achats │ Besoin validé
                    │ État: Brouillon → Validé

30/04/2026 16:15:00 │ 📤 TRANSM   │ Besoin #8 │ Alice Achats │ Transmission au Resp.
                    │ État: Validé → Transmis

01/05/2026 09:30:20 │ 📋 CRÉATION │ Devis #5  │ Bob Resp.    │ Devis créé depuis besoin
                    │ Création devis suite au besoin #8

COLONNES AFFICHÉES:
1. Date & Heure (JJMMAAAA HH:MM:SS)
2. Type d'action (icône + label):
   - 📝 CRÉATION
   - ✏️ MODIFICATION
   - ✅ VALIDATION
   - 📤 TRANSMISSION
   - 📦 RÉCEPTION
   - 🗑️ SUPPRESSION
   - 👁️ CONSULTATION
3. Document (N° et type)
4. Utilisateur (qui a fait l'action)
5. Description courte
```

#### **ACTION "VOIR DÉTAILS"**

Cliquer sur une ligne → Affiche popup avec détails complets :

```html
Popup "Détails Action":

═════════════════════════════════════════════════════════════
30/04/2026 14:45:32 - Création Besoin #8

Utilisateur      : Alice Achats (Magasinier)
Action           : CRÉATION
Type document    : Besoin
Numéro document  : #8
Description      : Création d'un besoin pour réapprovisionnement

DONNÉES SAISIES:
  • Article: Ciment Sac 35kg
  • Quantité: 150 sacs
  • Raison: Urgence
  • Priorité: Haute
  • Justification: Stock critique, chantier X démarre lundi
  • Date urgence: 30/04/2026
  • Délai requis: 3 jours

DONNÉES SYSTÈME:
  • Ancien statut: —
  • Nouveau statut: Brouillon
  • Ancien stock: 85 sacs
  • Nouveau stock: — (pas modifié à cette étape)
  • IP source: 192.168.1.45
  • Navigateur: Firefox 125.0
  • Durée session: 2h 15m

═════════════════════════════════════════════════════════════

Boutons: [📄 Exporter] [🖨️ Imprimer] [❌ Fermer]
```

#### **EXPORT & RAPPORT**

```html
Boutons en haut:
- [📥 Importer] (optionnel)
- [📤 Exporter Excel]
- [📄 Exporter PDF]
- [🖨️ Imprimer]

L'export doit inclure:
- Toutes les lignes filtrées
- Date & heure
- Type d'action
- Document
- Utilisateur
- Description
- Date d'export
```

#### **INTÉGRATION BASE DE DONNÉES**

```sql
CREATE TABLE Tracabilite (
    id SERIAL PRIMARY KEY,
    utilisateur_id INTEGER REFERENCES Utilisateurs(id),
    action VARCHAR(100) NOT NULL, -- 'CRÉATION', 'MODIFICATION', 'VALIDATION', etc.
    type_document VARCHAR(50), -- 'Besoin', 'BL', 'Commande', 'Facture'
    numero_document VARCHAR(50),
    description TEXT,
    ancien_statut VARCHAR(50),
    nouveau_statut VARCHAR(50),
    ancien_stock DECIMAL(10, 2),
    nouveau_stock DECIMAL(10, 2),
    donnees_saisies JSONB, -- Stockage des données modifiées
    donnees_anciennes JSONB, -- Pour les modifs
    ip_source VARCHAR(45), -- IPv4 ou IPv6
    navigateur VARCHAR(255),
    date_action TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT fk_utilisateur FOREIGN KEY (utilisateur_id) REFERENCES Utilisateurs(id)
);
```

---

---

## 🟢 PRIORITÉ 3 - AMÉLIORATIONS OPTIONNELLES

### **Améliorer les graphiques**

```
1. Ajouter courbe de tendance sur graphe "Consommation"
2. Ajouter heatmap de stock (rouge/orange/vert)
3. Ajouter prévisions de rupture (courbe en pointillé futur)
```

### **Ajouter système de notifications**

```
- Notification toast (popup en bas à droite) lors de:
  * Besoin rejeté
  * Commande retardée
  * Stock critique
  * BL reçu
- Notification email (optionnel)
```

### **Ajouter calendrier planification**

```
- Vue calendrier des livraisons attendues
- Drag & drop pour planifier
- Color-coding par urgence
```

---

---

## 📊 RÉSUMÉ COMPLET DES DÉVELOPPEMENTS

| # | Élément | Priorité | Effort | Statut |
|---|---------|----------|--------|--------|
| 1 | Page "Gestion des Besoins" (complet) | 🔴 CRITIQUE | 6-8h | ❌ À faire |
| 2 | Dashboard amélioré | 🟡 Important | 3-4h | ⚠️ Partiel |
| 3 | Analyse Stock améliorée | 🟡 Important | 2-3h | ⚠️ Partiel |
| 4 | Réceptions & BL complet | 🟡 Important | 4-6h | ❌ À faire |
| 5 | Page Historique/Audit | 🟡 Important | 3-4h | ❌ À faire |
| 6 | Graphiques améliorés | 🟢 Optionnel | 3-4h | ⏳ Plus tard |
| 7 | Notifications système | 🟢 Optionnel | 2-3h | ⏳ Plus tard |

**Effort total estimé** : 23-32 heures
**Durée de développement** : 4-6 semaines (à temps plein)

---

---

## 🚀 COMMANDES SQL - DONNÉES DE TEST

Ajouter des données de test pour développement/test :

```sql
-- Articles supplémentaires
INSERT INTO Articles VALUES 
(6, 'Mortier M400', 'sac', 25, 50, 5.00, 6.00, NOW(), true),
(7, 'Brique pleine 20cm', 'm2', 300, 100, 8.00, 9.60, NOW(), true),
(8, 'Chaux vive', 'sac', 15, 30, 4.50, 5.40, NOW(), true),
(9, 'PVC tuyau ø25', 'm', 120, 50, 3.00, 3.60, NOW(), true),
(10, 'Clous 3 pouces', 'kg', 50, 20, 2.50, 3.00, NOW(), true);

-- Besoins de test
INSERT INTO Besoins VALUES
(1, 'B-001', '2026-04-30', 1, 'Ciment Sac 35kg', 150, 'sac', 'Urgence', 'Stock critique, chantier X lundi', 'Haute', '2026-04-30', 3, 'Approvisionnement urgent', 1, NOW(), NULL, NULL),
(2, 'B-002', '2026-04-29', 2, 'Graviers 40mm', 80, 'm3', 'Réappro régulière', 'Stock bas, consommation normale', 'Normale', '2026-05-07', 7, NULL, 1, NOW(), NULL, NULL),
(3, 'B-003', '2026-04-28', 4, 'Acier HA8', 5, 'T', 'Rupture imminente', 'Plus de stock, projet urgent', 'Haute', '2026-04-28', 2, NULL, 1, '2026-04-28 10:00:00', NULL, NULL);

-- BL de test
INSERT INTO Bons_Livraison VALUES
(1, 'BL-001', '2026-04-30', 1, 1, 200, 200, 'Complète', NULL, NULL, 'Conforme', 'Validée', 1, NOW(), NOW()),
(2, 'BL-002', '2026-04-28', 2, 2, 100, 80, 'Partielle', 'Manquait 20 unités', NULL, 'Partielle', 'Enregistrée', 1, NOW(), NULL);

-- Historique de test
INSERT INTO Tracabilite VALUES
(1, 1, 'CRÉATION', 'Besoin', 'B-001', 'Création besoin ciment', NULL, 'Brouillon', NULL, NULL, '{"article": "Ciment 35kg", "quantite": 150}', NULL, '192.168.1.45', 'Firefox 125.0', NOW()),
(2, 1, 'VALIDATION', 'Besoin', 'B-001', 'Validation besoin', 'Brouillon', 'Validé', NULL, NULL, NULL, NULL, '192.168.1.45', 'Firefox 125.0', NOW()),
(3, 1, 'RÉCEPTION', 'BL', 'BL-001', 'Réception marchandise', NULL, 'Validée', 85, 285, NULL, NULL, '192.168.1.45', 'Firefox 125.0', NOW());
```

---

Voilà ! C'est le prompt COMPLET prêt à utiliser. 🚀
