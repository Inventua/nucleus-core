using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Nucleus.DeveloperTools.VisualStudioTemplates
{
	public partial class ProjectWizardForm : Form
	{
		public string ExtensionNamespace
		{
			get
			{
				return this.txtExtensionNamespace.Text;
			}
			set
			{
				this.txtExtensionNamespace.Text = value;
			}
		}

		public string ExtensionName
		{
			get
			{
				return this.txtExtensionName.Text;
			}
			set
			{
				this.txtExtensionName.Text = value;
			}
		}

		public string FriendlyName
		{
			get
			{
				return this.txtFriendlyName.Text;
			}
			set
			{
				this.txtFriendlyName.Text = value;
			}
		}

		public string ExtensionDescription
		{
			get
			{
				return this.txtExtensionDescription.Text;
			}
			set
			{
				this.txtExtensionDescription.Text = value;
			}
		}

		public string PublisherName
		{
			get
			{
				return this.txtPublisherName.Text;
			}
			set
			{
				this.txtPublisherName.Text = value;
			}
		}

		public string PublisherUrl
		{
			get
			{
				return this.txtPublisherUrl.Text;
			}
			set
			{
				this.txtPublisherUrl.Text = value;
			}
		}

		public string PublisherEmail
		{
			get
			{
				return this.txtPublisherEmail.Text;
			}
			set
			{
				this.txtPublisherEmail.Text = value;
			}
		}


		public ProjectWizardForm()
		{
			InitializeComponent();
			this.lblVersion.Text = this.GetType().Assembly.GetName().Version.ToString();
		}
	}
}
