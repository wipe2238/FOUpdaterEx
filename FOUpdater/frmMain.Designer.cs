namespace FOUpdater
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose( bool disposing )
        {
            if( disposing && (components != null) )
            {
                components.Dispose();
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( typeof( frmMain ) );
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.groupMain = new System.Windows.Forms.GroupBox();
            this.progressFile = new System.Windows.Forms.ProgressBar();
            this.labelDetailed = new System.Windows.Forms.Label();
            this.progressAll = new System.Windows.Forms.ProgressBar();
            this.labelGeneral = new System.Windows.Forms.Label();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.buttonExit = new System.Windows.Forms.Button();
            this.labelFOUpdater = new System.Windows.Forms.Label();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.groupMain.SuspendLayout();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler( this.backgroundWorker_DoWork );
            // 
            // groupMain
            // 
            this.groupMain.AutoSize = true;
            this.groupMain.Controls.Add( this.progressFile );
            this.groupMain.Controls.Add( this.labelDetailed );
            this.groupMain.Controls.Add( this.progressAll );
            this.groupMain.Controls.Add( this.labelGeneral );
            this.groupMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupMain.Location = new System.Drawing.Point( 6, 6 );
            this.groupMain.Margin = new System.Windows.Forms.Padding( 3, 3, 3, 10 );
            this.groupMain.Name = "groupMain";
            this.groupMain.Padding = new System.Windows.Forms.Padding( 10, 2, 10, 10 );
            this.groupMain.Size = new System.Drawing.Size( 330, 95 );
            this.groupMain.TabIndex = 0;
            this.groupMain.TabStop = false;
            // 
            // progressFile
            // 
            this.progressFile.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressFile.ForeColor = System.Drawing.Color.Green;
            this.progressFile.Location = new System.Drawing.Point( 10, 67 );
            this.progressFile.Name = "progressFile";
            this.progressFile.Size = new System.Drawing.Size( 310, 18 );
            this.progressFile.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressFile.TabIndex = 3;
            this.progressFile.Value = 25;
            // 
            // labelDetailed
            // 
            this.labelDetailed.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelDetailed.Location = new System.Drawing.Point( 10, 49 );
            this.labelDetailed.Margin = new System.Windows.Forms.Padding( 0 );
            this.labelDetailed.Name = "labelDetailed";
            this.labelDetailed.Size = new System.Drawing.Size( 310, 18 );
            this.labelDetailed.TabIndex = 3;
            this.labelDetailed.Text = "[Download status]";
            this.labelDetailed.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // progressAll
            // 
            this.progressAll.Dock = System.Windows.Forms.DockStyle.Top;
            this.progressAll.ForeColor = System.Drawing.Color.Green;
            this.progressAll.Location = new System.Drawing.Point( 10, 31 );
            this.progressAll.Name = "progressAll";
            this.progressAll.Size = new System.Drawing.Size( 310, 18 );
            this.progressAll.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressAll.TabIndex = 2;
            this.progressAll.Value = 75;
            // 
            // labelGeneral
            // 
            this.labelGeneral.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelGeneral.Location = new System.Drawing.Point( 10, 15 );
            this.labelGeneral.Margin = new System.Windows.Forms.Padding( 3, 0, 3, 7 );
            this.labelGeneral.Name = "labelGeneral";
            this.labelGeneral.Size = new System.Drawing.Size( 310, 16 );
            this.labelGeneral.TabIndex = 0;
            this.labelGeneral.Text = "[General status]";
            this.labelGeneral.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonCancel
            // 
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point( 3, 9 );
            this.buttonCancel.MaximumSize = new System.Drawing.Size( 75, 23 );
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size( 75, 23 );
            this.buttonCancel.TabIndex = 5;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler( this.buttonCancel_Click );
            // 
            // buttonExit
            // 
            this.buttonExit.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExit.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonExit.Location = new System.Drawing.Point( 255, 9 );
            this.buttonExit.MaximumSize = new System.Drawing.Size( 76, 23 );
            this.buttonExit.Name = "buttonExit";
            this.buttonExit.Size = new System.Drawing.Size( 75, 23 );
            this.buttonExit.TabIndex = 6;
            this.buttonExit.Text = "Exit";
            this.buttonExit.UseVisualStyleBackColor = true;
            this.buttonExit.Click += new System.EventHandler( this.buttonExit_Click );
            // 
            // labelFOUpdater
            // 
            this.labelFOUpdater.Dock = System.Windows.Forms.DockStyle.Fill;
            this.labelFOUpdater.Enabled = false;
            this.labelFOUpdater.Location = new System.Drawing.Point( 0, 0 );
            this.labelFOUpdater.Name = "labelFOUpdater";
            this.labelFOUpdater.Size = new System.Drawing.Size( 330, 41 );
            this.labelFOUpdater.TabIndex = 4;
            this.labelFOUpdater.Text = "FOnline Updater";
            this.labelFOUpdater.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add( this.buttonExit );
            this.panelButtons.Controls.Add( this.buttonCancel );
            this.panelButtons.Controls.Add( this.labelFOUpdater );
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point( 6, 101 );
            this.panelButtons.Margin = new System.Windows.Forms.Padding( 10 );
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size( 330, 41 );
            this.panelButtons.TabIndex = 4;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF( 6F, 13F );
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.ClientSize = new System.Drawing.Size( 342, 142 );
            this.Controls.Add( this.panelButtons );
            this.Controls.Add( this.groupMain );
            this.Icon = ((System.Drawing.Icon)(resources.GetObject( "$this.Icon" )));
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size( 350, 166 );
            this.Name = "frmMain";
            this.Padding = new System.Windows.Forms.Padding( 6, 6, 6, 0 );
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FOUpdater";
            this.Load += new System.EventHandler( this.frmUpdater_Load );
            this.groupMain.ResumeLayout( false );
            this.panelButtons.ResumeLayout( false );
            this.ResumeLayout( false );
            this.PerformLayout();

        }

        #endregion

        private System.ComponentModel.BackgroundWorker backgroundWorker;
        private System.Windows.Forms.GroupBox groupMain;
        private System.Windows.Forms.ProgressBar progressFile;
        private System.Windows.Forms.Label labelDetailed;
        private System.Windows.Forms.ProgressBar progressAll;
        private System.Windows.Forms.Label labelGeneral;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Button buttonExit;
        private System.Windows.Forms.Label labelFOUpdater;
        private System.Windows.Forms.Panel panelButtons;
    }
}