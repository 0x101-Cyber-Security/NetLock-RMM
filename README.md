<div align="center">

<img src="NetLock-RMM-Web-Console/wwwroot/media/images/NetLock-RMM-Logo-Transparent.svg" alt="NetLock RMM Logo" width="220" />

# NetLock RMM

### Open-Source Remote Monitoring & Management for MSPs and IT Teams

[![Website](https://img.shields.io/badge/Website-netlockrmm.com-blue?style=for-the-badge)](https://netlockrmm.com/)
[![Documentation](https://img.shields.io/badge/Docs-netlockrmm.com%2Fdocs-blue?style=for-the-badge)](https://netlockrmm.com/docs)
[![Live Demo](https://img.shields.io/badge/Live%20Demo-Try%20it%20now-purple?style=for-the-badge)](https://members.netlockrmm.com/demo)
[![Pricing](https://img.shields.io/badge/Pricing-Community%20%26%20Pro-orange?style=for-the-badge)](https://netlockrmm.com/pricing/)
[![Discord](https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/HqUpZgtX4U)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-black?style=for-the-badge&logo=github)](https://github.com/0x101-Cyber-Security/NetLock-RMM)
[![Roadmap](https://img.shields.io/badge/Roadmap-View%20Progress-green?style=for-the-badge)](https://netlockrmm.com/roadmap)
[![Security](https://img.shields.io/badge/Security-Policy-critical?style=for-the-badge)](https://netlockrmm.com/products/security)
[![License](https://img.shields.io/github/license/0x101-Cyber-Security/NetLock-RMM?style=for-the-badge)](LICENSE)

</div>

---

> **⭐ If NetLock RMM is useful to you, please star this repo — it helps others discover the project!**

## What is NetLock RMM?

**NetLock RMM** is a fully open-source Remote Monitoring & Management platform built for **Managed Service Providers** and **internal IT teams**, with a strong focus on **cybersecurity**. Lightweight cross-platform agents stream telemetry to a self-hostable server so you can monitor, manage and remotely support your entire fleet from a single web console.

Whether you run NetLock RMM in the cloud, on-premises, in Docker / Kubernetes, or in fully isolated / air-gapped environments — and whether you manage ten endpoints or ten thousand — NetLock RMM gives you the visibility, automation and remote tooling that modern IT operations demand, without per-seat license fees.

Built on **C# / .NET, Blazor, ASP.NET Core and SignalR** for performance, scalability and a native cross-platform agent experience.

<div align="center">

🎬 **[Watch the Web Console Demo](https://netlockrmm.com/assets/hero-demo-MVb7uUdN.webm)**

</div>

---

## 🧭 Our Story & Philosophy

NetLock RMM is **not** a "vibe-coded" weekend project and it is definitely not AI slop.

- **2021** — The very first NetLock RMM prototype was built as a proof of concept.
- **Early 2022** — Full-scale development of NetLock RMM officially started.
- **End of 2024** — The current open-source version of NetLock RMM was publicly released.

Years of real engineering, real architectural decisions and real production usage went into this platform long before the current AI hype cycle. We are aware that AI will permanently change how software is built, and we do adopt it where it sensibly accelerates our work — but **every security-relevant and architectural decision in NetLock RMM is designed, reviewed and implemented by humans, by hand, without AI**. That is a hard rule for us, and it is not negotiable.

### 🔍 Why Open Source?

We made NetLock RMM open source for one simple reason: **maximum transparency and trust**. An RMM agent runs with high privileges on every endpoint it touches — you should never have to take a vendor's word for what that agent does. With NetLock RMM you can read the source, audit the network traffic, verify that there is no hidden telemetry, and convince yourself (and your customers, and your auditors) that the platform behaves exactly the way it claims to.

Open source is how we earn — and keep — your trust.

---

## ✨ Features at a Glance

| Category | What You Can Do |
|---|---|
| 🖥️ **Cross-Platform Agents** | One-click installer for **Windows** (7 → 11 / Server 2012 → 2025), **Linux** (Ubuntu, Debian, RHEL, CentOS Stream, Fedora) and **macOS** (Ventura, Sonoma, Sequoia). Both **x64 and ARM64** are supported. |
| 🧩 **Multi-Tenancy** | Full tenant, location and group management — perfect for MSPs juggling many customers from one console. |
| 👥 **Users, Roles & SSO** | Granular role-based permissions, **two-factor authentication (TOTP)** and **Single Sign-On** via Keycloak, Entra ID, Auth0 or Okta. |
| 🛡️ **Flexible & Scalable Architecture** | Run everything on a single box, or split server roles across multiple machines for HA. **Fallback servers** per role and full **reverse-proxy** support. |
| 🚀 **Streamlined Setup** | Server and Web Console ship as a standalone **Kestrel**-based binary — no Apache, no IIS, no fuss. Run it on bare metal, in Docker or in Kubernetes. |
| 🛠️ **Powerful Remote Tools** | Real-time **remote shell** (PowerShell / Bash / Zsh), **file browser**, **task manager**, **service manager** and **remote registry** at your fingertips. |
| 🖱️ **Remote Screen Control** | TeamViewer-grade remote control for **Windows & Linux**. Session & display switching, unattended + attended access, user chat, **Ctrl+Alt+Del**, **session recording**, keystroke injection — and a **Relay App** for end-to-end encrypted tunnels. |
| 🎧 **Custom User Tray Icon & Chat** | Deploy a white-label tray icon to end users with custom logo, custom interface texts, action buttons (open URL / run command) and a built-in chat interface. |
| 🧾 **Inventory & Hardware Monitoring** | Software & hardware inventory, CPU, RAM, drives, network adapters, drivers, services, scheduled tasks, logon history and more. |
| 🛡️ **Antivirus & Security** | Manage **Microsoft Defender Antivirus**, monitor firewall status and enforce security baselines through policies. |
| ⚙️ **Automation Engine** | **Jobs** for scheduled PowerShell, Bash and Zsh scripts. **Sensors** for CPU, RAM, disk, ping, Windows Event Logs, services and **custom RegEx-based script sensors**. |
| 📜 **Policy Management** | Auto-assign policies by tenant, location, group, domain, IP or device name. Define and enforce antivirus, notification, sensor and job policies. |
| 📊 **Dashboards & Event Viewer** | Centralised dashboards with statistics, unread events and a powerful **event browser** with severity-based filtering. |
| 🔔 **Event Notifications** | Get alerted via **Email**, **Microsoft Teams**, **ntfy.sh**, **Telegram** or **custom webhooks** with templated variables. |
| 📁 **Integrated File Server** | Host your favorite scripts and tools directly inside NetLock RMM and reference them from your jobs. |
| 🎨 **White-Label Branding** | Customise the console title and logo to match your brand. |
| 🔐 **Security by Design** | Special agent handshake so only authorised agents talk to your server, role-based server architecture, full data sovereignty through self-hosting and **end-to-end encryption** for relay connections. **No hidden telemetry — verify it yourself in the source.** |

> Looking for the full feature list? Head over to **[netlockrmm.com/docs/features](https://netlockrmm.com/docs/features)**.

---

## 🏗️ Architecture

NetLock RMM is built on a modular role-based architecture so you can scale individual components independently.

| Component | Technology |
|-----------|------------|
| Web Console | Blazor Server + MudBlazor |
| Server | ASP.NET Core (Kestrel) + SignalR |
| Agents | C# / .NET — Windows, Linux, macOS (x64 & ARM64) |
| Realtime Transport | SignalR over WebSockets |
| Database | MySQL |
| Relay | Standalone end-to-end encrypted tunnel app |

```mermaid
flowchart LR
    A[Browser / Admin UI] -- HTTPS --> B[Reverse Proxy]
    B -- HTTP --> C[NetLock Web Console - Blazor]
    B -- HTTP --> S[NetLock Server Roles]
    C -- TCP --> D[(MySQL)]
    S -- TCP --> D
    E[Agents - Windows / Linux / macOS] -- HTTPS / SignalR --> S
    R[Relay App] <-- E2E Encrypted --> S
```

---

## 🎟️ Editions & Members Portal

NetLock RMM is built for **enterprises, MSPs, MSP startups and any organisation** that takes a secure, professional platform seriously. To serve everyone — from a homelab tinkerer to a fully managed service provider running thousands of endpoints — we offer NetLock RMM in three editions, all managed through our **[Members Portal](https://members.netlockrmm.com)**:

| Edition | Hosting | Who it's for | Device limit |
|---|---|---|---|
| 🆓 **Community Edition** | Self-hosted | Homelabs, evaluators, small teams, anyone who wants to try or run NetLock RMM for free. The full open-source platform, all core features, no per-seat fees. Community support via [Discord](https://discord.gg/HqUpZgtX4U) and [GitHub Issues](https://github.com/0x101-Cyber-Security/NetLock-RMM/issues). | **Up to 25 devices** |
| 💼 **Self-Hosted Paid** | Self-hosted | Enterprises, MSPs and businesses that want to run NetLock RMM in their own infrastructure but need an SLA-backed professional partner. Includes professional support directly from the maintainers and prioritised handling. | **Unlimited** — no device cap |
| ☁️ **Cloud-Hosted Edition** | Hosted by us | Organisations that want NetLock RMM up and running **without managing any infrastructure**. We handle provisioning, updates, backups, scaling and uptime — you focus on managing your fleet. | **Per-package limit** — pick the tier that fits your fleet |

> 👉 **See the full plan comparison and pricing at [netlockrmm.com/pricing](https://netlockrmm.com/pricing/)**

### Why a Members Portal?

The **[Members Portal](https://members.netlockrmm.com)** is the professional interface between the NetLock RMM community, the businesses that depend on the platform, and us as the maintainers. It's where you manage your subscription and licence, access professional support, run the live demo, and interact with the team in a structured way that scales beyond a Discord channel — **without ever locking the open-source platform behind a paywall**.

The platform stays open source. The portal exists so that the people who need a professional vendor relationship can have one.

> ⚡ **The Cloud-Hosted Edition is by far the easiest way to deploy and maintain NetLock RMM.** Provisioning, updates, backups and infrastructure are all handled for you — no Docker, no reverse proxy, no maintenance windows. Just sign up and start enrolling agents. See **[netlockrmm.com/docs/install](https://netlockrmm.com/docs/install)** for the full installation guide and all available deployment paths.

---

## 🚀 Get Started

### Option 1 — Try the Live Demo

Want to play with NetLock RMM before installing anything? Spin up an instant demo session:

👉 **[members.netlockrmm.com/demo](https://members.netlockrmm.com/demo)**

### Option 2 — Cloud-Hosted Edition *(easiest)*

The fastest, most reliable way to get a production-ready NetLock RMM up and running. We host and maintain the platform for you — provisioning, updates, backups, scaling and uptime are all taken care of, so you can focus on managing your fleet instead of the tooling behind it. Pick the package that matches your device count and you're live in minutes.

👉 **[See plans at netlockrmm.com/pricing](https://netlockrmm.com/pricing/)**

### Option 3 — Self-Host It

Prefer to run everything yourself? NetLock RMM can be self-hosted via Docker, on bare metal or in fully air-gapped environments. The full step-by-step installation guide covers every supported deployment path:

👉 **[Installation Guide — netlockrmm.com/docs/install](https://netlockrmm.com/docs/install)**

Common deployment paths:

- **Docker / Docker Compose** — quickest way to self-host
- **Air-gapped / offline** — fully supported for isolated environments

After installation, open your console URL in the browser and follow the first-time admin setup.

---

## 📚 Documentation

Full documentation — including installation guides, agent management, policies, sensors, jobs, integrations and API reference — lives at:

👉 **[netlockrmm.com/docs](https://netlockrmm.com/docs)**

---

## 💬 Community & Support

- **Discord:** [discord.gg/HqUpZgtX4U](https://discord.gg/HqUpZgtX4U) — fastest way to get help, chat with the community and post to the **#wishlist**
- **Website:** [netlockrmm.com](https://netlockrmm.com/)
- **Bug Reports & Feature Requests:** [GitHub Issues](https://github.com/0x101-Cyber-Security/NetLock-RMM/issues)
- **Security:** see [SECURITY.md](SECURITY.md) for our responsible disclosure policy

---

## 🗺️ Roadmap

We publish our roadmap publicly so you can follow what's next:

👉 **[netlockrmm.com/roadmap](https://netlockrmm.com/roadmap)**

---

## 💡 Feature Requests & Contributions

We **love** hearing from the people who actually use NetLock RMM — your feedback shapes the roadmap. There are two great ways to suggest a new feature, report a bug or share an idea:

- 🐛 **GitHub Issues** — [open an issue](https://github.com/0x101-Cyber-Security/NetLock-RMM/issues) for bug reports and feature requests
- 💬 **Discord Wishlist** — drop your idea in the **#wishlist** channel on our [Discord server](https://discord.gg/HqUpZgtX4U)

### A Note on Code Contributions

> **We do not accept external code contributions (Pull Requests) at this time.**

This is a deliberate, considered decision — not laziness and not gatekeeping:

- NetLock RMM follows a **strict internal development plan** and tightly controlled architecture. Drive-by PRs make that plan very hard to keep coherent.
- Every line of code that runs on customer endpoints with elevated privileges has to meet our security and quality bar. Reviewing the current flood of AI-generated, low-context "AI slop" PRs would consume more time than it saves.
- All **security-relevant and architectural code** is written and reviewed by us personally, by hand, without AI — and we want to keep that promise honest.

If you have an idea, please open an issue or post in the Discord wishlist. We read every single one, and many features in NetLock RMM today started exactly that way.

### Repository Layout

| Folder | What lives there |
|---|---|
| [NetLock-RMM-Web-Console/](NetLock-RMM-Web-Console/) | Blazor Server admin UI (MudBlazor) |
| [NetLock-RMM-Server/](NetLock-RMM-Server/) | ASP.NET Core / Kestrel server roles |
| [NetLock RMM Agent Comm/](NetLock%20RMM%20Agent%20Comm/) | Cross-platform agent — communication core |
| [NetLock RMM Agent Health/](NetLock%20RMM%20Agent%20Health/) | Agent health watchdog |
| [NetLock RMM Agent Remote/](NetLock%20RMM%20Agent%20Remote/) | Remote control / remote screen agent |
| [NetLock RMM Agent Installer/](NetLock%20RMM%20Agent%20Installer/) | Agent installer (CLI) |
| [NetLock RMM Agent Installer GUI/](NetLock%20RMM%20Agent%20Installer%20GUI/) | Agent installer (GUI) |
| [NetLock RMM Tray Icon/](NetLock%20RMM%20Tray%20Icon/) | White-label end-user tray icon & chat |
| [NetLock-RMM-User-Process/](NetLock-RMM-User-Process/) | User-context companion process |
| [NetLock RMM Relay App/](NetLock%20RMM%20Relay%20App/) | End-to-end encrypted relay tunnel app |
| [Community Events Games/](Community%20Events%20Games/) | Community challenges & events |

---

## 📜 License

See [LICENSE](LICENSE) for details.

---

<div align="center">

**Built with ❤️ for the open-source IT community.**

If NetLock RMM helps you secure or run your infrastructure, please consider **starring the repo** — it really does help others find the project.

[![Website](https://img.shields.io/badge/Website-netlockrmm.com-blue?style=for-the-badge)](https://netlockrmm.com/)
[![Discord](https://img.shields.io/badge/Discord-Join%20Server-5865F2?style=for-the-badge&logo=discord&logoColor=white)](https://discord.gg/HqUpZgtX4U)
[![GitHub](https://img.shields.io/badge/GitHub-Repository-black?style=for-the-badge&logo=github)](https://github.com/0x101-Cyber-Security/NetLock-RMM)
[![Documentation](https://img.shields.io/badge/Docs-netlockrmm.com%2Fdocs-blue?style=for-the-badge)](https://netlockrmm.com/docs)

**Happy monitoring! 🥳**

</div>
