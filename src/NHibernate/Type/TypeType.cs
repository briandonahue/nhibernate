using System;
using System.Data;

using NHibernate.SqlTypes;
using NHibernate.Util;

namespace NHibernate.Type {
	
	/// <summary>
	/// Maps the Assembly Qualified Name of a <see cref="System.Type"/> to a 
	/// <see cref="DbType.Stirng" /> column.
	/// </summary>
	public class TypeType : ImmutableType 
	{

		internal TypeType() : base( new StringSqlType() ) 
		{
		}

		/// <summary>
		/// Initialize a new instance of the TypeType class using a 
		/// <see cref="SqlType"/>. 
		/// </summary>
		/// <param name="sqlType">The underlying <see cref="SqlType"/>.</param>
		internal TypeType(StringSqlType sqlType) : base(sqlType) 
		{
		}

		/// <summary>
		/// Gets the <see cref="System.Type"/> in the <see cref="IDataReader"/> for the Property.
		/// </summary>
		/// <param name="rs">The <see cref="IDataReader"/> that contains the value.</param>
		/// <param name="index">The index of the field to get the value from.</param>
		/// <returns>The <see cref="System.Type"/> from the database.</returns>
		/// <exception cref="TypeLoadException">
		/// Thrown when the value in the database can not be loaded as a <see cref="System.Type"/>
		/// </exception>
		public override object Get(IDataReader rs, int index) 
		{
			string str = (string) NHibernate.String.Get(rs, index);
			if (str == null) 
			{
				return null;
			}
			else 
			{
				try 
				{
					return ReflectHelper.ClassForName(str);
				}
				catch (TypeLoadException) 
				{
					throw new HibernateException("Class not found: " + str);
				}
			}
		}


		/// <summary>
		/// Gets the <see cref="System.Type"/> in the <see cref="IDataReader"/> for the Property.
		/// </summary>
		/// <param name="rs">The <see cref="IDataReader"/> that contains the value.</param>
		/// <param name="name">The name of the field to get the value from.</param>
		/// <returns>The <see cref="System.Type"/> from the database.</returns>
		/// <remarks>
		/// This just calls gets the index of the name in the IDataReader
		/// and calls the overloaded version <see cref="Get(IDataReader, Int32)"/>
		/// (IDataReader, Int32). 
		/// </remarks>
		/// <exception cref="TypeLoadException">
		/// Thrown when the value in the database can not be loaded as a <see cref="System.Type"/>
		/// </exception>
		public override object Get(IDataReader rs, string name) 
		{
			return Get(rs, rs.GetOrdinal(name));

		}

		/// <summary>
		/// Puts the Assembly Qualified Name of the <see cref="System.Type"/> 
		/// Property into to the <see cref="IDbCommand"/>.
		/// </summary>
		/// <param name="cmd">The <see cref="IDbCommand"/> to put the value into.</param>
		/// <param name="value">The <see cref="System.Type"/> that contains the value.</param>
		/// <param name="index">The index of the <see cref="IDbDataParameter"/> to start writing the value to.</param>
		/// <remarks>
		/// This uses the <see cref="NHibernate.String.Set(IDbCommand, Object,Int32)"/> method of the 
		/// <see cref="NHibernate.String"/> object to do the work.
		/// </remarks>
		public override void Set(IDbCommand cmd, object value, int index) 
		{
			NHibernate.String.Set(cmd, ( (System.Type) value ).AssemblyQualifiedName, index);
		}
	
		/// <summary>
		/// A representation of the value to be embedded in an XML element 
		/// </summary>
		/// <param name="val">The <see cref="System.Type"/> that contains the values.
		/// </param>
		/// <returns>An Xml formatted string that contains the Assembly Qualified Name.</returns>
		public override string ToXML(object value) 
		{
			return ( (System.Type) value ).AssemblyQualifiedName;
		}
	
		/// <summary>
		/// Gets the <see cref="System.Type"/> that will be returned 
		/// by the <c>NullSafeGet()</c> methods.
		/// </summary>
		/// <value>
		/// A <see cref="System.Type"/> from the .NET framework.
		/// </value>
		public override System.Type ReturnedClass 
		{
			get { return typeof(System.Type); }
		}
	
		public override bool Equals(object x, object y) 
		{
			
			if(x==null && y==null) 
			{
				return true;
			}
			
			if(x==null || y==null) 
			{
				return false;
			}

			return x.Equals(y);
		}
	
		public override string Name 
		{
			get { return "Type"; }
		}
	}
}