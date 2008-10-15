using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.Util;

namespace NHibernate.Exceptions
{
	public static class ADOExceptionHelper
	{
		public const string SQLNotAvailable = "SQL not available";

	    /// <summary> 
		/// Converts the given SQLException into NHibernate's ADOException hierarchy, as well as performing
		/// appropriate logging. 
		/// </summary>
		/// <param name="converter">The converter to use.</param>
		/// <param name="sqlException">The exception to convert.</param>
		/// <param name="message">An optional error message.</param>
		/// <param name="sql">The SQL executed.</param>
		/// <returns> The converted <see cref="ADOException"/>.</returns>
		public static ADOException Convert(ISQLExceptionConverter converter, Exception sqlException, string message, SqlString sql)
		{
	        sql = TryGetActualSqlQuery(sqlException, sql);
			ADOExceptionReporter.LogExceptions(sqlException, ExtendMessage(message, sql, null, null));
			return converter.Convert(sqlException, message, sql);
		}

		/// <summary> 
		/// Converts the given SQLException into NHibernate's ADOException hierarchy, as well as performing
		/// appropriate logging. 
		/// </summary>
		/// <param name="converter">The converter to use.</param>
		/// <param name="sqlException">The exception to convert.</param>
		/// <param name="message">An optional error message.</param>
		/// <returns> The converted <see cref="ADOException"/>.</returns>
		public static ADOException Convert(ISQLExceptionConverter converter, Exception sqlException, string message)
		{
            SqlString sql = new SqlString(SQLNotAvailable);
            sql = TryGetActualSqlQuery(sqlException, sql);
		    return Convert(converter, sqlException, message, sql);
		}

		public static ADOException Convert(ISQLExceptionConverter converter, Exception sqle, string message, SqlString sql,
			object[] parameterValues, IDictionary<string, TypedValue> namedParameters)
		{
            sql = TryGetActualSqlQuery(sqle, sql);
            string extendMessage = ExtendMessage(message, sql, parameterValues, namedParameters);
			ADOExceptionReporter.LogExceptions(sqle, extendMessage);
			return new ADOException(extendMessage, sqle, sql);
		}

		/// <summary> For the given <see cref="Exception"/>, locates the <see cref="System.Data.Common.DbException"/>. </summary>
		/// <param name="sqlException">The exception from which to extract the <see cref="System.Data.Common.DbException"/> </param>
		/// <returns> The <see cref="System.Data.Common.DbException"/>, or null. </returns>
		public static DbException ExtractDbException(Exception sqlException)
		{
			Exception baseException = sqlException;
			DbException result = sqlException as DbException;
			while (result == null && baseException != null)
			{
				baseException = baseException.InnerException;
				result = baseException as DbException;
			}
			return result;
		}

		public static string ExtendMessage(string message, SqlString sql, object[] parameterValues, IDictionary<string, TypedValue> namedParameters)
		{
			StringBuilder sb = new StringBuilder();
			sb.Append(message).Append(Environment.NewLine).
				Append("[ ").Append(sql).Append(" ]");
			if (parameterValues != null && parameterValues.Length > 0)
			{
				sb.Append(Environment.NewLine).Append("Positional parameters: ");
				int index = 0;
				foreach (object parameterValue in parameterValues)
				{
					object value = parameterValue;
					if (value == null)
						value = "null";
					sb.Append(" #").Append(index).Append(">").Append(value);
				}
			}
			if (namedParameters != null && namedParameters.Count > 0)
			{
				sb.Append(Environment.NewLine);
				foreach (KeyValuePair<string, TypedValue> namedParameter in namedParameters)
				{
					object value = namedParameter.Value.Value;
					if (value == null)
						value = "null";
					sb.Append("  ").Append("Name:").Append(namedParameter.Key)
						.Append(" - Value:").Append(value);

				}
			}
			sb.Append(Environment.NewLine);
			return sb.ToString();
		}

        public static SqlString TryGetActualSqlQuery(Exception sqle, SqlString sql)
        {
            string query = (string)sqle.Data["actual-sql-query"];
            if(query!=null)
                sql = new SqlString(query);
            return sql;
        }
	}
}