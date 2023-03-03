using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static Nucleus.Extensions.Excel.ExcelWriter;

namespace Nucleus.Extensions.Excel;

/// <summary>
/// Base class for the ExcelReader and ExcelWriter classes.
/// </summary>
public abstract class ExcelWorksheet
{
    /// <summary>
    /// MIME type for Excel (xlsx) format.
    /// </summary>
    public const string MIMETYPE_EXCEL = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    /// <summary>
    /// Excel workbook being created.
    /// </summary>
    public IXLWorkbook Workbook { get; protected set; }

    /// <summary>
    /// Excel worksheet being created.
    /// </summary>
    /// <remarks>
    /// Callers can create multiple worksheets by calling WorkBook.WorkSheets.Add.
    /// </remarks>
    public IXLWorksheet Worksheet { get; set; }

    /// <summary>
    /// List of worksheet columns.
    /// </summary>
    protected List<ExcelWorksheetColumn> WorksheetColumns { get; } = new();

    /// <summary>
    /// Current row index.
    /// </summary>
    public int RowIndex { get; set; } = 1;

    /// <summary>
    /// Enumeration used by the <see cref="ExcelWriter{TModel}"/> constructor to specify whether the properties to export are 
    /// automatically detected, or whether only specified columns are included.
    /// </summary>
    /// <type>Enum</type>
    public enum Modes
    {
        /// <summary>
        /// Auto-detect columns to export and specify any columns to exclude.
        /// </summary>
        AutoDetect,

        /// <summary>
        /// Include only specified columns.
        /// </summary>
        IncludeSpecifiedPropertiesOnly,
    }

    /// <summary>
    /// Add a column and auto-detect values using reflection.
    /// </summary>
    /// <param name="propertyInfo"></param>
    /// <returns></returns>
    public ExcelWorksheetColumn AddColumn(PropertyInfo propertyInfo)
    {
        ExcelWorksheetColumn result = new(propertyInfo);
        WorksheetColumns.Add(result);
        return result;
    }

    /// <summary>
    /// Add a column.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="caption"></param>
    /// <param name="dataType"></param>
    /// <returns></returns>
    public ExcelWorksheetColumn AddColumn(string name, string caption, XLDataType dataType)
    {
        ExcelWorksheetColumn result = new(name, caption, dataType);
        WorksheetColumns.Add(result);
        return result;
    }

    /// <summary>
    /// Remove the specified column column.
    /// </summary>
    /// <param name="name"></param>
    public void RemoveColumn(string name)
    {
        ExcelWorksheetColumn column = WorksheetColumns.Where(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

        if (column != null)
        {
            WorksheetColumns.Remove(column);
        }
    }

    /// <summary>
    /// Return the spreadsheet data type which matches the specified type
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static XLDataType GetXLDataType(Type type)
    {
        if (type.IsAssignableTo(typeof(DateTime)))
        {
            return XLDataType.DateTime;
        }
        else if (type.IsAssignableTo(typeof(bool)))
        {
            return XLDataType.Boolean;
        }
        else if (type.IsAssignableTo(typeof(int)) || type.IsAssignableTo(typeof(double)) || type.IsAssignableTo(typeof(float)))
        {
            return XLDataType.Number;
        }
        else if (type.IsAssignableTo(typeof(TimeSpan)))
        {
            return XLDataType.TimeSpan;
        }
        else
        {
            return XLDataType.Text;
        }
    }


}
