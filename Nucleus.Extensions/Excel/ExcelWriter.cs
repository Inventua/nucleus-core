using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.Reflection;

namespace Nucleus.Extensions.Excel
{
  /// <summary>
  /// Class used to write (export) an Excel file from an IEnumerable of the class specified by <typeparamref name="TModel"/>.
  /// </summary>
  /// <typeparam name="TModel">Type being exported.</typeparam>
  public class ExcelWriter<TModel> : ExcelWriter
  {
    /// <summary>
    /// Use this constructor if you want to set up columns manually using the <see cref="ExcelWorksheet.AddColumn(PropertyInfo)"/> method.
    /// </summary>
    public ExcelWriter() : this(Modes.IncludeSpecifiedPropertiesOnly, Array.Empty<string>()) { }

    /// <summary>
    /// Use this constructor to automatically set up columns.
    /// </summary>
    /// <param name="mode">Specifies whether to include or exclude the properties in the properies argument.</param>
    /// <param name="properties"></param>
    /// <remarks>
    /// When <paramref name="mode"/> is <see cref="ExcelWorksheet.Modes.AutoDetect"/>, all properties of <typeparamref name="TModel"/> are automatically included 
    /// in the output, except for those listed in <paramref name="properties"/>.  When <paramref name="mode"/> is <see cref="ExcelWorksheet.Modes.IncludeSpecifiedPropertiesOnly"/>,
    /// only the properties listed in <paramref name="properties"/> are included in the output.
    /// </remarks>
    public ExcelWriter(Modes mode, params string[] properties)
    {
      Workbook = new XLWorkbook();
      Worksheet = Workbook.Worksheets.Add(typeof(TModel).Name);

      PropertyInfo[] typeProperties = typeof(TModel).GetProperties();

      if (mode == Modes.IncludeSpecifiedPropertiesOnly)
      {
        // Don't auto-detect properties, only include those in the properties argument				
        foreach (string propertyName in properties)
        {
          PropertyInfo prop = typeProperties.Where(prop => prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
          if (prop == null)
          {
            throw new ArgumentException($"'{propertyName}' is not a property of the '{typeof(TModel).Name}' class.");
          }
          else
          {
            if (!prop.CanRead)
            {
              throw new ArgumentException($"'{typeof(TModel).Name}.{propertyName}' is not a readable property.");
            }
            else
            {
              AddColumn(prop);
            }
          }
        }
      }
      else
      {
        // auto-detect properties and exclude those in the properties argument
        foreach (PropertyInfo prop in typeProperties)
        {
          if (!properties.Contains(prop.Name, StringComparer.OrdinalIgnoreCase) && prop.CanRead)
          {
            AddColumn(prop);
          }
        }
      }
    }

    /// <summary>
    /// Add a column using an expression to specify the column.
    /// </summary>
    /// <param name="expression"></param>
    /// <returns></returns>
    public ExcelWorksheetColumn AddColumn<TType>(Expression<Func<TModel, TType>> expression)
    {
      var memberExpression = expression.Body as MemberExpression;
      if (memberExpression == null)
      {
        throw new ArgumentException("Expression must be a member expression.");
      }

      string name = memberExpression.Member.Name;

      ExcelWorksheetColumn result = new(name, name, null);
      WorksheetColumns.Add(result);

      return result;
    }

    /// <summary>
    /// Add a column with an expression which is called to determine the output value.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="caption"></param>
    /// <param name="dataType"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public ExcelWorksheetColumn AddColumn<TValue>(string name, string caption, XLDataType dataType, Expression<Func<TModel, TValue>> expression)
    {
      ExcelWorksheetColumn result = new(name, caption, dataType, expression);
      WorksheetColumns.Add(result);

      return result;
    }

    /// <summary>
    /// Add a column with an expression which is called to determine the output value.  This overload is for lambda expressions which do
    /// not reference the object being exported.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="caption"></param>
    /// <param name="dataType"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public ExcelWorksheetColumn AddColumn<TValue>(string name, string caption, XLDataType dataType, Expression<Func<TValue>> expression)
    {
      ExcelWorksheetColumn result = new(name, caption, dataType, expression);
      WorksheetColumns.Add(result);

      return result;
    }


    /// <summary>
    /// Automatically export items to the current worksheet.
    /// </summary>
    /// <param name="items"></param>
    /// <returns></returns>
    public void Export(IEnumerable<TModel> items)
    {
      WriteHeadingRow();

      foreach (TModel item in items)
      {
        List<object> values = new();
        foreach (ExcelWorksheetColumn column in WorksheetColumns)
        {
          if (column.Expression != null)
          {
            if (column.Expression.Parameters.Any())
            {
              values.Add(Expression.Lambda(column.Expression.Body, column.Expression.Parameters).Compile().DynamicInvoke(item));
            }
            else
            {
              values.Add(Expression.Lambda(column.Expression.Body, column.Expression.Parameters).Compile().DynamicInvoke());
            }
          }
          else if (column.PropertyInfo == null)
          {
            values.Add(typeof(TModel).GetProperty(column.Name).GetValue(item, null));
          }
          else
          {
            values.Add(column.PropertyInfo.GetValue(item, null));
          }
        }

        WriteRow(values);
      }

      AdjustColumnWidths();
    }
  }

  /// <summary>
  /// Non-generic exporter.
  /// </summary>
  public class ExcelWriter : ExcelWorksheet
  {

    /// <summary>
    /// Constructor, used by the generic ExcelWriter&lt;T&gt; class. 
    /// </summary>
    public ExcelWriter() { }

    /// <summary>
    /// Use this constructor if you want to create columns and output rows manually.  The generic <see cref="ExcelWriter{TModel}"/> class
    /// can auto-detect columns.
    /// </summary>
    /// <param name="worksheet"></param>
    public ExcelWriter(IXLWorksheet worksheet)
    {
      Workbook = worksheet.Workbook;
      Worksheet = worksheet;
    }

    /// <summary>
    /// Return the spreadsheet as a stream.
    /// </summary>
    /// <returns></returns>
    public System.IO.Stream GetOutputStream()
    {
      System.IO.MemoryStream output = new();
      Workbook.SaveAs(output);
      output.Position = 0;
      return output;
    }



    /// <summary>
    /// Write the heading row.
    /// </summary>
    public void WriteHeadingRow()
    {
      int cellIndex = 1;

      foreach (ExcelWorksheetColumn column in WorksheetColumns)
      {
        // We set the *column* data type rather than each cell, because it results in a smaller output file.  
        // Individual cells data types can override this setting.
        if (column.DataType.HasValue)
        {
          // Set the column format to the specified column format, if specifed
          Worksheet.Columns(cellIndex, cellIndex).SetDataType(column.DataType.Value);
        }
        else
        {
          // If not specified, set the default format to text
          Worksheet.Columns(cellIndex, cellIndex).SetDataType(XLDataType.Text);
        }

        // Set all cells to left-aligned
        Worksheet.Columns(cellIndex, cellIndex).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

        Worksheet.Cell(RowIndex, cellIndex).SetDataType(XLDataType.Text);
        // stop excel from "interpreting" the value and mangling data
        Worksheet.Cell(RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
        Worksheet.Cell(RowIndex, cellIndex).SetValue(column.Caption);

        cellIndex++;
      }

      Worksheet.Row(RowIndex).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
      Worksheet.Row(RowIndex).Style.Font.SetBold(true);
      Worksheet.SheetView.FreezeRows(1);

      RowIndex++;
    }

    /// <summary>
    /// Write the specified values to the next row of the current worksheet.
    /// </summary>
    /// <param name="values"></param>
    /// <exception cref="ArgumentException"></exception>
    public void WriteRow(params object[] values)
    {
      WriteRow((IEnumerable<object>)values);
    }

    /// <summary>
    /// Write the specified values to the next row of the current worksheet.
    /// </summary>
    /// <param name="values"></param>
    /// <exception cref="ArgumentException"></exception>
    public void WriteRow(IEnumerable<object> values)
    {
      int cellIndex = 1;

      if (values.Count() != WorksheetColumns.Count)
      {
        throw new ArgumentException("The number of values must match the number of columns.", nameof(values));
      }

      foreach (object value in values)
      {
        bool customValueSet = false;
        if (WorksheetColumns[cellIndex - 1].DataType.HasValue)
        {
          // if the column already has a type, we don't need to specify it for the cell - specifying a type for every
          // cell makes the output file larger.
        }
        else
        {
          if (value is DateTime)
          {
            Worksheet.Cell(RowIndex, cellIndex).SetDataType(XLDataType.DateTime);
          }
          else if (value is bool)
          {
            Worksheet.Cell(RowIndex, cellIndex).SetDataType(XLDataType.Boolean);
          }
          else if (value is int || value is double || value is float)
          {
            Worksheet.Cell(RowIndex, cellIndex).SetDataType(XLDataType.Number);
          }
          else if (value is TimeSpan)
          {
            Worksheet.Cell(RowIndex, cellIndex).SetDataType(XLDataType.TimeSpan);
          }
          else if (value is System.Collections.IList)
          {
            // stop excel from "interpreting" the value and mangling data
            Worksheet.Cell(RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
            Worksheet.Cell(RowIndex, cellIndex).SetValue(ListToString(value as System.Collections.IList));

            customValueSet = true;
          }
          else
          {
            // We don't need to set the cell format to text, because the default is text

            // stop excel from "interpreting" the value and mangling data
            Worksheet.Cell(RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
            SetValue(cellIndex, value);
            //this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(value.ToString());

            customValueSet = true;
          }
        }

        if (!customValueSet)
        {
          if (value is System.Collections.IList)
          {
            // stop excel from "interpreting" the value and mangling data
            Worksheet.Cell(RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
            Worksheet.Cell(RowIndex, cellIndex).SetValue(ListToString(value as System.Collections.IList));
          }
          else
          {
            SetValue(cellIndex, value);
            //this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(value.ToString());
          }
        }

        cellIndex++;
      }

      RowIndex++;
    }

    private void SetValue(int cellIndex, object value)
    {
      if (value != null)
      {
        if (value.ToString().Length > 32767)
        {
          Worksheet.Cell(RowIndex, cellIndex).SetValue(value.ToString()[..32767]);
        }
        else
        {
          Worksheet.Cell(RowIndex, cellIndex).SetValue(value.ToString());
        }
      }
      else
      {
        Worksheet.Cell(RowIndex, cellIndex).SetValue("");
      }
    }

    /// <summary>
    /// Convert a list of items to a comma-separated string.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    private string ListToString(System.Collections.IList value)
    {
      if (value == null) return "";

      Type valueType = value.GetType().GenericTypeArguments.FirstOrDefault();

      if (valueType != null)
      {
        // Use a name property, if one exists
        PropertyInfo prop = valueType.GetProperty("Name");
        if (prop != null && prop.CanRead)
        {
          List<string> listValues = new();
          foreach (object valueObj in value)
          {
            listValues.Add(prop.GetValue(valueObj, null).ToString());
          }

          return string.Join(',', listValues.OrderBy(x => x));
        }
      }

      return value.ToString();
    }

    /// <summary>
    /// Automatically expand column widths to fit content.
    /// </summary>
    public void AdjustColumnWidths()
    {
      Worksheet.Columns().AdjustToContents();
    }
  }
}
