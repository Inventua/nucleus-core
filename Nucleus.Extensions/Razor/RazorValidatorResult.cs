using System;
using System.Collections.Generic;

namespace Nucleus.Extensions.Razor;

/// <summary>
/// Return value for the RazorValidator class functions.
/// </summary>
public class RazorValidatorResult
{
  /// <summary>
  /// Indicates that the template was compiled successfully (true) or had errors (false)
  /// </summary>
  public Boolean Success { get; }

  /// <summary>
  /// List of error messages when Success=false.
  /// </summary>
  public IEnumerable<string> Errors { get; }

  internal RazorValidatorResult(Boolean success)
  {
    this.Success = success;
  }

  internal RazorValidatorResult(Boolean success, IEnumerable<string> errors)
  {
    this.Success = success;
    this.Errors = errors;
  }
}