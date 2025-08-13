# SCNCORE-PLUS

SCNCore Plus is an advanced Remote Monitoring & Management (RMM) software built for Managed Service Providers, with a future focus on cybersecurity. SCNCore Plus is primarily written in C#, Blazor, ASP.NET Core, and SignalR.

SCNCore Plus can be deployed in various environments, including cloud setups or isolated/offline configurations. It supports multiple operating systems, including Windows and Linux, and can also run within Docker & Kubernetes, providing flexibility for different deployment preferences.

![Web Console Preview](https://scncore.com/assets/images/web-console-animation.webp)

## [Website](https://scncore.com/)
## [Documentation](https://docs.scncore.com/en/home)
## [Live Demo](https://docs.scncore.com/en/home) (top info)
## [Supported OS & Features](https://docs.scncore.com/en/supported-os)
## [Roadmap](https://docs.scncore.com/en/roadmap)
## [Discord](https://discord.gg/scncore)

## **SCNCore Plus – Feature Overview**

### 🚀 **Streamlined Server Setup**
- The **Web Console** and **server software** are delivered as a standalone server built on **Kestrel**, eliminating the need for complex configurations with traditional servers like Apache.
- **SSL Made Simple**: Love Let's Encrypt? Our integration with **LettuceEncrypt** makes it effortless to secure your connection with SSL certificates.
- **Modular Server Roles**: Run all components on a single machine or distribute them across multiple roles for scalability.
- **Fallback Servers**: Define backup servers for each role to ensure high availability.
- **Reverse Proxy Support**: Reverse proxies are fully supported.

### 🖥️ **Cross-Platform Agent Support**
- Compatible with **Windows**, **Linux**, and **macOS** (x64 & ARM64).
- **One-click installer** available for all platforms.

### 🏢 **Multi-Tenancy & Access Control**
- Full support for **multi-tenancy**, including location and group management.
- **User & Permission Management** with role-based access control and two-factor authentication.

### 🛠️ **Remote Management Features**
- Real-time **remote shell**, **file browser**, **task manager**, and **service manager**.
- **Remote screen control** (windows) and support that's as intuitive as TeamViewer.
  - Full support for session switching and display switching
  - Unattended access
  - Ctrl + Alt + Del support for elevated access
  - Built-in session recording
  - Send input as keystrokes, ideal for automating password entry
  - Mobile support (wip)

### 📜 **Script & Tool Hosting**
- Host your favorite tools directly within SCNCore Plus and embed them into your scripts.

### 🔔 **Notification & Alert Management**
- Get alerts via **Email**, **Microsoft Teams**, **ntfy.sh**, and **Telegram**.

### 📊 **Inventory & Monitoring**
- Software & hardware inventory tracking.
- **Microsoft Defender Antivirus** management.
- **Policy Management**: Define and enforce policies (e.g., antivirus settings, notifications, sensors, jobs).
- **Centralized dashboard** with statistics and unread events.
- **Event viewer** with filtering by severity and more.

### ⚙️ **Automation & Scheduling**
- **Jobs**: Schedule and run PowerShell, Bash, or Zsh scripts.
- **Sensors**: Monitor CPU, RAM, disk usage, Windows event logs, services, ping, and more—including custom sensors via PowerShell, Bash, Zsh, and RegEx.

### 👥 **User Management**
- Add, edit, or remove users.
- Assign tenants and control access to specific panels (e.g., remote shell, device authorization).

---

Explore the full capabilities in our documentation to see everything SCNCore Plus has to offer.

---

## 🐳 Docker Installation (Quick Setup)

Do you wish to get started as fast as possible? Our docker compose script ensures an easy and smooth setup, currently supporting three deployment scenarios:
- Single IP / VPS / Bare Metal, basically a machine & ip only for SCNCore Plus
- Reverse Proxy (supports any reverse proxy)
- Local Testing (only for local test environments, not for production)

The script expects docker compose to be installed, on a linux distribution. Best to use would be Ubuntu 24.04. The script deploys the SCNCore Plus components as well as a MySQL 8.0 server and connects everything with each other.

### Steps:
1. **Download the script with wget:**
   ```bash
   sudo wget -P /home https://raw.githubusercontent.com/ahmetkarakayaoffical/SCNCORE-PLUS/main/docker-compose-quick-setup.sh
