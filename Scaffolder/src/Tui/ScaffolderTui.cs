using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Scaffolder.Commands;
using Scaffolder.Services;
using Terminal.Gui.App;
using Terminal.Gui.Configuration;
using Terminal.Gui.Drawing;
using Terminal.Gui.Input;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;
using Attr = Terminal.Gui.Drawing.Attribute;

namespace Scaffolder.Tui;

/// <summary>
/// Interface terminal plein écran (Terminal.Gui) au thème sombre.
/// Menu à gauche, page active au centre, raccourcis F1–F9 et barre de statut en bas.
/// Toute action passe par le binaire `scaffold` (une seule source de vérité).
/// </summary>
public static class ScaffolderTui
{
    private sealed record TemplateEntry(string Label, string Template, string? Language, string Description, bool NeedsConfig);

    private static readonly string[] AuthChoices = ["none", "jwt", "identity"];
    private static readonly string[] DbChoices = ["none", "sqlite", "sqlserver", "postgresql"];
    private static readonly string[] ArchChoices = ["mvc", "clean", "minimal"];
    private static readonly string[] ProviderChoices = ["openai", "claude", "gemini", "grok"];
    private static readonly string[] FrontendChoices = ["react", "vue", "svelte", "next", "nuxt", "none"];
    private static readonly string[] BackendChoices = ["webapi", "fastapi", "laravel", "rails", "none"];
    private static readonly string[] StackDbChoices = ["postgres", "mysql", "sqlite", "mongodb", "none"];

    // Ordre du menu = ordre des touches F1..F9.
    private static readonly (string Title, Key Hotkey)[] PageDefs =
    [
        ("Aide", Key.F1),
        ("Nouveau projet", Key.F2),
        ("Stack fullstack", Key.F3),
        ("Ajouter", Key.F4),
        ("Templates", Key.F5),
        ("Diagnostic", Key.F6),
        ("Projet", Key.F10),
        ("Assistant IA", Key.F7),
        ("Configuration", Key.F8),
        ("Toutes les commandes", Key.F9)
    ];

    private static readonly Regex AnsiPattern = new(@"\x1B\[[0-9;?]*[A-Za-z]", RegexOptions.Compiled);

    // Palette (thème sombre bleu nuit).
    private static readonly Color Bg = new(13, 17, 23);
    private static readonly Color Panel = new(22, 27, 34);
    private static readonly Color Fg = new(201, 209, 217);
    private static readonly Color Muted = new(139, 148, 158);
    private static readonly Color Accent = new(88, 166, 255);
    private static readonly Color Select = new(31, 111, 235);
    private static readonly Color White = new(255, 255, 255);

    // Palette claire.
    private static readonly Color L_Bg = new(246, 248, 250);
    private static readonly Color L_Panel = new(255, 255, 255);
    private static readonly Color L_Fg = new(31, 35, 40);
    private static readonly Color L_Muted = new(101, 109, 118);
    private static readonly Color L_Accent = new(9, 105, 218);
    private static readonly Color L_Select = new(9, 105, 218);

    private sealed class SharedState
    {
        public TemplateEntry? Preselected { get; set; }
        public List<TemplateEntry> Entries { get; } = [];
        public List<(string Role, string Text)> Chat { get; } = [];
        public string? PendingCommand { get; set; }
    }

    public static void Run()
    {
        using var app = Application.Create();
        app.Init();

        var dark = (ConfigService.Get(ConfigService.Keys.Theme) ?? "dark") != "light";

        var state = new SharedState();
        state.Entries.AddRange(BuildEntries());

        using var win = BuildWindow(app, state, dark);
        app.Run(win);
    }

    private static List<TemplateEntry> BuildEntries()
    {
        var list = new List<TemplateEntry>();

        foreach (var lang in NewCommand.HelloLanguages)
            list.Add(new TemplateEntry($"hello · {lang}", "hello", lang, $"Hello World en {lang}", false));

        foreach (var adapter in NewCommand.Adapters)
        {
            foreach (var sub in adapter.SubTemplates)
            {
                var tag = adapter.IsAvailable ? "" : "  (outil absent)";
                list.Add(new TemplateEntry($"{adapter.Name} · {sub}{tag}", sub, null,
                    adapter.Description, sub is "webapi" or "blazor"));
            }
        }

        return list;
    }

    private static Scheme BuildScheme(bool dark)
    {
        if (dark)
        {
            return new Scheme(new Attr(Fg, Bg))
            {
                HotNormal = new Attr(Accent, Bg),
                Focus = new Attr(White, Select),
                HotFocus = new Attr(White, Select),
                Highlight = new Attr(White, Select),
                Disabled = new Attr(Muted, Bg),
                Editable = new Attr(Fg, Panel)
            };
        }

        return new Scheme(new Attr(L_Fg, L_Bg))
        {
            HotNormal = new Attr(L_Accent, L_Bg),
            Focus = new Attr(Color.White, L_Select),
            HotFocus = new Attr(Color.White, L_Select),
            Highlight = new Attr(Color.White, L_Select),
            Disabled = new Attr(L_Muted, L_Bg),
            Editable = new Attr(L_Fg, L_Panel)
        };
    }

    private static Window BuildWindow(IApplication app, SharedState state, bool dark)
    {
        SchemeManager.AddScheme("Scaffolder", BuildScheme(dark));

        var win = new Window
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            Title = $" ◆ Scaffolder v{UpdateService.CurrentVersion} ",
            SchemeName = "Scaffolder"
        };

        var header = new Label
        {
            Text = HeaderText(),
            X = 1,
            Y = 0,
            Width = Dim.Fill(1),
            SchemeName = "Scaffolder"
        };

        var sidebar = new FrameView
        {
            Title = "Menu",
            X = 0,
            Y = 1,
            Width = 26,
            Height = Dim.Fill(1),
            SchemeName = "Scaffolder"
        };
        var menu = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        menu.SetSource(new ObservableCollection<string>(
            PageDefs.Select((p, i) => $" F{i + 1}  {p.Title}")));
        sidebar.Add(menu);

        var content = new FrameView
        {
            Title = PageDefs[0].Title,
            X = Pos.Right(sidebar),
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1),
            SchemeName = "Scaffolder",
            CanFocus = true
        };

        var status = new StatusBar(
        [
            new Shortcut(Key.F1, "Aide", null, null),
            new Shortcut(Key.F9, "Commandes", null, null),
            new Shortcut(Key.F7, "Assistant", null, null),
            new Shortcut(Key.Tab, "Zone suivante", null, null),
            new Shortcut(Key.Esc, "Quitter", null, null),
        ])
        {
            X = 0,
            Y = Pos.AnchorEnd(1),
            Width = Dim.Fill(),
            SchemeName = "Scaffolder"
        };

        var current = -1;

        void ShowPage(int index)
        {
            if (index == current) return;
            current = index;
            content.RemoveAll();
            content.Title = $" {PageDefs[index].Title} ";
            content.Add(index switch
            {
                0 => HelpPage(),
                1 => ProjectPage(app, state),
                2 => StackPage(app),
                3 => AddPage(app),
                4 => TemplatesPage(app, state, () => JumpTo(1)),
                5 => DiagnosticPage(app),
                6 => ProjectCommandsPage(app),
                7 => AssistantPage(app, state),
                8 => ConfigPage(app),
                _ => CommandsPage(app)
            });
            menu.Value = index;
            header.Text = HeaderText();
            // Après la mise en page : une vue fraîchement ajoutée ne peut pas recevoir le focus avant.
            app.Invoke(() => FocusPrimary(content));
            app.AddTimeout(TimeSpan.FromMilliseconds(300), () =>
            {
                var f = FindFirst<TextField>(content);
                var l = FindFirst<ListView>(content);
                File.AppendAllText("/tmp/opencode/focus.log", $"T300 field.has={f?.HasFocus} list.has={l?.HasFocus} win.has={win.HasFocus} menu.has={menu.HasFocus}\n");
                return false;
            });
        }

        void JumpTo(int index)
        {
            current = -1;
            ShowPage(index);
            app.AddTimeout(TimeSpan.FromMilliseconds(400), () =>
            {
                var field = FindFirst<TextField>(content);
                var list = FindFirst<ListView>(content);
                if (field is not null) { field.SetFocus(); return false; }
                list?.SetFocus();
                return false;
            });
        }

        menu.ValueChanged += (_, _) =>
        {
            if (menu.Value is int i && i >= 0 && i < PageDefs.Length) ShowPage(i);
        };

        menu.Accepting += (_, e) =>
        {
            e.Handled = true;
            app.Invoke(() => FocusPrimary(content));
        };

        if (Environment.GetEnvironmentVariable("SCAFF_NOKEYDOWN") is null)
        app.Keyboard.KeyDown += (_, e) =>
        {
            if (e == Key.Esc || (e.IsCtrl && e.NoCtrl == Key.Q))
            {
                e.Handled = true;
                app.RequestStop();
                return;
            }

            var index = Array.FindIndex(PageDefs, p => p.Hotkey == e);
            if (index < 0) return;
            e.Handled = true;
            JumpTo(index);
        };

        win.Add(header, sidebar, content, status);
        ShowPage(0);
        menu.SetFocus();
        return win;
    }

    /// <summary>Donne le focus au premier champ de saisie de la page (sinon à la première liste).</summary>
    private static void FocusPrimary(View root)
    {
        var field = FindFirst<TextField>(root);
        var okDbg = field?.SetFocus();
        var listDbg = FindFirst<ListView>(root);
        File.AppendAllText("/tmp/opencode/focus.log", $"T0 field.has={field?.HasFocus} list.has={listDbg?.HasFocus} list.canfocus={listDbg?.CanFocus} tabstop={field?.TabStop}\n");
        File.AppendAllText("/tmp/opencode/focus.log", $"page={root.SubViews.Count} field={field?.Text}/{field?.Y} ok={okDbg} can={field?.CanFocus} vis={field?.Visible} en={field?.Enabled} init={field?.IsInitialized} has={field?.HasFocus} hasfocus-root={root.HasFocus}\n");
        if (field is not null && okDbg == true) return;
        FindFirst<ListView>(root)?.SetFocus();
    }

    private static T? FindFirst<T>(View root) where T : View
    {
        foreach (var child in root.SubViews)
        {
            if (child is T match) return match;
            var nested = FindFirst<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }

    private static string HeaderText()
    {
        var provider = AIService.HasApiKey ? $"IA : {AIService.Provider} · {AIService.Model}" : "IA : hors ligne";
        return $"Dossier : {Directory.GetCurrentDirectory()}   ·   {provider}";
    }

    private static string Clean(string text) => AnsiPattern.Replace(text, "").TrimEnd();

    private static void AppendLines(IApplication app, ObservableCollection<string> lines, ListView view, string text)
    {
        app.Invoke(() =>
        {
            foreach (var line in Clean(text).Split('\n'))
                lines.Add(line.TrimEnd('\r'));
            if (lines.Count > 0) view.Value = lines.Count - 1;
        });
    }

    private static ListView LogView(out ObservableCollection<string> lines, string first)
    {
        lines = new ObservableCollection<string> { first };
        var view = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        view.SetSource(lines);
        return view;
    }

    private static Button Btn(string text, Pos x, Pos y) =>
        new() { Text = text, X = x, Y = y, SchemeName = "Scaffolder" };

    private static Label Lbl(string text, Pos x, Pos y) =>
        new() { Text = text, X = x, Y = y, SchemeName = "Scaffolder" };

    private static TextField Field(string text, Pos x, Pos y, Dim? width = null) => new()
    {
        Text = text,
        X = x,
        Y = y,
        Width = width ?? Dim.Fill(2)
    };

    private static View Page(params View[] children)
    {
        // Les conteneurs doivent être focusables pour que leurs champs reçoivent le clavier.
        var v = new View { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), SchemeName = "Scaffolder", CanFocus = true };
        v.Add(children);
        return v;
    }

    // ───────────────────────────── Aide ─────────────────────────────

    private static View HelpPage()
    {
        var lines = new ObservableCollection<string>
        {
            "Navigation",
            "  F1 Aide · F2 Nouveau projet · F3 Stack · F4 Ajouter · F5 Templates",
            "  F6 Diagnostic · F7 Assistant IA · F8 Configuration · F9 Toutes les commandes",
            "  Flèches : se déplacer · Entrée : valider · Tab : zone suivante · Échap : quitter",
            "",
            "Toutes les commandes (F9)",
            "  Tape pour filtrer. Choisis un paramètre à droite, saisis sa valeur, puis « Exécuter ».",
            "  Chaque sous-commande du CLI est listée automatiquement, sans liste codée à la main.",
            "  Les commandes qui ouvrent une interface (ui, tui, watch, bug, update) ne sont pas lancées ici.",
            "",
            "Assistant IA (F7)",
            "  Pose une question en français. Il connaît le dossier, les outils et les templates.",
            "  Si il propose une commande, un bouton « Exécuter » apparaît : rien ne se lance sans clic.",
            "",
            "Pour l'utiliser sans interface : scaffold <commande> --help, ou scaffold ai ask \"…\"."
        };
        var list = new ListView { X = 1, Y = 1, Width = Dim.Fill(1), Height = Dim.Fill(1) };
        list.SetSource(lines);
        return Page(list);
    }

    // ───────────────────────────── Nouveau projet ─────────────────────────────

    private static View ProjectPage(IApplication app, SharedState state)
    {
        var listFrame = new FrameView { Title = "Templates", X = 0, Y = 0, Width = Dim.Percent(40), Height = Dim.Percent(60), SchemeName = "Scaffolder" };
        var list = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        list.SetSource(new ObservableCollection<string>(state.Entries.Select(e => e.Label)));
        listFrame.Add(list);

        var formFrame = new FrameView { Title = "Options", X = Pos.Right(listFrame), Y = 0, Width = Dim.Fill(), Height = Dim.Percent(60), SchemeName = "Scaffolder" };
        var name = Field("mon-projet", 12, 1);
        var dir = Field(Path.Combine(Directory.GetCurrentDirectory(), "mon-projet"), 12, 3);
        var lang = Field("", 12, 5);
        var git = new CheckBox { Text = "Git", X = 1, Y = 7, Value = CheckState.Checked, SchemeName = "Scaffolder" };
        var docker = new CheckBox { Text = "Dockerfile", X = 12, Y = 7, SchemeName = "Scaffolder" };
        var ci = new CheckBox { Text = "CI/CD", X = 26, Y = 7, SchemeName = "Scaffolder" };
        var auth = new OptionSelector { X = 12, Y = 9, Width = Dim.Fill(2), Labels = AuthChoices, SchemeName = "Scaffolder" };
        var db = new OptionSelector { X = 12, Y = 10, Width = Dim.Fill(2), Labels = DbChoices, SchemeName = "Scaffolder" };
        var arch = new OptionSelector { X = 12, Y = 11, Width = Dim.Fill(2), Labels = ArchChoices, SchemeName = "Scaffolder" };
        formFrame.Add(Lbl("Nom", 1, 1), name, Lbl("Dossier", 1, 3), dir, Lbl("Langage", 1, 5), lang, git, docker, ci,
            Lbl("Auth", 1, 9), auth, Lbl("BDD", 1, 10), db, Lbl("Archi.", 1, 11), arch,
            Lbl("Auth / BDD / Archi. : webapi et blazor uniquement.", 1, 13));

        var logFrame = new FrameView { Title = "Journal", X = 0, Y = Pos.Percent(60), Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };
        var log = LogView(out var logLines, "Choisis un template à gauche, puis « Générer ».");
        logFrame.Add(log);

        var generate = Btn("▶ Générer", 1, Pos.AnchorEnd(1));
        var doctor = Btn("Diagnostic", Pos.Right(generate) + 2, Pos.AnchorEnd(1));

        void RefreshSelection()
        {
            if (list.Value is not int i || i < 0 || i >= state.Entries.Count) return;
            var e = state.Entries[i];
            lang.Text = e.Language ?? "";
            lang.Enabled = e.Template == "hello";
            auth.Enabled = db.Enabled = arch.Enabled = e.NeedsConfig;
        }

        list.ValueChanged += (_, _) => RefreshSelection();
        if (state.Preselected is { } pre)
        {
            var idx = state.Entries.FindIndex(e => e.Label == pre.Label);
            list.Value = idx >= 0 ? idx : 0;
            state.Preselected = null;
        }
        else
        {
            list.Value = 0;
        }
        RefreshSelection();

        generate.Accepting += (_, e) =>
        {
            e.Handled = true;
            if (list.Value is not int i || i < 0 || i >= state.Entries.Count) return;
            var entry = state.Entries[i];
            var projectName = name.Text?.ToString()?.Trim() ?? "";
            var target = dir.Text?.ToString()?.Trim() ?? "";
            if (projectName.Length == 0 || target.Length == 0)
            {
                AppendLines(app, logLines, log, "✖ Nom et dossier sont obligatoires.");
                return;
            }

            var args = new List<string>
            {
                "new", $"--template={entry.Template}", $"--name={projectName}", $"--output={target}", "--silent"
            };
            if (lang.Text?.ToString() is { Length: > 0 } l && entry.Template == "hello") args.Add($"--language={l}");
            if (!git.Value.HasFlag(CheckState.Checked)) args.Add("--no-git");
            if (docker.Value.HasFlag(CheckState.Checked)) args.Add("--docker");
            if (ci.Value.HasFlag(CheckState.Checked)) args.Add("--ci");
            if (entry.NeedsConfig)
            {
                args.Add($"--auth={AuthChoices[Math.Max(0, auth.Value ?? 0)]}");
                args.Add($"--db={DbChoices[Math.Max(0, db.Value ?? 0)]}");
                args.Add($"--arch={ArchChoices[Math.Max(0, arch.Value ?? 0)]}");
            }

            AppendLines(app, logLines, log, $"$ scaffold {string.Join(' ', args)}");
            RunScaffold(app, logLines, log, args, $"Projet « {projectName} » créé dans {target}.");
        };

        doctor.Accepting += (_, e) =>
        {
            e.Handled = true;
            RunScaffold(app, logLines, log, ["doctor"], null);
        };

        return Page(listFrame, formFrame, logFrame, generate, doctor);
    }

    // ───────────────────────────── Stack ─────────────────────────────

    private static View StackPage(IApplication app)
    {
        var form = new FrameView { Title = "Stack fullstack", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Percent(55), SchemeName = "Scaffolder" };
        var name = Field("mon-app", 14, 1);
        var output = Field("", 14, 3);
        var fe = new OptionSelector { X = 14, Y = 5, Width = Dim.Fill(2), Labels = FrontendChoices, SchemeName = "Scaffolder" };
        var be = new OptionSelector { X = 14, Y = 8, Width = Dim.Fill(2), Labels = BackendChoices, SchemeName = "Scaffolder" };
        var db = new OptionSelector { X = 14, Y = 11, Width = Dim.Fill(2), Labels = StackDbChoices, SchemeName = "Scaffolder" };
        form.Add(Lbl("Nom", 1, 1), name, Lbl("Dossier parent", 1, 3), output,
            Lbl("Frontend", 1, 5), fe, Lbl("Backend", 1, 8), be, Lbl("Base", 1, 11), db);

        var logFrame = new FrameView { Title = "Journal", X = 0, Y = Pos.Percent(55), Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };
        var log = LogView(out var lines, "Choisis la stack puis « Générer la stack ».");
        logFrame.Add(log);

        var go = Btn("▶ Générer la stack", 1, Pos.AnchorEnd(1));
        go.Accepting += (_, e) =>
        {
            e.Handled = true;
            var args = new List<string>
            {
                "stack",
                $"--name={name.Text?.ToString()?.Trim()}",
                $"--frontend={FrontendChoices[Math.Max(0, fe.Value ?? 0)]}",
                $"--backend={BackendChoices[Math.Max(0, be.Value ?? 0)]}",
                $"--db={StackDbChoices[Math.Max(0, db.Value ?? 0)]}"
            };
            if (output.Text?.ToString()?.Trim() is { Length: > 0 } o) args.Add($"--output={o}");
            AppendLines(app, lines, log, $"$ scaffold {string.Join(' ', args)}");
            RunScaffold(app, lines, log, args, null);
        };

        return Page(form, logFrame, go);
    }

    // ───────────────────────────── Ajouter ─────────────────────────────

    private static View AddPage(IApplication app)
    {
        var form = new FrameView { Title = "Ajouter un composant", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Percent(40), SchemeName = "Scaffolder" };
        var type = Field("", 16, 1);
        var name = Field("", 16, 3);
        var dir = Field(Directory.GetCurrentDirectory(), 16, 5);
        form.Add(Lbl("Type (ex: controller)", 1, 1), type, Lbl("Nom", 1, 3), name, Lbl("Dossier projet", 1, 5), dir);

        var logFrame = new FrameView { Title = "Journal", X = 0, Y = Pos.Percent(40), Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };
        var log = LogView(out var lines, "Astuce : « scaffold add --list » montre les types disponibles pour ton langage.");
        logFrame.Add(log);

        var go = Btn("▶ Ajouter", 1, Pos.AnchorEnd(1));
        go.Accepting += (_, e) =>
        {
            e.Handled = true;
            var t = type.Text?.ToString()?.Trim() ?? "";
            var n = name.Text?.ToString()?.Trim() ?? "";
            var d = dir.Text?.ToString()?.Trim() ?? "";
            if (t.Length == 0 || n.Length == 0 || !Directory.Exists(d))
            {
                AppendLines(app, lines, log, "✖ Type, nom et dossier existant sont obligatoires.");
                return;
            }
            AppendLines(app, lines, log, $"$ scaffold add {t} {n}   (dans {d})");
            RunScaffold(app, lines, log, ["add", t, n], null, d);
        };

        return Page(form, logFrame, go);
    }

    // ───────────────────────────── Templates ─────────────────────────────

    private static View TemplatesPage(IApplication app, SharedState state, Action showProject)
    {
        var filterL = Lbl("Filtrer", 1, 0);
        var filter = Field("", 12, 0, Dim.Fill(2));

        var listFrame = new FrameView { Title = "Tous les templates", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };
        var list = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        listFrame.Add(list);

        List<TemplateEntry> visible = [];
        void Refresh()
        {
            var q = filter.Text?.ToString()?.Trim().ToLowerInvariant() ?? "";
            visible = state.Entries
                .Where(e => q.Length == 0 || (e.Label + " " + e.Description).ToLowerInvariant().Contains(q))
                .ToList();
            list.SetSource(new ObservableCollection<string>(visible.Select(e => $"{e.Label,-30} {e.Description}")));
        }
        Refresh();
        filter.TextChanged += (_, _) => Refresh();

        var use = Btn("▶ Utiliser ce template", 1, Pos.AnchorEnd(1));
        use.Accepting += (_, e) =>
        {
            e.Handled = true;
            if (list.Value is int i && i >= 0 && i < visible.Count)
            {
                state.Preselected = visible[i];
                showProject();
            }
        };

        return Page(filterL, filter, listFrame, use);
    }

    // ───────────────────────────── Diagnostic ─────────────────────────────

    private static View DiagnosticPage(IApplication app)
    {
        var tools = Lbl(AssistantService.DetectTools(), 1, 1);
        tools.Width = Dim.Fill(1);

        var logFrame = new FrameView { Title = "Résultat", X = 0, Y = 3, Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };
        var log = LogView(out var lines, "Lance le diagnostic pour vérifier les outils et la configuration.");
        logFrame.Add(log);

        var run = Btn("▶ Lancer le diagnostic", 1, Pos.AnchorEnd(1));
        run.Accepting += (_, e) =>
        {
            e.Handled = true;
            RunScaffold(app, lines, log, ["doctor"], null);
        };

        return Page(Lbl("Outils détectés :", 1, 0), tools, logFrame, run);
    }

    // ───────────────────────────── Assistant IA ─────────────────────────────

    private static View AssistantPage(IApplication app, SharedState state)
    {
        var provider = AIService.HasApiKey ? $"{AIService.Provider} · {AIService.Model}" : "hors ligne (pas de clé API)";
        var info = Lbl($"Fournisseur : {provider}   ·   Entrée envoie, Échap quitte", 1, 0);
        info.Width = Dim.Fill(1);

        var chatFrame = new FrameView { Title = "Conversation", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Fill(5), SchemeName = "Scaffolder" };
        var chat = LogView(out var lines, "Demande-moi ce que tu veux : « crée une API .NET avec Postgres », « quels outils me manquent ? »…");
        chatFrame.Add(chat);
        foreach (var (role, text) in state.Chat)
            foreach (var line in text.Split('\n'))
                lines.Add((role == "user" ? "› Toi    " : "◆ Scaffolder  ") + line);

        var input = Field("", 1, Pos.AnchorEnd(4), Dim.Fill(20));
        var send = new Button { Text = "Envoyer", X = Pos.Right(input) + 1, Y = Pos.AnchorEnd(4), SchemeName = "Scaffolder" };
        var exec = new Button { Text = "", X = 1, Y = Pos.AnchorEnd(2), Visible = false, SchemeName = "Scaffolder" };
        var clear = Btn("Effacer", Pos.Right(exec) + 2, Pos.AnchorEnd(2));

        void SetPending(string? command)
        {
            state.PendingCommand = command;
            exec.Visible = command is not null;
            exec.Text = command is null ? "" : $"▶ Exécuter : {command}";
        }

        void Send()
        {
            var message = input.Text?.ToString()?.Trim() ?? "";
            if (message.Length == 0) return;

            input.Text = "";
            SetPending(null);
            state.Chat.Add(("user", message));
            AppendLines(app, lines, chat, "› Toi    " + message);
            AppendLines(app, lines, chat, "◆ Scaffolder  …réflexion");

            _ = Task.Run(async () =>
            {
                var reply = await AssistantService.AskAsync(
                    message, state.Chat.SkipLast(1).ToList(), Directory.GetCurrentDirectory());

                state.Chat.Add(("assistant", reply.Text));
                app.Invoke(() =>
                {
                    if (lines.Count > 0 && lines[^1].Contains("réflexion")) lines.RemoveAt(lines.Count - 1);
                    var first = true;
                    foreach (var line in reply.Text.Split('\n'))
                    {
                        lines.Add((first ? "◆ Scaffolder  " : "            ") + line);
                        first = false;
                    }
                    if (reply.Command is not null) SetPending(reply.Command);
                    chat.Value = lines.Count - 1;
                });
            });
        }

        send.Accepting += (_, e) => { e.Handled = true; Send(); };
        input.Accepting += (_, e) => { e.Handled = true; Send(); };

        exec.Accepting += (_, e) =>
        {
            e.Handled = true;
            if (state.PendingCommand is not { } command) return;
            SetPending(null);
            AppendLines(app, lines, chat, $"$ {command}");
            RunScaffold(app, lines, chat, AssistantService.ToArguments(command), null);
        };

        clear.Accepting += (_, e) =>
        {
            e.Handled = true;
            state.Chat.Clear();
            lines.Clear();
            lines.Add("Conversation effacée.");
            SetPending(null);
        };

        input.SetFocus();
        return Page(info, chatFrame, input, send, exec, clear);
    }

    // ───────────────────────────── Configuration ─────────────────────────────

    // ───────────────────────────── Commandes projet ─────────────────────────────

    private static View ProjectCommandsPage(IApplication app)
    {
        var form = new FrameView { Title = "Commandes projet", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Percent(45), SchemeName = "Scaffolder" };
        var dir = Field(Directory.GetCurrentDirectory(), 16, 1);
        var cmdSel = new OptionSelector
        {
            X = 16, Y = 3, Width = Dim.Fill(2),
            Labels = new[] { "run", "build", "test", "clean", "info", "lint", "format" },
            SchemeName = "Scaffolder"
        };
        var argsField = Field("", 16, 5, 40);
        form.Add(Lbl("Dossier projet", 1, 1), dir, Lbl("Commande", 1, 3), cmdSel, Lbl("Arguments", 1, 5), argsField);

        var logFrame = new FrameView { Title = "Sortie", X = 0, Y = Pos.Percent(45), Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };
        var log = LogView(out var lines, "Choisis une commande projet, puis « Exécuter ».");
        logFrame.Add(log);

        var cmds = new[] { "run", "build", "test", "clean", "info", "lint", "format" };
        var run = Btn("▶ Exécuter", 1, Pos.AnchorEnd(1));
        run.Accepting += (_, e) =>
        {
            e.Handled = true;
            var d = dir.Text?.ToString()?.Trim() ?? "";
            if (!Directory.Exists(d)) { AppendLines(app, lines, log, "✖ Dossier introuvable."); return; }
            var cmd = cmds[Math.Max(0, cmdSel.Value ?? 0)];
            var extra = argsField.Text?.ToString()?.Trim() ?? "";
            var args = new List<string> { cmd };
            if (extra.Length > 0) args.AddRange(extra.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            AppendLines(app, lines, log, $"$ scaffold {string.Join(' ', args)}  (dans {d})");
            RunScaffold(app, lines, log, args, null, d);
        };

        return Page(form, logFrame, run);
    }

    private static View ConfigPage(IApplication app)
    {
        var form = new FrameView { Title = "Configuration", X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(3), SchemeName = "Scaffolder" };

        // Lignes : label Y, champ Y
        int y = 1;
        // Thème
        var themeL = Lbl("Thème", 1, y);
        var themeSel = new OptionSelector { X = 22, Y = y, Width = Dim.Fill(2), Labels = new[] { "sombre", "clair" }, SchemeName = "Scaffolder" };
        var currentTheme = ConfigService.Get(ConfigService.Keys.Theme) ?? "dark";
        themeSel.Value = currentTheme == "light" ? 1 : 0;
        y += 2;

        // Clé API
        var keyState = AIService.HasApiKey ? "clé enregistrée ✔" : "aucune clé";
        var keyL = Lbl($"Clé API ({keyState})", 1, y);
        var key = new TextField { Text = "", Secret = true, X = 22, Y = y, Width = Dim.Fill(14), SchemeName = "Scaffolder" };
        var saveKey = Btn("Enregistrer", Pos.AnchorEnd(14), y);
        y += 2;

        // Fournisseur
        var providerL = Lbl("Fournisseur", 1, y);
        var providerSel = new OptionSelector { X = 22, Y = y, Width = Dim.Fill(2), Labels = ProviderChoices, SchemeName = "Scaffolder" };
        var current = Array.IndexOf(ProviderChoices, AIService.Provider);
        providerSel.Value = current >= 0 ? current : 2;
        y += 2;

        // Modèle
        var modelL = Lbl("Modèle", 1, y);
        var model = Field(AIService.Model, 22, y, 40);
        y += 2;

        // Auteur / Société
        var authorL = Lbl("Auteur", 1, y);
        var author = Field(ConfigService.Get(ConfigService.Keys.Author) ?? "", 22, y, 40);
        y += 2;
        var companyL = Lbl("Société", 1, y);
        var company = Field(ConfigService.Get(ConfigService.Keys.Company) ?? "", 22, y, 40);
        y += 2;

        // Licence / Framework
        var licenseL = Lbl("Licence", 1, y);
        var license = Field(ConfigService.Get(ConfigService.Keys.License) ?? "", 22, y, 40);
        y += 2;
        var fwL = Lbl("Framework", 1, y);
        var fw = Field(ConfigService.Get(ConfigService.Keys.Framework) ?? "", 22, y, 40);
        y += 2;

        // Defaults auth/db/arch
        var dAuthL = Lbl("Auth par défaut", 1, y);
        var dAuth = new OptionSelector { X = 22, Y = y, Width = Dim.Fill(2), Labels = AuthChoices, SchemeName = "Scaffolder" };
        var curAuth = Array.IndexOf(AuthChoices, ConfigService.Get(ConfigService.Keys.DefaultAuth) ?? "none");
        dAuth.Value = curAuth >= 0 ? curAuth : 0;
        y += 2;
        var dDbL = Lbl("BDD par défaut", 1, y);
        var dDb = new OptionSelector { X = 22, Y = y, Width = Dim.Fill(2), Labels = DbChoices, SchemeName = "Scaffolder" };
        var curDb = Array.IndexOf(DbChoices, ConfigService.Get(ConfigService.Keys.DefaultDb) ?? "none");
        dDb.Value = curDb >= 0 ? curDb : 0;
        y += 2;
        var dArchL = Lbl("Archi. par défaut", 1, y);
        var dArch = new OptionSelector { X = 22, Y = y, Width = Dim.Fill(2), Labels = ArchChoices, SchemeName = "Scaffolder" };
        var curArch = Array.IndexOf(ArchChoices, ConfigService.Get(ConfigService.Keys.DefaultArch) ?? "mvc");
        dArch.Value = curArch >= 0 ? curArch : 0;
        y += 2;

        // Template favori
        var favL = Lbl("Template favori", 1, y);
        var fav = Field(ConfigService.Get(ConfigService.Keys.Fav) ?? "", 22, y, 40);
        y += 2;

        var status = Lbl("", 1, y);
        status.Width = Dim.Fill(1);
        y += 2;

        var saveAll = Btn("✔ Enregistrer tout", 1, y);
        var testAi = Btn("Tester l'IA", Pos.Right(saveAll) + 2, y);

        form.Add(
            themeL, themeSel,
            keyL, key, saveKey,
            providerL, providerSel,
            modelL, model,
            authorL, author,
            companyL, company,
            licenseL, license,
            fwL, fw,
            dAuthL, dAuth,
            dDbL, dDb,
            dArchL, dArch,
            favL, fav,
            status, saveAll, testAi);

        saveKey.Accepting += (_, e) =>
        {
            e.Handled = true;
            var value = key.Text?.ToString()?.Trim() ?? "";
            if (value.Length == 0) { status.Text = "✖ Clé vide : rien enregistré."; return; }
            ConfigService.Set(ConfigService.Keys.ApiKey, value);
            key.Text = "";
            keyL.Text = "Clé API (clé enregistrée ✔)";
            status.Text = "✔ Clé API enregistrée.";
        };

        saveAll.Accepting += (_, e) =>
        {
            e.Handled = true;
            var theme = (themeSel.Value ?? 0) == 1 ? "light" : "dark";
            ConfigService.Set(ConfigService.Keys.Theme, theme);
            ConfigService.Set(ConfigService.Keys.Provider, ProviderChoices[Math.Max(0, providerSel.Value ?? 0)]);
            var m = model.Text?.ToString()?.Trim() ?? "";
            if (m.Length > 0) ConfigService.Set(ConfigService.Keys.Model, m);
            SetIfNotEmpty(ConfigService.Keys.Author, author.Text?.ToString());
            SetIfNotEmpty(ConfigService.Keys.Company, company.Text?.ToString());
            SetIfNotEmpty(ConfigService.Keys.License, license.Text?.ToString());
            SetIfNotEmpty(ConfigService.Keys.Framework, fw.Text?.ToString());
            SetIfNotEmpty(ConfigService.Keys.Fav, fav.Text?.ToString());
            ConfigService.Set(ConfigService.Keys.DefaultAuth, AuthChoices[Math.Max(0, dAuth.Value ?? 0)]);
            ConfigService.Set(ConfigService.Keys.DefaultDb, DbChoices[Math.Max(0, dDb.Value ?? 0)]);
            ConfigService.Set(ConfigService.Keys.DefaultArch, ArchChoices[Math.Max(0, dArch.Value ?? 0)]);
            status.Text = $"✔ Enregistré. Thème : {theme} (relance le TUI pour l'appliquer).";
        };

        testAi.Accepting += (_, e) =>
        {
            e.Handled = true;
            status.Text = "Test en cours…";
            _ = Task.Run(() =>
            {
                var (ok, msg) = AIService.TestAsync().GetAwaiter().GetResult();
                app.Invoke(() => status.Text = (ok ? "✔ " : "✖ ") + msg);
            });
        };

        var hint = Lbl("La clé API est stockée chiffrée. Le thème s'applique au prochain lancement.", 1, Pos.AnchorEnd(2));
        hint.Width = Dim.Fill(1);
        return Page(form, hint);
    }

    private static void SetIfNotEmpty(string key, string? value)
    {
        var v = value?.Trim();
        if (!string.IsNullOrEmpty(v)) ConfigService.Set(key, v!);
    }

    // ───────────────────────────── Toutes les commandes ─────────────────────────────

    private sealed class CommandForm
    {
        public Dictionary<string, string> Values { get; } = new(StringComparer.Ordinal);
        public HashSet<string> Flags { get; } = new(StringComparer.Ordinal);
    }

    private static string KeyOf(CliEntry entry, CliParameter p) => entry.Path + "|" + p.Token;

    /// <summary>Construit les arguments de la commande à partir des valeurs saisies. Renvoie une erreur si un champ requis manque.</summary>
    private static (List<string> Args, string? Error) BuildArgs(CliEntry entry, CommandForm form)
    {
        var args = entry.Path.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();

        foreach (var p in entry.Parameters.Where(p => p.IsArgument))
        {
            form.Values.TryGetValue(KeyOf(entry, p), out var value);
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                if (p.Required) return ([], $"Argument requis manquant : {p.Token}");
                continue;
            }
            if (p.Multiple) args.AddRange(value.Split(' ', StringSplitOptions.RemoveEmptyEntries));
            else args.Add(value);
        }

        foreach (var p in entry.Parameters.Where(p => !p.IsArgument))
        {
            if (p.IsFlag)
            {
                if (form.Flags.Contains(KeyOf(entry, p))) args.Add(p.Token);
                continue;
            }

            form.Values.TryGetValue(KeyOf(entry, p), out var value);
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
            {
                if (p.Required) return ([], $"Option requise manquante : {p.Token}");
                continue;
            }
            args.Add(p.Token);
            args.Add(value);
        }

        return (args, null);
    }

    private static View CommandsPage(IApplication app)
    {
        var entries = CliCatalog.All;
        var forms = new Dictionary<string, CommandForm>(StringComparer.Ordinal);

        var filter = Field("", 12, 0, Dim.Fill(1));
        filter.SetFocus();
        var left = new FrameView
        {
            Title = $"Commandes ({entries.Count})",
            X = 0, Y = 1, Width = Dim.Percent(36), Height = Dim.Fill(),
            SchemeName = "Scaffolder"
        };
        var commandList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        left.Add(commandList);

        var right = new View { X = Pos.Right(left), Y = 1, Width = Dim.Fill(), Height = Dim.Fill(), SchemeName = "Scaffolder" };
        var description = Lbl("", 1, 0);
        description.Width = Dim.Fill(1);

        var paramsFrame = new FrameView { Title = "Paramètres", X = 0, Y = 1, Width = Dim.Fill(), Height = Dim.Percent(38), SchemeName = "Scaffolder" };
        var paramsList = new ListView { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        paramsFrame.Add(paramsList);

        var editorFrame = new FrameView { Title = "Valeur", X = 0, Y = Pos.Bottom(paramsFrame), Width = Dim.Fill(), Height = 6, SchemeName = "Scaffolder" };
        var editorHelp = Lbl("Choisis un paramètre.", 1, 0);
        editorHelp.Width = Dim.Fill(1);
        var editorText = Field("", 1, 2, Dim.Fill(1));
        var editorFlag = new CheckBox { Text = "Activé", X = 1, Y = 2, Visible = false, SchemeName = "Scaffolder" };
        editorFrame.Add(editorHelp, editorText, editorFlag);

        var preview = Lbl("", 1, Pos.Bottom(editorFrame));
        preview.Width = Dim.Fill(1);
        var run = Btn("▶ Exécuter", 1, Pos.Bottom(preview));
        var reset = Btn("Réinitialiser", Pos.Right(run) + 2, Pos.Bottom(preview));

        var logFrame = new FrameView { Title = "Sortie", X = 0, Y = Pos.Bottom(run), Width = Dim.Fill(), Height = Dim.Fill(), SchemeName = "Scaffolder" };
        var log = LogView(out var logLines, "Choisis une commande, remplis ses paramètres, puis « Exécuter ».");
        logFrame.Add(log);

        right.Add(description, paramsFrame, editorFrame, preview, run, reset, logFrame);

        List<CliEntry> visible = [];
        CliEntry? selected = null;
        CommandForm? currentForm = null;
        CliParameter? selectedParam = null;

        string Marker(CliEntry e, CliParameter p, CommandForm f)
        {
            var key = KeyOf(e, p);
            if (p.IsFlag) return f.Flags.Contains(key) ? "[x]" : "[ ]";
            return f.Values.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? $"« {Trunc(v, 14)} »" : "";
        }

        void RefreshPreview()
        {
            if (selected is null || currentForm is null) return;
            var (args, error) = BuildArgs(selected, currentForm);
            preview.Text = error is not null
                ? $"✖ {error}"
                : "$ scaffold " + string.Join(' ', args.Select(Quote));
        }

        void RefreshParams()
        {
            if (selected is null || currentForm is null) return;
            var rows = selected.Parameters.Select(p =>
                $"{(p.Required ? "*" : " ")} {Trunc(p.Token, 18),-18} {Marker(selected, p, currentForm),-16} {Trunc(p.Description, 40)}");
            var index = paramsList.Value;
            paramsList.SetSource(new ObservableCollection<string>(rows));
            if (index is int i && i >= 0 && i < selected.Parameters.Count) paramsList.Value = i;
            RefreshPreview();
        }

        void LoadParam(CliParameter? p)
        {
            selectedParam = p;
            if (selected is null || currentForm is null || p is null)
            {
                editorHelp.Text = "Choisis un paramètre.";
                editorText.Visible = true;
                editorFlag.Visible = false;
                return;
            }

            var key = KeyOf(selected, p);
            editorHelp.Text = Trunc($"{p.Token} — {p.Description}" + (p.Required ? " (requis)" : ""), 200);
            if (p.IsFlag)
            {
                editorText.Visible = false;
                editorFlag.Visible = true;
                editorFlag.Value = currentForm.Flags.Contains(key) ? CheckState.Checked : CheckState.UnChecked;
            }
            else
            {
                editorFlag.Visible = false;
                editorText.Visible = true;
                editorText.Text = currentForm.Values.TryGetValue(key, out var v) ? v : "";
                editorText.SetFocus();
            }
        }

        void LoadCommand(CliEntry? entry)
        {
            selected = entry;
            if (entry is null) return;

            if (!forms.TryGetValue(entry.Path, out var form))
            {
                form = new CommandForm();
                forms[entry.Path] = form;
            }
            currentForm = form;

            description.Text = Trunc($"scaffold {entry.Path} — {entry.Description}", 300);
            paramsList.Value = null;
            RefreshParams();
            paramsList.Value = entry.Parameters.Count > 0 ? 0 : null;
            LoadParam(entry.Parameters.Count > 0 ? entry.Parameters[0] : null);
            if (entry.Parameters.Count == 0) RefreshPreview();
        }

        void Refresh()
        {
            var q = filter.Text?.ToString()?.Trim().ToLowerInvariant() ?? "";
            visible = entries
                .Where(e => q.Length == 0 || (e.Path + " " + e.Description).ToLowerInvariant().Contains(q))
                .ToList();
            commandList.SetSource(new ObservableCollection<string>(
                visible.Select(e => $"{Trunc(e.Path, 26),-26} {Trunc(e.Description, 60)}")));
            if (visible.Count > 0) commandList.Value = 0;
        }

        filter.TextChanged += (_, _) => Refresh();
        commandList.ValueChanged += (_, _) =>
        {
            if (commandList.Value is int i && i >= 0 && i < visible.Count) LoadCommand(visible[i]);
        };
        paramsList.ValueChanged += (_, _) =>
        {
            if (selected is null || paramsList.Value is not int i || i < 0 || i >= selected.Parameters.Count) return;
            LoadParam(selected.Parameters[i]);
        };

        editorText.TextChanged += (_, _) =>
        {
            if (selected is null || currentForm is null || selectedParam is null || selectedParam.IsFlag) return;
            currentForm.Values[KeyOf(selected, selectedParam)] = editorText.Text?.ToString() ?? "";
            RefreshParams();
        };

        editorFlag.ValueChanged += (_, _) =>
        {
            if (selected is null || currentForm is null || selectedParam is null || !selectedParam.IsFlag) return;
            var key = KeyOf(selected, selectedParam);
            if (editorFlag.Value.HasFlag(CheckState.Checked)) currentForm.Flags.Add(key);
            else currentForm.Flags.Remove(key);
            RefreshParams();
        };

        run.Accepting += (_, e) =>
        {
            e.Handled = true;
            if (selected is null || currentForm is null) return;
            var (args, error) = BuildArgs(selected, currentForm);
            if (error is not null)
            {
                AppendLines(app, logLines, log, $"✖ {error}");
                return;
            }
            AppendLines(app, logLines, log, "$ scaffold " + string.Join(' ', args.Select(Quote)));
            RunScaffold(app, logLines, log, args, null);
        };

        reset.Accepting += (_, e) =>
        {
            e.Handled = true;
            if (selected is null || currentForm is null) return;
            currentForm.Values.Clear();
            currentForm.Flags.Clear();
            RefreshParams();
            LoadParam(selectedParam);
        };

        Refresh();
        var page = Page(Lbl("Rechercher", 1, 0), filter, left, right);
        commandList.SetFocus();
        return page;
    }

    private static string Trunc(string text, int max) =>
        text.Length <= max ? text : text[..Math.Max(0, max - 1)] + "…";

    private static string Quote(string arg) =>
        arg.Length > 0 && !arg.Any(c => char.IsWhiteSpace(c) || c == '"') ? arg : "\"" + arg.Replace("\"", "\\\"") + "\"";

    // ───────────────────────────── Exécution ─────────────────────────────

    /// <summary>Lance `scaffold` avec les arguments donnés et écrit la sortie dans le journal.</summary>
    private static void RunScaffold(IApplication app, ObservableCollection<string> lines, ListView view,
        IEnumerable<string> args, string? successMessage, string? workingDirectory = null)
    {
        _ = Task.Run(async () =>
        {
            var (file, cliArgs) = ProcessService.ScaffoldCommand(args);
            var result = await ProcessService.RunResultAsync(
                file, cliArgs, workingDirectory ?? "",
                streamOutput: false, timeout: TimeSpan.FromMinutes(20));

            AppendLines(app, lines, view, result.Output);
            var summary = result.Success
                ? "✔ " + (successMessage ?? "Terminé.")
                : $"✖ Échec (code {result.ExitCode}).";
            AppendLines(app, lines, view, summary);
        });
    }
}
