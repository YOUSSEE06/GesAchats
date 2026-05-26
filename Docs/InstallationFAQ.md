# 🚀 Guide d'Installation & FAQ

## ⚙️ Prérequis Système
- Windows 10 ou 11 (64-bit).
- .NET 10.0 Runtime installé.
- Accès au serveur PostgreSQL (Local ou Réseau).

## 🛠️ Installation
1. **Extraction** : Extraire le dossier `GesAchats` sur le disque local.
2. **Configuration** : Éditer `appsettings.json` pour pointer vers votre instance PostgreSQL.
3. **Lancement** : Exécuter `GesAchats.WPF.exe`.
4. **Connexion** : Utiliser l'identifiant par défaut `admin` / `0000`.

## ❓ FAQ & Dépannage

### L'application freeze au démarrage ?
- Vérifiez que le serveur PostgreSQL est allumé.
- Vérifiez la chaîne de connexion dans `appsettings.json`.

### Le stock ne se met pas à jour ?
- Assurez-vous de cliquer sur "Valider Réception" dans le module "Bons de Livraison".

### Comment réinitialiser le mot de passe ?
- Contactez l'administrateur système pour une modification manuelle en base de données.

## 📞 Support
Pour toute assistance technique, contactez le service informatique interne.
