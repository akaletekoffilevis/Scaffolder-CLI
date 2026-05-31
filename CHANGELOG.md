# Changelog

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
