using Nucleus.Abstractions.Managers;
using Nucleus.DNN.Migration.Models.DNN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.MigrationEngines;

public class ListMigration : MigrationEngineBase<Models.DNN.List>
{
  private IListManager ListManager { get; }
  
  private Nucleus.Abstractions.Models.Context Context { get; }
  private DNNMigrationManager MigrationManager { get; }

  public ListMigration(Nucleus.Abstractions.Models.Context context, DNNMigrationManager migrationManager, IListManager listManager) : base("Migrating Lists")
  {
    this.MigrationManager = migrationManager;
    this.Context = context;
    this.ListManager = listManager; 
  }

  public override async Task Migrate(Boolean updateExisting)
  {    
    foreach (List list in this.Items)
    {
      if (list.CanSelect && list.IsSelected)
      {
        try
        {
          Nucleus.Abstractions.Models.List newList = null;
          
          if (updateExisting)
          {
            newList = (await this.ListManager.List(this.Context.Site))
              .Where(existingList => existingList.Name.Equals(list.ListName, StringComparison.OrdinalIgnoreCase))
              .FirstOrDefault();
          }

          if (newList == null)
          {
            newList = await this.ListManager.CreateNew();
            newList.Items = new();
          }

          newList.Name = list.ListName;
          newList.Description = "";

          foreach (var dnnListItem in list.ListItems)
          {
            Nucleus.Abstractions.Models.ListItem newListItem = null;

            if (updateExisting)
            {
              newListItem = newList.Items.Where(listItem => listItem.Value.Equals(dnnListItem.Value))
                .FirstOrDefault();
            }

            if (newListItem == null)
            {
              newListItem = new();
              newList.Items.Add(newListItem);
            }

            newListItem.Value = dnnListItem.Value;
            newListItem.Name = dnnListItem.Text;
          }
          
          await this.ListManager.Save(this.Context.Site, newList);
        }
        catch (Exception ex)
        {
          list.AddError($"Error importing list '{list.ListName}': {ex.Message}");
        }
        this.Progress();
      }
      else
      {
        list.AddInformation($"List '{list.ListName}' was not selected for import.");
      }
    }

    this.SignalCompleted();
  }

  public override Task Validate()
  {
    foreach (List list in this.Items)
    {
      string[] RESERVED_ROLES = { "Country", "Currency", "DataType", "DisplayPosterLocation", "ForumThreadRate", "Frequency", "Processor", "Region", "Site Log Reports", "TrackingDuration", "BannedPasswords", "BannedPasswords-0", "ProfanityFilter", "ProfanityFilter-0" };
      if (list.SystemList || RESERVED_ROLES.Contains(list.ListName, StringComparer.OrdinalIgnoreCase)) 
      {
        list.AddError($"'{list.ListName}' is a reserved/special list in DNN and will not be migrated.");
      }

      string[] EXCLUDED_ROLES = { "Installer" };
      if (EXCLUDED_ROLES.Contains(list.ListName, StringComparer.OrdinalIgnoreCase))
      {
        list.AddWarning($"'{list.ListName}' is typically a system list in DNN.  It has been un-selected by default, but you can choose to migrate it.");
        list.IsSelected = false;
      }


      if (!list.ListItems.Any())
      {
        list.AddInformation($"'{list.ListName}' doesn't have any list items.");
        list.IsSelected = false;
      }
    }

    return Task.CompletedTask;
  }
}
