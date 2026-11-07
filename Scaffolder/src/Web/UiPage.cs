namespace Scaffolder.Web;

/// <summary>Page unique de l'interface web locale (HTML, CSS et JS embarqués, aucune dépendance externe).</summary>
public static class UiPage
{
    public const string Html = """
<!DOCTYPE html>
<html lang="fr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Scaffolder — interface web</title>
<style>
  :root { --bg:#0f1117; --panel:#171a23; --line:#262b38; --text:#e6e8ee; --muted:#8b93a7; --accent:#7c9cff; --ok:#3ecf8e; --err:#ff6b6b; }
  * { box-sizing: border-box; }
  body { margin:0; font:15px/1.5 system-ui,-apple-system,Segoe UI,Roboto,sans-serif; background:var(--bg); color:var(--text); display:flex; min-height:100vh; }
  nav { width:220px; background:var(--panel); border-right:1px solid var(--line); padding:20px 12px; flex-shrink:0; }
  nav h1 { font-size:18px; margin:0 8px 2px; }
  nav .ver { color:var(--muted); font-size:12px; margin:0 8px 18px; }
  nav button { display:block; width:100%; text-align:left; background:none; border:0; color:var(--muted); padding:9px 10px; border-radius:8px; cursor:pointer; font:inherit; }
  nav button:hover { color:var(--text); background:#1d2130; }
  nav button.active { color:var(--text); background:#23283a; }
  main { flex:1; padding:28px 36px; max-width:980px; }
  section { display:none; } section.active { display:block; }
  h2 { margin:0 0 6px; font-size:22px; } p.lead { color:var(--muted); margin:0 0 22px; }
  .card { background:var(--panel); border:1px solid var(--line); border-radius:12px; padding:20px; margin-bottom:18px; }
  .grid { display:grid; grid-template-columns:1fr 1fr; gap:14px 18px; }
  .grid .full { grid-column:1 / -1; }
  label { display:block; font-size:13px; color:var(--muted); margin-bottom:5px; }
  input, select, textarea { width:100%; background:#0c0e14; color:var(--text); border:1px solid var(--line); border-radius:8px; padding:9px 10px; font:inherit; }
  textarea { min-height:90px; resize:vertical; }
  .checks { display:flex; gap:18px; flex-wrap:wrap; align-items:center; }
  .checks label { color:var(--text); display:flex; gap:6px; align-items:center; margin:0; }
  .checks input { width:auto; }
  .actions { margin-top:16px; display:flex; gap:10px; align-items:center; }
  button.primary { background:var(--accent); color:#0b0d12; border:0; padding:10px 18px; border-radius:8px; font-weight:600; cursor:pointer; }
  button.ghost { background:none; border:1px solid var(--line); color:var(--text); padding:9px 14px; border-radius:8px; cursor:pointer; font:inherit; }
  button:disabled { opacity:.5; cursor:wait; }
  pre { background:#0c0e14; border:1px solid var(--line); border-radius:8px; padding:14px; white-space:pre-wrap; word-break:break-word; max-height:360px; overflow:auto; font:13px/1.45 ui-monospace,Consolas,monospace; margin:14px 0 0; }
  .status { font-weight:600; } .ok { color:var(--ok); } .err { color:var(--err); }
  table { width:100%; border-collapse:collapse; } th,td { text-align:left; padding:8px 10px; border-bottom:1px solid var(--line); font-size:14px; }
  th { color:var(--muted); font-weight:500; }
  .tag { font-size:12px; padding:2px 8px; border-radius:99px; border:1px solid var(--line); color:var(--muted); }
  .tag.ok { color:var(--ok); border-color:#25604a; } .tag.err { color:var(--err); border-color:#6b2c2c; }
  .muted { color:var(--muted); font-size:13px; }
  @media (max-width:760px) { body { flex-direction:column; } nav { width:auto; } .grid { grid-template-columns:1fr; } }
</style>
</head>
<body>
<nav>
  <h1>Scaffolder</h1>
  <div class="ver" id="ver">…</div>
  <button data-page="dashboard" class="active">Tableau de bord</button>
  <button data-page="new">Nouveau projet</button>
  <button data-page="stack">Stack fullstack</button>
  <button data-page="add">Ajouter un composant</button>
  <button data-page="templates">Templates</button>
  <button data-page="config">Configuration</button>
  <button data-page="ai">Assistant IA</button>
  <button data-page="stats">Statistiques</button>
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

function show(page) {
  pages.forEach(b => b.classList.toggle('active', b.dataset.page === page));
  document.querySelectorAll('section').forEach(s => s.classList.toggle('active', s.id === page));
  if (page === 'dashboard') loadDoctor();
  if (page === 'templates' && !templates.length) loadTemplates();
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

loadVersion();
loadDoctor();
loadTemplates();
</script>
</body>
</html>
""";
}
