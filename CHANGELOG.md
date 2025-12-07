# Changelog

## Latest Release

### ⚠️ Important Notice

**Breaking Changes:**
- All agents must be updated to the latest version for remote access to function properly
- Review and update your policies to enable remote capabilities in agent settings
- The new tray icon must be enabled in policy settings for attended remote access

### ✨ New Features

#### User Tray Icon
- Introduced fully customizable tray icon for enhanced user engagement
- Added new remote access experience with user-controlled accept/decline functionality
- Administrator-controlled remote access behavior through policy settings
- Ideal for situations requiring confidentiality and user consent

#### Device Labels
- New device labels provide clearer device overview
- Automation policy preview shows which policies are applied to each device

#### Shutdown & Reboot Actions
- Quick shutdown and reboot actions added
- Direct device control without navigating through remote shell → script selection → execution

#### Agent Policy Controls
- Configure device synchronization frequency with server
- Override global auto-update settings per assigned policy
- Enable or disable specific remote features per policy
- Enable or disable entire remote service per policy
- Policy settings are managed directly by remote devices (requires policy sync)
- Define unattended access permissions or require user confirmation before connection

### 🔧 Improvements

- **Remote Agent Service:** Complete overhaul of connection behavior
  - Automatic connection re-establishment after system reboot when network connectivity is restored
  - Fixed edge cases where security programs could silently block connections
  
- **User Session Tracking:** Last active user now correctly reflects RDP sessions

- **Linux Software Inventory:** Added support for additional package managers:
  - Yum
  - Zypper
  - Pacman
  - DNF

- **Remote Screen Control:** Performance optimizations
  - Reduced CPU resource usage
  - ~33% reduction in bandwidth consumption
  - Noticeable performance improvements

- **Agent Installer:** Added support for hidden and no-log parameters

- Various small tweaks and improvements

### 💬 Feedback

We value your feedback! Please share your experience to help us continue improving NetLock RMM.

---

*Release Date: December 7, 2025*
