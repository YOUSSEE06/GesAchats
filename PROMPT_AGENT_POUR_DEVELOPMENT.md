# 🤖 PROMPT POUR AGENT AI - Développement GesAchats

**À utiliser pour chaque étape de développement | .NET 10.0 | Approche Progressive**

---

## 📋 INSTRUCTIONS GLOBALES POUR L'AGENT

```
Tu es un agent de développement senior spécialisé en C#/.NET et WPF.
Tu vas développer l'application "GesAchats" ÉTAPE PAR ÉTAPE.

RÈGLES CRITIQUES À RESPECTER :
1. ✅ Développe UNE SEULE étape à la fois
2. ✅ Arrête à la fin de l'étape (ne commence PAS la suivante)
3. ✅ Mets avencement.md à jour AVANT de finir
4. ✅ Code = prêt à compiler et tester
5. ✅ Demande confirmation avant chaque nouvel étape
6. ✅ Fournis du contexte technique pour chaque décision

CONTEXTE DU PROJET :
- Stack : .NET 10.0 LTS, C# 14.0+, WPF, EF Core 10+, SQL Server Express
- Solution : 3 projets (GesAchats.WPF, GesAchats.Core, GesAchats.Data)
- Architecture : 3-tiers (Présentation → Métier → Données)
- Utilisateurs : 10-12 simultanés max
- Plateforme : Windows 10/11 uniquement
```

---

## 🎯 TEMPLATE DE PROMPT POUR CHAQUE ÉTAPE

**À adapter selon l'étape à faire :**

```
═══════════════════════════════════════════════════════════
ÉTAPE [N] : [NOM DE L'ÉTAPE]
═══════════════════════════════════════════════════════════

📍 STATUT ACTUEL :
[Copie le contenu de la section "Phase actuelle" dans avencement.md]

🎯 OBJECTIF DE CETTE ÉTAPE :
[Copie l'objectif de l'étape dans le plan]

📦 LIVRABLES ATTENDUS :
[Liste les fichiers/classes/éléments à créer]

📋 DÉTAILS TECHNIQUES :
- Architecture : [Patterns utilisés]
- Technologies : [NuGet packages, outils]
- Dépendances : [Quoi de la phase précédente est requis]

✅ CRITÈRES D'ACCEPTATION :
- Le code compile sans erreurs
- Pas de warnings (niveau 4)
- Structure respecte 3-tiers
- Commentaires XML sur classes/méthodes publiques

📝 RESSOURCES À UTILISER :
- Plan : PLAN_DEVELOPPEMENT_GESACHATS.md
- Schéma BD : Section "Structure Base de Données"
- Stack Tech : Section "Stack Technologique Détaillé"

⚠️ POINTS IMPORTANTS POUR CETTE ÉTAPE :
1. [Point technique spécifique]
2. [Point technique spécifique]
3. [Point technique spécifique]

🚀 INSTRUCTIONS :
1. Lis attentivement le plan (section correspondant à l'étape)
2. Crée les fichiers/structure
3. Écris le code complet et fonctionnel
4. Fournis les étapes pour tester
5. Mets avencement.md à jour
6. ATTENDS MA CONFIRMATION pour l'étape suivante

═══════════════════════════════════════════════════════════
```

---

## 📝 EXEMPLES DE PROMPTS PAR ÉTAPE

### **ÉTAPE 1 : Structure Solution & NuGet Packages**

```
═══════════════════════════════════════════════════════════
ÉTAPE 1 : Structure Solution Visual Studio & Configuration NuGet
═══════════════════════════════════════════════════════════

📍 STATUT ACTUEL :
Visual Studio project créé mais vide

🎯 OBJECTIF :
Mettre en place la structure 3-tiers complète et installer tous les packages NuGet

📦 LIVRABLES ATTENDUS :
Structure de dossiers :
```
GesAchats.sln
├── GesAchats.WPF/
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── ViewModels/
│   ├── Views/
│   ├── Styles/
│   └── GesAchats.WPF.csproj
├── GesAchats.Core/
│   ├── Entities/
│   ├── Services/
│   ├── DTOs/
│   ├── Validators/
│   ├── Exceptions/
│   └── GesAchats.Core.csproj
├── GesAchats.Data/
│   ├── Context/
│   ├── Repositories/
│   ├── Migrations/
│   └── GesAchats.Data.csproj
```

📋 DÉTAILS TECHNIQUES :

Packages NuGet à installer :

**GesAchats.WPF :**
- Microsoft.Extensions.DependencyInjection (8.0.0+)
- Serilog (4.0.0+)

**GesAchats.Core :**
- FluentValidation (11.9.0+)
- Serilog (4.0.0+)

**GesAchats.Data :**
- Microsoft.EntityFrameworkCore (10.0.0+)
- Microsoft.EntityFrameworkCore.SqlServer (10.0.0+)
- Microsoft.EntityFrameworkCore.Design (10.0.0+)
- Dapper (2.1.0+)

Tous les projets :
```xml
<TargetFramework>net10.0-windows</TargetFramework>
<UseWPF>true</UseWPF>
<LangVersion>latest</LangVersion>
<Nullable>enable</Nullable>
```

✅ CRITÈRES D'ACCEPTATION :
- ✅ Solution compile (Ctrl+Shift+B)
- ✅ Pas de warnings
- ✅ Structure de dossiers respecte le plan
- ✅ .csproj files contiennent les bonnes références
- ✅ Chaque projet référence correctement les dépendances
- ✅ GesAchats.WPF référence GesAchats.Core et GesAchats.Data
- ✅ GesAchats.Core référence GesAchats.Data

⚠️ POINTS IMPORTANTS :
1. TargetFramework DOIT être "net10.0-windows" (pas seulement "net10.0")
2. UseWPF=true seulement dans le projet WPF
3. Assure-toi que les références inter-projets sont correctes
4. Nullable=enable dès le départ (type safety)

🚀 INSTRUCTIONS :
1. Crée les 3 projets dans Visual Studio
2. Crée la structure de dossiers
3. Ajoute les packages NuGet
4. Configure les .csproj files
5. Teste la compilation
6. Mets à jour avencement.md avec les détails des packages installés
7. ATTENDS MA CONFIRMATION pour l'étape 2

═══════════════════════════════════════════════════════════
```

### **ÉTAPE 2 : Entités Métier & DbContext**

```
═══════════════════════════════════════════════════════════
ÉTAPE 2 : Créer les Entités Métier & DbContext EF Core
═══════════════════════════════════════════════════════════

📍 STATUT ACTUEL :
Solution structurée, packages installés

🎯 OBJECTIF :
Créer les entités métier et le DbContext EF Core selon le schéma BD du plan

📦 LIVRABLES ATTENDUS :
Fichiers dans GesAchats.Core/Entities/ :
- User.cs
- Role.cs
- Supplier.cs
- Quotation.cs
- PurchaseOrder.cs
- DeliveryNote.cs
- Invoice.cs
- Payment.cs
- StockLevel.cs
- AuditLog.cs

Fichier dans GesAchats.Data/Context/ :
- GesAchatsDbContext.cs

📋 DÉTAILS TECHNIQUES :
- Utilise Entity Framework Core 10.0
- Conventions de nommage : PascalCase pour classes/propriétés
- Validation avec Data Annotations
- Shadow properties pour audit (CreatedAt, UpdatedAt, CreatedBy)

✅ CRITÈRES D'ACCEPTATION :
- Toutes les entités compilent
- DbContext configuré correctement
- Relations FK/PK définies
- Conventions EF Core respectées
- Pas de warnings null-safety

🚀 INSTRUCTIONS :
1. Examine la section "Structure Base de Données" du plan
2. Crée chaque entité dans GesAchats.Core/Entities/
3. Ajoute validations data annotations
4. Crée DbContext dans GesAchats.Data/Context/
5. Configure les relationships dans OnModelCreating
6. Teste la compilation
7. Mets à jour avencement.md

═══════════════════════════════════════════════════════════
```

### **ÉTAPE 3 : Migration EF Core & Seed Données**

```
═══════════════════════════════════════════════════════════
ÉTAPE 3 : Créer Migration EF Core & Seed Données
═══════════════════════════════════════════════════════════

📍 STATUT ACTUEL :
Entités & DbContext créés

🎯 OBJECTIF :
Créer la migration EF Core et initialiser la BD avec données de base

📦 LIVRABLES ATTENDUS :
- Initial migration (GesAchats.Data/Migrations/)
- Seed data (rôles, utilisateurs test, fournisseurs)
- Script de création BD local

📋 DÉTAILS TECHNIQUES :
Connection string : 
- Dev : (localdb)\mssqllocaldb (LocalDB)
- Production : À configurer avec SQL Server Express

Commandes :
```powershell
dotnet ef migrations add Initial -p GesAchats.Data -s GesAchats.WPF
dotnet ef database update -p GesAchats.Data -s GesAchats.WPF
```

✅ CRITÈRES D'ACCEPTATION :
- Migration crée sans erreurs
- BD créée localement (LocalDB)
- Données seed présentes
- Toutes les tables créées
- Relations étrangères en place

🚀 INSTRUCTIONS :
1. Configure appsettings.json avec connection string
2. Crée migration initiale
3. Exécute la migration
4. Crée seed data (roles, users, suppliers)
5. Vérifie les tables dans SSMS
6. Mets à jour avencement.md

═══════════════════════════════════════════════════════════
```

---

## 🔄 CYCLE DE COMMUNICATION À CHAQUE ÉTAPE

### Phase 1 : Tu demandes à l'agent
```
[Copie le prompt de l'étape]
```

### Phase 2 : L'agent développe
```
L'agent crée le code complet
```

### Phase 3 : L'agent signale la fin
```
✅ ÉTAPE [N] COMPLÉTÉE

📦 Fichiers créés :
- ...
- ...

✅ Tests réussis :
- Le code compile
- ...

📝 avencement.md mis à jour

⏸️ EN ATTENTE DE CONFIRMATION POUR L'ÉTAPE [N+1]
```

### Phase 4 : Tu valides et dis "prêt pour étape suivante"
```
Merci ! C'est bon. Prêt pour l'étape [N+1].
```

### Phase 5 : L'agent passe à l'étape suivante
```
[Commence la prochaine étape]
```

---

## 📊 TEMPLATE AVANCEMENT.MD

Crée ce fichier à la racine de ton projet :

```markdown
# 📊 Avancement GesAchats - Suivi du Développement

**Version .NET : 10.0 LTS**
**Dernière mise à jour : [DATE]**
**Étape actuelle : [N]**

---

## 🎯 Phase Actuelle

### Étape [N] : [NOM]
- Status : 🔄 EN COURS | ✅ COMPLÉTÉE | ❌ BLOQUÉE
- Début : [DATE]
- Fin estimée : [DATE]

**Détails :**
- Tâches complétées :
  - [ ] Tâche 1
  - [ ] Tâche 2
  
**Fichiers créés :**
- GesAchats.WPF/...
- GesAchats.Core/...
- GesAchats.Data/...

**Notes techniques :**
- [Problèmes rencontrés]
- [Décisions prises]

---

## 📋 Historique Phases

### ✅ Phase 1 : Structure Solution & NuGet (Semaine 1)
- Status : COMPLÉTÉE
- Détails :
  - Solution créée avec 3 projets
  - NuGet packages installés
  - Structure de dossiers mise en place

### 🔄 Phase 2 : Entités Métier & DbContext (Semaine 2)
- Status : EN COURS
- Début : [DATE]
- Fin estimée : [DATE]

---

## 🔗 Dépendances Bloquantes

- Aucune actuellement

---

## 📌 Points Critiques

1. [Point important à surveiller]
2. [Point important à surveiller]

---

## 📞 Contact & Ressources

- Plan complet : PLAN_DEVELOPPEMENT_GESACHATS.md
- Stack : .NET 10.0, C# 14.0+, WPF, EF Core 10+
```

---

## 🚀 COMMENT UTILISER CETTE PROMPT

### Pour chaque étape :

1. **Copie le template approprié** (voir exemples ÉTAPE 1, 2, 3...)
2. **Adapte avec ton contexte actuel** (remplace les [...]
3. **Envoie à l'agent IA**
4. **Attends que l'agent signale "ÉTAPE COMPLÉTÉE"**
5. **Valide le code**
6. **Dis "Prêt pour étape suivante"**
7. **Répète**

### Règles d'or :

✅ **Une étape = Un prompt complet**
✅ **L'agent doit faire le code complet en une réponse**
✅ **Mise à jour avencement.md obligatoire**
✅ **Pas de passage à l'étape suivante sans confirmation**
✅ **Code = compilable immédiatement**

---

**Bonne chance avec ton développement GesAchats ! 🚀**
