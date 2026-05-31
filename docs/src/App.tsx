import { useState } from 'react'
import { commands, type CommandCategory, type CommandEntry } from './data/commands'
import './App.css'

function copyText(text: string) {
  navigator.clipboard.writeText(text.replace(/<br\s*\/?>/g, '\n'))
}

function Nav() {
  return (
    <nav>
      <div className="nav-inner">
        <a href="#" className="logo">
          <img src="/logo-minimal.svg" alt="" />
          Scaffolder
        </a>
        <ul>
          <li><a href="#features">Fonctionnalités</a></li>
          <li><a href="#install">Installation</a></li>
          <li><a href="#commands">Commandes</a></li>
          <li><a href="#examples">Exemples</a></li>
          <li><a href="#bug">Signaler un bug</a></li>
        </ul>
      </div>
    </nav>
  )
}

function Hero() {
  return (
    <header>
      <div className="hero">
        <img src="/logo.svg" alt="Scaffolder Logo" className="hero-logo" />
        <h1>Scaffolder</h1>
        <p className="subtitle">CLI universel pour générer des projets dans tous les langages</p>
        <div className="hero-actions">
          <a href="#install" className="btn btn-primary">Télécharger</a>
          <a href="#commands" className="btn btn-secondary">Documentation</a>
        </div>
        <p className="version">v2.0.0 · 40 commandes · 12 MB AOT · Multi-plateforme</p>
      </div>
    </header>
  )
}

function Features() {
  const features = [
    { icon: '⚡', title: '40 commandes', desc: 'Init, build, test, docker, github, lint, format, registry, migration, batch, et plus.' },
    { icon: '🌐', title: 'Multi-langage', desc: '.NET, Node.js, Rust, Go, Python, Flutter, Laravel, Symfony, Rails, et 20+ autres.' },
    { icon: '🔒', title: 'Fullstack', desc: 'Génère un projet complet frontend + backend + base de données en une commande.' },
    { icon: '🧠', title: 'IA intégrée', desc: 'Suggestions, explications et correction d\'erreurs via OpenAI, Claude, Gemini ou Grok.' },
    { icon: '🛠', title: 'AOT natif', desc: 'Compilé en binaire natif. Pas de runtime nécessaire. Démarrage instantané.' },
    { icon: '📄', title: 'Extensible', desc: 'Plugins, templates custom, registry communautaire, workspace VS Code.' },
  ]
  return (
    <section id="features">
      <h2>Pourquoi Scaffolder ?</h2>
      <div className="grid">
        {features.map((f, i) => (
          <div className="card" key={i}>
            <div className="card-icon">{f.icon}</div>
            <h3>{f.title}</h3>
            <p>{f.desc}</p>
          </div>
        ))}
      </div>
    </section>
  )
}

const platforms = {
  linux: { rids: ['linux-x64', 'linux-musl-x64', 'linux-arm64'], cmd: `curl -L -o scaffold.tar.gz https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v2.0.0/scaffold-linux-x64.tar.gz\ntar -xzf scaffold.tar.gz\nsudo mv scaffold /usr/local/bin/\nscaffold --help` },
  macos: { rids: ['osx-x64', 'osx-arm64'], cmd: `curl -L -o scaffold.tar.gz https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v2.0.0/scaffold-osx-x64.tar.gz\ntar -xzf scaffold.tar.gz\nsudo mv scaffold /usr/local/bin/\nscaffold --help` },
  windows: { rids: ['win-x64', 'win-x86', 'win-arm64'], cmd: `# PowerShell\ncurl -L -o scaffold.zip https://github.com/akaletekoffilevis/Scaffolder-CLI/releases/download/v2.0.0/scaffold-win-x64.tar.gz\ntar -xzf scaffold.zip\n./scaffold.exe --help` },
}

function InstallSection() {
  const [activeOs, setActiveOs] = useState<'linux' | 'macos' | 'windows'>('linux')
  const current = platforms[activeOs]

  return (
    <section id="install">
      <h2>Installation</h2>
      <div className="tabs">
        {(['linux', 'macos', 'windows'] as const).map(os => (
          <button key={os} className={`tab${activeOs === os ? ' active' : ''}`} onClick={() => setActiveOs(os)}>
            {os === 'linux' ? 'Linux' : os === 'macos' ? 'macOS' : 'Windows'}
          </button>
        ))}
      </div>
      <div className="tab-content active">
        <div className="install-cmd">
          <code>{current.cmd}</code>
          <button className="btn-copy" onClick={() => copyText(current.cmd)}>Copier</button>
        </div>
        <div className="platform-grid">
          {current.rids.map(rid => (
            <span key={rid} className="platform-btn">{rid}</span>
          ))}
        </div>
      </div>
      <div className="install-alt">
        <h3>Homebrew (macOS / Linux)</h3>
        <div className="install-cmd">
          <code>brew tap akaletekoffilevis/scaffolder\nbrew install scaffold</code>
          <button className="btn-copy" onClick={() => copyText('brew tap akaletekoffilevis/scaffolder\nbrew install scaffold')}>Copier</button>
        </div>
      </div>
    </section>
  )
}

function CommandAccordion({ cmd }: { cmd: CommandEntry }) {
  const [open, setOpen] = useState(false)

  return (
    <div className={`cmd-detail${open ? ' open' : ''}`} data-search={cmd.search}>
      <div className="cmd-detail-header" onClick={() => setOpen(!open)}>
        <code className="cmd-syntax">{cmd.syntax}</code>
        <span className="cmd-badge">{cmd.badge}</span>
        <span className="cmd-arrow">▼</span>
      </div>
      <div className="cmd-detail-body">
        <p>{cmd.description}</p>
        {cmd.options.length > 0 && (
          <>
            <h4>Arguments / Options</h4>
            <table>
              <tbody>
                {cmd.options.map((opt, i) => (
                  <tr key={i}><td><code>{opt.flag}</code></td><td>{opt.desc}</td></tr>
                ))}
              </tbody>
            </table>
          </>
        )}
        {cmd.examples.length > 0 && (
          <>
            <h4>Exemples</h4>
            {cmd.examples.map((ex, i) => (
              <div className="install-cmd" key={i}>
                <code>{ex}</code>
                <button className="btn-copy" onClick={() => copyText(ex)}>Copier</button>
              </div>
            ))}
          </>
        )}
      </div>
    </div>
  )
}

function CommandSection({ cat }: { cat: CommandCategory }) {
  return (
    <>
      <h3 className="cat-title">{cat.category}</h3>
      {cat.commands.map((cmd, i) => (
        <CommandAccordion key={i} cmd={cmd} />
      ))}
    </>
  )
}

function CommandRef() {
  const [query, setQuery] = useState('')

  const display = !query.trim() ? commands : commands
    .map(cat => ({ ...cat, commands: cat.commands.filter(c =>
      c.search.toLowerCase().includes(query.toLowerCase()) ||
      c.syntax.toLowerCase().includes(query.toLowerCase()) ||
      c.description.toLowerCase().includes(query.toLowerCase()) ||
      c.badge.toLowerCase().includes(query.toLowerCase())
    )}))
    .filter(cat => cat.commands.length > 0)

  return (
    <section id="commands">
      <h2>Référence complète des 40 commandes</h2>
      <div className="search-box">
        <input
          type="text"
          placeholder="Rechercher une commande, une option, un mot-clé..."
          value={query}
          onChange={e => setQuery(e.target.value)}
        />
      </div>
      <div id="cmdRef">
        {display.map((cat, i) => (
          <CommandSection key={i} cat={cat} />
        ))}
      </div>
    </section>
  )
}

function Examples() {
  const exs = [
    { label: 'Mode interactif', cmd: 'scaffold new' },
    { label: 'Projet .NET Web API', cmd: 'scaffold new --template=webapi --name=mon-api' },
    { label: 'Fullstack vite+dotnet', cmd: 'scaffold new webapi+react --name=full-app' },
    { label: 'Déploiement Docker', cmd: 'scaffold deploy --target=docker' },
    { label: 'Mise à jour', cmd: 'scaffold upgrade' },
  ]
  return (
    <section id="examples">
      <h2>Exemples rapides</h2>
      <div className="grid">
        {exs.map((ex, i) => (
          <div className="card" key={i}>
            <h3>{ex.label}</h3>
            <div className="install-cmd" style={{ marginBottom: 0, marginTop: 12 }}>
              <code>{ex.cmd}</code>
              <button className="btn-copy" onClick={() => copyText(ex.cmd)}>Copier</button>
            </div>
          </div>
        ))}
      </div>
    </section>
  )
}

function BugSection() {
  return (
    <section id="bug">
      <h2>Signaler un bug</h2>
      <p style={{ textAlign: 'center', color: 'var(--text2)', marginBottom: 24 }}>
        Une anomalie ? Utilisez la commande intégrée ou ouvrez une issue GitHub.
      </p>
      <div style={{ display: 'flex', gap: 12, justifyContent: 'center', flexWrap: 'wrap' }}>
        <span className="btn btn-primary" onClick={() => copyText('scaffold bug "description du probleme"')}>
          📋 Copier la commande
        </span>
        <a
          href="https://github.com/akaletekoffilevis/Scaffolder-CLI/issues/new?labels=bug&template=bug_report.md"
          className="btn btn-secondary"
          target="_blank"
          rel="noopener noreferrer"
        >
          📝 Ouvrir une issue GitHub
        </a>
      </div>
    </section>
  )
}

function Footer() {
  return (
    <footer>
      <p>Scaffolder — CLI de génération de projets — <a href="https://github.com/akaletekoffilevis/Scaffolder-CLI" target="_blank" rel="noopener noreferrer">GitHub</a></p>
    </footer>
  )
}

export default function App() {
  return (
    <>
      <Nav />
      <Hero />
      <Features />
      <InstallSection />
      <CommandRef />
      <Examples />
      <BugSection />
      <Footer />
    </>
  )
}
