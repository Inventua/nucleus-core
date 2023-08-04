namespace Nucleus.DeveloperTools.VisualStudioTemplates
{
	partial class ProjectWizardForm
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
      System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ProjectWizardForm));
      this.label2 = new System.Windows.Forms.Label();
      this.txtExtensionName = new System.Windows.Forms.TextBox();
      this.cmdNext = new System.Windows.Forms.Button();
      this.cmdBack = new System.Windows.Forms.Button();
      this.panel1 = new System.Windows.Forms.Panel();
      this.label5 = new System.Windows.Forms.Label();
      this.label4 = new System.Windows.Forms.Label();
      this.lblHeading = new System.Windows.Forms.Label();
      this.txtExtensionNamespace = new System.Windows.Forms.TextBox();
      this.lblExtensionNamespace = new System.Windows.Forms.Label();
      this.txtPublisherUrl = new System.Windows.Forms.TextBox();
      this.label3 = new System.Windows.Forms.Label();
      this.panel2 = new System.Windows.Forms.Panel();
      this.label6 = new System.Windows.Forms.Label();
      this.label7 = new System.Windows.Forms.Label();
      this.txtPublisherName = new System.Windows.Forms.TextBox();
      this.label8 = new System.Windows.Forms.Label();
      this.txtPublisherEmail = new System.Windows.Forms.TextBox();
      this.label9 = new System.Windows.Forms.Label();
      this.lblVersion = new System.Windows.Forms.Label();
      this.txtFriendlyName = new System.Windows.Forms.TextBox();
      this.label11 = new System.Windows.Forms.Label();
      this.txtExtensionDescription = new System.Windows.Forms.TextBox();
      this.label12 = new System.Windows.Forms.Label();
      this.txtModelName = new System.Windows.Forms.TextBox();
      this.lblModelName = new System.Windows.Forms.Label();
      this.pictureBox1 = new System.Windows.Forms.PictureBox();
      this.panel1.SuspendLayout();
      this.panel2.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
      this.SuspendLayout();
      // 
      // label2
      // 
      this.label2.AutoSize = true;
      this.label2.Location = new System.Drawing.Point(79, 308);
      this.label2.Name = "label2";
      this.label2.Size = new System.Drawing.Size(93, 15);
      this.label2.TabIndex = 1;
      this.label2.Text = "Extension Name";
      // 
      // txtExtensionName
      // 
      this.txtExtensionName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExtensionName.Location = new System.Drawing.Point(224, 305);
      this.txtExtensionName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtExtensionName.Name = "txtExtensionName";
      this.txtExtensionName.Size = new System.Drawing.Size(448, 23);
      this.txtExtensionName.TabIndex = 3;
      this.txtExtensionName.Validating += new System.ComponentModel.CancelEventHandler(this.txtExtensionName_Validating);
      // 
      // cmdNext
      // 
      this.cmdNext.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.cmdNext.DialogResult = System.Windows.Forms.DialogResult.OK;
      this.cmdNext.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.cmdNext.Location = new System.Drawing.Point(654, 528);
      this.cmdNext.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.cmdNext.Name = "cmdNext";
      this.cmdNext.Size = new System.Drawing.Size(87, 26);
      this.cmdNext.TabIndex = 8;
      this.cmdNext.Text = "Next";
      this.cmdNext.UseVisualStyleBackColor = true;
      this.cmdNext.Click += new System.EventHandler(this.cmdNext_Click);
      // 
      // cmdBack
      // 
      this.cmdBack.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
      this.cmdBack.DialogResult = System.Windows.Forms.DialogResult.Cancel;
      this.cmdBack.Location = new System.Drawing.Point(561, 528);
      this.cmdBack.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.cmdBack.Name = "cmdBack";
      this.cmdBack.Size = new System.Drawing.Size(87, 26);
      this.cmdBack.TabIndex = 9;
      this.cmdBack.Text = "Back";
      this.cmdBack.UseVisualStyleBackColor = true;
      // 
      // panel1
      // 
      this.panel1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.panel1.BackColor = System.Drawing.SystemColors.Window;
      this.panel1.Controls.Add(this.label5);
      this.panel1.Controls.Add(this.label4);
      this.panel1.Location = new System.Drawing.Point(54, 219);
      this.panel1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.panel1.Name = "panel1";
      this.panel1.Size = new System.Drawing.Size(687, 69);
      this.panel1.TabIndex = 10;
      // 
      // label5
      // 
      this.label5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.label5.Location = new System.Drawing.Point(25, 3);
      this.label5.Name = "label5";
      this.label5.Size = new System.Drawing.Size(649, 64);
      this.label5.TabIndex = 1;
      this.label5.Text = resources.GetString("label5.Text");
      // 
      // label4
      // 
      this.label4.AutoSize = true;
      this.label4.Font = new System.Drawing.Font("Webdings", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
      this.label4.ForeColor = System.Drawing.SystemColors.MenuHighlight;
      this.label4.Location = new System.Drawing.Point(7, 2);
      this.label4.Name = "label4";
      this.label4.Size = new System.Drawing.Size(21, 18);
      this.label4.TabIndex = 0;
      this.label4.Text = "i";
      // 
      // lblHeading
      // 
      this.lblHeading.AutoSize = true;
      this.lblHeading.Font = new System.Drawing.Font("Segoe UI", 21.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.lblHeading.Location = new System.Drawing.Point(46, 22);
      this.lblHeading.Name = "lblHeading";
      this.lblHeading.Size = new System.Drawing.Size(324, 40);
      this.lblHeading.TabIndex = 13;
      this.lblHeading.Text = "Nucleus Project Settings";
      // 
      // txtExtensionNamespace
      // 
      this.txtExtensionNamespace.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExtensionNamespace.Location = new System.Drawing.Point(224, 434);
      this.txtExtensionNamespace.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtExtensionNamespace.Name = "txtExtensionNamespace";
      this.txtExtensionNamespace.Size = new System.Drawing.Size(448, 23);
      this.txtExtensionNamespace.TabIndex = 6;
      this.txtExtensionNamespace.Validating += new System.ComponentModel.CancelEventHandler(this.txtExtensionNamespace_Validating);
      // 
      // lblExtensionNamespace
      // 
      this.lblExtensionNamespace.AutoSize = true;
      this.lblExtensionNamespace.Location = new System.Drawing.Point(79, 437);
      this.lblExtensionNamespace.Name = "lblExtensionNamespace";
      this.lblExtensionNamespace.Size = new System.Drawing.Size(123, 15);
      this.lblExtensionNamespace.TabIndex = 15;
      this.lblExtensionNamespace.Text = "Extension Namespace";
      // 
      // txtPublisherUrl
      // 
      this.txtPublisherUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtPublisherUrl.Location = new System.Drawing.Point(224, 152);
      this.txtPublisherUrl.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtPublisherUrl.Name = "txtPublisherUrl";
      this.txtPublisherUrl.Size = new System.Drawing.Size(448, 23);
      this.txtPublisherUrl.TabIndex = 1;
      // 
      // label3
      // 
      this.label3.AutoSize = true;
      this.label3.Location = new System.Drawing.Point(79, 155);
      this.label3.Name = "label3";
      this.label3.Size = new System.Drawing.Size(74, 15);
      this.label3.TabIndex = 20;
      this.label3.Text = "Publisher Url";
      // 
      // panel2
      // 
      this.panel2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.panel2.BackColor = System.Drawing.SystemColors.Window;
      this.panel2.Controls.Add(this.label6);
      this.panel2.Controls.Add(this.label7);
      this.panel2.Location = new System.Drawing.Point(54, 78);
      this.panel2.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.panel2.Name = "panel2";
      this.panel2.Size = new System.Drawing.Size(661, 27);
      this.panel2.TabIndex = 18;
      // 
      // label6
      // 
      this.label6.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.label6.Location = new System.Drawing.Point(25, 3);
      this.label6.Name = "label6";
      this.label6.Size = new System.Drawing.Size(623, 22);
      this.label6.TabIndex = 1;
      this.label6.Text = "Publisher information is automatically added to the extension manifest, which is " +
    "used by the extension installer.";
      // 
      // label7
      // 
      this.label7.AutoSize = true;
      this.label7.Font = new System.Drawing.Font("Webdings", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(2)));
      this.label7.ForeColor = System.Drawing.SystemColors.MenuHighlight;
      this.label7.Location = new System.Drawing.Point(7, 2);
      this.label7.Name = "label7";
      this.label7.Size = new System.Drawing.Size(21, 18);
      this.label7.TabIndex = 0;
      this.label7.Text = "i";
      // 
      // txtPublisherName
      // 
      this.txtPublisherName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtPublisherName.Location = new System.Drawing.Point(224, 121);
      this.txtPublisherName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtPublisherName.Name = "txtPublisherName";
      this.txtPublisherName.Size = new System.Drawing.Size(448, 23);
      this.txtPublisherName.TabIndex = 0;
      // 
      // label8
      // 
      this.label8.AutoSize = true;
      this.label8.Location = new System.Drawing.Point(79, 124);
      this.label8.Name = "label8";
      this.label8.Size = new System.Drawing.Size(91, 15);
      this.label8.TabIndex = 17;
      this.label8.Text = "Publisher Name";
      // 
      // txtPublisherEmail
      // 
      this.txtPublisherEmail.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtPublisherEmail.Location = new System.Drawing.Point(224, 183);
      this.txtPublisherEmail.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtPublisherEmail.Name = "txtPublisherEmail";
      this.txtPublisherEmail.Size = new System.Drawing.Size(448, 23);
      this.txtPublisherEmail.TabIndex = 2;
      // 
      // label9
      // 
      this.label9.AutoSize = true;
      this.label9.Location = new System.Drawing.Point(79, 186);
      this.label9.Name = "label9";
      this.label9.Size = new System.Drawing.Size(88, 15);
      this.label9.TabIndex = 22;
      this.label9.Text = "Publisher Email";
      // 
      // lblVersion
      // 
      this.lblVersion.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
      this.lblVersion.AutoSize = true;
      this.lblVersion.Location = new System.Drawing.Point(12, 527);
      this.lblVersion.Name = "lblVersion";
      this.lblVersion.Size = new System.Drawing.Size(22, 15);
      this.lblVersion.TabIndex = 23;
      this.lblVersion.Text = "1.0";
      // 
      // txtFriendlyName
      // 
      this.txtFriendlyName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtFriendlyName.Location = new System.Drawing.Point(224, 336);
      this.txtFriendlyName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtFriendlyName.Name = "txtFriendlyName";
      this.txtFriendlyName.Size = new System.Drawing.Size(448, 23);
      this.txtFriendlyName.TabIndex = 4;
      // 
      // label11
      // 
      this.label11.AutoSize = true;
      this.label11.Location = new System.Drawing.Point(79, 339);
      this.label11.Name = "label11";
      this.label11.Size = new System.Drawing.Size(84, 15);
      this.label11.TabIndex = 25;
      this.label11.Text = "Friendly Name";
      // 
      // txtExtensionDescription
      // 
      this.txtExtensionDescription.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtExtensionDescription.Location = new System.Drawing.Point(224, 367);
      this.txtExtensionDescription.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtExtensionDescription.Multiline = true;
      this.txtExtensionDescription.Name = "txtExtensionDescription";
      this.txtExtensionDescription.Size = new System.Drawing.Size(448, 52);
      this.txtExtensionDescription.TabIndex = 5;
      // 
      // label12
      // 
      this.label12.AutoSize = true;
      this.label12.Location = new System.Drawing.Point(79, 370);
      this.label12.Name = "label12";
      this.label12.Size = new System.Drawing.Size(67, 15);
      this.label12.TabIndex = 27;
      this.label12.Text = "Description";
      // 
      // txtModelName
      // 
      this.txtModelName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
      this.txtModelName.Location = new System.Drawing.Point(224, 465);
      this.txtModelName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.txtModelName.Name = "txtModelName";
      this.txtModelName.Size = new System.Drawing.Size(448, 23);
      this.txtModelName.TabIndex = 7;
      this.txtModelName.Validating += new System.ComponentModel.CancelEventHandler(this.txtModelName_Validating);
      // 
      // lblModelName
      // 
      this.lblModelName.AutoSize = true;
      this.lblModelName.Location = new System.Drawing.Point(79, 468);
      this.lblModelName.Name = "lblModelName";
      this.lblModelName.Size = new System.Drawing.Size(106, 15);
      this.lblModelName.TabIndex = 29;
      this.lblModelName.Text = "Model Class Name";
      // 
      // pictureBox1
      // 
      this.pictureBox1.Image = global::Nucleus.DeveloperTools.VisualStudioTemplates.Properties.Resources.atom_90;
      this.pictureBox1.Location = new System.Drawing.Point(681, 11);
      this.pictureBox1.Name = "pictureBox1";
      this.pictureBox1.Size = new System.Drawing.Size(60, 60);
      this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
      this.pictureBox1.TabIndex = 30;
      this.pictureBox1.TabStop = false;
      // 
      // ProjectWizardForm
      // 
      this.AcceptButton = this.cmdNext;
      this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.BackColor = System.Drawing.SystemColors.Window;
      this.CancelButton = this.cmdBack;
      this.ClientSize = new System.Drawing.Size(763, 575);
      this.ControlBox = false;
      this.Controls.Add(this.pictureBox1);
      this.Controls.Add(this.txtModelName);
      this.Controls.Add(this.lblModelName);
      this.Controls.Add(this.txtExtensionDescription);
      this.Controls.Add(this.label12);
      this.Controls.Add(this.txtFriendlyName);
      this.Controls.Add(this.label11);
      this.Controls.Add(this.lblVersion);
      this.Controls.Add(this.txtPublisherEmail);
      this.Controls.Add(this.label9);
      this.Controls.Add(this.txtPublisherUrl);
      this.Controls.Add(this.label3);
      this.Controls.Add(this.panel2);
      this.Controls.Add(this.txtPublisherName);
      this.Controls.Add(this.label8);
      this.Controls.Add(this.txtExtensionNamespace);
      this.Controls.Add(this.lblExtensionNamespace);
      this.Controls.Add(this.lblHeading);
      this.Controls.Add(this.panel1);
      this.Controls.Add(this.cmdBack);
      this.Controls.Add(this.cmdNext);
      this.Controls.Add(this.txtExtensionName);
      this.Controls.Add(this.label2);
      this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
      this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
      this.MaximizeBox = false;
      this.MinimizeBox = false;
      this.Name = "ProjectWizardForm";
      this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
      this.panel1.ResumeLayout(false);
      this.panel1.PerformLayout();
      this.panel2.ResumeLayout(false);
      this.panel2.PerformLayout();
      ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
      this.ResumeLayout(false);
      this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.TextBox txtExtensionName;
		private System.Windows.Forms.Button cmdNext;
		private System.Windows.Forms.Button cmdBack;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label lblHeading;
		private System.Windows.Forms.TextBox txtExtensionNamespace;
		private System.Windows.Forms.Label lblExtensionNamespace;
		private System.Windows.Forms.TextBox txtPublisherUrl;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.TextBox txtPublisherName;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.TextBox txtPublisherEmail;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label lblVersion;
		private System.Windows.Forms.TextBox txtFriendlyName;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.TextBox txtExtensionDescription;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.TextBox txtModelName;
		private System.Windows.Forms.Label lblModelName;
    private System.Windows.Forms.PictureBox pictureBox1;
  }
}