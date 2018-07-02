﻿using Postulate.Lite.Core;
using Postulate.Lite.Core.Attributes;
using Postulate.Lite.Core.Extensions;
using Postulate.Lite.Core.Metadata;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;

namespace Postulate.Lite.MySql
{
	public partial class MySqlProvider<TKey> : CommandProvider<TKey>
	{
		public MySqlProvider(Func<object, TKey> identityConverter, string identitySyntax) : base(identityConverter, identitySyntax)
		{
		}

		protected override string ApplyDelimiter(string name)
		{
			return $"`{name}`";
		}

		protected override string FindCommand<T>(string whereClause)
		{
			var type = typeof(T);
			var props = GetMappedColumns(type);
			var columns = props.Select(pi => new ColumnInfo(pi));
			return $"SELECT {string.Join(", ", columns.Select(col => ApplyDelimiter(col.ColumnName)))} FROM {ApplyDelimiter(TableName(typeof(T)))} WHERE {whereClause}";
		}

		protected override string InsertCommand<T>()
		{
			var columns = EditableColumns<T>(SaveAction.Insert);
			string columnList = string.Join(", ", columns.Select(c => ApplyDelimiter(c.ColumnName)));
			string valueList = string.Join(", ", columns.Select(c => $"@{c.PropertyName}"));
			return $"INSERT INTO {ApplyDelimiter(TableName(typeof(T)))} ({columnList}) VALUES ({valueList}); SELECT LAST_INSERT_ID()";
		}

		protected override string UpdateCommand<T>()
		{
			var columns = EditableColumns<T>(SaveAction.Update);
			return $"UPDATE {ApplyDelimiter(TableName(typeof(T)))} SET {string.Join(", ", columns.Select(col => $"{ApplyDelimiter(col.ColumnName)}=@{col.PropertyName}"))} WHERE {ApplyDelimiter(typeof(T).GetIdentityName())}=@id";
		}

		protected override string DeleteCommand<T>()
		{
			return $"DELETE FROM {ApplyDelimiter(TableName(typeof(T)))} WHERE {ApplyDelimiter(typeof(T).GetIdentityName())}=@id";
		}

		protected override string SqlColumnSyntax(PropertyInfo propertyInfo, bool isIdentity)
		{
			ColumnInfo col = new ColumnInfo(propertyInfo);
			string result = ApplyDelimiter(col.ColumnName);

			var calcAttr = propertyInfo.GetCustomAttribute<CalculatedAttribute>();
			if (calcAttr != null)
			{
				result += $" AS {calcAttr.Expression}";
			}
			else
			{
				string nullSyntax = (col.AllowNull) ? "NULL" : "NOT NULL";

				string dataType = (col.HasExplicitType()) ?
					col.DataType :
					SupportedTypes(col.Length, col.Precision, col.Scale)[propertyInfo.PropertyType];

				if (isIdentity) dataType += " " + IdentityColumnSyntax();

				result += $" {dataType} {nullSyntax}";
			}

			return result;
		}

		protected override Dictionary<Type, string> SupportedTypes(int length = 0, int precision = 0, int scale = 0)
		{
			return new Dictionary<Type, string>()
			{
				{ typeof(string), $"varchar({length})" },
				{ typeof(int), "int" },
				{ typeof(DateTime), "datetime" },
				{ typeof(bool), "bit" },
				{ typeof(decimal), $"decimal({scale}, {precision})" },
				{ typeof(long), "bigint" },
				{ typeof(short), "smallint" },
				{ typeof(byte), "tinyint" },
				{ typeof(TimeSpan), "time" },
				{ typeof(double), "float" },
				{ typeof(float), "float" },
				{ typeof(char), "char(1)" },
				{ typeof(byte[]), $"varbinary({length})" }
			};
		}

		protected override string TableName(Type modelType)
		{
			string result = modelType.Name;

			var tblAttr = modelType.GetCustomAttribute<TableAttribute>();
			if (tblAttr != null && !string.IsNullOrEmpty(tblAttr.Name))
			{
				result = tblAttr.Name;
			}

			return result;
		}
	}
}