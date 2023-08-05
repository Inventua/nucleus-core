using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using System.Windows.Forms;
using EnvDTE;
using System.Linq;
using Microsoft.VisualStudio.Package;
using System.IO;

namespace Nucleus.DeveloperTools.VisualStudioTemplates
{
  /// <summary>
  /// IWizard implementation for Nucleus templates.  Handles Nucleus project and item templates.
  /// </summary>
	public class ProjectWizard : IWizard
	{
		public void BeforeOpeningFile(ProjectItem projectItem)
		{
		}

		public void ProjectFinishedGenerating(Project project)
		{
		}

		public void ProjectItemFinishedGenerating(ProjectItem projectItem)
		{
		}

    /// <summary>
    /// Set NUCLEUS_PATH if it is not already set.
    /// </summary>
		public void RunFinished()
		{
			if (String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("NUCLEUS_PATH")))
			{
				if (MessageBox.Show("The NUCLEUS_PATH environment variable is not set.  Nucleus build scripts require this path in order to find required resources.  Do you want to automatically set NUCLEUS_PATH?", "Set Path", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
				{
					System.Environment.SetEnvironmentVariable("NUCLEUS_PATH", System.IO.Path.GetDirectoryName(this.GetType().Assembly.Location), EnvironmentVariableTarget.User);
				}
			}
		}

		private string Get(Dictionary<string, string> replacementsDictionary, string key)
		{
			if (replacementsDictionary.ContainsKey(key))
			{
				return replacementsDictionary[key];
			}
			return "";
		}

    /// <summary>
    /// For project templates, display the wizard UI to collect additional Nucleus-related settings.  
    /// For Item templates, read the project (csproj) file and set Nucleus-related template replacement tokens so that they can be used when Visual Studio replaces
    /// tokens during item creation, and create required project folders for the relevant item template.
    /// </summary>
    /// <param name="automationObject"></param>
    /// <param name="replacementsDictionary"></param>
    /// <param name="runKind"></param>
    /// <param name="customParams"></param>
    /// <remarks>
    /// We check/create project folders for item templates because Visual Studio does not create folders when executing an item template.  The .vstemplate "CreateInPlace" 
    /// element is supposed to work for this, but instead we get a null reference exception message from Visual Studio if the folder does not exist.
    /// </remarks>
		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			_DTE projectProperties = automationObject as _DTE;

      try
			{
				if (runKind == WizardRunKind.AsNewProject)
				{
          string projectType="";

          // parse the project template file name to get the project type
					if (customParams != null && customParams.Length > 0)
					{
            projectType = System.IO.Path.GetDirectoryName(customParams[0].ToString()).Split(new char[] { '/','\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
          }

          // set default values based on what the user has entered for the project 
					string defaultExtensionName = Get(replacementsDictionary, "$safeprojectname$");
					string[] defaultExtensionNameParts = defaultExtensionName.Split('.');
					if (defaultExtensionNameParts.Length > 1)
					{
						defaultExtensionName = defaultExtensionNameParts.Last();
					}
					string defaultModelName = defaultExtensionName;

          // show the wizard form
					ProjectWizardForm projectOptionsForm = new ProjectWizardForm
					{
						ClassNameEnabled = true, //!isSimpleExtension,
						ExtensionNamespace = Get(replacementsDictionary, "$safeprojectname$"),
						ExtensionName = defaultExtensionName,
						FriendlyName = defaultExtensionName,
						PublisherName = Get(replacementsDictionary, "$registeredorganization$"),
						ModelClassName = defaultModelName,
					};

          projectOptionsForm.SetProjectType(projectType);

          if (projectOptionsForm.ShowDialog() == DialogResult.OK)
					{
            // Add custom tokens
            replacementsDictionary.Add("$nucleus.extension.namespace$", projectOptionsForm.ExtensionNamespace);

            AddExtensionNameTokens(replacementsDictionary, projectOptionsForm.ExtensionName);

            replacementsDictionary.Add("$nucleus.extension.description$", projectOptionsForm.ExtensionDescription);
						replacementsDictionary.Add("$nucleus.extension.friendlyname$", projectOptionsForm.FriendlyName);

						replacementsDictionary.Add("$nucleus.extension.model_class_name$", projectOptionsForm.ModelClassName);
						replacementsDictionary.Add("$nucleus.extension.model_class_name.camelcase$", projectOptionsForm.ModelClassName.Substring(0, 1).ToLower() + projectOptionsForm.ModelClassName.Substring(1));
            replacementsDictionary.Add("$nucleus.extension.model_class_name.lowercase$", projectOptionsForm.ModelClassName.ToLower());

            replacementsDictionary.Add("$publisher.name$", projectOptionsForm.PublisherName);
						replacementsDictionary.Add("$publisher.url$", projectOptionsForm.PublisherUrl);
						replacementsDictionary.Add("$publisher.email$", projectOptionsForm.PublisherEmail);
					}
					else
					{
						throw new WizardBackoutException();
					}
				}
				else if (runKind == WizardRunKind.AsNewItem)
				{
					string projectFile = "";
          string itemType = "";
                    
          if (customParams != null && customParams.Length > 0)
          {
            itemType = System.IO.Path.GetDirectoryName(customParams[0].ToString()).Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
          }

          // Get the active project, read the project filename and create required project folders
          Array activeProjects = (Array)projectProperties.ActiveSolutionProjects;

          if (activeProjects.Length > 0)
					{
						Project activeProj = (Project)activeProjects.GetValue(0);
						projectFile = activeProj.FileName;
            
            switch (itemType)
            {
              case "Controller":
                CheckAndCreateFolder(activeProj, "Controllers");
                break;
              case "Layout":
                CheckAndCreateFolder(activeProj, "Layouts");
                break;
              case "Container":
                CheckAndCreateFolder(activeProj, "Containers");
                break;
              case "View":
                CheckAndCreateFolder(activeProj, "Views");
                break;
            }
          }

          // read the project file to find the <ExtensionFolder> element, which contains the extension name, and add nucleus extension tokens to 
          // the replacements dictionary.
          if (!String.IsNullOrEmpty(projectFile))
					{
						System.Xml.XmlDocument projectFileXml = new System.Xml.XmlDocument();
						projectFileXml.Load(projectFile);

						System.Xml.XmlNode pluginFolderXml = projectFileXml.SelectSingleNode("//ExtensionFolder");
						if (pluginFolderXml != null)
						{
              string extensionName = pluginFolderXml.InnerText;

              AddExtensionNameTokens(replacementsDictionary, extensionName);
            }
          }
				}
			}
			catch (WizardBackoutException ex)
			{
				throw ex;
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.ToString());
			}
		}

    /// <summary>
    /// Specifies whether we should add the item from an item template to the project.
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>
		public bool ShouldAddProjectItem(string filePath)
		{
			return true;
		}

    /// <summary>
    /// Check for a required folder, and create it if it does not exist.
    /// </summary>
    /// <param name="activeProj"></param>
    /// <param name="folder"></param>
    private void CheckAndCreateFolder(Project activeProj, string folder)
    {
      Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();
      string projectPath = System.IO.Path.GetDirectoryName(activeProj.FullName);

      string fullPath = System.IO.Path.Combine(projectPath, folder);

      if (!System.IO.Directory.Exists(fullPath))
      {
        activeProj.ProjectItems.AddFolder(folder);
      }
    }

    /// <summary>
    /// Add nucleus extension name tokens to the replacements dictionary.
    /// </summary>
    /// <param name="replacementsDictionary"></param>
    /// <param name="extensionName"></param>
    private void AddExtensionNameTokens(Dictionary<string, string> replacementsDictionary, string extensionName)
    {
      replacementsDictionary.Add("$nucleus.extension.name$", extensionName);
      replacementsDictionary.Add("$nucleus.extension.name.camelcase$", extensionName.Substring(0, 1).ToLower() + extensionName.Substring(1));
      replacementsDictionary.Add("$nucleus.extension.name.lowercase$", extensionName.ToLower());

      string extensionNameSingular = extensionName;
      if (extensionNameSingular.EndsWith("s"))
      {
        extensionNameSingular = extensionNameSingular.Substring(0, extensionNameSingular.Length - 1);
      }

      replacementsDictionary.Add("$nucleus.extension.name-singular$", extensionNameSingular);
      replacementsDictionary.Add("$nucleus.extension.name-singular.camelcase$", extensionNameSingular.Substring(0, 1).ToLower() + extensionNameSingular.Substring(1));
      replacementsDictionary.Add("$nucleus.extension.name-singular.lowercase$", extensionNameSingular.ToLower());
    }
  }
}
