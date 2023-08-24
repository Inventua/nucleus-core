using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Nucleus.DNN.Migration.Models.DNN;

public abstract class DNNEntity
{
  public abstract int Id();
  public abstract string DisplayName();


  [NotMapped]
  public Boolean IsSelected { get; set; } = true;

  [NotMapped]
  public Boolean CanSelect { get; set; } = true;

  [NotMapped]
  public List<Models.DNN.ValidationResult> Results { get; } = new();

  public void AddInformation(string message)
  {
    this.Results.Add(new(ValidationResult.ValidationResultTypes.Information, message));
  }

  public void AddWarning(string message)
  {
    this.Results.Add(new(ValidationResult.ValidationResultTypes.Warning, message));
  }

  public void AddError(string message)
  {
    // the error messages returned by the data provider system are aimed at users who are entering in a new value.  Parse and replace
    // key phrases to make them make more sense in the context of import/migration.
    message = message.Replace("The name that you entered", "This name");
    int position = message.IndexOf("Please enter a unique");
    if (position > 0)
    {
      message = message[..position];
    }
    if (!message.TrimEnd().EndsWith('.'))
    {
      message += ".";
    }

    this.Results.Add(new(ValidationResult.ValidationResultTypes.Error, message));
    
  }

  public void PreventSelection()
  {
    this.CanSelect = false;
    this.IsSelected = false;
  }
}
