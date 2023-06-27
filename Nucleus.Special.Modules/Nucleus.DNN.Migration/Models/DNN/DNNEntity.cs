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

  [NotMapped]
  public Boolean IsSelected { get; set; } = true;

  [NotMapped]
  public Boolean CanSelect { get; set; } = true;

  [NotMapped]
  public List<Models.DNN.ValidationResult> Results { get; } = new();

  public void AddWarning(string message)
  {
    this.Results.Add(new(ValidationResult.ValidationResultTypes.Warning, message));
  }

  public void AddError(string message)
  {
    this.Results.Add(new(ValidationResult.ValidationResultTypes.Error, message));
    this.CanSelect = false;
    this.IsSelected = false;
  }
}
