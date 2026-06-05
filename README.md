# Scaffolder

<p align="center">
  <img src="https://raw.githubusercontent.com/akaletekoffilevis/scaffold-docs/main/public/logo-horizontal.svg" alt="Scaffolder" width="500">
</p>

<p align="center">
  <b>CLI universel pour générer des projets dans tous les langages</b><br>
  40+ commandes · 12 MB AOT · Multi-plateforme · IA intégrée
</p>

<p align="center">
  <picture>
    <source media="(prefers-color-scheme: dark)" srcset="https://raw.githubusercontent.com/akaletekoffilevis/scaffold-docs/main/public/logo-minimal.svg">
    <img src="https://raw.githubusercontent.com/akaletekoffilevis/scaffold-docs/main/public/logo-light.svg" width="80" alt="Logo">
  </picture>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/version-2.0.0-blue?style=flat-square" alt="Version">
  <img src="https://img.shields.io/badge/.NET-9-512BD4?style=flat-square&logo=dotnet" alt=".NET 9">
  <img src="https://img.shields.io/badge/AOT-ready-green?style=flat-square" alt="AOT">
  <img src="https://img.shields.io/badge/platform-linux%20%7C%20macOS%20%7C%20Windows-lightgrey?style=flat-square" alt="Platforms">
</p>

---

## Installation

### Linux (glibc)

```bash
curl -L -o scaffold.tar.gz https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v2.0.0/scaffold-linux-x64.tar.gz
tar -xzf scaffold.tar.gz
sudo mv scaffold /usr/local/bin/
scaffold --help
```

Autres variantes : `linux-musl-x64` (Alpine), `linux-arm64`.

### macOS

```bash
curl -L -o scaffold.tar.gz https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v2.0.0/scaffold-osx-x64.tar.gz
tar -xzf scaffold.tar.gz
sudo mv scaffold /usr/local/bin/
scaffold --help
```

Variante Apple Silicon : `osx-arm64`.

### Windows (PowerShell)

```powershell
curl -L -o scaffold.tar.gz https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v2.0.0/scaffold-win-x64.tar.gz
tar -xzf scaffold.tar.gz
scaffold --help
```

### Homebrew

```bash
brew tap akaletekoffilevis/scaffolder
brew install scaffold
```

---

## Utilisation rapide

```bash
# Mode interactif (menu graphique)
scaffold new

# Générer un projet spécifique
scaffold new --template=webapi --name=mon-api
scaffold new --template=vite --name=frontend
scaffold new hello --language=rust --name=hello-rust

# Composition de templates (fullstack)
scaffold new webapi+react --name=full-app

# Déployer sur Vercel/Railway/Docker/GitHub Pages
scaffold deploy

# Mettre à jour l'outil
scaffold update

# Aide
scaffold --help
```

---

## Commandes (40+)

| Catégorie | Commandes |
|-----------|-----------|
| **Génération** | `new`, `stack`, `generate` |
| **Conteneurisation** | `docker`, `docker-compose`, `kubernetes` |
| **CI/CD** | `github`, `gitlab-ci` |
| **Éditeur** | `vscode`, `vscode-extension`, `ui` |
| **Déploiement** | `deploy` — Vercel, Railway, Docker, GitHub Pages |
| **Infrastructure** | `terraform`, `cloud-init` |
| **Base de données** | `migration` |
| **Monorepo** | `workspace` — npm, dotnet, Cargo |
| **Mise à jour** | `update-deps`, `update` |
| **Utilitaires** | `batch`, `config`, `bug`, `template from-dir`, `plugin`, `store`, `search`, `ai` |

Documentation complète : [https://scaffolder-cli.vercel.app](https://scaffolder-cli.vercel.app)

---

## Fonctionnalités

- **40+ commandes** — Init, build, test, docker, github, lint, format, registry, migration, batch
- **Multi-langage** — .NET, Node.js, Rust, Go, Python, Flutter, Laravel, Symfony, Rails, et 20+ autres
- **Fullstack** — Génère projet complet frontend + backend + base de données en une commande
- **IA intégrée** — Suggestions, explications et correction d'erreurs via OpenAI, Claude, Gemini ou Grok
- **AOT natif** — Compilé en binaire natif, pas de runtime nécessaire, démarrage instantané
- **Extensible** — Plugins, templates custom, registry communautaire, workspace VS Code
- **Auto-update** — Mise à jour automatique avec backup et rollback
- **Composition** — Assemblez plusieurs templates avec `+`

---

## Développement

```bash
git clone https://github.com/akaletekoffilevis/Scaffolder-CLI.git
cd scaffolder/Scaffolder
dotnet restore
dotnet build
dotnet run -- --help
```

### Publication AOT

```bash
dotnet publish -c Release -r linux-x64 --self-contained
./bin/Release/net9.0/linux-x64/publish/scaffold --help
```

### Tests

```bash
dotnet test
```

---

## Stack technique

- **Langage :** C# (.NET 9)
- **Framework CLI :** `System.CommandLine`
- **Compilation :** AOT (NativeAOT) — binaire unique sans runtime
- **CI :** GitHub Actions — build automatique pour 7 plateformes sur tag git

---

## Signaler un bug

```bash
scaffold bug
```

Ou ouvrir une issue : [github.com/akaletekoffilevis/Scaffolder-CLI/issues](https://github.com/akaletekoffilevis/Scaffolder-CLI/issues/new?labels=bug&template=bug_report.md)

---

<p align="center">
  <a href="https://scaffolder-cli.vercel.app">Documentation</a> &middot;
  <a href="https://github.com/akaletekoffilevis/Scaffolder-CLI">GitHub</a><br>
  <sub>Construit avec .NET 9 et System.CommandLine</sub>
</p>
