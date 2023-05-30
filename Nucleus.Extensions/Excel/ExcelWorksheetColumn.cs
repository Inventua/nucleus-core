using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nucleus.Extensions.Excel;

/// <summary>
/// Class representing a worksheet column in an Excel export.
/// </summary>
public class ExcelWorksheetColumn
{
  /// <summary>
  /// PropertyInfo instance used when creating the column using reflection.  This value is used during export to improve
  /// performance.
  /// </summary>
  public PropertyInfo PropertyInfo { get; }

  /// <summary>
  /// If set, specifies the expression to evaluate in order to derive a value.
  /// </summary>
  public LambdaExpression Expression { get; }

  /// <summary>
  /// Column name.  This is the unique identifier for the column.
  /// </summary>
  public string Name { get; }

  /// <summary>
  /// Caption used in the heading row.
  /// </summary>
  public string Caption { get; set; }

  /// <summary>
  /// Current cell index.
  /// </summary>
  public int? Index { get; set; }

  /// <summary>
  /// Excel data type for the column.
  /// </summary>
  public XLDataType? DataType { get; set; }

  /// <summary>
  /// Constructor used when you want to specify the name, caption and data type.
  /// </summary>
  /// <param name="name"></param>
  /// <param name="caption"></param>
  /// <param name="dataType"></param>

  public ExcelWorksheetColumn(string name, string caption, XLDataType? dataType)
  {
    Name = name;
    Caption = caption;
    DataType = dataType;
  }

  /// <summary>
  /// Constructor used when you want to specify the name, caption and data type, and an expression to evaluate the data to output.
  /// </summary>
  /// <param name="name"></param>
  /// <param name="caption"></param>
  /// <param name="dataType"></param>
  /// <param name="expression"></param>

  public ExcelWorksheetColumn(string name, string caption, XLDataType? dataType, LambdaExpression expression)
  {
    Name = name;
    Caption = caption;
    DataType = dataType;
    Expression = expression;
  }

  /// <summary>
  /// Constructed used to auto-detect column properties.
  /// </summary>
  /// <param name="propertyInfo"></param>

  public ExcelWorksheetColumn(PropertyInfo propertyInfo)
  {
    PropertyInfo = propertyInfo;
    Name = propertyInfo.Name;
    Caption = propertyInfo.Name;
    DataType = ExcelWorksheet.GetXLDataType(propertyInfo.PropertyType);
  }
}
