using DocumentFormat.OpenXml.Office2010.CustomUI;
using Nucleus.Abstractions.Managers;
using Nucleus.Abstractions.Models;
using Nucleus.Data.Common;
using Nucleus.DNN.Migration.DataProviders;
using Nucleus.DNN.Migration.MigrationEngines;
using Nucleus.DNN.Migration.Models;
using Nucleus.DNN.Migration.Models.Nucleus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration;
/// <summary>
/// Provides functions to manage database data.
/// </summary>
public class DNNMigrationManager
{
  private IDataProviderFactory DataProviderFactory { get; }
  private IFileSystemManager FileSystemManager { get; }

  private static List<MigrationEngineBase> MigrationEngines { get; } = new();
  
  public DNNMigrationManager(IDataProviderFactory dataProviderFactory, IFileSystemManager fileSystemManager)
  {
    this.DataProviderFactory = dataProviderFactory;
    this.DataProviderFactory.PreventSchemaCheck(Startup.DNN_SCHEMA_NAME);    

    this.FileSystemManager = fileSystemManager;
  }

  public void ClearMigrationEngines()
  {
    if (MigrationEngines.Any() && MigrationEngines.Any(engine => engine.State() == MigrationEngineBase.EngineStates.InProgress))
    {
      throw new InvalidOperationException("A migration operation is already in progress.");
    }

    MigrationEngines.Clear();
  }

  public MigrationEngineBase<TModel> GetMigrationEngine<TModel>()
    where TModel : Models.DNN.DNNEntity
  {    
    return MigrationEngines.Where(engine => engine as MigrationEngineBase<TModel> != null)
      .Select(engine => engine as MigrationEngineBase<TModel>)
      .FirstOrDefault();    
  }


  public List<MigrationEngineBase> GetMigrationEngines
  {
    get
    {
      return MigrationEngines;
    }
  }


  public Task<MigrationEngineBase<TModel>> CreateMigrationEngine<TModel>(IServiceProvider services, List<TModel> items)
    where TModel : Models.DNN.DNNEntity
  {
    if (MigrationEngines.Any() && MigrationEngines.Any(engine => engine.State() == MigrationEngineBase.EngineStates.InProgress))
    {
      throw new InvalidOperationException("A migration operation is already in progress.");
    }

    MigrationEngineBase<TModel> engine = services.CreateEngine<TModel>();
    engine.Init(items);
    MigrationEngines.Add(engine);

    return Task.FromResult(engine);
  }

  #region "    Migration History    "
  ///// <summary>
  ///// Create a new <see cref="Models.Migration"/> with default values.
  ///// </summary>
  ///// <param name="site"></param>
  ///// <returns></returns>
  ///// <remarks>
  ///// The new <see cref="Models.Migration"/> is not saved to the database until you call <see cref="Save(PageModule, Models.Migration)"/>.
  ///// </remarks>
  //public Models.MigrationHistory CreateNew()
  //{
  //  Models.MigrationHistory result = new();

  //  return result;
  //}

  /////// <summary>
  /////// List all <see cref="Models.Migration"/>s within the specified site.
  /////// </summary>
  /////// <param name="module"></param>
  /////// <returns></returns>
  ////public async Task<IList<Models.Migration>> List(PageModule module)
  ////{
  ////  using (IDNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<IDNNMigrationDataProvider>())
  ////  {
  ////    return await provider.List(module);
  ////  }
  ////}

  ///// <summary>
  ///// Create or update a <see cref="Models.Migration"/>.
  ///// </summary>
  ///// <param name="module"></param>
  ///// <param name="Models.Migration"></param>
  //public async Task Save(PageModule module, Models.MigrationHistory migrationHistory)
  //{
  //  using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
  //  {
  //    await provider.Save(migrationHistory);      
  //  }
  //}
  #endregion


  public async Task<Models.DNN.Version> GetDNNVersion()
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      // we must handle a null provider here, which will happen if there is no configured database schema with name "DNN".  The
      // main view shows a warning message when version = null
      if (provider == null) { return null; }

      return await provider.GetVersion();
    }
  }

  #region "    Core Entities    "
  public async Task<List<Models.DNN.Portal>> ListDNNPortals()
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPortals();
    }
  }

  public async Task<Models.DNN.RoleGroup> GetDNNRoleGroup(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetRoleGroup(id);
    }
  }

  public async Task<List<Models.DNN.RoleGroup>> ListDNNRoleGroups(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListRoleGroups(portalId);
    }
  }

  public async Task<Models.DNN.Role> GetDNNRole(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetRole(id);
    }
  }

  public async Task<List<Models.DNN.Role>> ListDNNRoles(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListRoles(portalId);
    }
  }

  public async Task<List<Models.DNN.List>> ListDNNLists(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListLists(portalId);
    }
  }

  public async Task<Models.DNN.File> GetDNNFile(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetFile(id);
    }
  }

  public async Task<Models.DNN.User> GetDNNUser(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetUser(id);
    }
  }

  public async Task<List<Models.DNN.User>> ListDNNUsers(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListUsers(portalId);
    }
  }

  public async Task<Models.DNN.Page> GetDNNPage(int id)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetPage(id);
    }
  }

  public async Task<List<Models.DNN.Page>> ListDNNPages(int portalId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListPages(portalId);
    }
  }
  #endregion 

  public async Task<Models.DNN.Modules.TextHtml> GetDnnHtmlContent(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetHtmlContent(moduleId);
    }
  }

  public async Task<List<Models.DNN.Modules.Document>> ListDnnDocuments(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.ListDocuments(moduleId);
    }
  }

  #region "    Documents    "

  /// <summary>
  /// Retrieve an existing <see cref="Document"/> from the database.
  /// </summary>
  /// <param name="module"></param>
  /// <param name="title"></param>
  /// <returns></returns>
  public async Task<Document> GetDocument(Site site, PageModule module, string title)
  {    
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      Document document = await provider.GetDocumentByTitle(module.Id, title);

      if (document != null)
      {
        try
        {
          document.File = await this.FileSystemManager.GetFile(site, document.File.Id);
        }
        catch (Exception)
        {
          document.File = null;
        }
      }

      return document;
    }
    
  }

  /// <summary>
  /// Retrieve document settings for the specified module.
  /// </summary>
  /// <param name="moduleId"></param>
  /// <returns></returns>
  public async Task<Models.DNN.Modules.DocumentsSettings> GetDnnDocumentsSettings(int moduleId)
  {
    using (DNNDataProvider provider = this.DataProviderFactory.CreateProvider<DNNDataProvider>())
    {
      return await provider.GetDocumentsSettings(moduleId);
    }
  }

  ///// <summary>
  ///// List all <see cref="Document"/>s for the specified module.
  ///// </summary>
  ///// <param name="site"></param>
  ///// <param name="module"></param>		
  ///// <returns></returns>
  //public async Task<IList<Document>> List(Site site, PageModule module)
  //{
  //  IEnumerable<Guid> results = await this.CacheManager.ModuleDocumentsCache().GetAsync(module.Id, async id =>
  //  {
  //    using (IDocumentsDataProvider provider = this.DataProviderFactory.CreateProvider<IDocumentsDataProvider>())
  //    {
  //      IList<Document> documents = await provider.List(module);

  //      return documents.Select(document => document.Id);
  //    }
  //  });

  //  return new List<Document>(await Task.WhenAll(results.Select(async id => await Get(site, id))));
  //}


  /// <summary>
  /// Create or update a <see cref="Document"/>.
  /// </summary>
  /// <param name="site"></param>
  /// <param name="document"></param>
  public async Task SaveDocument(PageModule module, Document document)
  {
    using (DNNMigrationDataProvider provider = this.DataProviderFactory.CreateProvider<DNNMigrationDataProvider>())
    {
      await provider.SaveDocument(module, document);
    }
  }


  #endregion
}
