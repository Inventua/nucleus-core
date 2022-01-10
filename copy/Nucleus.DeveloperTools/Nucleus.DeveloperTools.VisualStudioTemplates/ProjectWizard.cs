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

			DTE projectProperties = automationObject as DTE;

			try
			{
				if (runKind == WizardRunKind.AsNewProject)
				{
					ProjectWizardForm ProjectOptionsForm = new ProjectWizardForm();

					ProjectOptionsForm.ExtensionNamespace = Get(replacementsDictionary, "$projectname$");
					ProjectOptionsForm.ExtensionName = Get(replacementsDictionary, "$projectname$");
					ProjectOptionsForm.PublisherName = Get(replacementsDictionary, "$registeredorganization$");


					if (ProjectOptionsForm.ShowDialog() == DialogResult.OK)
					{
						// Add custom parameters.
						replacementsDictionary.Add("$nucleus_extension_name$", ProjectOptionsForm.ExtensionName);
						replacementsDictionary.Add("$nucleus_extension_namespace$", ProjectOptionsForm.ExtensionNamespace);
						replacementsDictionary.Add("$nucleus_extension_name_lcase$", ProjectOptionsForm.ExtensionName.Substring(0, 1).ToLower() + ProjectOptionsForm.ExtensionName.Substring(1));
						replacementsDictionary.Add("$nucleus_extension_description$", ProjectOptionsForm.ExtensionDescription);
						replacementsDictionary.Add("$nucleus_extension_friendlyname$", ProjectOptionsForm.FriendlyName);

						replacementsDictionary.Add("$publisher_name$", ProjectOptionsForm.PublisherName);
						replacementsDictionary.Add("$publisher_url$", ProjectOptionsForm.PublisherUrl);
						replacementsDictionary.Add("$publisher_email$", ProjectOptionsForm.PublisherEmail);
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

					// Determine whether we are running the RazorView or RazorPage template, set appropriate replacements
					//if (customParams.Length > 0)
					//{
					//	if (((string)customParams[0]).EndsWith("razorview.vstemplate", StringComparison.OrdinalIgnoreCase))
					//	{
					//		// Razor view template
					//		string areaName = "";

					//		// Check that the user has selected an Area, if not, auto-select the first one
					//		if (replacementsDictionary.ContainsKey("$rootnamespace$"))
					//		{
					//			if (replacementsDictionary["$rootnamespace$"].ToLower().Contains(".areas."))
					//			{
					//				// An area folder is selected
					//				Boolean isNext = false;
					//				string[] pathParts = replacementsDictionary["$rootnamespace$"].Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
					//				foreach (string part in pathParts)
					//				{
					//					if (isNext)
					//					{
					//						areaName = part;
					//						break;
					//					}

					//					if (part.ToLower() == "areas")
					//					{
					//						isNext = true;
					//					}
					//				}
					//				//areaName = replacementsDictionary["$rootnamespace$"].Substring(replacementsDictionary["$rootnamespace$"].LastIndexOf(".") + 1);
					//			}
					//			else
					//			{
					//				// An area folder is not selected
					//				areaName = defaultArea;
					//			}

					//			replacementsDictionary.Add("$nucleus_areaname$", areaName);
					//		}

					//		if (replacementsDictionary.ContainsKey("$safeitemname$"))
					//		{
					//			replacementsDictionary.Add("$nucleus_viewname$", replacementsDictionary["$safeitemname$"]);
					//		}
					//	}
					//	else if (((string)customParams[0]).EndsWith("razorpage.vstemplate", StringComparison.OrdinalIgnoreCase))
					//	{
					//		// Razor page template

					//		// the only string replacement that the razor pages template needs is $nucleus_pluginname$, which is already set (above)
					//	}
					//}
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
