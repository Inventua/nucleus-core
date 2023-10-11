using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nucleus.Extensions;
using Nucleus.Abstractions.Models.FileSystem;
using Nucleus.Abstractions.FileSystemProviders;
using Markdig.Helpers;

namespace Nucleus.DNN.Migration.MigrationEngines.ModuleContent;

public class DocumentsModuleContentMigration : ModuleContentMigrationBase
{
  private DNNMigrationManager DnnMigrationManager { get; }
  private IListManager ListManager { get; }
  private IFileSystemManager FileSystemManager { get; }
  private ISiteManager SiteManager { get; }
  
  private const string MODULESETTING_CATEGORYLIST_ID = "documents:categorylistid";
  private const string MODULESETTING_DEFAULTFOLDER_ID = "documents:defaultfolder:provider-id";
  private const string MODULESETTING_ALLOWSORTING = "documents:allowsorting";
  private const string MODULESETTING_LAYOUT = "documents:layout";

  private const string MODULESETTING_SHOW_CATEGORY = "documents:show:category";
  private const string MODULESETTING_SHOW_SIZE = "documents:show:size";
  private const string MODULESETTING_SHOW_MODIFIEDDATE = "documents:show:modifieddate";
  private const string MODULESETTING_SHOW_DESCRIPTION = "documents:show:description";

  public DocumentsModuleContentMigration(DNNMigrationManager dnnMigrationManager, ISiteManager siteManager, IListManager listManager, IFileSystemManager fileSystemManager)
  {
    this.ListManager = listManager;
    this.SiteManager = siteManager;
    this.FileSystemManager = fileSystemManager;
    this.DnnMigrationManager = dnnMigrationManager;
  }

  public override Guid ModuleDefinitionId => new("28df7ff3-6407-459e-8608-c1ef4181807c");

  public override string ModuleFriendlyName => "Documents";

  public override Boolean IsMatch(DesktopModule desktopModule)
  {
    string[] matches = { "documents", "dnn_documents" };

    return matches.Contains(desktopModule.ModuleName, StringComparer.OrdinalIgnoreCase);
  }

  public override async Task MigrateContent(Models.DNN.Page dnnPage, Models.DNN.PageModule dnnModule, Abstractions.Models.Page newPage, Abstractions.Models.PageModule newModule, Dictionary<int, Guid> createdPagesKeys)
  {
    Site site = await this.SiteManager.Get(newPage.SiteId);
    Nucleus.Abstractions.Portable.IPortable portable = this.DnnMigrationManager.GetPortableImplementation(this.ModuleDefinitionId);

    Nucleus.Abstractions.Models.List categoriesList = null;
    FileSystemProviderInfo fileSystemProvider = this.FileSystemManager.ListProviders().FirstOrDefault();

    // migrate settings
    Models.DNN.Modules.DocumentsSettings settings = await this.DnnMigrationManager.GetDnnDocumentsSettings(dnnModule.ModuleId);

    if (settings.UseCategoriesList == true && !String.IsNullOrEmpty(settings.CategoriesListName))
    {
      categoriesList = (await this.ListManager.List(site))
          .Where(list => list.Name.Equals(settings.CategoriesListName))
          .FirstOrDefault();

      if (categoriesList != null)
      {
        newModule.ModuleSettings.Set(MODULESETTING_CATEGORYLIST_ID, categoriesList.Id);
      }
      else
      {
        dnnPage.AddWarning($"Unable to set the category list setting for documents module '{dnnModule.ModuleTitle}' because the list '{settings.CategoriesListName}' was not found in Nucleus.");
      }
    }

    newModule.ModuleSettings.Set(MODULESETTING_ALLOWSORTING, settings.AllowUserSort);

    if (!String.IsNullOrEmpty(settings.DefaultFolder))
    {
      Nucleus.Abstractions.Models.FileSystem.Folder defaultFolder = await this.FileSystemManager.GetFolder(site, fileSystemProvider.Key, RemoveTrailingDelimiter(settings.DefaultFolder));
      if (defaultFolder != null)
      {
        newModule.ModuleSettings.Set(MODULESETTING_DEFAULTFOLDER_ID, settings.DefaultFolder);
      }
      else
      {
        dnnPage.AddWarning($"Unable to set the default folder setting for documents module '{dnnModule.ModuleTitle}'.  The default folder '{settings.DefaultFolder}' was not found.");        
      }
    }

    string[] displayColumns = settings.DisplayColumns.Split(',', StringSplitOptions.RemoveEmptyEntries);
    foreach (string column in displayColumns)
    {
      string[] values = column.Split(';');

      if (values.Length == 2)
      {
        string columnName = values[0];
        if (Boolean.TryParse(values[1], out Boolean showColumn))
        {
          switch (columnName)
          {
            case "Category":
              newModule.ModuleSettings.Set(MODULESETTING_SHOW_CATEGORY, showColumn);
              break;

            case "Size":
              newModule.ModuleSettings.Set(MODULESETTING_SHOW_SIZE, showColumn);
              break;

            case "ModifiedDate":
              newModule.ModuleSettings.Set(MODULESETTING_SHOW_MODIFIEDDATE, showColumn);
              break;

            case "Description":
              newModule.ModuleSettings.Set(MODULESETTING_SHOW_DESCRIPTION, showColumn);
              break;
          }
        }
      }
    }

    List<Models.DNN.Modules.Document> contentSource = await this.DnnMigrationManager.ListDnnDocuments(dnnModule.ModuleId);
    foreach (Nucleus.DNN.Migration.Models.DNN.Modules.Document dnnDocument in contentSource)
    {
      Nucleus.Abstractions.Models.FileSystem.File newDocumentFile = null;
      Boolean doSave = true;

      if (dnnDocument.Url.StartsWith("FileID=", StringComparison.OrdinalIgnoreCase))
      {
        // document is a file
        Models.DNN.File dnnDocumentFile = await this.DnnMigrationManager.GetDnnFile(Int32.Parse(dnnDocument.Url.Replace("fileid=", "", StringComparison.OrdinalIgnoreCase)));

        try
        {
          newDocumentFile = await this.FileSystemManager.GetFile(site, fileSystemProvider.Key, dnnDocumentFile.Path());          
        }
        catch (System.IO.FileNotFoundException)
        {
          dnnPage.AddWarning($"Document '{dnnDocument.Title}' in documents module '{dnnModule.ModuleTitle}' was not migrated because its file '{dnnDocumentFile.Path()}' could not be found.");
          doSave = false;
        }
      }
      else
      {
        // unsupported type.  The Nucleus documents module does not support Urls/anything but files
        dnnPage.AddWarning($"Document '{dnnDocument.Title}' in documents module '{dnnModule.ModuleTitle}' was not migrated because it does not reference a file.");
        doSave = false;
      }

      if (doSave)
      {
        object newDocument = new
        {
          Title = dnnDocument.Title,
          Description = dnnDocument.Description,
          Category = categoriesList?.Items
          .Where(item => item.Name.Equals(dnnDocument.Category, StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault(),
          File = newDocumentFile,
          SortOrder = dnnDocument.SortOrderIndex
        };

        await portable.Import(newModule, new Nucleus.Abstractions.Portable.PortableContent( "urn:nucleus:entities:document", newDocument ));
      }
    };
  }

  private string RemoveTrailingDelimiter(string path)
  {
    if (path.EndsWith('/') || path.EndsWith('\\'))
    {
      return path[..^1];
    }
    return path;
  }
}

