using Newtonsoft.Json.Bson;
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
    public void SetProjectType(string type)
    {
      switch (type)
      {
        case "Complex Extension":
          this.ClassNameEnabled = true;
          break;

        case "Simple Extension":
          this.ClassNameEnabled = false;
          break;
        
        case "Empty Extension":
          this.ClassNameEnabled = false;
          break;
        
        case "Layout Extension":
          this.ClassNameEnabled = false;
          break;
      }
    }

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

    public Boolean ExtensionNamespaceEnabled
    {
      get
      {
        return this.txtExtensionNamespace.Visible;
      }
      set
      {
        this.txtExtensionNamespace.Visible = value;
        this.lblExtensionNamespace.Visible = value;
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

    public Boolean ClassNameEnabled
    {
      get
      {
        return this.txtModelName.Visible;
      }
      set
      {
        this.txtModelName.Visible = value;
        this.lblModelName.Visible = value;
      }
    }

    public string ModelClassName
    {
      get
      {
        return this.txtModelName.Text;
      }
      set
      {
        this.txtModelName.Text = value;
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

    private void cmdNext_Click(object sender, EventArgs e)
    {
      // If the user selects a model name which matches the end of the namespace, add a "Models" prefix to the
      // model name so that the compiler can defferentiate between the model class name and the namespace.
      if (this.txtExtensionNamespace.Text.EndsWith(this.txtModelName.Text))
      {
        this.txtModelName.Text = $"Models.{this.txtModelName.Text}";
      }
    }

    private void txtExtensionName_Validating(object sender, CancelEventArgs e)
    {
      if (!System.Text.RegularExpressions.Regex.IsMatch(this.txtExtensionName.Text, "^([A-Za-z0-9_])*$"))
      {        
        MessageBox.Show("Extension names can contain letters, numbers and the underscore character.","Invalid Characters", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        e.Cancel = true;
      }
    }

    private void txtModelName_Validating(object sender, CancelEventArgs e)
    {
      if (!System.Text.RegularExpressions.Regex.IsMatch(this.txtModelName.Text, "^([A-Za-z0-9_])*$"))
      {
        MessageBox.Show("Model class names can contain letters, numbers and the underscore character.", "Invalid Characters", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        e.Cancel = true;
      }
    }

    private void txtExtensionNamespace_Validating(object sender, CancelEventArgs e)
    {
      if (!System.Text.RegularExpressions.Regex.IsMatch(this.txtExtensionNamespace.Text, "^([A-Za-z0-9_])*$"))
      {
        MessageBox.Show("Namespace can contain letters, numbers and the underscore character.", "Invalid Characters", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        e.Cancel = true;
      }
    }
  }
}