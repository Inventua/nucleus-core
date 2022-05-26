using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TemplateWizard;
using System.Windows.Forms;
using EnvDTE;

namespace Nucleus.DeveloperTools.VisualStudioTemplates
{
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

		public void RunFinished()
		{
			// Set NUCLEUS_PATH if it is not already set
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

		public void RunStarted(object automationObject, Dictionary<string, string> replacementsDictionary, WizardRunKind runKind, object[] customParams)
		{
			Microsoft.VisualStudio.Shell.ThreadHelper.ThrowIfNotOnUIThread();

			_DTE projectProperties = automationObject as _DTE;

			try
			{
				if (runKind == WizardRunKind.AsNewProject)
				{
					Boolean isSimpleExtension = false;

					if (customParams != null && customParams.Length > 0)
					{
						isSimpleExtension = !(customParams[0].ToString().Contains("Complex Extension"));
					}

					string defaultExtensionName = Get(replacementsDictionary, "$safeprojectname$");
					string[] defaultExtensionNameParts = defaultExtensionName.Split('.');
					if (defaultExtensionNameParts.Length > 1)
					{
						defaultExtensionName = defaultExtensionNameParts[defaultExtensionNameParts.Length - 1];
					}
					string defaultModelName = defaultExtensionName;

					ProjectWizardForm projectOptionsForm = new ProjectWizardForm
					{
						ClassNameEnabled = !isSimpleExtension,
						ExtensionNamespace = Get(replacementsDictionary, "$safeprojectname$"),
						ExtensionName = defaultExtensionName,
						FriendlyName = defaultExtensionName,
						PublisherName = Get(replacementsDictionary, "$registeredorganization$"),
						ModelClassName = defaultModelName,
					};
									
					if (projectOptionsForm.ShowDialog() == DialogResult.OK)
					{
						// Add custom parameters.
						replacementsDictionary.Add("$nucleus_extension_name$", projectOptionsForm.ExtensionName);
						replacementsDictionary.Add("$nucleus_extension_namespace$", projectOptionsForm.ExtensionNamespace);
						replacementsDictionary.Add("$nucleus_extension_name_lcase$", projectOptionsForm.ExtensionName.Substring(0, 1).ToLower() + projectOptionsForm.ExtensionName.Substring(1));
						replacementsDictionary.Add("$nucleus_extension_description$", projectOptionsForm.ExtensionDescription);
						replacementsDictionary.Add("$nucleus_extension_friendlyname$", projectOptionsForm.FriendlyName);
						replacementsDictionary.Add("$nucleus_extension_modelname$", projectOptionsForm.ModelClassName);

						replacementsDictionary.Add("$nucleus_extension_modelname_lcase$", projectOptionsForm.ModelClassName.Substring(0, 1).ToLower() + projectOptionsForm.ModelClassName.Substring(1));

						string extensionNameSingular = projectOptionsForm.ExtensionName;
						if (extensionNameSingular.EndsWith("s"))
						{
							extensionNameSingular = extensionNameSingular.Substring(0, extensionNameSingular.Length - 1);
						}

						replacementsDictionary.Add("$nucleus_extension_name_singular$", extensionNameSingular);
						replacementsDictionary.Add("$nucleus_extension_name_singular_lcase$", extensionNameSingular.Substring(0, 1).ToLower() + extensionNameSingular.Substring(1));



						replacementsDictionary.Add("$publisher_name$", projectOptionsForm.PublisherName);
						replacementsDictionary.Add("$publisher_url$", projectOptionsForm.PublisherUrl);
						replacementsDictionary.Add("$publisher_email$", projectOptionsForm.PublisherEmail);
					}
					else
					{
						throw new WizardBackoutException();
					}
				}
				else if (runKind == WizardRunKind.AsNewItem)
				{
					string defaultArea = "";
					string areasFolder = "";
					string projectFile = "";

					Array activeProjects = (Array)projectProperties.ActiveSolutionProjects;

					if (activeProjects.Length > 0)
					{
						Project activeProj = (Project)activeProjects.GetValue(0);
						projectFile = activeProj.FileName;

						foreach (ProjectItem pi in activeProj.ProjectItems)
						{
							// Get the default area
							if (pi.Name == "Areas")
							{
								foreach (ProjectItem pi2 in pi.ProjectItems)
								{
									defaultArea = pi2.Name;
									areasFolder = pi.FileNames[0];
									break;
								}
							}
						}
					}

					if (!String.IsNullOrEmpty(projectFile))
					{
						// read the project file to find the <ExtensionFolder> element, which contains the extension name
						System.Xml.XmlDocument projectFileXml = new System.Xml.XmlDocument();
						projectFileXml.Load(projectFile);

						System.Xml.XmlNode pluginFolderXml = projectFileXml.SelectSingleNode("//ExtensionFolder");
						if (pluginFolderXml != null)
						{
							replacementsDictionary.Add("$nucleus_extension_name$", pluginFolderXml.InnerText);
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

		public bool ShouldAddProjectItem(string filePath)
		{
			return true;
		}

	}
}
