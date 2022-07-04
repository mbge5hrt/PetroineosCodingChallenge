
namespace PowerPositionReporting
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.powerPositionServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.powerPositionServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // powerPositionServiceProcessInstaller
            // 
            this.powerPositionServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.powerPositionServiceProcessInstaller.Password = null;
            this.powerPositionServiceProcessInstaller.Username = null;
            // 
            // powerPositionServiceInstaller
            // 
            this.powerPositionServiceInstaller.Description = "Provides regular power position reports";
            this.powerPositionServiceInstaller.ServiceName = "Power Position Service";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.powerPositionServiceProcessInstaller,
            this.powerPositionServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller powerPositionServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller powerPositionServiceInstaller;
    }
}