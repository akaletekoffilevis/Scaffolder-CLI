# Contribuer à Scaffolder

Merci de votre intérêt pour Scaffolder ! Ce document décrit comment contribuer au projet.

## Code de conduite

Ce projet adhère au [Contributor Covenant](CODE_OF_CONDUCT.md). En participant, vous vous engagez à respecter ce code.

## Comment contribuer

### Signaler un bug

1. Vérifiez que le bug n'a pas déjà été signalé dans les [issues](https://github.com/akaletekoffilevis/Scaffolder-CLI/issues).
2. Ouvrez une nouvelle issue avec le template **Bug report**.
3. Incluez : version de Scaffolder (`scaffold --version`), OS, étapes de reproduction, comportement attendu vs observé.

### Proposer une fonctionnalité

1. Ouvrez une issue avec le template **Feature request**.
2. Décrivez le besoin, la solution proposée et les alternatives envisagées.

### Soumettre une pull request

1. Forkez le dépôt et créez une branche depuis `main` :
   ```bash
   git checkout -b feat/ma-fonctionnalite
   ```
2. Installez le SDK .NET 10.
3. Compilez et testez :
   ```bash
   cd Scaffolder
   dotnet build Scaffolder.csproj -c Release
   dotnet test ../Scaffolder.Tests/Scaffolder.Tests.csproj -c Release
   ```
4. Respectez le style existant (C# 12, nullable activé, pas de warnings nouveaux).
5. Commitez avec un message clair (convention Conventional Commits recommandée).
6. Ouvrez une PR vers `main` en remplissant le template.

### Ajouter un adapter (nouveau langage/framework)

1. Créez une classe implémentant `IAdapter` dans `Scaffolder/src/Adapters/`.
2. Enregistrez-la dans `NewCommand.Adapters`.
3. Ajoutez les entrées correspondantes dans le catalogue (automatique via `CliCatalog`).
4. Ajoutez des tests dans `Scaffolder.Tests`.

### Tests

- Tous les tests doivent passer (`dotnet test`).
- Les nouvelles fonctionnalités doivent être accompagnées de tests.
- Évitez les tests qui dépendent du réseau ou d'outils externes non installés (marquez-les `[Fact(Skip = ...)]` si nécessaire).

## Structure du projet

```
Scaffolder-CLI/
├── Scaffolder/           # Projet principal (.NET 10, AOT)
│   ├── src/
│   │   ├── Adapters/     # Adapters par écosystème
│   │   ├── Cli/          # Program.cs, RootFactory
│   │   ├── Commands/     # Commandes CLI
│   │   ├── Services/     # Services partagés
│   │   ├── Tui/          # Interface terminal (Terminal.Gui)
│   │   └── Web/          # Page HTML de l'interface web
│   └── scripts/          # Scripts de build et install
├── Scaffolder.Tests/     # Tests xUnit
├── .github/workflows/    # CI/CD
├── install.sh            # Script d'installation Unix
├── install.bat           # Script d'installation Windows
├── README.md
├── CHANGELOG.md
├── LICENSE
└── CONTRIBUTING.md
```

## Licence

Ce projet est sous licence MIT. Voir [LICENSE](LICENSE).
