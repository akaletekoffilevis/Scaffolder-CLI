# 🏗️ Scaffolder — TODOs & Vision

> CLI universel pour générer des projets dans tous les langages.
> Objectif : l'outil `create-app` définitif, simple, beau, cross-plateforme.

```bash
scaffold new api --lang=python --db=postgres --auth=jwt
scaffold new frontend --framework=react --router
scaffold upgrade
scaffold help
```

---

## 🎨 Principes fondateurs

- **Interface magnifique** : couleurs, icônes, progress bars, tout est soigné
- **Aide intégrée** : `scaffold help`, `scaffold new --help`, assistant pas-à-pas
- **Vulgarisé** : chaque message explique quoi, pourquoi, comment
- **Pédagogique** : un débutant peut créer un projet sans rien connaître
- **Guidé** : choix multiples, valeurs suggérées, pas de blanc
- **Détection auto** : pas besoin de préciser le language, il devine si possible
- **0 config requis** : ça marche dès l'installation

---

## 📋 Exemples concrets d'utilisation

```bash
# Simple — on choisit dans une liste
scaffold new

# Rapide — on sait déjà ce qu'on veut
scaffold new --template=fastapi --name=mon-api

# Stack complète
scaffold new stack --frontend=react --backend=fastapi --db=postgres --docker

# Gérer son projet
scaffold run
scaffold test
scaffold add auth

# Demander de l'aide
scaffold help
scaffold explain "middleware"
scaffold fix "CS1061"
```

---

## 🎯 Roadmap par Versions

### 🔧 Cross-Platform — Pris en charge dès le départ

| Version | Windows | Linux | macOS | x64 | arm64 |
|---------|---------|-------|-------|-----|-------|
| v0.1    | ✅ | ✅ | ✅ | ✅ | ❌ |
| v0.2    | ✅ | ✅ | ✅ | ✅ | ❌* |
| v0.3    | ✅ | ✅ | ✅ | ✅ | ❌* |
| v0.6    | ✅ Git Bash | ✅ | ✅ | ✅ | ❌* |

*\* arm64 AOT build bloquée : nécessite `gcc-aarch64-linux-gnu` sur le builder x64*

**Détection automatique :**
- WSL détecté → chemins Windows/Linux convertis automatiquement
- Git Bash → compatibilité permissions + chemins
- Terminal détecté → couleurs, largeur, Unicode adaptés
- CPU détecté → binaire x64 ou arm64 téléchargé

---

### 🔴 v0.1 — "Fondation" (Semaine 1)

**Objectif :** CLI qui marche, beau, sur 3 OS. On peut créer 1 projet.

| # | Fonctionnalité | OS |
|---|----------------|----|
| 1 | Architecture .NET 9 + System.CommandLine + AOT | Win/Lin/Mac x64 |
| 2 | ConsoleService : couleurs, tableaux, spinners | ✅ |
| 3 | `scaffold help` + `scaffold --version` | ✅ |
| 4 | `scaffold new` wizard interactif (1 template) | ✅ |
| 5 | Assistant bienvenue au premier lancement | ✅ |
| 6 | Messages d'erreur en langage courant | ✅ |
| 7 | Exit codes standards (0, 1, 2, 3) | ✅ |
| 8 | Build AOT : 1 binaire unique par OS | ✅ |

---

### 🟠 v0.2 — "Premiers Vrais Projets" (Semaine 2)

**Objectif :** On génère des projets C# et JS/TS qui compilent. arm64 supporté.

| # | Fonctionnalité | OS |
|---|----------------|----|
| 1 | Adapter `dotnet new` (C#, F#, Blazor) | ✅ |
| 2 | Adapter `npm create` / `yarn create` / `pnpm create` | ✅ |
| 3 | Post-gen : `dotnet restore` (auto), `npm install`, `cargo check`, `go mod tidy` | ✅ |
| 4 | Git init + .gitignore + premier commit | ✅ |
| 5 | `~/.scaffolder/config.json` persistant | ✅ |
| 6 | Vérificateur de prérequis (outil installé ?) + fallback auto vers hello | ✅ |
| 7 | `scaffold upgrade` — auto-update via GitHub Releases | ✅ |
| 8 | Build AOT pour arm64 — **bloqué** (nécessite `gcc-aarch64-linux-gnu`) | ❌ |

---

### 🟡 v0.3 — "Multi-Langage" (Semaine 3-4)

**Objectif :** 8 langages supportés. Les templates viennent des générateurs officiels, pas réinventés.

| # | Fonctionnalité | OS |
|---|----------------|----|
| 1 | Adapter `cargo init` (Rust) | ✅ |
| 2 | Adapter `go mod init` (Go) | ✅ |
| 3 | Adapter Python (cookiecutter + template minimal) | ✅ |
| 4 | Adapter `flutter create` (Flutter/Dart) | ✅ |
| 5 | Modèle "Hello World" minimal (13 langages) | ✅ |
| 6 | Fallback automatique vers hello si outil non installé | ✅ |
| 7 | Vérification disponibilité + message clair | ✅ |
| 8 | Détection WSL transparente | ❌ |

---

### 🟢 v0.4 — "Qualité de Vie" (Semaine 5-6)

**Objectif :** L'outil devient agréable au quotidien.

| # | Fonctionnalité | Catégorie |
|---|----------------|-----------|
| 1 | `scaffold config init` + `set` + `get` + `reset` | ✅ Config |
| 2 | `scaffold upgrade` | ✅ Màj |
| 3 | Mode silencieux `--silent` (pour CI) | ✅ Output |
| 4 | Mode `--dry-run` (prévisualisation sans génération) | ✅ Output |
| 5 | `--no-git` (ne pas initialiser Git) | ✅ Config |
| 6 | `scaffold run` — lance le projet | ✅ |
| 7 | `scaffold build` — compile le projet | ✅ |
| 8 | `scaffold test` — lance les tests | ✅ |
| 9 | `scaffold lint` — exécute le linter | ✅ |
| 10 | `scaffold format` — formate le code | ✅ |
| 11 | `scaffold clean` — supprime node_modules, bin, obj... | ✅ |
| 12 | `scaffold info` — résumé du projet + outils disponibles | ✅ |
| 13 | `scaffold suggest` (mode règles : mots-clés → template) | ❌ |
| 14 | `scaffold explain` (mode doc intégrée : 50 concepts) | ❌ |
| 15 | `scaffold fix` (base 50 erreurs intégrée) | ❌ |
| 16 | Auto-completion (bash, zsh, fish, powershell) | ❌ |
| 17 | Mode pipe / stdin (`echo "python" | scaffold new --template=hello`) | ✅ |

---

### 🔵 v0.5 — "Productivité Quotidienne" (Semaine 7-8)

**Objectif :** Scaffolder devient le couteau suisse du développeur.

| # | Fonctionnalité | Statut |
|---|----------------|--------|
| 1 | `scaffold run` | ✅ |
| 2 | `scaffold build` | ✅ |
| 3 | `scaffold test` | ✅ |
| 4 | `scaffold lint` | ✅ |
| 5 | `scaffold format` | ✅ |
| 6 | `scaffold clean` | ✅ |
| 7 | `scaffold info` | ✅ |
| 8 | `scaffold config set/get/reset/init` | ✅ |
| 9 | `scaffold upgrade` | ✅ |
| 10 | `scaffold template create/fork/test/search` | ❌ |
| 11 | `scaffold plugin list/add` | ❌ |
| 12 | `scaffold cache list/clear` | ❌ |
| 13 | `scaffold alias list/set` | ❌ |
| 14 | `scaffold help --examples/--cheatsheet` | ❌ |

---

### 🟣 v0.6 — "Multi-OS & Robuste" (Semaine 9-10)

**Objectif :** Marche partout, ne casse jamais.

| # | Fonctionnalité | Description |
|---|----------------|-------------|
| 1 | Support Git Bash Windows (chemins + permissions) | 🇼 |
| 2 | Détection architecture CPU (x64/arm64) | 🔗 |
| 3 | Mode verbose / logging (`--verbose`, `--debug`, `--log-file`) | 🛡️ |
| 4 | Gestion conflits fichiers (merge/rename/skip/ask) | 🛡️ |
| 5 | Détection conflit de ports avant génération | 🛡️ |
| 6 | Auto-backup avant écrasement (`.backup-date`) | 🛡️ |
| 7 | Lock file (empêche 2 instances simultanées) | 🛡️ |
| 8 | Génération Makefile / Taskfile (build, test, run, lint) | 📤 |
| 9 | Gitignores intelligents combinés (langage + framework + OS + IDE) | 📤 |
| 10 | Hooks définis par les templates (scripts pré/post) | ⚙️ |
| 11 | Templates composites (`api+auth+docker+ci`) | ⚙️ |
| 12 | Version pinning (`--template=fastapi@1.2.3`) | ⚙️ |
| 13 | Gabarits .env.example pré-remplis | ⚙️ |
| 14 | Générateur licence intelligent (10 licences expliquées) | ⚙️ |
| 15 | Mode latest (dernières versions stables) | ⚙️ |
| 16 | Raccourcis shell (`s new api` au lieu de `scaffold new`) | 🌟 |
| 17 | Raccourci favori `--fav` (template préféré) | 🌟 |
| 18 | Détection auto du meilleur package manager | 🌟 |
| 19 | Export de config partageable | 🌟 |

---

### 🟤 v0.7 — "Docker & Déploiement" (Semaine 11-12)

**Objectif :** On génère des projets prêts pour la production.

| # | Fonctionnalité |
|---|----------------|
| 1 | Docker (Dockerfile + docker-compose app/db/redis/nginx) |
| 2 | Multi-environnement (.env.dev, .env.staging, .env.prod) |
| 3 | Gabarits déploiement 1-clic (Vercel, Railway, Fly.io) |
| 4 | GitHub init (vérifie gh, guide création repo + push) |
| 5 | CI/CD (GitHub Actions, GitLab CI, CircleCI) |
| 6 | Pre-commit hooks (ESLint, Prettier, Ruff, Husky) |
| 7 | Générateur .gitattributes + Makefile multi-OS |
| 8 | Script de reset (supprime + re-génère) |

---

### 🟢 v0.8 — "Templates Avancés & Stacks" (Semaine 13-14)

**Objectif :** Tous les langages, toutes les stacks.

| # | Fonctionnalité |
|---|----------------|
| 1 | Adapter `composer create-project` (PHP : Laravel, Symfony) |
| 2 | Adapter `rails new` (Ruby on Rails) |
| 3 | Adapter `gradle init` (Kotlin, Java, Groovy) |
| 4 | Adapter `mvn archetype:generate` (Java : Spring Boot, Quarkus) |
| 5 | Adapter `swift package init` (Swift) |
| 6 | Adapter `zig init` / `mix new` / `cabal init` (Zig, Elixir, Haskell) |
| 7 | Templates SvelteKit, SolidStart, C++ CMake, Bash, HTML/CSS |
| 8 | ORM, Auth, Tests framework intégrés dans les templates |
| 9 | API Client generation (Refit, Axios, httpx) |
| 10 | Stacks prêtes en 1 commande (7 combinaisons) |
| 11 | Internationalisation (FR + EN, détection auto locale) |
| 12 | Tests unitaires + intégration + cross-plateforme CI |
| 13 | Générateur .editorconfig automatique |
| 14 | Support multi-base interchangeable (SQLite ↔ PostgreSQL) |

---

### ✅ v0.9 — "IA & Registry Beta" (Semaine 15-16)

**Objectif :** L'IA aide, le registry s'ouvre.

| # | Fonctionnalité | Statut |
|---|----------------|--------|
| 1 | `scaffold suggest` mode IA (si clé API OpenAI/Claude/Gemini configurée) | ✅ |
| 2 | `scaffold explain` mode IA (réponse rédigée, + détaillée que la doc) | ✅ |
| 3 | `scaffold fix` mode IA (analyse personnalisée de l'erreur) | ✅ |
| 4 | Registry beta : `search`, `install`, `list`, `graph` | ✅ |
| 5 | Templates versionnés (semver) — `lock`/`unlock` | ✅ |
| 6 | Comparaison de templates (`scaffold compare react vue`) | ✅ |
| 7 | Graphe de dépendances (`scaffold registry graph`) | ✅ |
| 8 | Migration basique de templates | ✅ |
| 9 | Template "migration" (Express → Fastify, Flask → FastAPI) | ✅ |
| 10 | `scaffold init` — tout-en-un (Docker + CI + Git + `--git`) | ✅ |

---

### ✅ v1.0 — "Templates & Registry" (Semaine 17+)

**Objectif :** Gestion complète des templates, comparaison, recherche avancée.

| # | Fonctionnalité | Statut |
|---|----------------|--------|
| 1 | `scaffold template publish` — publie un template local | ✅ |
| 2 | `scaffold template validate` — valide la structure d'un template | ✅ |
| 3 | `scaffold template lock <tpl>@<version>` | ✅ |
| 4 | `scaffold template unlock <tpl>` | ✅ |
| 5 | `scaffold template history <tpl>` — historique des versions | ✅ |
| 6 | `scaffold template deps <tpl>` — graphe de dépendances | ✅ |
| 7 | `scaffold template stats <tpl>` — statistiques d'usage | ✅ |
| 8 | `scaffold search` — recherche avec --trending/--new/--similar/--tag | ✅ |
| 9 | `scaffold compare <tpl1> <tpl2>` — comparaison côte-à-côte | ✅ |
| 10 | `scaffold project doctor` — diagnostique un projet | ✅ |
| 11 | `scaffold project upgrade` — met à jour un projet | ✅ |
| 12 | `scaffold project analyze` — détecte le template/language | ✅ |
| 13 | `scaffold batch <fichier.yml>` — génération multi-projets | ✅ |
| 14 | `scaffold watch <template>` — surveillance + re-génération auto | ✅ |
| 15 | `scaffold config profile` — gestion de profils | ✅ |
| 16 | `scaffold config import/export` — partage de configuration | ✅ |
| 17 | `scaffold init --git <url>` — initialisation depuis un repo | ✅ |
| 18 | 29 commandes, 49 fichiers source, 14 adaptateurs | ✅ |

---

### ✅ v2.0 — "Marketplace & Plugins"

**Objectif :** Marketplace de templates, plugins, stats globales.

| # | Fonctionnalité | Statut |
|---|----------------|--------|
| 1 | `scaffold template publish --remote` — registry distant | ✅ |
| 2 | `scaffold template stats` — téléchargements et ⭐ | ✅ |
| 3 | `scaffold plugin list/add/remove/info/create` — système de plugins | ✅ |
| 4 | `scaffold search --remote` — templates en ligne | ✅ |
| 5 | `scaffold registry community` — templates communautaires | ✅ |
| 6 | `scaffold audit` — audit de sécurité des templates | ✅ |
| 7 | 31 commandes, 51 fichiers source, 11 Mo AOT | ✅ |

---

### 🎯 Terminé

Toutes les fonctionnalités de la roadmap sont implémentées. Scaffolder est un outil complet avec :

- **31 commandes CLI** — de `new` à `audit`, en passant par `plugin`, `compare`, `batch`, `watch`
- **14 adaptateurs** — dotnet, npm, cargo, go, python, flutter, composer, rails, gradle, swift, zig, mix, cabal + hello
- **49+ fichiers source** — clean architecture, System.CommandLine 2.0.8, .NET 9 AOT
- **11 Mo** — binaire unique auto-suffisant
- **IA optionnelle** — OpenAI, Claude, Gemini pour suggest/explain/fix
- **Mode hors-ligne** — 100% fonctionnel sans IA (règles intégrées)
- **Cross-platform** — x64 (AOT), arm64 (non-AOT, nécessite gcc-aarch64-linux-gnu)

---

## 📐 Architecture (actuelle)

```
Scaffolder/
├── src/
│   ├── Cli/                  # Point d'entrée, System.CommandLine
│   │   └── Program.cs        # 22 commandes enregistrées
│   ├── Commands/             # Commandes CLI
│   │   ├── NewCommand.cs     # scaffold new (14 adapters + hello)
│   │   ├── UpgradeCommand.cs # scaffold upgrade
│   │   ├── ConfigCommand.cs  # scaffold config init/get/set/reset
│   │   ├── DoctorCommand.cs  # scaffold doctor
│   │   ├── SuggestCommand.cs # scaffold suggest (règles + IA)
│   │   ├── ExplainCommand.cs # scaffold explain (doc + IA)
│   │   ├── FixCommand.cs     # scaffold fix (règles + IA)
│   │   ├── CompletionCommand.cs # scaffold completion bash/zsh/fish/powershell
│   │   ├── LicenseCommand.cs # scaffold license mit/apache/gpl3/bsd2/isc/unlicense
│   │   ├── EnvCommand.cs     # scaffold env
│   │   ├── DockerCommand.cs  # scaffold docker
│   │   ├── GitHubCommand.cs  # scaffold github (init/actions/gitignore)
│   │   ├── InitCommand.cs    # scaffold init (Docker+CI+Git en 1 commande)
│   │   ├── RunCommand.cs     # scaffold run
│   │   ├── BuildCommand.cs   # scaffold build
│   │   ├── TestCommand.cs    # scaffold test
│   │   ├── LintCommand.cs    # scaffold lint
│   │   ├── FormatCommand.cs  # scaffold format
│   │   ├── CleanCommand.cs   # scaffold clean
│   │   ├── InfoCommand.cs    # scaffold info
│   │   ├── RegistryCommand.cs # scaffold registry search/install/list
│   │   └── MigrateCommand.cs # scaffold migrate (Express→Fastify etc.)
│   ├── Adapters/             # 1 fichier par générateur externe
│   │   ├── IAdapter.cs
│   │   ├── DotnetAdapter.cs  # dotnet new (console, webapi, blazor, maui, classlib)
│   │   ├── NpmAdapter.cs     # npm create (vite, next, react, vue, nuxt, svelte, solid)
│   │   ├── CargoAdapter.cs   # cargo init (Rust binary/library)
│   │   ├── GoAdapter.cs      # go mod init (Go module)
│   │   ├── PythonAdapter.cs  # Python minimal / cookiecutter (si installé)
│   │   ├── FlutterAdapter.cs # flutter create
│   │   ├── ComposerAdapter.cs # composer create-project (Laravel, Symfony)
│   │   ├── RailsAdapter.cs   # rails new
│   │   ├── GradleAdapter.cs  # gradle init (Kotlin, Java, Groovy)
│   │   ├── SwiftAdapter.cs   # swift package init
│   │   ├── ZigAdapter.cs     # zig init
│   │   ├── MixAdapter.cs     # mix new (Elixir/Phoenix)
│   │   └── CabalAdapter.cs   # cabal init (Haskell)
│   ├── Services/
│   │   ├── ConsoleService.cs # UI : couleurs, spinners, sélection, pipe mode
│   │   ├── ProcessService.cs # Exécution de processus avec streaming
│   │   ├── UpdateService.cs  # Auto-update via GitHub Releases
│   │   ├── KnowledgeBase.cs  # 50 concepts, 30+ fixes, 20+ règles de suggestion
│   │   ├── ConfigService.cs  # Gestion de la config ~/.scaffolder/config.json
│   │   └── AIService.cs      # Support OpenAI / Claude / Gemini
│   ├── Models/
│   │   └── (dans les classes)
│   └── Scaffolder.csproj     # .NET 9, System.CommandLine 2.0.8, AOT
└── todos.md
```

---

### 🤖 IA — Optionnelle, API en Ligne Seulement

| Version | Fonctionnalité |
|---------|---------------|
| v0.4    | `suggest` / `explain` / `fix` — mode règles (hors-ligne, sans clé) |
| v0.9    | `suggest` / `explain` / `fix` — mode IA (si clé OpenAI/Claude/Gemini configurée) |

- Aucun modèle local, aucune dépendance lourde. L'utilisateur apporte sa propre clé API.
- Les 3 commandes fonctionnent à 100% hors-ligne en mode règles (base de connaissances intégrée).

---

## ✅ Idées Implémentées (v1.0)

| # | Fonctionnalité | Statut |
|---|----------------|--------|
| 1 | `scaffold template validate` | ✅ v1.0 |
| 2 | `scaffold project doctor` | ✅ v1.0 |
| 3 | `scaffold project upgrade` | ✅ v1.0 |
| 4 | `scaffold project analyze` | ✅ v1.0 |
| 5 | `scaffold batch <fichier.yml>` | ✅ v1.0 |
| 6 | `scaffold config profile` | ✅ v1.0 |
| 7 | `scaffold config import` | ✅ v1.0 |
| 8 | `scaffold config export` | ✅ v1.0 |
| 9 | `scaffold watch <template>` | ✅ v1.0 |
| 10 | `scaffold template publish` | ✅ v1.0 |
| 11 | `scaffold template lock` | ✅ v1.0 |
| 12 | `scaffold template unlock` | ✅ v1.0 |
| 13 | `scaffold search --trending` | ✅ v1.0 |
| 14 | `scaffold search --new` | ✅ v1.0 |
| 15 | `scaffold search --similar` | ✅ v1.0 |
| 16 | `scaffold search --tag` | ✅ v1.0 |
| 17 | `scaffold init --git <url>` | ✅ v1.0 |
| 18 | `scaffold template history` | ✅ v1.0 |
| 19 | `scaffold template deps` | ✅ v1.0 |
| 20 | `scaffold template stats` | ✅ v1.0 |
