using System;
using System.Data;

using NHibernate.SqlTypes;

namespace NHibernate.Type 
{
	
	/// <summary>
	/// Maps a <see cref="System.Single" /> Property to an 
	/// <see cref="DbType.Single" /> column.
	/// </summary>
	/// <remarks>
	/// Verify through your database's documentation if there is a column type that
	/// matches up with the capabilities of <see cref="System.Single" />  
	/// </remarks>
	public class SingleType : ValueTypeType 
	{
		internal SingleType() : base( new SingleSqlType() ) 
		{
		}

		public override object Get(IDataReader rs, int index)
		{
			return Convert.ToSingle(rs[index]);
		}

		public override object Get(IDataReader rs, string name) 
		{
			return Convert.ToSingle(rs[name]);
		}

		public override System.Type ReturnedClass 
		{
			get { return typeof(System.Single); }
		}

		public override void Set(IDbCommand st, object value, int index) 
		{
			IDataParameter parm = st.Parameters[index] as IDataParameter;
			parm.Value = value;
		}

		public override string Name 
		{
			get { return "Single"; }
		}

		public override string ObjectToSQLString(object value) 
		{
			return value.ToString();
		}
	}
}
