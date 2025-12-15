# FatouraDZ 🇩🇿

**Application de facturation pour auto-entrepreneurs algériens**

FatouraDZ est une application de bureau permettant aux auto-entrepreneurs algériens de créer, gérer et exporter des factures conformes à la législation algérienne.

> ⚠️ **Note importante** : Cette application est conçue exclusivement pour le marché algérien et reflète les lois fiscales et juridiques algériennes (TVA, timbre fiscal, numérotation des factures, etc.).

Screeshots:

<img width="1730" height="980" alt="image" src="https://github.com/user-attachments/assets/9d2f4b28-66c3-484d-b741-1664b3a6f5ec" />

<img width="1600" height="843" alt="image" src="https://github.com/user-attachments/assets/7ca39a78-9cea-43ef-bcc7-c2bd7c614e84" />


## Fonctionnalités

- **Création de factures** : Factures normales, avoirs et annulations
- **Calculs automatiques** : TVA (19% et 9%), timbre fiscal, retenue à la source
- **Gestion des clients** : Informations fiscales complètes (RC, NIS, NIF, AI)
- **Export PDF** : Génération de factures professionnelles au format PDF
- **Historique** : Suivi et filtrage des factures avec statuts (En attente, Payée, Annulée)
- **Montant en lettres** : Conversion automatique en français

## Technologies

- **Framework** : .NET 10 / Avalonia UI
- **Base de données** : SQLite (locale)
- **PDF** : QuestPDF
- **Architecture** : MVVM avec CommunityToolkit.Mvvm

## Installation

```bash
git clone https://github.com/amuza2/FatouraDZ.git
cd FatouraDZ/src
dotnet run
```

## Licence

Ce projet est sous licence [MIT](LICENSE).

---

*Développé avec ❤️ pour les entrepreneurs algériens*
