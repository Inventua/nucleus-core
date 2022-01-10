using System;
using System.Collections.Generic;
using System.Text;

namespace Nucleus.Core.DataProviders.Abstractions
{
  /// <summary>
  /// Exception class used by the DataProvider class.
  /// </summary>
  public class GeneralException : System.Exception
  {
    public GeneralException()
    {
    }

    public GeneralException(string Message, Exception InnerException) : base(Message, InnerException)
    {
    }

  }
}
