namespace NetLock_RMM_Remote_Agent__Windows
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Komponenten-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.ServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            this.ServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            // 
            // ServiceInstaller
            // 
            this.ServiceInstaller.DisplayName = "NetLock RMM Remote Agent Windows";
            this.ServiceInstaller.ServiceName = "NetLock_RMM_Remote_Agent_Windows";
            this.ServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ServiceProcessInstaller
            // 
            this.ServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.ServiceProcessInstaller.Password = null;
            this.ServiceProcessInstaller.Username = null;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.ServiceInstaller,
            this.ServiceProcessInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceInstaller ServiceInstaller;
        private System.ServiceProcess.ServiceProcessInstaller ServiceProcessInstaller;
    }
}