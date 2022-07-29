using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using System.Reflection;

namespace Nucleus.Extensions
{
	/// <summary>
	/// Class used to write (export) an Excel file from an IEnumerable of the class specified by <typeparamref name="TModel"/>.
	/// </summary>
	/// <typeparam name="TModel">Type being exported.</typeparam>
	public class ExcelWriter<TModel> : ExcelWriter
	{
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
		/// Use this constructor if you want to set up columns manually using the <see cref="ExcelWriter.AddColumn(PropertyInfo)"/> method.
		/// </summary>
		public ExcelWriter() : this(Modes.IncludeSpecifiedPropertiesOnly, Array.Empty<string>()) { }

		/// <summary>
		/// Use this constructor to automatically set up columns.
		/// </summary>
		/// <param name="mode">Specifies whether to include or exclude the properties in the properies argument.</param>
		/// <param name="properties"></param>
		/// <remarks>
		/// When <paramref name="mode"/> is <see cref="Modes.AutoDetect"/>, all properties of <typeparamref name="TModel"/> are automatically included 
		/// in the output, except for those listed in <paramref name="properties"/>.  When <paramref name="mode"/> is <see cref="Modes.IncludeSpecifiedPropertiesOnly"/>,
		/// only the properties listed in <paramref name="properties"/> are included in the output.
		/// </remarks>
		public ExcelWriter(Modes mode, params string[] properties)
		{
			base.Workbook = new XLWorkbook();
			base.Worksheet = base.Workbook.Worksheets.Add(typeof(TModel).Name);

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
							base.AddColumn(prop);
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
						base.AddColumn(prop);
					}
				}
			}
		}

		/// <summary>
		/// Add a column using an expression to specify the column.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns></returns>
		public WorksheetColumn AddColumn<TType>(Expression<Func<TModel, TType>> expression)
		{
			var memberExpression = expression.Body as MemberExpression;
			if (memberExpression == null)
			{
				throw new ArgumentException("Expression must be a member expression.");
			}

			string name = memberExpression.Member.Name;

			WorksheetColumn result = new(name, name, null);
			this.WorksheetColumns.Add(result);

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
		public WorksheetColumn AddColumn<TValue>(string name, string caption, XLDataType dataType, Expression<Func<TModel, TValue>> expression)
		{
			WorksheetColumn result = new(name, caption, dataType, expression);
			this.WorksheetColumns.Add(result);

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
		public WorksheetColumn AddColumn<TValue>(string name, string caption, XLDataType dataType, Expression<Func<TValue>> expression)
		{
			WorksheetColumn result = new(name, caption, dataType, expression);
			this.WorksheetColumns.Add(result);

			return result;
		}


		/// <summary>
		/// Automatically export items to the current worksheet.
		/// </summary>
		/// <param name="items"></param>
		/// <returns></returns>
		public void Export(IEnumerable<TModel> items)
		{
			base.WriteHeadingRow();

			foreach (TModel item in items)
			{
				List<object> values = new();
				foreach (WorksheetColumn column in base.WorksheetColumns)
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
	public class ExcelWriter
	{
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
		protected List<WorksheetColumn> WorksheetColumns { get; } = new();

		/// <summary>
		/// Current row index.
		/// </summary>
		public int RowIndex { get; set; } = 1;

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
			this.Workbook = worksheet.Workbook;
			this.Worksheet = worksheet;
		}

		/// <summary>
		/// Return the spreadsheet as a stream.
		/// </summary>
		/// <returns></returns>
		public System.IO.Stream GetOutputStream()
		{
			System.IO.MemoryStream output = new();
			this.Workbook.SaveAs(output);
			output.Position = 0;
			return output;
		}

		/// <summary>
		/// Add a column and auto-detect values using reflection.
		/// </summary>
		/// <param name="propertyInfo"></param>
		/// <returns></returns>
		public WorksheetColumn AddColumn(PropertyInfo propertyInfo)
		{
			WorksheetColumn result = new(propertyInfo);
			this.WorksheetColumns.Add(result);
			return result;
		}

		/// <summary>
		/// Add a column.
		/// </summary>
		/// <param name="name"></param>
		/// <param name="caption"></param>
		/// <param name="dataType"></param>
		/// <returns></returns>
		public WorksheetColumn AddColumn(string name, string caption, XLDataType dataType)
		{
			WorksheetColumn result = new(name, caption, dataType);
			this.WorksheetColumns.Add(result);
			return result;
		}

		/// <summary>
		/// Remove the specified column column.
		/// </summary>
		/// <param name="name"></param>
		public void RemoveColumn(string name)
		{
			WorksheetColumn column = this.WorksheetColumns.Where(column => column.Name.Equals(name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

			if (column != null)
			{
				this.WorksheetColumns.Remove(column);
			}
		}

		/// <summary>
		/// Write the heading row.
		/// </summary>
		public void WriteHeadingRow()
		{
			int cellIndex = 1;

			foreach (WorksheetColumn column in this.WorksheetColumns)
			{
				// We set the *column* data type rather than each cell, because it results in a smaller output file.  
				// Individual cells data types can override this setting.
				if (column.DataType.HasValue)
				{
					// Set the column format to the specified column format, if specifed
					this.Worksheet.Columns(cellIndex, cellIndex).SetDataType(column.DataType.Value);
				}
				else
				{
					// If not specified, set the default format to text
					this.Worksheet.Columns(cellIndex, cellIndex).SetDataType(XLDataType.Text);
				}

				// Set all cells to left-aligned
				this.Worksheet.Columns(cellIndex, cellIndex).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);

				this.Worksheet.Cell(this.RowIndex, cellIndex).SetDataType(XLDataType.Text);
				// stop excel from "interpreting" the value and mangling data
				this.Worksheet.Cell(this.RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
				this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(column.Caption);

				cellIndex++;
			}

			this.Worksheet.Row(this.RowIndex).Style.Alignment.SetHorizontal(XLAlignmentHorizontalValues.Left);
			this.Worksheet.Row(this.RowIndex).Style.Font.SetBold(true);
			this.Worksheet.SheetView.FreezeRows(1);

			this.RowIndex++;
		}

		/// <summary>
		/// Write the specified values to the next row of the current worksheet.
		/// </summary>
		/// <param name="values"></param>
		/// <exception cref="ArgumentException"></exception>
		public void WriteRow(params object[] values)
		{
			WriteRow(values);
		}

		/// <summary>
		/// Write the specified values to the next row of the current worksheet.
		/// </summary>
		/// <param name="values"></param>
		/// <exception cref="ArgumentException"></exception>
		public void WriteRow(IEnumerable<object> values)
		{
			int cellIndex = 1;

			if (values.Count() != this.WorksheetColumns.Count)
			{
				throw new ArgumentException("The number of values must match the number of columns.", nameof(values));
			}

			foreach (object value in values)
			{
				Boolean customValueSet = false;
				if (this.WorksheetColumns[cellIndex - 1].DataType.HasValue)
				{
					// if the column already has a type, we don't need to specify it for the cell - specifying a type for every
					// cell makes the output file larger.
				}
				else
				{
					if (value is DateTime)
					{
						this.Worksheet.Cell(this.RowIndex, cellIndex).SetDataType(XLDataType.DateTime);
					}
					else if (value is Boolean)
					{
						this.Worksheet.Cell(this.RowIndex, cellIndex).SetDataType(XLDataType.Boolean);
					}
					else if (value is Int32 || value is double || value is float)
					{
						this.Worksheet.Cell(this.RowIndex, cellIndex).SetDataType(XLDataType.Number);
					}
					else if (value is TimeSpan)
					{
						this.Worksheet.Cell(this.RowIndex, cellIndex).SetDataType(XLDataType.TimeSpan);
					}
					else if (value is System.Collections.IList)
					{
						// stop excel from "interpreting" the value and mangling data
						this.Worksheet.Cell(this.RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
						this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(ListToString(value as System.Collections.IList));

						customValueSet = true;
					}
					else
					{
						// We don't need to set the cell format to text, because the default is text

						// stop excel from "interpreting" the value and mangling data
						this.Worksheet.Cell(this.RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
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
						this.Worksheet.Cell(this.RowIndex, cellIndex).Style.IncludeQuotePrefix = true;
						this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(ListToString(value as System.Collections.IList));
					}
					else
					{
						SetValue(cellIndex, value);
						//this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(value.ToString());
					}
				}

				cellIndex++;
			}

			this.RowIndex++;
		}

		private void SetValue(int cellIndex, object value)
		{
			if (value != null)
			{
				this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue(value.ToString());
			}
			else
			{
				this.Worksheet.Cell(this.RowIndex, cellIndex).SetValue("");
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
				System.Reflection.PropertyInfo prop = valueType.GetProperty("Name");
				if (prop != null && prop.CanRead)
				{
					List<String> listValues = new();
					foreach (Object valueObj in value)
					{
						listValues.Add(prop.GetValue(valueObj, null).ToString());
					}

					return String.Join(',', listValues.OrderBy(x => x));
				}
			}

			return value.ToString();
		}

		/// <summary>
		/// Automatically expand column widths to fit content.
		/// </summary>
		public void AdjustColumnWidths()
		{
			foreach (IXLColumn column in this.Worksheet.Columns(1, this.WorksheetColumns.Count))
			{
				column.AdjustToContents();
			}
		}

		/// <summary>
		/// Return the spreadsheet data type which matches the specified type
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static XLDataType GetXLDataType(System.Type type)
		{
			if (type.IsAssignableTo(typeof(DateTime)))
			{
				return XLDataType.DateTime;
			}
			else if (type.IsAssignableTo(typeof(Boolean)))
			{
				return XLDataType.Boolean;
			}
			else if (type.IsAssignableTo(typeof(Int32)) || type.IsAssignableTo(typeof(double)) || type.IsAssignableTo(typeof(float)))
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


		/// <summary>
		/// Class representing a worksheet column in an Excel export.
		/// </summary>
		public class WorksheetColumn
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
			/// Excel data type for the column.
			/// </summary>
			public XLDataType? DataType { get; set; }

			/// <summary>
			/// Constructor used when you want to specify the name, caption and data type.
			/// </summary>
			/// <param name="name"></param>
			/// <param name="caption"></param>
			/// <param name="dataType"></param>

			public WorksheetColumn(string name, string caption, XLDataType? dataType)
			{
				this.Name = name;
				this.Caption = caption;
				this.DataType = dataType;
			}

			/// <summary>
			/// Constructor used when you want to specify the name, caption and data type, and an expression to evaluate the data to output.
			/// </summary>
			/// <param name="name"></param>
			/// <param name="caption"></param>
			/// <param name="dataType"></param>
			/// <param name="expression"></param>

			public WorksheetColumn(string name, string caption, XLDataType? dataType, LambdaExpression expression)
			{
				this.Name = name;
				this.Caption = caption;
				this.DataType = dataType;
				this.Expression = expression;
			}

			/// <summary>
			/// Constructed used to auto-detect column properties.
			/// </summary>
			/// <param name="propertyInfo"></param>

			public WorksheetColumn(PropertyInfo propertyInfo)
			{
				this.PropertyInfo = propertyInfo;
				this.Name = propertyInfo.Name;
				this.Caption = propertyInfo.Name;
				this.DataType = ExcelWriter.GetXLDataType(propertyInfo.PropertyType);
			}
		}
	}
}
