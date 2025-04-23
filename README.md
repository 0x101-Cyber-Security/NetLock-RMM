# NetLock RMM
NetLock RMM is an Remote Monitoring & Management (RMM) software built for Managed Service Providers, with a future focus on cybersecurity. NetLock RMM is primarily written in C#, Blazor, ASP.NET Core, and SignalR.

NetLock RMM can be deployed in various environments, including cloud setups or isolated/offline configurations. It supports multiple operating systems, including Windows and Linux, and can also run within Docker & Kubernetes, providing flexibility for different deployment preferences.



![Web Console Preview](https://netlockrmm.com/assets/images/remote-features.gif)

## [Website](https://netlockrmm.com/)
## [Documentation](https://docs.netlockrmm.com/en/home)
## [Live Demo](https://netlockrmm.com/demo.html)
## [Supported OS & Features](https://docs.netlockrmm.com/en/supported-os)
## [Roadmap](https://docs.netlockrmm.com/en/roadmap)
## [Discord](https://discord.gg/HqUpZgtX4U)

## Features? We have plenty of them. Here is a small preview

The Web Console and server software come as a standalone server based on Kestrel, eliminating the need to configure and maintain complex server software such as Apache. Do you like Let's Encrypt? The Let's Encrypt integration by LettuceEncrypt offers an easy way to protect your connection with SSL. Our security concept offers high flexibility with server roles, meaning you could run all server components on one machine or split them into different roles. You can also define fallback servers for each role.

- Multi platform. The agent supports windows, linux & macos. (x64 & arm64) 
- Multi-tenancy, including locations and group management
- One-click agent installer for all platforms
- Real-time remote shell, file browser, task manager & service manager
- Remote control & support your users. It's as easy to use as TeamViewer. ;)
- File server. Host your favourite tools directly with NetLock RMM and embed them directly into your scripting.
- Event notifications (email, Microsoft Teams, ntfy.sh, and Telegram)
- Software & hardware inventory
- Microsoft Defender Antivirus Management
- Policy Management (define policies that will be applied to your devices. Example: Microsoft Defender Antivirus settings and notifications, sensors, and jobs)
- Dashboard (statistics and unread events)
- Events (browsing through events, filtered by severity and more)
- Jobs (run PowerShell, Bash & Zsh scripts on a regular basis)
- Sensors (like cpu, drive, ram utilization, windows event logs, services, ping, custom via powershell, bash, zsh & regex)
- Users (add/edit/remove)
  - Two-factor authentication
  - Permissions system
    - Assigned tenants
    - Access to panels (e.g., authorizing devices, accessing remote shell, and more)

### Jobs

- All kind of sensors, take a look on the documentation for more details.

### Sensors

- All kind of sensors, take a look on the documentation for more details.

& a lot more...

Note: Why are the unsigned packages not on GitHub?
https://blog.netlockrmm.com/2024/12/22/why-are-the-unsigned-packages-not-on-github/

# Setup NetLock RMM using Docker in 10 minutes
[See in our documentation](https://docs.netlockrmm.com/en/server-installation-docker)

Do you wish to get started as fast as possible? Our docker compose script ensures a easy and smooth setup. The script expects docker compose to be installed, on a linux disctribution. Best to use would be Ubuntu 24.04. The script deploys the NetLock RMM componments as well as a MySQL 8.0 server.
 
## Video Tutorial
[Watch the video on YouTube](https://youtu.be/-VMoL6wnSKs)
 
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

Happy monitoring! 🥳
