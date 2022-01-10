using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Core.DataProviders.Abstractions
{
  /// <summary>
  /// Exception class used by the DataProvider class.
  /// </summary>
  public class ConstraintException : System.Data.Common.DbException
  {
    public string FieldName { get; }
    //public DataException()
    //{
    //}

    public ConstraintException(string fieldName, string message, Exception innerException) : base(message, innerException)
    {
      this.FieldName = fieldName;
    }

    public Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary ModelState
		{
      get
			{
        Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary result = new();
        result.AddModelError(this.FieldName, this.Message);
        return result;
			}
		}
  }
}
