export interface CommandOption {
  flag: string;
  desc: string;
}

export interface CommandEntry {
  search: string;
  syntax: string;
  description: string;
  badge: string;
  options: CommandOption[];
  examples: string[];
}

export interface CommandCategory {
  category: string;
  commands: CommandEntry[];
}

export const commands: CommandCategory[] = [
  {
    "category": "Génération de projets",
    "commands": [
      {
        "search": "new cree projet template hello dotnet npm cargo go python flutter laravel rails gradle swift zig mix cabal",
        "syntax": "new [--template] [--name] [--language] [--output] [--dry-run] [--silent] [--no-git] [--fav] [--verbose]",
        "description": "Crée un nouveau projet à partir d'un template. Si aucun argument n'est fourni, affiche un menu interactif.",
        "badge": "génération",
        "options": [
          { "flag": "[type]", "desc": "Type de projet (optionnel si mode interactif)" },
          { "flag": "--template", "desc": "Template à utiliser (hello, dotnet, npm, cargo, go, python, flutter, laravel, rails, gradle, swift, zig, mix, cabal, ou un template du registry)" },
          { "flag": "--name", "desc": "Nom du projet (défaut : mon-projet)" },
          { "flag": "--language", "desc": "Langage pour le template hello (python, javascript, typescript, c#, rust, go, ruby, php, dart, swift, kotlin, java, zig)" },
          { "flag": "--output", "desc": "Dossier de sortie" },
          { "flag": "--dry-run", "desc": "Prévisualisation sans générer de fichiers" },
          { "flag": "--silent", "desc": "Mode silencieux (pour CI)" },
          { "flag": "--no-git", "desc": "Ne pas initialiser Git" },
          { "flag": "--fav", "desc": "Utilise le template favori (configuré avec scaffold config set fav ...)" },
          { "flag": "--verbose", "desc": "Mode verbeux (logs détaillés)" }
        ],
        "examples": [
          "scaffold new --template=webapi --name=mon-api",
          "scaffold new --template=vite --name=frontend --dry-run",
          "scaffold new hello --language=rust --name=hello-rust",
          "scaffold new",
          "scaffold new webapi+react --name=full-app",
          "scaffold new vite+python+postgres --name=stack"
        ]
      },
      {
        "search": "stack fullstack frontend backend database db vite dotnet postgres mysql redis nginx docker compose",
        "syntax": "stack <type> [--name] [--output]",
        "description": "Génère un projet fullstack complet avec frontend, backend et base de données.",
        "badge": "génération",
        "options": [
          { "flag": "type", "desc": "Type de stack : vite-dotnet, next-dotnet, react-go, vue-dotnet" },
          { "flag": "--name", "desc": "Nom du projet" },
          { "flag": "--output", "desc": "Dossier de sortie" }
        ],
        "examples": [
          "scaffold stack vite-dotnet --name=ecommerce",
          "scaffold stack next-dotnet --name=dashboard"
        ]
      },
      {
        "search": "generate ia ai template custom description creer personnalise",
        "syntax": "generate <description> [--name] [--output]",
        "description": "Crée un template personnalisé via IA à partir d'une description en langage naturel.",
        "badge": "ia",
        "options": [
          { "flag": "description", "desc": "Description en français ou anglais du template souhaité" },
          { "flag": "--name", "desc": "Nom du template" },
          { "flag": "--output", "desc": "Dossier de sortie" }
        ],
        "examples": [
          "scaffold generate \"api rest avec node et postgres\" --name=node-api",
          "scaffold generate \"blog avec react tailwind et CMS\""
        ]
      },
      {
        "search": "template from-dir list info gerer depuis dossier existant reverse scaffold",
        "syntax": "template [from-dir|list|info] [options]",
        "description": "Gère les templates de projet : création depuis un dossier existant, liste, consultation.",
        "badge": "génération",
        "options": [
          { "flag": "from-dir [path] --name", "desc": "Crée un template depuis un dossier existant (reverse scaffold). Détecte automatiquement le type de projet (.NET, Node.js, Rust, Go) et les tags." },
          { "flag": "list", "desc": "Liste tous les templates disponibles" },
          { "flag": "info <name>", "desc": "Affiche les détails d'un template" }
        ],
        "examples": [
          "scaffold template from-dir ./mon-projet --name=mon-template",
          "scaffold template list",
          "scaffold template info mon-template"
        ]
      },
      {
        "search": "workspace monorepo multi-projets init add list npm workspaces sln cargo",
        "syntax": "workspace [init|add|list] [options]",
        "description": "Crée et gère des workspaces multi-projets (monorepo). Supporte npm workspaces, solutions .NET, et Cargo workspaces.",
        "badge": "projet",
        "options": [
          { "flag": "init <name> --projects --manager", "desc": "Initialise un workspace monorepo avec un gestionnaire de paquets. --projects accepte une liste séparée par des virgules (ex: frontend+vite,backend+webapi). --manager : npm, dotnet, cargo." },
          { "flag": "add <name> <template>", "desc": "Ajoute un sous-projet au workspace existant" },
          { "flag": "list", "desc": "Liste les sous-projets du workspace" }
        ],
        "examples": [
          "scaffold workspace init monorepo --projects=frontend+vite,backend+webapi,admin+react",
          "cd monorepo && scaffold workspace add mobile flutter",
          "scaffold workspace list"
        ]
      }
    ]
  },
  {
    "category": "DevOps & Déploiement",
    "commands": [
      {
        "search": "deploy vercel railway docker github pages deploiement cible target prod",
        "syntax": "deploy [--target] [--prod]",
        "description": "Déploie le projet sur une plateforme cloud. Détecte automatiquement la plateforme selon les fichiers présents (vercel.json, railway.json, Dockerfile).",
        "badge": "devops",
        "options": [
          { "flag": "--target", "desc": "Plateforme cible : vercel, railway, docker, github-pages" },
          { "flag": "--prod", "desc": "Déploiement en production (Vercel seulement)" }
        ],
        "examples": [
          "scaffold deploy --target=vercel --prod",
          "scaffold deploy --target=docker",
          "scaffold deploy"
        ]
      },
      {
        "search": "docker compose file dockerfile dockerignore generer environnement conteneur",
        "syntax": "docker [--type] [--output]",
        "description": "Génère Dockerfile, .dockerignore et docker-compose.yml pour le projet.",
        "badge": "docker",
        "options": [
          { "flag": "--type", "desc": "Type de projet : api (.NET), web (Node.js/nginx), full (les deux)" },
          { "flag": "--output", "desc": "Dossier de sortie" }
        ],
        "examples": [
          "scaffold docker --type=api",
          "scaffold docker --type=web",
          "scaffold docker --type=full"
        ]
      },
      {
        "search": "github init actions action gitignore ignore workflow ci configuration remote",
        "syntax": "github [init|actions|action|ignore] [options]",
        "description": "Configure GitHub pour le projet courant : création de dépôt, workflows CI, gitignore.",
        "badge": "devops",
        "options": [
          { "flag": "init", "desc": "Crée un dépôt GitHub et pousse le projet (nécessite gh CLI)" },
          { "flag": "actions --language", "desc": "Génère un workflow GitHub Actions CI pour le langage spécifié (dotnet, node, python, rust, go)" },
          { "flag": "action", "desc": "Génère une GitHub Action Scaffolder pour exécuter scaffold batch dans la CI" },
          { "flag": "ignore", "desc": "Génère un fichier .gitignore adapté au projet" }
        ],
        "examples": [
          "scaffold github actions --language=dotnet",
          "scaffold github action",
          "scaffold github init"
        ]
      },
      {
        "search": "init projet complet docker ci github git environement initialisation",
        "syntax": "init",
        "description": "Initialise un projet Scaffolder complet en une commande : Docker, CI, GitHub, Git, .env, .gitignore. Combine les commandes docker, github actions, env, et git init en une seule étape.",
        "badge": "devops",
        "options": [],
        "examples": [
          "scaffold init"
        ]
      },
      {
        "search": "env environnement dotenv .env variables configuration",
        "syntax": "env",
        "description": "Génère des fichiers .env et .env.example avec les variables d'environnement typiques pour le projet détecté.",
        "badge": "devops",
        "options": [],
        "examples": [
          "scaffold env"
        ]
      }
    ]
  },
  {
    "category": "Cycle de vie du projet",
    "commands": [
      {
        "search": "build compiler projet dotnet npm cargo go",
        "syntax": "build",
        "description": "Compile le projet en utilisant l'outil approprié selon le type de projet détecté (dotnet build, npm run build, cargo build, go build, etc.).",
        "badge": "build",
        "options": [],
        "examples": [
          "scaffold build"
        ]
      },
      {
        "search": "test lancer tests projet dotnet npm cargo go pytest",
        "syntax": "test",
        "description": "Lance les tests du projet. Détecte automatiquement le framework de test (dotnet test, npm test, cargo test, go test, pytest, etc.).",
        "badge": "build",
        "options": [],
        "examples": [
          "scaffold test"
        ]
      },
      {
        "search": "run lancer demarrer projet dotnet run npm start cargo run go run python",
        "syntax": "run",
        "description": "Lance le projet (dotnet run, npm run dev, cargo run, go run, python main.py, etc.). Détecte automatiquement la commande appropriée.",
        "badge": "build",
        "options": [],
        "examples": [
          "scaffold run"
        ]
      },
      {
        "search": "clean nettoyer fichiers generes node_modules bin obj target dist pycache",
        "syntax": "clean",
        "description": "Nettoie les fichiers générés : node_modules, bin, obj, dist, target, __pycache__, .next, build.",
        "badge": "build",
        "options": [],
        "examples": [
          "scaffold clean"
        ]
      },
      {
        "search": "lint linter qualite code analyse dotnet npm cargo",
        "syntax": "lint",
        "description": "Exécute le linter du projet (dotnet format, npm run lint, cargo clippy, etc.).",
        "badge": "qualité",
        "options": [],
        "examples": [
          "scaffold lint"
        ]
      },
      {
        "search": "format formater code prettier dotnet format rustfmt",
        "syntax": "format",
        "description": "Formate le code du projet automatiquement (dotnet format, prettier, rustfmt, etc.).",
        "badge": "qualité",
        "options": [],
        "examples": [
          "scaffold format"
        ]
      },
      {
        "search": "update-deps mettre a jour dependances npm nuget cargo go pip outdated",
        "syntax": "update-deps",
        "description": "Met à jour toutes les dépendances du projet. Supporte npm, NuGet, Cargo, Go modules et pip.",
        "badge": "maintenance",
        "options": [],
        "examples": [
          "scaffold update-deps"
        ]
      },
      {
        "search": "info projet courant informations configuration detection",
        "syntax": "info",
        "description": "Affiche les informations du projet courant : type, version, dépendances, outils détectés.",
        "badge": "projet",
        "options": [],
        "examples": [
          "scaffold info"
        ]
      }
    ]
  },
  {
    "category": "Templates & Registry",
    "commands": [
      {
        "search": "registry template installer rechercher liste communaute remote",
        "syntax": "registry [search|install|list] [options]",
        "description": "Recherche et installe des templates depuis le registry distant ou local.",
        "badge": "templates",
        "options": [
          { "flag": "search <query>", "desc": "Recherche des templates par mot-clé" },
          { "flag": "install <name>", "desc": "Installe un template depuis le registry" },
          { "flag": "list", "desc": "Liste les templates installés localement" }
        ],
        "examples": [
          "scaffold registry search \"blog\"",
          "scaffold registry install blog-template",
          "scaffold registry list"
        ]
      },
      {
        "search": "search query trouver templates mots-cles chercher",
        "syntax": "search <query>",
        "description": "Recherche des templates dans le registry par mots-clés. Affiche les templates correspondants avec leur description et tags.",
        "badge": "templates",
        "options": [],
        "examples": [
          "scaffold search \"react\"",
          "scaffold search \"api rest\"",
          "scaffold search \"mobile\""
        ]
      },
      {
        "search": "compare deux templates cote a cote difference similaire",
        "syntax": "compare <template1> <template2>",
        "description": "Compare deux templates côte à côte : structure, fichiers, dépendances, métadonnées.",
        "badge": "templates",
        "options": [],
        "examples": [
          "scaffold compare vite next",
          "scaffold compare webapi blazor"
        ]
      },
      {
        "search": "migrate migrer projet template vers un autre conversion",
        "syntax": "migrate <from> <to> [--path]",
        "description": "Migre un projet d'un template source vers un template cible. Préserve le code personnalisé autant que possible.",
        "badge": "outils",
        "options": [],
        "examples": [
          "scaffold migrate webapi blazor --path=./mon-projet"
        ]
      },
      {
        "search": "audit securite template vulnerabilite dependances npm audit dotnet cargo",
        "syntax": "audit <template>",
        "description": "Audite la sécurité d'un template ou projet. Vérifie les dépendances, les vulnérabilités connues, les fichiers sensibles, et les mauvaises pratiques.",
        "badge": "sécurité",
        "options": [],
        "examples": [
          "scaffold audit mon-projet"
        ]
      },
      {
        "search": "store generer site web statique marketplace templates catalogue html",
        "syntax": "store [--output] [--publish]",
        "description": "Génère un site web statique complet pour le marketplace de templates. Inclut tous les templates intégrés et personnalisés avec recherche, tags, et boutons de copie de commande.",
        "badge": "templates",
        "options": [
          { "flag": "--output", "desc": "Dossier de sortie du site (défaut : ./template-store)" },
          { "flag": "--publish", "desc": "Publie sur GitHub Pages après génération" }
        ],
        "examples": [
          "scaffold store --output=./mon-site",
          "scaffold store --publish"
        ]
      }
    ]
  },
  {
    "category": "Automatisation",
    "commands": [
      {
        "search": "batch generer plusieurs projets fichier yaml lot automatisation",
        "syntax": "batch <file.yml>",
        "description": "Génère plusieurs projets à partir d'un fichier YAML de configuration. Idéal pour l'automatisation CI et les templates d'entreprise.",
        "badge": "automatisation",
        "options": [],
        "examples": [
          "scaffold batch scaffold-batch.yml"
        ]
      },
      {
        "search": "watch surveiller dossier re-generer automatiquement watch mode developpement",
        "syntax": "watch <template> [--name] [--watch-dir]",
        "description": "Surveille un dossier et re-génère automatiquement le projet à chaque modification. Utilise un FileSystemWatcher.",
        "badge": "automatisation",
        "options": [
          { "flag": "--name", "desc": "Nom du projet" },
          { "flag": "--watch-dir", "desc": "Dossier à surveiller" }
        ],
        "examples": [
          "scaffold watch webapi --name=mon-api --watch-dir=./src"
        ]
      }
    ]
  },
  {
    "category": "Intelligence Artificielle",
    "commands": [
      {
        "search": "suggest mot cle template suggestion ia recommander proposer",
        "syntax": "suggest <keywords>",
        "description": "Suggère un template à partir de mots-clés en langage naturel. Utilise l'IA si configurée, sinon une base de connaissances intégrée.",
        "badge": "ia",
        "options": [],
        "examples": [
          "scaffold suggest \"je veux un blog avec react et tailwind\"",
          "scaffold suggest \"api rest en python\"",
          "scaffold suggest \"application mobile ios android\""
        ]
      },
      {
        "search": "explain concept explication ia apprentissage definition tutoriel",
        "syntax": "explain <concept>",
        "description": "Explique un concept de développement avec des exemples concrets. Utilise l'IA si configurée.",
        "badge": "ia",
        "options": [],
        "examples": [
          "scaffold explain \"dependency injection\"",
          "scaffold explain \"middleware\"",
          "scaffold explain \"jwt\"",
          "scaffold explain \"solid principles\""
        ]
      },
      {
        "search": "fix erreur solution correction ia debug probleme",
        "syntax": "fix <error>",
        "description": "Analyse un message d'erreur et propose une solution étape par étape. Utilise l'IA si configurée.",
        "badge": "ia",
        "options": [],
        "examples": [
          "scaffold fix \"CS1061: 'DbContext' does not contain a definition for 'Users'\"",
          "scaffold fix \"Module not found: Can't resolve 'react-router-dom'\"",
          "scaffold fix \"error[E0425]: cannot find value 'self' in this scope\""
        ]
      }
    ]
  },
  {
    "category": "Configuration",
    "commands": [
      {
        "search": "config configuration set get list import export cle api modele ia provider",
        "syntax": "config [set|get|list|import|export] [options]",
        "description": "Gère la configuration de Scaffolder : clés API, préférences, profils.",
        "badge": "configuration",
        "options": [
          { "flag": "set <key> <value>", "desc": "Définit une valeur de configuration" },
          { "flag": "get <key>", "desc": "Affiche une valeur de configuration" },
          { "flag": "list", "desc": "Liste toutes les configurations" },
          { "flag": "import <file.json>", "desc": "Importe une configuration depuis un fichier JSON" },
          { "flag": "export <file.json>", "desc": "Exporte la configuration vers un fichier JSON" },
          { "flag": "apiKey", "desc": "Clé API pour les services IA (OpenAI, Claude, etc.)" },
          { "flag": "provider", "desc": "Fournisseur IA : openai, claude, gemini, grok" },
          { "flag": "model", "desc": "Modèle IA (ex: gpt-4o-mini, claude-3-haiku)" },
          { "flag": "fav", "desc": "Template favori pour scaffold new --fav" },
          { "flag": "theme", "desc": "Thème du terminal" }
        ],
        "examples": [
          "scaffold config set apiKey sk-xxxxx",
          "scaffold config set provider grok",
          "scaffold config set model gpt-4o",
          "scaffold config set fav webapi",
          "scaffold config list",
          "scaffold config export ~/scaffold-config.json"
        ]
      },
      {
        "search": "diagnostic doctor environnement verifier outils installes dotnet node cargo go flutter",
        "syntax": "doctor",
        "description": "Diagnostique l'environnement : vérifie les outils installés (dotnet, node, npm, cargo, rustc, go, python, flutter, docker, git, gh), les versions, et la configuration Scaffolder.",
        "badge": "diagnostic",
        "options": [],
        "examples": [
          "scaffold doctor"
        ]
      },
      {
        "search": "project analyser diagnostiquer projet genere structure sante qualite",
        "syntax": "project [--path]",
        "description": "Analyse et diagnostique un projet généré : structure, santé, dépendances, configuration.",
        "badge": "projet",
        "options": [
          { "flag": "--path", "desc": "Chemin du projet à analyser" }
        ],
        "examples": [
          "scaffold project",
          "scaffold project --path=./mon-projet"
        ]
      }
    ]
  },
  {
    "category": "Extensions & Plugins",
    "commands": [
      {
        "search": "vscode extension visual studio code generer workspace configuration",
        "syntax": "vscode [init|workspace] [options]",
        "description": "Génère une extension VS Code complète ou configure l'espace de travail.",
        "badge": "vscode",
        "options": [
          { "flag": "init <name>", "desc": "Crée une extension VS Code avec package.json, tsconfig, launch.json, activation events, et une commande exemple prête à publier" },
          { "flag": "workspace", "desc": "Configure le dossier courant comme espace de travail VS Code (.code-workspace)" }
        ],
        "examples": [
          "scaffold vscode init mon-extension",
          "scaffold vscode workspace"
        ]
      },
      {
        "search": "plugin extensible add remove list search create marketplace",
        "syntax": "plugin [list|add|remove|info|create|search] [options]",
        "description": "Gère les plugins Scaffolder. Les plugins sont des scripts shell exécutables qui étendent les fonctionnalités.",
        "badge": "extensible",
        "options": [
          { "flag": "list", "desc": "Liste les plugins installés" },
          { "flag": "add <url>", "desc": "Ajoute un plugin depuis une URL Git" },
          { "flag": "remove <name>", "desc": "Supprime un plugin installé" },
          { "flag": "info <name>", "desc": "Affiche les détails d'un plugin" },
          { "flag": "create <name>", "desc": "Crée un squelette de plugin avec plugin.json et main.sh" },
          { "flag": "search <query>", "desc": "Cherche des plugins dans le marketplace communautaire" }
        ],
        "examples": [
          "scaffold plugin list",
          "scaffold plugin create mon-plugin",
          "scaffold plugin search eslint",
          "scaffold plugin info mon-plugin"
        ]
      }
    ]
  },
  {
    "category": "Utilitaires",
    "commands": [
      {
        "search": "upgrade mettre a jour scaffolder mise a jour auto-update version",
        "syntax": "upgrade",
        "description": "Vérifie la dernière version disponible sur GitHub et installe la mise à jour automatiquement. Gère les permissions sudo si nécessaire.",
        "badge": "maintenance",
        "options": [],
        "examples": [
          "scaffold upgrade"
        ]
      },
      {
        "search": "completion shell auto-completion bash zsh powershell fish script",
        "syntax": "completion <shell>",
        "description": "Génère les scripts d'auto-complétion pour le shell. Supporte bash, zsh, fish, powershell.",
        "badge": "shell",
        "options": [],
        "examples": [
          "scaffold completion bash > /etc/bash_completion.d/scaffold",
          "scaffold completion zsh > /usr/local/share/zsh/site-functions/_scaffold",
          "scaffold completion fish > ~/.config/fish/completions/scaffold.fish",
          "scaffold completion powershell > $PROFILE"
        ]
      },
      {
        "search": "license licence generer fichier mit apache gpl creative commons",
        "syntax": "license <type>",
        "description": "Génère un fichier de licence pour le projet. Supporte MIT, Apache-2.0, GPL-3.0, BSD-2, BSD-3, MPL-2.0, Unlicense, AGPL-3.0, LGPL-3.0.",
        "badge": "utilitaires",
        "options": [],
        "examples": [
          "scaffold license mit",
          "scaffold license apache",
          "scaffold license gpl"
        ]
      },
      {
        "search": "ui interface web locale navigateur lancher lancer dashboard port",
        "syntax": "ui [--port]",
        "description": "Lance une interface web locale pour Scaffolder. Un serveur HTTP intégré sert une page web interactive avec sélection de templates, génération de projets, et documentation intégrée.",
        "badge": "interface",
        "options": [
          { "flag": "--port", "desc": "Port du serveur (défaut : 8080)" }
        ],
        "examples": [
          "scaffold ui --port=3000"
        ]
      },
      {
        "search": "bug signaler rapport email github issue probleme",
        "syntax": "bug <description>",
        "description": "Signale un bug par email ou GitHub. Ouvre le client mail par défaut avec un rapport pré-rempli contenant la description et les informations système.",
        "badge": "support",
        "options": [],
        "examples": [
          "scaffold bug \"le template webapi ne genere pas les controleurs\"",
          "scaffold bug \"crash quand j'utilise --dry-run avec --verbose\""
        ]
      }
    ]
  }
]
