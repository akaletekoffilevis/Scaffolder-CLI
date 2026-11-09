namespace Scaffolder.Web;

/// <summary>Page unique de l'interface web locale (HTML, CSS et JS embarqués, aucune dépendance externe).</summary>
public static class UiPage
{
    public const string Html = """
<!DOCTYPE html>
<html lang="fr" data-theme="dark">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Scaffolder — interface web</title>
<style>
  :root, [data-theme="dark"] {
    --bg: #0d1117; --bg2: #161b22; --bg3: #21262d;
    --border: #30363d; --border2: #484f58;
    --text: #e6edf3; --text2: #8b949e; --text3: #6e7681;
    --accent: #58a6ff; --accent2: #3fb950; --accent3: #d2a8ff;
    --accent-glow: rgba(88,166,255,.15);
    --accent-gradient: linear-gradient(135deg, #58a6ff, #d2a8ff);
    --ok: #3fb950; --warn: #d29922; --err: #f85149;
    --card-bg: #161b22; --input-bg: #0d1117;
    --code-bg: #0d1117; --sidebar-bg: #0d1117;
    --shadow-sm: 0 1px 2px rgba(0,0,0,.3);
    --shadow-md: 0 4px 12px rgba(0,0,0,.4);
    --shadow-lg: 0 8px 30px rgba(0,0,0,.5);
    --radius: 8px; --radius-sm: 6px; --radius-lg: 12px; --radius-xl: 16px;
    --transition: .2s cubic-bezier(.4,0,.2,1);
    --transition-slow: .35s cubic-bezier(.4,0,.2,1);
    --font-mono: "SF Mono", Monaco, "Cascadia Code", "JetBrains Mono", monospace;
  }
  [data-theme="light"] {
    --bg: #ffffff; --bg2: #f6f8fa; --bg3: #eaeef2;
    --border: #d0d7de; --border2: #afb8c1;
    --text: #1f2328; --text2: #656d76; --text3: #8c959f;
    --accent: #0969da; --accent2: #1a7f37; --accent3: #8250df;
    --accent-glow: rgba(9,105,218,.1);
    --accent-gradient: linear-gradient(135deg, #0969da, #8250df);
    --ok: #1a7f37; --warn: #9a6700; --err: #cf222e;
    --card-bg: #ffffff; --input-bg: #f6f8fa;
    --code-bg: #f6f8fa; --sidebar-bg: #f6f8fa;
    --shadow-sm: 0 1px 2px rgba(0,0,0,.06);
    --shadow-md: 0 4px 12px rgba(0,0,0,.08);
    --shadow-lg: 0 8px 30px rgba(0,0,0,.1);
  }
  html { transition: background .3s, color .3s; }
  * { box-sizing: border-box; margin: 0; padding: 0; }
  html { scroll-behavior: smooth; }
  ::selection { background: var(--accent); color: #fff; }
  :focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; border-radius: 4px; }
  body {
    font: 15px/1.6 -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
    background: var(--bg); color: var(--text);
    display: flex; min-height: 100vh;
    -webkit-font-smoothing: antialiased;
  }
  ::-webkit-scrollbar { width: 8px; height: 8px; }
  ::-webkit-scrollbar-track { background: transparent; }
  ::-webkit-scrollbar-thumb { background: var(--border); border-radius: 4px; }
  ::-webkit-scrollbar-thumb:hover { background: var(--text2); }

  /* === Sidebar === */
  nav {
    width: 240px; background: var(--sidebar-bg);
    border-right: 1px solid var(--border);
    padding: 20px 12px; flex-shrink: 0;
    display: flex; flex-direction: column;
    position: sticky; top: 0; height: 100vh; overflow-y: auto;
  }
  nav .brand { display: flex; align-items: center; gap: 10px; padding: 8px 10px 18px; }
  nav .brand .logo {
    width: 32px; height: 32px; border-radius: 8px;
    background: var(--accent-gradient);
    display: flex; align-items: center; justify-content: center;
    font-weight: 700; color: #fff; font-size: 16px;
    box-shadow: 0 4px 12px rgba(88,166,255,.3);
  }
  nav .brand h1 { font-size: 17px; font-weight: 700; margin: 0; }
  nav .brand .ver { color: var(--text2); font-size: 11px; font-family: var(--font-mono); }
  nav .section-label {
    font-size: 11px; text-transform: uppercase; letter-spacing: .08em;
    color: var(--text3); padding: 14px 10px 6px; font-weight: 600;
  }
  nav button {
    display: flex; align-items: center; gap: 10px; width: 100%;
    text-align: left; background: none; border: 0; color: var(--text2);
    padding: 9px 10px; border-radius: var(--radius-sm); cursor: pointer;
    font: inherit; font-size: 14px; transition: background var(--transition), color var(--transition);
  }
  nav button:hover { color: var(--text); background: var(--bg3); }
  nav button.active { color: var(--text); background: var(--bg3); font-weight: 600; }
  nav button .icon { width: 18px; text-align: center; font-size: 15px; opacity: .8; }
  nav .footer { margin-top: auto; padding: 12px 10px; border-top: 1px solid var(--border); font-size: 12px; color: var(--text3); }
  .theme-toggle {
    display: inline-flex; align-items: center; gap: 6px;
    background: var(--bg3); border: 1px solid var(--border);
    color: var(--text); padding: 6px 10px; border-radius: var(--radius-sm);
    cursor: pointer; font: inherit; font-size: 12px;
    transition: background var(--transition), border-color var(--transition);
    width: 100%; justify-content: center;
  }
  .theme-toggle:hover { background: var(--bg2); border-color: var(--border2); }

  /* === Main === */
  main { flex: 1; padding: 32px 40px; max-width: 1080px; animation: fadeIn .35s ease; }
  section { display: none; } section.active { display: block; animation: fadeIn .3s ease; }
  @keyframes fadeIn { from { opacity: 0; transform: translateY(8px); } to { opacity: 1; transform: translateY(0); } }
  h2 { margin: 0 0 4px; font-size: 26px; font-weight: 700; letter-spacing: -.01em; }
  p.lead { color: var(--text2); margin: 0 0 24px; font-size: 15px; }

  /* === Cards === */
  .card {
    background: var(--card-bg); border: 1px solid var(--border);
    border-radius: var(--radius-lg); padding: 24px;
    margin-bottom: 20px; box-shadow: var(--shadow-sm);
    transition: border-color var(--transition), box-shadow var(--transition);
  }
  .card:hover { border-color: var(--border2); box-shadow: var(--shadow-md); }
  .card h3 { font-size: 16px; font-weight: 600; margin-bottom: 12px; }
  .card-header { display: flex; align-items: center; justify-content: space-between; margin-bottom: 16px; }

  /* === Grid === */
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 16px 20px; }
  .grid .full { grid-column: 1 / -1; }

  /* === Forms === */
  label { display: block; font-size: 13px; color: var(--text2); margin-bottom: 6px; font-weight: 500; }
  input, select, textarea {
    width: 100%; background: var(--input-bg); color: var(--text);
    border: 1px solid var(--border); border-radius: var(--radius);
    padding: 10px 12px; font: inherit; font-size: 14px;
    transition: border-color var(--transition), box-shadow var(--transition);
  }
  input:focus, select:focus, textarea:focus {
    outline: none; border-color: var(--accent);
    box-shadow: 0 0 0 3px var(--accent-glow);
  }
  input::placeholder { color: var(--text3); }
  textarea { min-height: 100px; resize: vertical; font-family: var(--font-mono); font-size: 13px; }

  .checks { display: flex; gap: 20px; flex-wrap: wrap; align-items: center; }
  .checks label { color: var(--text); display: flex; gap: 8px; align-items: center; margin: 0; font-weight: 400; cursor: pointer; }
  .checks input { width: 16px; height: 16px; accent-color: var(--accent); }

  /* === Buttons === */
  .actions { margin-top: 18px; display: flex; gap: 10px; align-items: center; flex-wrap: wrap; }
  button.primary {
    background: var(--accent-gradient); color: #fff; border: 0;
    padding: 10px 20px; border-radius: var(--radius); font-weight: 600;
    cursor: pointer; font-size: 14px; transition: transform var(--transition), box-shadow var(--transition);
    box-shadow: 0 4px 12px var(--accent-glow);
  }
  button.primary:hover { transform: translateY(-1px); box-shadow: 0 6px 16px var(--accent-glow); }
  button.primary:active { transform: translateY(0); }
  button.ghost {
    background: var(--bg3); border: 1px solid var(--border); color: var(--text);
    padding: 9px 16px; border-radius: var(--radius); cursor: pointer;
    font: inherit; font-size: 14px; transition: background var(--transition), border-color var(--transition);
  }
  button.ghost:hover { background: var(--bg2); border-color: var(--border2); }
  button:disabled { opacity: .5; cursor: wait; transform: none; }
  button.danger { background: var(--err); color: #fff; }

  /* === Pre / code === */
  pre {
    background: var(--code-bg); border: 1px solid var(--border);
    border-radius: var(--radius); padding: 16px;
    white-space: pre-wrap; word-break: break-word;
    max-height: 400px; overflow: auto;
    font: 13px/1.5 var(--font-mono); margin: 16px 0 0;
  }
  code { font-family: var(--font-mono); font-size: 13px; background: var(--bg3); padding: 2px 6px; border-radius: 4px; }

  /* === Status === */
  .status { font-weight: 600; font-size: 14px; }
  .ok { color: var(--ok); } .err { color: var(--err); } .warn { color: var(--warn); }
  .muted { color: var(--text2); font-size: 13px; }

  /* === Tags === */
  .tag { font-size: 11px; padding: 3px 9px; border-radius: 99px; border: 1px solid var(--border); color: var(--text2); font-weight: 500; }
  .tag.ok { color: var(--ok); border-color: rgba(63,185,80,.4); background: rgba(63,185,80,.08); }
  .tag.err { color: var(--err); border-color: rgba(248,81,73,.4); background: rgba(248,81,73,.08); }
  .tag.warn { color: var(--warn); border-color: rgba(210,153,34,.4); background: rgba(210,153,34,.08); }

  /* === Tables === */
  table { width: 100%; border-collapse: collapse; }
  th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid var(--border); font-size: 14px; }
  th { color: var(--text2); font-weight: 600; font-size: 12px; text-transform: uppercase; letter-spacing: .05em; background: var(--bg3); }
  tr:hover td { background: rgba(88,166,255,.04); }
  tbody tr:last-child td { border-bottom: 0; }

  /* === Toast === */
  .toast {
    position: fixed; bottom: 24px; right: 24px; z-index: 1000;
    background: var(--card-bg); border: 1px solid var(--border);
    border-radius: var(--radius-lg); padding: 14px 18px;
    box-shadow: var(--shadow-lg); max-width: 360px;
    transform: translateY(100px); opacity: 0;
    transition: transform var(--transition-slow), opacity var(--transition-slow);
    font-size: 14px;
  }
  .toast.show { transform: translateY(0); opacity: 1; }
  .toast.ok { border-left: 3px solid var(--ok); }
  .toast.err { border-left: 3px solid var(--err); }

  /* === Spinner === */
  .spinner {
    display: inline-block; width: 14px; height: 14px;
    border: 2px solid var(--border); border-top-color: var(--accent);
    border-radius: 50%; animation: spin .7s linear infinite;
    vertical-align: middle; margin-right: 6px;
  }
  @keyframes spin { to { transform: rotate(360deg); } }

  /* === Empty state === */
  .empty { text-align: center; padding: 40px 20px; color: var(--text2); }
  .empty .icon { font-size: 40px; margin-bottom: 12px; opacity: .5; }

  /* === Responsive === */
  @media (max-width: 860px) {
    body { flex-direction: column; }
    nav { width: 100%; height: auto; position: static; flex-direction: row; flex-wrap: wrap; padding: 12px; gap: 4px; }
    nav .brand { padding: 0 8px 0 0; }
    nav .section-label { display: none; }
    nav button { width: auto; padding: 7px 10px; font-size: 13px; }
    nav .footer { display: none; }
    main { padding: 20px; }
    .grid { grid-template-columns: 1fr; }
  }
  @media (max-width: 480px) {
    h2 { font-size: 20px; }
    .actions { flex-direction: column; align-items: stretch; }
    button.primary, button.ghost { width: 100%; }
  }
</style>
</head>
<body>
<nav>
  <div class="brand">
    <div class="logo">S</div>
    <div>
      <h1>Scaffolder</h1>
      <div class="ver" id="ver">v…</div>
    </div>
  </div>
  <div class="section-label">Génération</div>
  <button data-page="dashboard" class="active"><span class="icon">📊</span> Tableau de bord</button>
  <button data-page="new"><span class="icon">✨</span> Nouveau projet</button>
  <button data-page="stack"><span class="icon">🧱</span> Stack fullstack</button>
  <button data-page="add"><span class="icon">➕</span> Ajouter un composant</button>
  <button data-page="templates"><span class="icon">📋</span> Templates</button>
  <button data-page="commands"><span class="icon">⌨️</span> Commandes</button>
  <div class="section-label">Outils</div>
  <button data-page="config"><span class="icon">⚙️</span> Configuration</button>
  <button data-page="ai"><span class="icon">🤖</span> Assistant IA</button>
  <button data-page="stats"><span class="icon">📈</span> Statistiques</button>
  <div class="footer">
    <button class="theme-toggle" id="theme-toggle" title="Changer de thème">🌙 Mode sombre</button>
    <div style="margin-top:8px">100 % local · données chiffrées</div>
  </div>
</nav>

<main>
  <section id="dashboard" class="active">
    <h2>Tableau de bord</h2>
    <p class="lead">État de ton environnement et des outils détectés.</p>
    <div class="card">
      <div class="actions" style="margin:0 0 12px"><button class="ghost" id="refresh-doctor">Relancer le diagnostic</button><span class="muted" id="doctor-meta"></span></div>
      <table><thead><tr><th>Outil</th><th>Statut</th><th>Version</th></tr></thead><tbody id="doctor-body"><tr><td colspan="3" class="muted">Chargement…</td></tr></tbody></table>
    </div>
  </section>

  <section id="new">
    <h2>Nouveau projet</h2>
    <p class="lead">Crée un projet avec l'outil natif (dotnet new, npm create, cargo new…).</p>
    <div class="card">
      <div class="grid">
        <div class="full"><label for="new-tpl">Template</label><select id="new-tpl"></select></div>
        <div><label for="new-name">Nom du projet</label><input id="new-name" value="mon-projet" autocomplete="off"></div>
        <div><label for="new-output">Dossier (optionnel)</label><input id="new-output" placeholder="Par défaut : dossier courant / nom" autocomplete="off"></div>
        <div class="full checks">
          <label><input type="checkbox" id="new-git" checked> Git</label>
          <label><input type="checkbox" id="new-docker"> Dockerfile</label>
          <label><input type="checkbox" id="new-ci"> CI/CD</label>
        </div>
        <div id="new-config-group" class="full grid" style="display:none">
          <div><label for="new-auth">Authentification</label><select id="new-auth"><option>none</option><option>jwt</option><option>identity</option></select></div>
          <div><label for="new-db">Base de données</label><select id="new-db"><option>none</option><option>sqlite</option><option>sqlserver</option><option>postgresql</option></select></div>
          <div><label for="new-arch">Architecture</label><select id="new-arch"><option>mvc</option><option>clean</option><option>minimal</option></select></div>
        </div>
      </div>
      <div class="actions"><button class="primary" id="new-go">Générer le projet</button><span class="status" id="new-status"></span></div>
      <pre id="new-out" hidden></pre>
    </div>
  </section>

  <section id="stack">
    <h2>Stack fullstack</h2>
    <p class="lead">Frontend + backend + base de données, dans un seul dossier.</p>
    <div class="card">
      <div class="grid">
        <div><label for="stack-name">Nom du projet</label><input id="stack-name" value="mon-app"></div>
        <div><label for="stack-output">Dossier parent (optionnel)</label><input id="stack-output" placeholder="Dossier courant par défaut"></div>
        <div><label for="stack-fe">Frontend</label><select id="stack-fe"><option>react</option><option>vue</option><option>svelte</option><option>next</option><option>nuxt</option><option>none</option></select></div>
        <div><label for="stack-be">Backend</label><select id="stack-be"><option>webapi</option><option>fastapi</option><option>laravel</option><option>rails</option><option>none</option></select></div>
        <div class="full"><label for="stack-db">Base de données</label><select id="stack-db"><option>postgres</option><option>mysql</option><option>sqlite</option><option>mongodb</option><option>none</option></select></div>
      </div>
      <div class="actions"><button class="primary" id="stack-go">Générer la stack</button><span class="status" id="stack-status"></span></div>
      <pre id="stack-out" hidden></pre>
    </div>
  </section>

  <section id="add">
    <h2>Ajouter un composant</h2>
    <p class="lead">Ajoute un fichier type (contrôleur, modèle…) dans un projet existant. Liste des types : <code>scaffold add --list</code>.</p>
    <div class="card">
      <div class="grid">
        <div><label for="add-type">Type</label><input id="add-type" placeholder="ex : controller" autocomplete="off"></div>
        <div><label for="add-name">Nom</label><input id="add-name" placeholder="ex : Users" autocomplete="off"></div>
        <div class="full"><label for="add-dir">Dossier du projet (chemin absolu)</label><input id="add-dir" placeholder="/chemin/vers/mon-projet" autocomplete="off"></div>
      </div>
      <div class="actions"><button class="primary" id="add-go">Ajouter</button><span class="status" id="add-status"></span></div>
      <pre id="add-out" hidden></pre>
    </div>
  </section>

  <section id="templates">
    <h2>Templates</h2>
    <p class="lead">Tous les templates disponibles. Clique sur un template pour l'utiliser dans « Nouveau projet ».</p>
    <div class="card">
      <input id="tpl-filter" placeholder="Filtrer (ex : python, react, api…)" autocomplete="off" style="margin-bottom:14px">
      <table><thead><tr><th>Template</th><th>Langage</th><th>Description</th><th>Outil</th></tr></thead><tbody id="tpl-body"></tbody></table>
    </div>
  </section>

  <section id="commands">
    <h2>Commandes</h2>
    <p class="lead">Toutes les commandes du CLI, générées automatiquement. Choisis une commande, remplis les paramètres, exécute.</p>
    <div class="card">
      <input id="cmd-filter" placeholder="Filtrer (ex : docker, git, config…)" autocomplete="off" style="margin-bottom:14px">
      <select id="cmd-select" style="margin-bottom:14px"></select>
      <div id="cmd-params" class="grid" style="margin-bottom:14px"></div>
      <div class="actions">
        <button class="primary" id="cmd-run">Exécuter</button>
        <button class="ghost" id="cmd-reset">Réinitialiser</button>
        <span class="status" id="cmd-status"></span>
      </div>
      <pre id="cmd-out" hidden></pre>
    </div>
  </section>

  <section id="config">
    <h2>Configuration</h2>
    <p class="lead">Préférences enregistrées localement (~/.scaffolder). La clé API n'est jamais affichée en entier.</p>
    <div class="card" id="config-list"></div>
  </section>

  <section id="ai">
    <h2>Assistant IA</h2>
    <p class="lead">Fournisseur actuel : <span id="ai-provider" class="tag">—</span>. Sans clé API, le mode hors ligne répond.</p>
    <div class="card">
      <label for="ai-prompt">Ta question</label>
      <textarea id="ai-prompt" placeholder="ex : Quelle différence entre REST et GraphQL ?"></textarea>
      <div class="actions"><button class="primary" id="ai-go">Demander</button><span class="status" id="ai-status"></span></div>
      <pre id="ai-out" hidden></pre>
    </div>
  </section>

  <section id="stats">
    <h2>Statistiques</h2>
    <p class="lead">Données locales uniquement : historique, favoris, plugins.</p>
    <div class="card"><div class="actions" style="margin:0 0 6px"><button class="ghost" id="refresh-stats">Actualiser</button></div><pre id="stats-out">Chargement…</pre></div>
  </section>
</main>

<script>
const $ = (id) => document.getElementById(id);
const pages = document.querySelectorAll('nav button');

// Theme toggle
(function initTheme() {
  const saved = localStorage.getItem('scaffolder-theme');
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
  const theme = saved || (prefersDark ? 'dark' : 'light');
  document.documentElement.setAttribute('data-theme', theme);
  updateThemeButton(theme);
})();
function updateThemeButton(theme) {
  const btn = $('theme-toggle');
  if (btn) btn.textContent = (theme === 'dark' ? '☀️ Mode clair' : '🌙 Mode sombre');
}
$('theme-toggle')?.addEventListener('click', () => {
  const current = document.documentElement.getAttribute('data-theme') || 'dark';
  const next = current === 'dark' ? 'light' : 'dark';
  document.documentElement.setAttribute('data-theme', next);
  localStorage.setItem('scaffolder-theme', next);
  updateThemeButton(next);
});

function show(page) {
  pages.forEach(b => b.classList.toggle('active', b.dataset.page === page));
  document.querySelectorAll('section').forEach(s => s.classList.toggle('active', s.id === page));
  if (page === 'dashboard') loadDoctor();
  if (page === 'templates' && !templates.length) loadTemplates();
  if (page === 'commands' && !commands.length) loadCommands();
  if (page === 'config') loadConfig();
  if (page === 'stats') loadStats();
  if (page === 'ai') loadVersion();
}
pages.forEach(b => b.addEventListener('click', () => show(b.dataset.page)));

async function api(path, body) {
  const opts = body === undefined ? {} : { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) };
  const res = await fetch(path, opts);
  const text = await res.text();
  let data = null;
  try { data = JSON.parse(text); } catch { data = { error: text }; }
  return { ok: res.ok, status: res.status, data };
}

function setStatus(el, ok, msg) {
  el.textContent = msg;
  el.className = 'status ' + (ok ? 'ok' : 'err');
}

async function run(btn, statusEl, outEl, path, body, label) {
  btn.disabled = true;
  setStatus(statusEl, true, label + '…');
  outEl.hidden = true;
  const r = await api(path, body);
  btn.disabled = false;
  if (r.data && r.data.output !== undefined) {
    const ok = r.data.exitCode === 0;
    setStatus(statusEl, ok, ok ? 'Terminé' : 'Échec (code ' + r.data.exitCode + ')');
    outEl.textContent = r.data.output || '(aucune sortie)';
    toast(ok ? 'Génération terminée' : 'Échec de la génération', ok);
  } else if (r.data && r.data.error) {
    setStatus(statusEl, false, r.data.error);
    outEl.textContent = '';
  } else {
    setStatus(statusEl, r.ok, r.ok ? 'OK' : 'Erreur ' + r.status);
    outEl.textContent = JSON.stringify(r.data, null, 2);
  }
  outEl.hidden = false;
}

let templates = [];

async function loadVersion() {
  const r = await api('/api/version');
  if (r.ok) {
    $('ver').textContent = 'v' + r.data.version;
    $('ai-provider').textContent = '…';
    const cfg = await api('/api/config');
    $('ai-provider').textContent = (cfg.data && cfg.data.provider) || 'non configuré';
  }
  if (!$('new-output').dataset.cwd && r.ok) $('new-output').placeholder = 'Par défaut : ' + r.data.cwd + '/<nom>';
}

async function loadDoctor() {
  $('doctor-body').innerHTML = '<tr><td colspan="3" class="muted">Chargement…</td></tr>';
  const r = await api('/api/doctor');
  if (!r.ok || !r.data.tools) {
    $('doctor-body').innerHTML = '<tr><td colspan="3" class="err">Diagnostic indisponible</td></tr>';
    return;
  }
  $('doctor-meta').textContent = `v${r.data.version} · ${r.data.os} ${r.data.arch} · ${r.data.shell}`;
  $('doctor-body').innerHTML = '';
  for (const t of r.data.tools) {
    const tr = document.createElement('tr');
    const name = document.createElement('td'); name.textContent = t.name + (t.required ? '' : ' (optionnel)');
    const status = document.createElement('td');
    const tag = document.createElement('span');
    tag.className = 'tag ' + (t.found ? 'ok' : (t.required ? 'err' : ''));
    tag.textContent = t.found ? 'installé' : 'absent';
    status.appendChild(tag);
    const ver = document.createElement('td'); ver.className = 'muted'; ver.textContent = t.version || '—';
    tr.append(name, status, ver);
    $('doctor-body').appendChild(tr);
  }
}

async function loadTemplates() {
  const r = await api('/api/templates');
  templates = Array.isArray(r.data) ? r.data : [];
  fillTemplateSelect();
  renderTemplates();
}

let commands = [];
let selectedCommand = null;
const cmdValues = {};

async function loadCommands() {
  const r = await api('/api/commands');
  commands = Array.isArray(r.data) ? r.data : [];
  fillCommandSelect();
}

function fillCommandSelect() {
  const sel = $('cmd-select');
  const q = ($('cmd-filter').value || '').trim().toLowerCase();
  const visible = q ? commands.filter(c => (c.path + ' ' + c.description).toLowerCase().includes(q)) : commands;
  sel.innerHTML = '';
  for (const c of visible) {
    const opt = document.createElement('option');
    opt.value = c.path;
    opt.textContent = c.path + ' — ' + (c.description || '');
    sel.appendChild(opt);
  }
  if (visible.length) { sel.value = visible[0].path; renderCommandParams(visible[0].path); }
}
$('cmd-filter').addEventListener('input', fillCommandSelect);

function renderCommandParams(path) {
  const cmd = commands.find(c => c.path === path);
  selectedCommand = cmd || null;
  const box = $('cmd-params');
  box.innerHTML = '';
  if (!cmd) return;
  for (const p of (cmd.parameters || [])) {
    const wrap = document.createElement('div');
    const lab = document.createElement('label'); lab.textContent = p.token + (p.required ? ' *' : '');
    if (p.description) { const d = document.createElement('div'); d.className = 'muted'; d.textContent = p.description; d.style.fontSize = '11px'; d.style.marginBottom = '4px'; wrap.appendChild(d); }
    if (p.isFlag) {
      const cb = document.createElement('input'); cb.type = 'checkbox';
      cb.id = 'cp-' + p.token;
      cb.addEventListener('change', () => { cmdValues[p.token] = cb.checked; });
      const l = document.createElement('label'); l.style.display = 'flex'; l.style.gap = '6px'; l.style.alignItems = 'center';
      l.appendChild(cb); l.appendChild(document.createTextNode(p.token));
      wrap.appendChild(l);
    } else {
      const inp = document.createElement('input');
      inp.id = 'cp-' + p.token;
      inp.placeholder = p.description || '';
      inp.addEventListener('input', () => { cmdValues[p.token] = inp.value; });
      wrap.appendChild(lab); wrap.appendChild(inp);
    }
    box.appendChild(wrap);
  }
}
$('cmd-select').addEventListener('change', (e) => {
  // reset values
  for (const k in cmdValues) delete cmdValues[k];
  renderCommandParams(e.target.value);
});

$('cmd-run').addEventListener('click', async () => {
  if (!selectedCommand) return;
  const btn = $('cmd-run'); btn.disabled = true;
  $('cmd-status').textContent = 'Exécution…';
  $('cmd-status').className = 'status';
  $('cmd-out').hidden = true;
  // collect values
  const values = {};
  for (const p of (selectedCommand.parameters || [])) {
    const el = $('cp-' + p.token);
    if (!el) continue;
    if (p.isFlag) values[p.token] = el.checked;
    else if (el.value && el.value.trim()) values[p.token] = el.value.trim();
  }
  const r = await api('/api/run-command', { path: selectedCommand.path, values });
  btn.disabled = false;
  if (r.data && r.data.output !== undefined) {
    const ok = r.data.exitCode === 0;
    $('cmd-status').textContent = ok ? 'Terminé' : 'Échec (code ' + r.data.exitCode + ')';
    $('cmd-status').className = 'status ' + (ok ? 'ok' : 'err');
    $('cmd-out').textContent = r.data.output || '(aucune sortie)';
  } else if (r.data && r.data.error) {
    $('cmd-status').textContent = r.data.error;
    $('cmd-status').className = 'status err';
  }
  $('cmd-out').hidden = false;
  toast(r.data && r.data.exitCode === 0 ? 'Commande terminée' : 'Échec', !!(r.data && r.data.exitCode === 0));
});

$('cmd-reset').addEventListener('click', () => {
  for (const k in cmdValues) delete cmdValues[k];
  document.querySelectorAll('#cmd-params input').forEach(i => { if (i.type === 'checkbox') i.checked = false; else i.value = ''; });
});

function templateValue(t) { return t.template + '::' + (t.language || ''); }

function fillTemplateSelect() {
  const sel = $('new-tpl');
  sel.innerHTML = '';
  for (const t of templates) {
    const opt = document.createElement('option');
    opt.value = templateValue(t);
    opt.textContent = t.template + (t.language ? ' · ' + t.language : '') + (t.available ? '' : ' (outil absent)');
    sel.appendChild(opt);
  }
  updateConfigVisibility();
}

function updateConfigVisibility() {
  const [tpl] = $('new-tpl').value.split('::');
  $('new-config-group').style.display = (tpl === 'webapi' || tpl === 'blazor') ? '' : 'none';
}
$('new-tpl').addEventListener('change', updateConfigVisibility);

function renderTemplates() {
  const q = $('tpl-filter').value.trim().toLowerCase();
  const body = $('tpl-body');
  body.innerHTML = '';
  for (const t of templates) {
    const hay = (t.template + ' ' + (t.language || '') + ' ' + (t.description || '')).toLowerCase();
    if (q && !hay.includes(q)) continue;
    const tr = document.createElement('tr');
    tr.style.cursor = 'pointer';
    tr.title = 'Utiliser ce template';
    tr.addEventListener('click', () => { $('new-tpl').value = templateValue(t); updateConfigVisibility(); show('new'); });
    const cells = [t.template, t.language || '—', t.description || '', t.available ? '' : 'absent'];
    for (const c of cells) { const td = document.createElement('td'); td.textContent = c; tr.appendChild(td); }
    body.appendChild(tr);
  }
  if (!body.children.length) body.innerHTML = '<tr><td colspan="4" class="muted">Aucun résultat</td></tr>';
}
$('tpl-filter').addEventListener('input', renderTemplates);

async function loadConfig() {
  const r = await api('/api/config');
  const box = $('config-list');
  box.innerHTML = '';
  if (!r.ok) { box.textContent = 'Configuration indisponible'; return; }
  for (const [key, value] of Object.entries(r.data)) {
    const row = document.createElement('div');
    row.className = 'grid';
    row.style.marginBottom = '14px';
    const lab = document.createElement('div');
    lab.className = 'full';
    const l = document.createElement('label'); l.textContent = key; lab.appendChild(l);
    let input = document.createElement('input');
    input.value = value || '';
    input.id = 'cfg-' + key;
    if (key === 'provider') {
      const s = document.createElement('select');
      ['openai', 'claude', 'gemini', 'grok'].forEach(p => { const o = document.createElement('option'); o.value = p; o.textContent = p; s.appendChild(o); });
      s.value = value || 'openai'; s.id = input.id; input = s;
    }
    const save = document.createElement('button');
    save.className = 'ghost';
    save.textContent = 'Enregistrer';
    save.addEventListener('click', async () => {
      const el = $('cfg-' + key);
      const res = await api('/api/config', { key, value: el.value });
      save.textContent = res.ok ? 'Enregistré ✓' : (res.data.error || 'Erreur');
      setTimeout(() => save.textContent = 'Enregistrer', 1500);
    });
    row.append(lab, input, save);
    box.appendChild(row);
  }
}

async function loadStats() {
  const r = await api('/api/stats');
  $('stats-out').textContent = r.ok ? JSON.stringify(r.data, null, 2) : 'Statistiques indisponibles';
}
$('refresh-stats').addEventListener('click', loadStats);
$('refresh-doctor').addEventListener('click', loadDoctor);

$('new-go').addEventListener('click', () => {
  const [template, language] = $('new-tpl').value.split('::');
  run($('new-go'), $('new-status'), $('new-out'), '/api/new', {
    template, language,
    name: $('new-name').value.trim(),
    output: $('new-output').value.trim(),
    git: $('new-git').checked,
    docker: $('new-docker').checked,
    ci: $('new-ci').checked,
    auth: $('new-auth').value, db: $('new-db').value, arch: $('new-arch').value
  }, 'Génération');
});

$('stack-go').addEventListener('click', () => run($('stack-go'), $('stack-status'), $('stack-out'), '/api/stack', {
  name: $('stack-name').value.trim(),
  output: $('stack-output').value.trim(),
  frontend: $('stack-fe').value,
  backend: $('stack-be').value,
  db: $('stack-db').value
}, 'Génération de la stack'));

$('add-go').addEventListener('click', () => run($('add-go'), $('add-status'), $('add-out'), '/api/add', {
  type: $('add-type').value.trim(),
  name: $('add-name').value.trim(),
  dir: $('add-dir').value.trim()
}, 'Ajout'));

$('ai-go').addEventListener('click', () => run($('ai-go'), $('ai-status'), $('ai-out'), '/api/ai', {
  prompt: $('ai-prompt').value.trim()
}, 'Réflexion'));

function toast(msg, ok = true) {
  const t = $('toast');
  t.textContent = msg;
  t.className = 'toast show ' + (ok ? 'ok' : 'err');
  clearTimeout(t._timer);
  t._timer = setTimeout(() => t.classList.remove('show'), 3200);
}

// Override run to also toast on completion
const _run = run;
// (run already updates status; toast on success/error is handled below via wrapping fetch? keep simple)

loadVersion();
loadDoctor();
loadTemplates();
loadCommands();
</script>
<div class="toast" id="toast"></div>
</body>
</html>
""";
}
