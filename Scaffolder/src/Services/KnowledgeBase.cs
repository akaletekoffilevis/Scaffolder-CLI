namespace Scaffolder.Services;

public static class KnowledgeBase
{
    private static readonly (string[] Keywords, string Template)[] SuggestRules =
    [
        (["api", "rest", "webapi", "backend", "api rest"], "dotnet webapi"),
        (["web", "site", "frontend", "site web"], "npm vite"),
        (["blazor", "wasm", "webassembly"], "dotnet blazor"),
        (["console", "cli", "terminal", "outil"], "dotnet console"),
        (["react", "jsx", "spa"], "npm react"),
        (["vue", "vuejs", "vue3"], "npm vue"),
        (["next", "nextjs", "ssr"], "npm next"),
        (["nuxt", "nuxtjs"], "npm nuxt"),
        (["svelte", "sveltekit"], "npm svelte"),
        (["solid", "solidjs", "solidstart"], "npm solid"),
        (["vite", "vitejs"], "npm vite"),
        (["rust", "cargo"], "cargo"),
        (["go", "golang"], "go"),
        (["python", "django", "flask", "fastapi", "pip"], "python"),
        (["flutter", "mobile", "android", "ios", "dart"], "flutter"),
        (["mobile", "app mobile"], "flutter"),
        (["maui", "dotnet mobile"], "dotnet maui"),
        (["classlib", "library", "lib", "bibliotheque"], "dotnet classlib"),
        (["hello", "hello world", "test", "demo"], "hello"),
    ];

    private static readonly (string Concept, string Title, string Content)[] Explanations =
    [
        ("middleware", "Middleware",
         "Un middleware est un logiciel qui se situe entre le systeme d'exploitation et les applications. Dans le web (Express, ASP.NET), ce sont des fonctions qui interceptent les requetes HTTP pour ajouter de la logique (auth, logs, CORS)."),
        ("mvc", "MVC (Modele-Vue-Controleur)",
         "Pattern d'architecture qui separe une application en 3 parties :\n- Modele : donnees et logique metier\n- Vue : interface utilisateur\n- Controleur : orchestre les entrees et reponses\nUtilise par ASP.NET, Rails, Django, Laravel."),
        ("rest", "REST (Representational State Transfer)",
         "Style d'architecture API qui utilise les methodes HTTP (GET, POST, PUT, DELETE) pour manipuler des ressources identifiees par des URLs. Sans etat, chaque requete contient toutes les infos necessaires."),
        ("docker", "Docker",
         "Plateforme de conteneurisation qui permet d'empaqueter une application avec ses dependances dans un conteneur isole. Un conteneur est plus leger qu'une machine virtuelle."),
        ("docker compose", "Docker Compose",
         "Outil pour definir et executer des applications multi-conteneurs Docker. Configure via un fichier docker-compose.yml."),
        ("git", "Git",
         "Systeme de controle de version distribue. Permet de suivre l'historique des modifications, collaborer via des branches, et synchroniser avec des depots distants (GitHub, GitLab)."),
        ("github actions", "GitHub Actions",
         "Plateforme CI/CD integree a GitHub. Permet d'automatiser les tests, la compilation et le deploiement via des workflows YAML."),
        ("ssl", "SSL/TLS",
         "Protocole de securite pour chiffrer les communications entre un client et un serveur. HTTPS = HTTP + SSL/TLS. Les certificats sont delivres par des autorites comme Let's Encrypt."),
        ("jwt", "JWT (JSON Web Token)",
         "Format de jeton pour l'authentification. Contient un header, un payload (donnees) et une signature. Utilise dans les API REST pour les sessions sans etat."),
        ("orm", "ORM (Object-Relational Mapping)",
         "Technique qui convertit les tables d'une base de donnees relationnelle en objets dans le code. Exemples : Entity Framework (C#), Prisma (JS/TS), SQLAlchemy (Python), Diesel (Rust)."),
        ("ci", "CI (Continuous Integration)",
         "Pratique qui consiste a integrer et tester automatiquement le code a chaque commit. Les outils courants : GitHub Actions, GitLab CI, Jenkins, CircleCI."),
        ("cd", "CD (Continuous Deployment)",
         "Extension de la CI qui automatise le deploiement en production apres chaque validation des tests."),
        ("aot", "AOT (Ahead-of-Time)",
         "Compilation en code machine avant l'execution (vs JIT qui compile a chaud). Avantages : demarrage rapide, binaire unique, moins de memoire. .NET Native AOT, Go, Rust l'utilisent."),
        ("cors", "CORS (Cross-Origin Resource Sharing)",
         "Mecanisme de securite des navigateurs qui controle quelles origines (domaines) peuvent acceder aux ressources d'un serveur. Configure via des en-tetes HTTP."),
        ("spa", "SPA (Single Page Application)",
         "Application web qui charge une seule page HTML et met a jour dynamiquement le contenu sans rechargement. Frameworks : React, Vue, Svelte, Angular."),
        ("ssr", "SSR (Server-Side Rendering)",
         "Technique qui genere le HTML cote serveur au lieu du navigateur. Ameliore le SEO et le temps d'affichage initial. Next.js, Nuxt, SvelteKit le supportent."),
        ("microservices", "Microservices",
         "Architecture qui decoupe une application en petits services independants, chacun avec sa propre base de donnees et communiquant via API."),
        ("monolithe", "Monolithe",
         "Architecture traditionnelle ou toute l'application est un seul bloc. Plus simple a developper au debut, mais plus dur a maintenir a grande echelle."),
        ("websocket", "WebSocket",
         "Protocole de communication bidirectionnelle persistante entre client et serveur. Utilise pour le temps reel (chat, notifications, jeux)."),
        ("graphql", "GraphQL",
         "Langage de requete pour API developpe par Meta. Permet au client de demander exactement les donnees dont il a besoin, contrairement a REST."),
        ("design pattern", "Design Pattern",
         "Solution eprouvee a un probleme recurrent. Exemples : Singleton, Factory, Observer, Strategy, Repository."),
        ("solid", "SOLID",
         "5 principes de conception orientee objet :\nS - Responsabilite unique\nO - Ouvert/ferme\nL - Substitution de Liskov\nI - Segregation des interfaces\nD - Inversion de dependances"),
        ("repository", "Repository Pattern",
         "Pattern de conception qui abstrait l'acces aux donnees derriere une interface. Le code metier ne connait pas la base de donnees, seulement le repository."),
        ("di", "DI (Dependance Injection)",
         "Technique ou les dependances d'une classe sont fournies de l'exterieur (injectees) plutot que creees a l'interieur. Favorise le decouplage et les tests."),
        ("mvvm", "MVVM (Model-View-ViewModel)",
         "Variante de MVC pour les applications a interface riche (WPF, MAUI, Blazor). Le ViewModel expose des donnees et commandes que la Vue lie via le data-binding."),
        ("clean architecture", "Clean Architecture",
         "Architecture en cercles concentriques de Robert C. Martin. Les regles metier sont au centre, les details techniques (BDD, UI, frameworks) a l'exterieur."),
        ("ddd", "DDD (Domain-Driven Design)",
         "Approche de conception ou le domaine metier guide l'architecture. Concepts cles : Entite, Value Object, Aggregat, Repository, Domaine Service."),
        ("tdd", "TDD (Test-Driven Development)",
         "Methode de developpement : 1. Ecrire un test qui echoue 2. Ecrire le code minimal pour le passer 3. Refactorer. Red-Green-Refactor."),
        ("bdd", "BDD (Behavior-Driven Development)",
         "Extension du TDD qui utilise un langage naturel (Gherkin) pour decrire les comportements attendus. Outils : SpecFlow, Cucumber."),
        ("sonar", "SonarQube",
         "Plateforme d'analyse statique de code qui detecte les bugs, les failles de securite, la dette technique et les mauvaises pratiques."),
        ("sonarcloud", "SonarCloud",
         "Version SaaS de SonarQube, integree aux workflows CI/CD. Analyse le code sur GitHub/GitLab.)"),
        ("log", "Logging",
         "Journalisation des evenements d'une application. Niveaux : Debug, Info, Warning, Error, Fatal. Outils : Serilog (C#), Winston (JS), Log4j (Java)."),
        ("serilog", "Serilog",
         "Bibliotheque de logging pour .NET avec support du logging structure (JSON). S'integre avec des destinations multiples : fichier, console, Elasticsearch, Seq."),
        ("elasticsearch", "Elasticsearch",
         "Moteur de recherche et d'analyse distribue, base sur Lucene. Utilise pour la recherche plein texte, les logs et l'analytics."),
        ("kibana", "Kibana",
         "Interface de visualisation pour Elasticsearch. Permet de creer des dashboards, des graphiques et d'explorer les donnees."),
        ("prometheus", "Prometheus",
         "Systeme de monitoring open-source. Collecte des metriques via HTTP, stocke dans une base de donnees temporelle, et alerte selon les regles definies."),
        ("grafana", "Grafana",
         "Plateforme d'observabilite et de visualisation. S'integre avec Prometheus, Elasticsearch, InfluxDB, et d'autres sources de donnees."),
        ("redis", "Redis",
         "Base de donnees en memoire de type cle-valeur. Utilisee pour le cache, les sessions, les files d'attente et le temps reel."),
        ("postgresql", "PostgreSQL",
         "Base de donnees relationnelle open-source avancee. Supporte les JSON, les index avances, la replication."),
        ("sqlite", "SQLite",
         "Base de donnees relationnelle legere integree dans un seul fichier. Parfaite pour le developpement, les tests et les applis mobiles."),
        ("mongodb", "MongoDB",
         "Base de donnees NoSQL orientee documents (JSON). Flexible, scale horizontalement. Utilisee pour les applications web modernes."),
        ("nginx", "Nginx",
         "Serveur web et reverse proxy. Utilise pour servir du contenu statique, equilibrer la charge, et comme proxy inverse."),
        ("dockerfile", "Dockerfile",
         "Fichier de configuration qui decrit les etapes pour construire une image Docker : OS de base, dependances, copie du code, commande de demarrage."),
        ("yaml", "YAML",
         "Format de serialisation lisible par l'humain. Utilise pour la configuration (Docker Compose, GitHub Actions, Ansible, Kubernetes). Indentation sensible."),
        ("json", "JSON",
         "Format leger d'echange de donnees. Syntaxe : objets {cles: valeurs}, tableaux, types (string, number, boolean, null). Universel en programmation."),
        ("markdown", "Markdown",
         "Langage de balisage leger pour formater du texte. Utilise pour les README, la documentation, les issues GitHub. Syntaxe : # titre, **gras**, [lien](url)."),
        ("semver", "SemVer (Semantic Versioning)",
         "Standard de versionnement : MAJEUR.MINEUR.PATCH.\n- MAJEUR : changement incompatible\n- MINEUR : nouvelle fonctionnalite retrocompatible\n- PATCH : correction de bug retrocompatible"),
        ("openapi", "OpenAPI (Swagger)",
         "Standard pour decrire les API REST via un fichier YAML/JSON. Permet la generation de documentation et de clients API automatiquement."),
        ("grpc", "gRPC",
         "Framework d'appels de procedures distantes (RPC) de Google. Utilise HTTP/2 et Protocol Buffers. Plus rapide que REST pour la communication entre microservices."),
        ("kubernetes", "Kubernetes (K8s)",
         "Plateforme d'orchestration de conteneurs. Automatise le deploiement, le scaling et la gestion des applications conteneurisees."),
        ("terraform", "Terraform",
         "Outil Infrastructure as Code (IaC) de HashiCorp. Permet de definir et provisionner l'infrastructure (cloud, serveurs) via du code declaratif."),
    ];

    private static readonly (string[] Errors, string Title, string Fix)[] Fixes =
    [
        (["CS1061", "does not contain a definition"], "CS1061 - Methode manquante",
         "Le type n'a pas la methode que tu appelles.\n1. Verifie le nom de la methode (casse)\n2. Verifie que tu as bien importe le namespace\n3. Verifie le type de l'objet"),
        (["CS0246", "The type or namespace name could not be found"], "CS0246 - Type introuvable",
         "Le type ou namespace n'existe pas.\n1. Ajoute le using manquant\n2. Installe le package NuGet manquant\n3. Verifie le nom du type"),
        (["CS0103", "The name does not exist in the current context"], "CS0103 - Nom inconnu",
         "La variable ou methode n'existe pas dans ce contexte.\n1. Verifie l'orthographe\n2. Declare la variable avant de l'utiliser\n3. Verifie la portee (scope)"),
        (["CS0116", "A namespace cannot directly contain members"], "CS0116 - Membre dans namespace",
         "Un member doit etre dans une classe, pas directement dans un namespace.\nAjoute : class MonProjet { ... }"),
        (["CS1501", "No overload for method takes", "No overload for method"], "CS1501 - Surcharge manquante",
         "La methode existe mais pas avec ces parametres.\n1. Verifie le nombre et le type des arguments\n2. Ajoute les parametres optionnels"),
        (["CS0029", "Cannot implicitly convert type"], "CS0029 - Conversion impossible",
         "Tu ne peux pas convertir ce type en un autre automatiquement.\n1. Utilise un cast explicite : (type)valeur\n2. Verifie le type attendu"),
        (["CS0165", "Use of unassigned local variable"], "CS0165 - Variable non initialisee",
         "Tu utilises une variable avant de lui avoir donne une valeur.\nInitialise-la : int x = 0;"),
        (["CS0433", "The type exists in both"], "CS0433 - Type duplique",
         "Le meme type existe dans deux DLL differentes.\n1. Supprime l'une des references\n2. Utilise l'alias externe"),
        (["CS1729", "does not contain a constructor that takes", "cannot contain a constructor"], "CS1729 - Constructeur manquant",
         "La classe n'a pas de constructeur avec ces parametres.\n1. Verifie les parametres du constructeur\n2. Ajoute un constructeur avec ces parametres"),
        (["CS1955", "Non-invocable member cannot be used like a method"], "CS1955 - Membre non invocable",
         "Tu essaies d'appeler un membre qui n'est pas une methode.\nEnleve les parenteses : propriete au lieu de propriete()"),
        (["CS7036", "There is no argument given that corresponds to the required parameter"], "CS7036 - Argument manquant",
         "Il manque un argument obligatoire.\nAjoute l'argument manquant dans l'appel de la methode."),
        (["TS2304", "Cannot find name"], "TS2304 - Nom introuvable (TypeScript)",
         "Le type ou la variable n'existe pas.\n1. Verifie l'import\n2. Installe les types : npm install @types/..."),
        (["TS2551", "Property does not exist on type"], "TS2551 - Propriete inexistante",
         "La propriete n'existe pas sur ce type.\n1. Verifie le nom de la propriete\n2. Ajoute-la a l'interface."),
        (["TS2322", "Type is not assignable to type"], "TS2322 - Type incompatible (TypeScript)",
         "Le type attribue ne correspond pas au type attendu.\n1. Verifie les types\n2. Ajoute un cast ou corrige la declaration."),
        (["ERR_MODULE_NOT_FOUND", "MODULE_NOT_FOUND", "Cannot find module"], "Module introuvable (Node.js)",
         "Node ne trouve pas le module importe.\n1. npm install <module>\n2. Verifie le chemin d'import\n3. Verifie que package.json contient \"type\": \"module\""),
        (["command not found", "commande introuvable"], "Commande introuvable",
         "L'outil n'est pas installe ou pas dans le PATH.\n1. Installe l'outil (npm install -g, dotnet tool install, cargo install)\n2. Verifie le PATH"),
        (["port already in use", "address already in use", "EADDRINUSE"], "Port deja utilise",
         "Le port est deja occupe par un autre processus.\n1. Change le port dans la configuration\n2. Tue le processus : kill $(lsof -ti:3000)\n3. Utilise un port different"),
        (["permission denied", "Permission non accordee"], "Permission refusee",
         "Tu n'as pas les droits pour executer ce fichier ou acceder a ce dossier.\n1. chmod +x fichier\n2. sudo (avec precaution)\n3. Verifie les droits du dossier"),
        (["not found", "introuvable", "not recognized"], "Fichier introuvable",
         "Le fichier ou dossier n'existe pas.\n1. Verifie le chemin\n2. Verifie que tu es dans le bon dossier (pwd/ls)\n3. Cree le dossier si necessaire"),
        (["missing", "manquant", "required"], "Element manquant",
         "Un element requis est manquant.\n1. Lis le message d'erreur pour identifier quoi\n2. Ajoute l'element manquant\n3. Verifie la documentation"),
        (["syntaxerror", "syntax error", "unexpected token"], "Erreur de syntaxe",
         "Le code contient une erreur de syntaxe.\n1. Verifie les parenteses et accolades\n2. Verifie les points-virgules\n3. Utilise un linter pour detecter les erreurs"),
        (["typeerror", "type error", "cannot read property"], "Erreur de type",
         "Tu essaies d'acceder a une propriete d'une valeur indefinie ou du mauvais type.\n1. Verifie que la variable existe\n2. Verifie le type avec console.log(typeof x)\n3. Ajoute une verification : if (x !== undefined)"),
        (["referenceerror", "reference error", "is not defined"], "Erreur de reference",
         "Une variable ou fonction n'est pas definie dans ce contexte.\n1. Declare la variable avec let/const/var\n2. Verifie l'orthographe\n3. Verifie la portee"),
        (["dotnet restore", "NU1100", "NU1101", "NU1102", "NU1103", "NU1104", "NU1105", "NU1106", "NU1107", "NU1108"], "Erreur de restauration NuGet",
         "Le package NuGet n'a pas pu etre restaure.\n1. Verifie le nom du package\n2. Verifie la source NuGet (nuget.config)\n3. dotnet restore --force\n4. Vide le cache : dotnet nuget locals all --clear"),
        (["npm ERR", "npm error"], "Erreur npm",
         "npm a rencontre une erreur.\n1. Supprime node_modules et package-lock.json\n2. npm cache clean --force\n3. npm install\n4. Verifie la version de Node"),
        (["pip install", "Could not find a version that satisfies the requirement"], "Paquet pip introuvable",
         "Le paquet Python n'existe pas ou n'est pas disponible.\n1. Verifie l'orthographe\n2. pip install --upgrade pip\n3. Verifie le nom du paquet sur PyPI"),
        (["cargo", "error[E0463]", "can't find crate"], "Crate Rust introuvable",
         "La crate Rust n'est pas dans le registre.\n1. Verifie le nom et la version dans Cargo.toml\n2. cargo update\n3. Verifie sur crates.io"),
        (["git merge conflict", "Automerge", "merge conflict"], "Conflit de fusion Git",
         "Git ne peut pas fusionner automatiquement.\n1. Ouvre les fichiers en conflit\n2. Cherche les marqueurs <<<<<<< et >>>>>>>\n3. Resous manuellement\n4. git add + git commit"),
        (["fatal: not a git repository", "fatal: not a git repo"], "Pas un depot Git",
         "Le dossier courant n'est pas un depot Git.\n1. git init\n2. Verifie que tu es dans le bon dossier\n3. git status pour confirmer"),
        (["remote origin already exists", "remote already exists"], "Deja lie a un remote",
         "Un depot distant est deja configure.\n1. git remote -v pour voir l'URL existante\n2. git remote set-url origin <nouvelle-url>\n3. Ou supprime : git remote remove origin"),
    ];

    public static string Suggest(string[] keywords)
    {
        var input = string.Join(" ", keywords).ToLowerInvariant();
        var bestScore = 0;
        var bestMatch = "hello";

        foreach (var (ruleKeywords, template) in SuggestRules)
        {
            var score = ruleKeywords.Sum(k => input.Contains(k) ? k.Length : 0);
            if (score > bestScore)
            {
                bestScore = score;
                bestMatch = template;
            }
        }

        return bestMatch;
    }

    public static (string? Title, string? Content) Explain(string concept)
    {
        var lower = concept.ToLowerInvariant();
        var match = Explanations.FirstOrDefault(e =>
            e.Concept == lower ||
            e.Concept.Contains(lower) ||
            lower.Contains(e.Concept));
        return (match.Title, match.Content);
    }

    public static string[] ExplainAllConcepts() =>
        Explanations.Select(e => e.Concept).ToArray();

    public static (string? Title, string? Fix) Fix(string error)
    {
        var lower = error.ToLowerInvariant();
        var bestScore = 0;
        (string? Title, string? Fix) best = (null, null);

        foreach (var (patterns, title, fix) in Fixes)
        {
            var score = patterns.Sum(p => lower.Contains(p.ToLowerInvariant()) ? p.Length : 0);
            if (score > bestScore)
            {
                bestScore = score;
                best = (title, fix);
            }
        }

        return best;
    }

    public static string[] ListAdapters()
    {
        return SuggestRules.Select(r => r.Template).Distinct().ToArray();
    }
}
