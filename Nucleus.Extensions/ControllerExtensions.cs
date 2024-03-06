using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Mvc;

namespace Nucleus.Extensions;

/// <summary>
/// Extensions for Mvc controllers.
/// </summary>
public static class ControllerExtensions
{
  /// <summary>
  /// Enum used to specify an icon type for <see cref="PopupMessage(Controller, string, string, PopupIcons)"/>
  /// </summary>
  public enum PopupIcons
  {
    /// <summary>
    /// Information Icon
    /// </summary>
    Info,
    /// <summary>
    /// Alert Icon
    /// </summary>
    Alert,
    /// <summary>
    /// Warning Icon
    /// </summary>
    Warning,
    /// <summary>
    /// Question Icon
    /// </summary>
    Question,
    /// <summary>
    /// Error Icon
    /// </summary>
    Error
  }

  /// <summary>
  /// Return Json which triggers a popup dialog in Nucleus client-side code.
  /// </summary>
  /// <param name="controller"></param>
  /// <param name="title"></param>
  /// <param name="message"></param>
  /// <param name="icon"></param>
  /// <returns></returns>
  public static JsonResult PopupMessage(this Controller controller, string title, string message, PopupIcons icon)
  {
    return new JsonResult(new { Title = title, Message = message, Icon = icon.ToString().ToLower() });
  }
}
