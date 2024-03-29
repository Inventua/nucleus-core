﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.DNN.Migration.Models.DNN;

public class ValidationResult
{
  public enum ValidationResultTypes
  {
    Information,
    Warning,
    Error
  }

  public ValidationResultTypes Type { get; set; }
  public string Message { get; set; }

  public ValidationResult(ValidationResultTypes type, string message)
  {
    this.Type = type;
    this.Message = message;
  }

  public override string ToString()
  {
    return $"{Type}: {Message}";
  }
}
