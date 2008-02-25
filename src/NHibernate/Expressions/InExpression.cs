using System;
using System.Collections;
using NHibernate.Engine;
using NHibernate.SqlCommand;
using NHibernate.Type;
using NHibernate.Util;
using System.Collections.Generic;

namespace NHibernate.Expressions
{
	/// <summary>
	/// An <see cref="ICriterion"/> that constrains the property 
	/// to a specified list of values.
	/// </summary>
	/// <remarks>
	/// InExpression - should only be used with a Single Value column - no multicolumn properties...
	/// </remarks>
	[Serializable]
	public class InExpression : AbstractCriterion
	{
		private readonly IProjection projection;
		private readonly string _propertyName;
		private object[] _values;

		/// <summary>
		/// Initializes a new instance of the <see cref="InExpression"/> class.
		/// </summary>
		/// <param name="projection">The projection.</param>
		/// <param name="_values">The _values.</param>
		public InExpression(IProjection projection, object[] _values)
		{
			this.projection = projection;
			this._values = _values;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="values"></param>
		public InExpression(string propertyName, object[] values)
		{
			_propertyName = propertyName;
			_values = values;
		}

		public override SqlString ToSqlString(ICriteria criteria, ICriteriaQuery criteriaQuery,
		                                      IDictionary<string, IFilter> enabledFilters)
		{
			if (projection == null)
				AssertPropertyIsNotCollection(criteriaQuery, criteria);

			if (_values.Length == 0)
			{
				// "something in ()" is always false
				return new SqlString("1=0");
			}

			//TODO: add default capacity
			SqlStringBuilder result = new SqlStringBuilder();
			SqlString[] columnNames =
				CriterionUtil.GetColumnNames(_propertyName, projection, criteriaQuery, criteria, enabledFilters);
			
			// Generate SqlString of the form:
			// columnName1 in (values) and columnName2 in (values) and ...

			for (int columnIndex = 0; columnIndex < columnNames.Length; columnIndex++)
			{
				SqlString columnName = columnNames[columnIndex];

				if (columnIndex > 0)
				{
					result.Add(" and ");
				}

				result
					.Add(columnName)
					.Add(" in (");

				for (int i = 0; i < _values.Length; i++)
				{
					if (i > 0)
					{
						result.Add(StringHelper.CommaSpace);
					}
					result.AddParameter();
				}

				result.Add(")");
			}

			return result.ToSqlString();
		}

		private void AssertPropertyIsNotCollection(ICriteriaQuery criteriaQuery, ICriteria criteria)
		{
			IType type = criteriaQuery.GetTypeUsingProjection(criteria, _propertyName);
			if (type.IsCollectionType)
			{
				throw new QueryException("Cannot use collections with InExpression");
			}
		}

		public override TypedValue[] GetTypedValues(ICriteria criteria, ICriteriaQuery criteriaQuery)
		{
			ArrayList list = new ArrayList();
			IType type;
			if (projection == null)
			{
				type = criteriaQuery.GetTypeUsingProjection(criteria, _propertyName);
			}
			else
			{
				IType[] types = projection.GetTypes(criteria, criteriaQuery);
				if(types.Length!=1)
				{
					throw new QueryException("Cannot use projections that return more than a single column with InExpression");
				}
				type = types[0];
				list.AddRange(projection.GetTypedValues(criteria, criteriaQuery));
			}

			if (type.IsComponentType)
			{
				IAbstractComponentType actype = (IAbstractComponentType) type;
				IType[] types = actype.Subtypes;

				for (int i = 0; i < types.Length; i++)
				{
					for (int j = 0; j < _values.Length; j++)
					{
						object subval = _values[j] == null
						                	?
						                		null
						                	:
						                		actype.GetPropertyValues(_values[j], EntityMode.Poco)[i];
						list.Add(new TypedValue(types[i], subval));
					}
				}
			}
			else
			{
				for (int j = 0; j < _values.Length; j++)
				{
					list.Add(new TypedValue(type, _values[j]));
				}
			}

			return (TypedValue[]) list.ToArray(typeof (TypedValue));
		}

		public object[] Values
		{
			get { return _values; }
			protected set { _values = value; }
		}

		/// <summary></summary>
		public override string ToString()
		{
			return (projection ?? (object)_propertyName) + " in (" + StringHelper.ToString(_values) + ')';
		}
	}
}