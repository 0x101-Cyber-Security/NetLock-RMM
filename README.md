
# NetLock RMM
NetLock RMM is an Remote Monitoring & Management (RMM) software built for Managed Service Providers, with a future focus on cybersecurity. NetLock RMM is primarily written in C#, Blazor, ASP.NET Core, and SignalR.

NetLock RMM can be deployed in the cloud or in isolated/offline environments.

## [Website](https://netlockrmm.com/)
## [Members Portal](https://members.netlockrmm.com/)
## [Discord](https://discord.gg/HqUpZgtX4U)
## [Documentation](https://docs.netlockrmm.com/en/home)
## [Supported OS/Distributions](https://docs.netlockrmm.com/en/supported-os)
## [Live Demo](https://netlockrmm.com/demo.html)
## [Early Adopter Program](https://docs.netlockrmm.com/en/early-adopter)
## [Roadmap](https://docs.netlockrmm.com/en/roadmap)
## [LinkedIn Company](https://www.linkedin.com/company/0x101-cyber-security/about)
## [LinkedIn Developer](https://www.linkedin.com/in/nico-mak/)

## Early Adopters Version

On August 1, 2024, NetLock RMM released its first early adopters version. The documentation is still being written. The backend and agent are stable, and the web console is stable but has some cosmetic flaws. We encourage you to help us find and report bugs. Potential issues include missing translations (English or German) and missing form checks, which may allow duplicate entries/names in some scenarios if caution is not exercised. The web console can be used on mobile devices, but it is not yet optimized. These issues will be addressed and likely fixed with the next release around September 1, 2024. We will also significantly improve the code and syntax behind the web console.

## Early Adopter Program

Do you want to join our journey? Become an early adopter. While NetLock RMM is in the early adopters phase, it will be completely free to use, including compiled binaries, and soon they will be code-signed as well. Don't worry; NetLock RMM will remain free & publishing its source code after that phase. However, since this project is extensive and requires full-time attention, we will offer services in the future to make it profitable. These services will include offering code-signed binaries, special services, or support contracts for companies, managed hosting, a partner portal for consultants, and much more. Our pricing strategy will be fair and aligned with other source code available RMM vendors.

## [More Information about our Early Adopter Program](https://docs.netlockrmm.com/en/early-adopter)

## Features? We have plenty of them. Here is a small preview

The Web Console and server software come as a standalone server based on Kestrel, eliminating the need to configure and maintain complex server software such as Apache. Do you like Let's Encrypt? The Let's Encrypt integration by LettuceEncrypt offers an easy way to protect your connection with SSL. Our security concept offers high flexibility with server roles, meaning you could run all server components on one machine or split them into different roles. You can also define fallback servers for each role.

- Multi-tenancy, including locations and group management
- Event notifications (email, Microsoft Teams, ntfy.sh, and Telegram)
- Microsoft Defender Antivirus Management
- Real-time remote shell and file browser
  - Note: Will be more feature-rich in the future. The underlying technique allows for much more. ;)
- Task Manager
- Software Inventory (Installed, Logon, Task Scheduler, Services, and Drivers)
- Policy Management (define policies that will be applied to your devices. Example: Microsoft Defender Antivirus settings and notifications, sensors, and jobs)
- Dashboard (statistics and unread events)
  - Note: Will offer more statistics in the future.
- Events (browsing through events, filtered by severity and more)
- Users (add/edit/remove)
  - Two-factor authentication
  - Permissions system
    - Assigned tenants
    - Access to panels (e.g., authorizing devices, accessing remote shell, and more)

### Jobs Scheduler

- Run scripts regularly
- The time scheduler is detailed to cover all possible scenarios 

### Sensors

- The time scheduler is detailed to cover all possible scenarios
- Threshold (controls how often the sensor should trigger before performing the following)
  - Notification
  - Action
- Custom notification triggers
- All sensors can either execute a script or perform other native actions if configured
- Categories:
  - **Utilization**
    - Processor usage (%)
    - RAM usage (%)
    - Drive Space
      - More than X GB or X % occupied, and more
      - All drives or selected ones
      - Network drives
      - Removable drives
      - Only check drives with a capacity of X gigabytes or more
      - And more
    - Process CPU utilization (%)
    - Process RAM utilization (% or MB)
    - Actions:
      - PowerShell
  - **Windows Eventlog**
    - Eventlog support
      - Application
      - Security
      - Setup
      - System
      - Custom (define the one you want to monitor)
    - Event ID for filtering
      - RegEx is supported and can be used to trigger only on specific content
    - Actions:
      - PowerShell
  - **PowerShell**
    - Execute a custom script that returns a result
    - Define an expected PowerShell result that triggers the sensor
  - **Service**
    - Condition:
      - Running, paused, or stopped
    - Actions:
      - Start or stop the service depending on your selection and its status
      - PowerShell
    - Note: If your actions fail, you get back the error message.
  - **Ping**
    - IP address or hostname
    - Timeout
    - Condition:
      - Successful or failed
    - Actions:
      - PowerShell

