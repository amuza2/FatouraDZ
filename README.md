**Application de facturation pour entreprises algériennes**

FatouraDZ est une application de bureau permettant aux entreprises algériennes (Auto-entrepreneurs, Forfait, Sociétés) de créer, gérer et exporter des factures conformes à la législation algérienne.

> ⚠️ **Note** : Conçue pour le marché algérien (TVA, timbre fiscal, numérotation conforme).

<img width="1584" height="811" alt="image" src="https://github.com/user-attachments/assets/7c582724-7c47-40b1-9277-0f252d03fe65" />

## Fonctionnalités

- **Multi-entreprises** : Gestion de plusieurs entreprises (Auto-entrepreneur, Forfait, Société)
- **Gestion des clients** : Base de données clients avec recherche et informations fiscales
- **Création de factures** : Factures, proformas et avoirs avec remises (produit et globale)
- **Calculs automatiques** : TVA (19%/9%), timbre fiscal, retenue à la source
- **Export PDF / Excel** : Génération professionnelle avec aperçu intégré
- **Historique** : Suivi des factures avec statuts, paiement et archivage
- **Comptabilité** : Journal de transactions avec recettes et dépenses, catégories personnalisées, filtrage par type/période/catégorie et pagination
- **Archivage** : Archivage des transactions et factures avec possibilité de restauration
- **Interface Material Design** : UI moderne avec le thème Material.Avalonia

## Technologies

- .NET 10 / Avalonia UI
- Material.Avalonia (Material Design)
- SQLite (locale)
- QuestPDF / ClosedXML
- MVVM (CommunityToolkit.Mvvm)

## Installation

### Téléchargement

Les versions publiées sont sur la page [Releases](https://github.com/amuza2/FatouraDZ/releases).
Le runtime .NET est embarqué : aucune dépendance à installer au préalable.

`FatouraDZ-<version>-linux-x64.tar.gz` — installe le lanceur et l'icône pour
l'utilisateur courant :

```bash
tar -xzf FatouraDZ-*-linux-x64.tar.gz
cd FatouraDZ-*-linux-x64
./install.sh              # --prefix /usr/local pour une installation système
```

`FatouraDZ-<version>-x86_64.AppImage` — exécutable unique, sans installation :

```bash
chmod +x FatouraDZ-*-x86_64.AppImage
./FatouraDZ-*-x86_64.AppImage
```

`FatouraDZ-<version>-amd64.deb` — paquet Debian/Ubuntu : `sudo apt install ./FatouraDZ-*-amd64.deb`

`FatouraDZ-<version>-windows-x64-setup.exe` — installateur Windows.
L'archive `FatouraDZ-<version>-win-x64.zip` est la version portable.

Chaque artefact est accompagné d'un fichier `.sha256`.

### Depuis les sources

```bash
git clone https://github.com/amuza2/FatouraDZ.git
cd FatouraDZ
dotnet run --project src/FatouraDZ.csproj
```

## Construction des livrables

```bash
./scripts/build-release.sh                          # Linux (courant) + Windows portable
./scripts/build-release.sh --archs "linux-x64 linux-arm64"
./scripts/build-release.sh --no-windows --no-appimage
```

Tout est écrit dans `artifacts/` : tarball, `.deb`, `.AppImage`, `.zip` portable
Windows, et leurs empreintes SHA-256. L'installateur Windows est construit par la
CI (le compilateur Inno Setup n'existe que sous Windows).

L'icône de l'application vient d'une source unique, `src/Assets/invoice.png` :
`./scripts/gen-icons.sh` en dérive les PNG empaquetés (`packaging/icons/`) et
l'ICO Windows (`src/Assets/fatouradz.ico`). Les fichiers générés sont versionnés,
pour qu'une release n'exige ni ImageMagick ni rsvg-convert ; `build-release.sh`
refuse de construire une release dont les icônes ne correspondent plus à la
source (empreinte enregistrée dans `packaging/icons/icone-source.sha256`).

Le fichier `src/FatouraDZ.csproj` contient la version par défaut ; une release
l'écrase avec le tag (`-p:Version=<tag>`), donc l'écran « À propos » annonce
toujours la version réellement installée.

## Mises à jour et rapports de plantage

L'application interroge l'API GitHub au plus une fois par jour pour savoir si une
version plus récente est publiée, et l'annonce dans un bandeau ; elle ne
télécharge ni n'installe rien toute seule. Le contrôle se désactive dans
**Paramètres → À propos** (il ne transmet aucune donnée personnelle).

En cas de plantage, un rapport est écrit dans
`%LOCALAPPDATA%\FatouraDZ\crashes` (Windows) ou `~/.local/share/FatouraDZ/crashes`
(Linux), et un dialogue propose de le copier ou d'ouvrir un ticket. Les 20
derniers rapports sont conservés.

## Journaux

L'application écrit un journal quotidien dans `logs/`, à côté des rapports de
plantage (`%LOCALAPPDATA%\FatouraDZ`, `~/.local/share/FatouraDZ`). Il ne sert pas
à grand-chose tout seul, mais il est recopié dans chaque rapport de plantage : il
dit ce que l'application faisait juste avant, sans quoi une pile d'appels seule
est souvent inexploitable. Les 30 derniers jours sont conservés.

Le dossier s'ouvre depuis **Paramètres → Données → Journaux et rapports**. Les
chemins du dossier personnel y sont remplacés par `~` : un journal transmis ne
publie pas le nom de session de l'utilisateur.

### Mode verbeux

En cas de problème, lancer l'application avec `--verbose` écrit le détail du
diagnostic (versions, chemins, décisions de migration, taille des PDF générés) :

```bash
fatouradz --verbose        # ou : ./FatouraDZ-*-x86_64.AppImage --verbose
```

`--version` affiche la version et `--help` la liste des options. Sans `--verbose`,
cette même commande n'affiche que les charges utiles : un journal livré à un
utilisateur reste lisible.

## Licence

[MIT](LICENSE)

---

*Développé avec ❤️ pour les entrepreneurs algériens*
