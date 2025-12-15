# FatouraDZ 🇩🇿

**Application de facturation pour auto-entrepreneurs algériens**

FatouraDZ est une application de bureau permettant aux auto-entrepreneurs algériens de créer, gérer et exporter des factures conformes à la législation algérienne.

> ⚠️ **Note importante** : Cette application est conçue exclusivement pour le marché algérien et reflète les lois fiscales et juridiques algériennes (TVA, timbre fiscal, numérotation des factures, etc.).

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

Ce projet est destiné à un usage personnel et professionnel pour les auto-entrepreneurs en Algérie.

---

*Développé avec ❤️ pour les entrepreneurs algériens*
