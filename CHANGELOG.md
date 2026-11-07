# Changelog

## [2.3.0] — 2026-11-07

### Ajouts
- Interface terminal (TUI) en plein écran : thème sombre/clair, menu F1–F9, assistant IA intégré, catalogue auto de toutes les commandes.
- Assistant IA contextuel : connaît le projet courant, les outils et les templates ; propose une commande que tu confirmes.
- Interface web locale réécrite : tableau de bord, nouveau projet, stack, ajout, templates, configuration, IA, statistiques.
- Page de configuration enrichie dans le TUI (thème, IA, auteur, société, licence, framework, defaults).
- Scripts d'installation automatique (`install.sh`, `install.bat`) publiés sur la release.
- Sérialisation JSON sans réflexion (AOT) pour `list`, `stats` et `doctor --json`.

### Corrections et durcissement
- Sécurité renforcée : suppression de `/api/run`, validation des noms, liste blanche des commandes, anti-CSRF.
- Les outils natifs créent eux-mêmes le dossier du projet.
- Correction des infos d'installation (suppression des méthodes non supportées).
- Mise à jour de la documentation et du site.

## [2.2.0] — 2026-11-06

### Migration
- Migration vers .NET 10 (csproj, workflows CI, scripts de publication, README).

### Ajouts
- Nouvelles commandes : `scaffold list`, `scaffold stats`, `scaffold ai`
- `scaffold new` : `--list`, `--force`, `--framework`, validation du nom, détection de dossier non vide
- `scaffold add` : générateurs par langage (.NET, Node, Python, Go, Rust) et `--list`
- `scaffold init` : clone sécurisé, compose `full` avec Dockerfiles, CI générée par langage
- `scaffold doctor` : versions réelles des outils, configuration DB, sortie `--json`
- Options globales `--yes`, `--no-color`, `--verbose`, `--quiet`, `--version`

### Corrections et durcissement
- Exécution des processus sans injection shell (`ArgumentList`), timeout et arrêt de l'arbre de processus
- Les outils natifs (`dotnet new`, `npm create`, `cargo new`, `flutter create`, `composer create-project`, `rails new`, `mix new`) créent eux-mêmes leur dossier
- Configuration chiffrée (AES-GCM), permissions 0600, écriture atomique
- Mises à jour : vérification SHA-256 et extraction sécurisée
- Comparaison SemVer correcte

## [2.0.0] — 2026-05-31

### Ajouts
- 40+ commandes : `stack`, `docker-compose`, `kubernetes`, `gitlab-ci`, `terraform`, `cloud-init`, `vscode-extension`, `vscode-settings`, `ui`, `migration`, `batch`, `config`, `plugin`, `search`, `generate`, `docker`, `github`, `vscode`, `deploy`, `workspace`, `update-deps`, `store`, `template from-dir`
- `scaffold new` : menu interactif, composition `+` (ex: `webapi+react`), templates registry
- `scaffold deploy` : Vercel, Railway, Docker, GitHub Pages avec auto-détection
- `scaffold update` : mise à jour automatique avec backup/restore, détection RID
- `scaffold workspace` : monorepo npm/dotnet/Cargo
- `scaffold update-deps` : npm, NuGet, Cargo, Go, pip
- `scaffold store` : marketplace HTML statique pour templates
- `scaffold bug` : rapport de bug par mail + GitHub
- IA intégrée : OpenAI, Claude, Gemini, Grok
- Template registry : `template from-dir` + génération depuis registry
- Support AOT natif, binaire unique ~12 MB
- 8 plateformes : linux-x64, linux-musl-x64, linux-arm64, win-x64, win-x86, win-arm64, osx-x64, osx-arm64
- GitHub Actions CI : build automatique des 8 plateformes sur tag `v*`
- Site de documentation complet avec références des 40 commandes
- Extension VS Code intégrée (scaffold + commandes)

### Technique
- .NET 9 avec `System.CommandLine` 2.0.8
- `PublishAot` avec zéro avertissement
- `JsonContext` source-generated pour sérialisation AOT
- `Models.cs` : records typés (pas d'anonymes)
- `AIService` refactoré avec `StringContent` pour Gemini
- Tous les services en statiques, zéro dépendance DI

### Corrections
- Plus de warnings AOT (anonymous types supprimés)
- `BugCommand` utilise `UseShellExecute` pour ouvrir mailto sans blocage
- Template composition gère correctement les chemins
- UpdateService lit la version depuis l'assembly
