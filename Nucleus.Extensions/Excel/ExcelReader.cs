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
  /// Class used to read Excel files into an IEnumerable of the class specified by <typeparamref name="TModel"/>.
  /// </summary>
  /// <typeparam name="TModel">Type being imported.</typeparam>
  public class ExcelReader<TModel> : ExcelReader
    where TModel : class, new()
  {
    /// <summary>
    /// Use this constructor if you want to set up columns manually using the <see cref="ExcelWorksheet.AddColumn(PropertyInfo)"/> method.
    /// </summary>
    public ExcelReader(System.IO.Stream input) : this(input, Modes.IncludeSpecifiedPropertiesOnly, Array.Empty<string>()) { }

    /// <summary>
    /// Use this constructor to automatically set up columns.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="mode">Specifies whether to include or exclude the properties in the properies argument.</param>
    /// <param name="properties"></param>
    /// <remarks>
    /// When <paramref name="mode"/> is <see cref="ExcelWorksheet.Modes.AutoDetect"/>, all properties of <typeparamref name="TModel"/> are automatically included 
    /// in the output, except for those listed in <paramref name="properties"/>.  When <paramref name="mode"/> is <see cref="ExcelWorksheet.Modes.IncludeSpecifiedPropertiesOnly"/>,
    /// only the properties listed in <paramref name="properties"/> are included in the output.
    /// </remarks>
    public ExcelReader(System.IO.Stream input, Modes mode, params string[] properties) : base(input)
    {
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
            if (!prop.CanWrite)
            {
              throw new ArgumentException($"'{typeof(TModel).Name}.{propertyName}' is not a writable property.");
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
    /// Add a column with an expression which is called to set the input value.
    /// </summary>
    /// <param name="name"></param>
    /// <param name="caption"></param>
    /// <param name="dataType"></param>
    /// <param name="expression"></param>
    /// <returns></returns>
    public ExcelWorksheetColumn AddColumn<TValue>(string name, string caption, XLDataType dataType, Action<TModel, TValue> expression)
    {
      ExcelWorksheetColumn result = new(name, caption, dataType, (TModel model, TValue value) => expression.Invoke(model, value));
      WorksheetColumns.Add(result);

      return result;
    }

    /// <summary>
    /// Automatically read items from the current worksheet.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TModel> Import()
    {
      List<TModel> items = new();
      long lastRow = Worksheet.LastRowUsed().RowNumber();

      ReadHeadingRow();

      while (RowIndex <= lastRow)
      {
        object[] values = ReadRow();
        TModel item = new();

        foreach (ExcelWorksheetColumn column in WorksheetColumns)
        {
          if (column.Index.HasValue)
          {
            // ClosedXML/Excel columns start at 1, arrays start at zero, so we need to -1 for the array index
            object value = values[column.Index.Value - 1];

            if (column.Expression != null)
            {
              if (column.Expression.Parameters.Any())
              {
                Expression.Lambda(column.Expression.Body, column.Expression.Parameters).Compile().DynamicInvoke(item, value);
              }
              else
              {
                throw new InvalidOperationException($"The expression for column '{column.Name}' is invalid.");
              }
            }
            else if (column.PropertyInfo == null)
            {
              typeof(TModel).GetProperty(column.Name).SetValue(item, value);
            }
            else
            {
              column.PropertyInfo.SetValue(item, value);
            }
          }
        }

        items.Add(item);
      }

      return items;
    }
  }

  /// <summary>
  /// Non-generic exporter.
  /// </summary>
  public class ExcelReader : ExcelWorksheet
  {
    /// <summary>
    /// Constructor, used by the generic ExcelReader&lt;T&gt; class. 
    /// </summary>
    public ExcelReader(System.IO.Stream input)
    {
      SetInputStream(input);
    }

    /// <summary>
    /// Use this constructor if you want to create columns and output rows manually.  The generic <see cref="ExcelReader{TModel}"/> class
    /// can auto-detect columns.
    /// </summary>
    /// <param name="worksheet"></param>
    public ExcelReader(IXLWorksheet worksheet)
    {
      Workbook = worksheet.Workbook;
      Worksheet = worksheet;
    }

    /// <summary>
    /// Read a spreadsheet from the specified stream.
    /// </summary>
    /// <returns></returns>
    public void SetInputStream(System.IO.Stream stream)
    {
      Workbook = new XLWorkbook(stream);
      Worksheet = Workbook.Worksheets.First();
    }

    /// <summary>
    /// Read the heading row and set the row index to the first data row.
    /// </summary>
    public void ReadHeadingRow()
    {
      int cellIndex = 1;

      foreach (IXLCell cell in Worksheet.Row(1).Cells())
      {
        ExcelWorksheetColumn column = WorksheetColumns
          .Where(col => col.Caption.Equals(cell.Value.ToString(), StringComparison.OrdinalIgnoreCase))
          .FirstOrDefault();

        if (column != null)
        {
          column.Index = cellIndex;
        }

        cellIndex++;
      }

      RowIndex = 2;
    }

    /// <summary>
    /// Read the next row of the current worksheet and return an object array containing the values.
    /// </summary>
    /// <exception cref="ArgumentException"></exception>
    public object[] ReadRow()
    {
      List<object> values = new();

      foreach (ExcelWorksheetColumn column in WorksheetColumns)
      {
        if (column.Index.HasValue)
        {
          values.Add(Worksheet.Cell(RowIndex, column.Index.Value).Value);
        }
      }

      RowIndex++;

      return values.ToArray();
    }
  }
}
