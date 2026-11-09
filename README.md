# Scaffolder

<p align="center">
  <img src="https://raw.githubusercontent.com/akaletekoffilevis/scaffold-docs/main/public/logo-horizontal.svg" alt="Scaffolder" width="500">
</p>

<p align="center">
  <b>CLI universel pour générer des projets dans tous les langages</b><br>
  48 commandes · 17 MB AOT · Multi-plateforme · IA intégrée
</p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/akaletekoffilevis/scaffold-docs/main/public/logo-minimal.svg">
    <img src="https://raw.githubusercontent.com/akaletekoffilevis/scaffold-docs/main/public/logo-light.svg" width="80" alt="Logo">
  </picture>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-2.5.0-blue?style=flat-square" alt="Version">
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet" alt=".NET 10">
  <img src="https://img.shields.io/badge/AOT-ready-green?style=flat-square" alt="AOT">
  <img src="https://img.shields.io/badge/platform-linux%20%7C%20macOS%20%7C%20Windows-lightgrey?style=flat-square" alt="Platforms">
  <img src="https://img.shields.io/badge/license-MIT-yellow?style=flat-square" alt="License">
</p>

---

## Installation

### Installation automatique (recommandée)

**Linux / macOS :**
```bash
curl -fsSL https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest/download/install.sh | bash
```

**Windows (PowerShell) :**
```powershell
irm https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest/download/install.bat -OutFile install.bat
.\install.bat
```

### Téléchargement direct

Téléchargez le binaire correspondant à votre plateforme depuis la [page des releases](https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/latest) :

| Plateforme | Fichier |
|------------|---------|
| Linux x64 | `scaffold-linux-x64.tar.gz` |
| Linux musl-x64 | `scaffold-linux-musl-x64.tar.gz` |
| Linux ARM64 | `scaffold-linux-arm64.tar.gz` |
| macOS Intel | `scaffold-osx-x64.tar.gz` |
| macOS Apple Silicon | `scaffold-osx-arm64.tar.gz` |
| Windows x64 | `scaffold-win-x64.tar.gz` |
| Windows x86 | `scaffold-win-x86.tar.gz` |

Puis :
```bash
tar -xzf scaffold-*.tar.gz
sudo mv scaffold /usr/local/bin/   # ou ~/.local/bin/
scaffold --version
```

### Compilation depuis les sources

```bash
git clone https://github.com/akaletekoffilevis/Scaffolder-CLI.git
cd Scaffolder-CLI/Scaffolder
dotnet restore
dotnet publish -c Release -r linux-x64 --self-contained
./bin/Release/net10.0/linux-x64/publish/scaffold --version
```

### Mise à jour

```bash
scaffold upgrade
```

---

## Utilisation rapide

```bash
# Assistant interactif (TUI)
scaffold new

# Générer un projet
scaffold new --template webapi --name mon-api --db postgresql --docker

# Stack fullstack
scaffold stack --name mon-app --frontend react --backend webapi --db postgres

# Ajouter un composant
scaffold add controller Users

# Diagnostic
scaffold doctor

# Interface web locale
scaffold ui

# Assistant IA
scaffold ai ask "Comment créer une API REST ?"
```

---

## Fonctionnalités

- **48 commandes** couvrant tout le cycle de vie d'un projet
- **15 écosystèmes** : .NET, Node.js, Rust, Go, Python, Flutter, PHP, Ruby, Java, etc.
- **63 templates** Hello World + frameworks
- **Interface terminal (TUI)** : thème sombre/clair, menu F1–F9, assistant IA intégré
- **Interface web locale** : tableau de bord, génération, configuration, IA
- **Assistant IA contextuel** : connaît votre projet et propose des commandes
- **AOT** : binaire natif ~17 MB, pas de runtime requis
- **Multi-plateforme** : Linux, macOS, Windows
- **Sécurité** : configuration chiffrée (AES-GCM), vérification SHA-256 des mises à jour
- **Mise à jour automatique** avec détection de plateforme

---

## Documentation

Documentation complète : [https://scaffolder-cli.vercel.app](https://scaffolder-cli.vercel.app)

---

## Contribuer

Les contributions sont les bienvenues ! Voir [CONTRIBUTING.md](CONTRIBUTING.md).

## Licence

Ce projet est sous licence [MIT](LICENSE).

---

<p align="center">
  <sub>Construit avec .NET 10 et System.CommandLine</sub>
</p>
