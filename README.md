# NetLock RMM
NetLock RMM is an Remote Monitoring & Management (RMM) software built for Managed Service Providers, with a future focus on cybersecurity. NetLock RMM is primarily written in C#, Blazor, ASP.NET Core, and SignalR.

NetLock RMM can be deployed in various environments, including cloud setups or isolated/offline configurations. It supports multiple operating systems, including Windows and Linux, and can also run within Docker & Kubernetes, providing flexibility for different deployment preferences.



![Web Console Preview](https://netlockrmm.com/assets/images/web-console-animation.webp)

## [Website](https://netlockrmm.com/)
## [Documentation](https://docs.netlockrmm.com/en/home)
## [Live Demo](https://docs.netlockrmm.com/en/home) (top info)
## [Supported OS & Features](https://docs.netlockrmm.com/en/supported-os)
## [Roadmap](https://docs.netlockrmm.com/en/roadmap)
## [Discord](https://discord.gg/HqUpZgtX4U)

## **NetLock RMM – Feature Overview**

### 🚀 **Streamlined Server Setup**
- The **Web Console** and **server software** are delivered as a standalone server built on **Kestrel**, eliminating the need for complex configurations with traditional servers like Apache.
- **SSL Made Simple**: Love Let's Encrypt? Our integration with **LettuceEncrypt** makes it effortless to secure your connection with SSL certificates.

### 🛡️ **Flexible & Scalable Architecture**
- **Modular Server Roles**: Run all components on a single machine or distribute them across multiple roles for scalability.
- **Fallback Servers**: Define backup servers for each role to ensure high availability.
- **Reverse Proxy Support**: Reverse proxies are fully supported.

### 🖥️ **Cross-Platform Agent Support**
- Compatible with **Windows, Linux, and macOS** (x64 & ARM64).
- **One-click installer** available for all platforms.

### 🧩 **Multi-Tenancy & Management**
- Full support for **multi-tenancy**, including **location** and **group management**.
- **User & Permission Management** with role-based access control and two-factor authentication.

### 🛠️ **Powerful Remote Tools**
- **Real-time remote shell**, file browser, task manager, and service manager.
- **Remote screen control (windows)** and support that’s as intuitive as TeamViewer.
  - Full support for session switching and display switching
  - Unattended access
  - Ctrl + Alt + Del support for elevated access
  - Built-in session recording
  - Send input as keystrokes, ideal for automating password entry
  - Mobile support (wip)    

### 📁 **Integrated File Server**
- Host your favorite tools directly within NetLock RMM and embed them into your scripts.

### 🔔 **Event Notifications**
- Get alerts via **Email**, **Microsoft Teams**, **ntfy.sh**, and **Telegram**.

### 🧾 **Inventory & Monitoring**
- **Software & hardware inventory** tracking.
- **Microsoft Defender Antivirus** management.
- **Policy Management**: Define and enforce policies (e.g., antivirus settings, notifications, sensors, jobs).

### 📊 **Dashboards & Event Logs**
- Centralized **dashboard** with statistics and unread events.
- **Event viewer** with filtering by severity and more.

### ⚙️ **Automation & Monitoring**
- **Jobs**: Schedule and run PowerShell, Bash, or Zsh scripts.
- **Sensors**: Monitor CPU, RAM, disk usage, Windows event logs, services, ping, and more—including custom sensors via PowerShell, Bash, Zsh, and RegEx.

### 👥 **User & Access Control**
- Add, edit, or remove users.
- Assign tenants and control access to specific panels (e.g., remote shell, device authorization).

---

### ➕ **And Much More...**
Explore the full capabilities in our documentation to see everything NetLock RMM has to offer.

Note: Why are the unsigned packages not on GitHub?
https://blog.netlockrmm.com/2024/12/22/why-are-the-unsigned-packages-not-on-github/

# Setup NetLock RMM using Docker in 10 minutes
[See in our documentation](https://docs.netlockrmm.com/en/server-installation-docker)

Do you wish to get started as fast as possible? Our docker compose script ensures a easy and smooth setup, currently supporting three deployment scenarios:
- Single IP / VPS / Bare Metal, basically a machine & ip only for NetLock RMM
- Reverse Proxy (supports any reverse proxy)
- Local Testing (only for local test environments, not for production)

The script expects docker compose to be installed, on a linux disctribution. Best to use would be Ubuntu 24.04. The script deploys the NetLock RMM componments as well as a MySQL 8.0 server and connects everything with each other.

1. Download the script with wget.
```plaintext
sudo wget -P /home https://raw.githubusercontent.com/0x101-Cyber-Security/NetLock-RMM/main/docker-compose-quick-setup.sh
```

2. Make it executable:
```plaintext
sudo chmod +x /home/docker-compose-quick-setup.sh
```

3. Execute it:
```plaintext
sudo ./home/docker-compose-quick-setup.sh
```

4. Follow the instructions.

## Video Tutorial
[Watch the video on YouTube](https://youtu.be/-VMoL6wnSKs)

Happy monitoring! 🥳
