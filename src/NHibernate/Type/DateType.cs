using System;
using System.Data;

namespace NHibernate.Type {
	
	public class DateType : MutableType, IIdentifierType, ILiteralType {
		
		public override object Get(IDataReader rs, string name) {
			return rs[name];
		}
		public override System.Type ReturnedClass {
			get { return typeof(DateTime); }
		}
		public override void Set(IDbCommand st, object value, int index) {
			IDataParameter parm = st.Parameters[index] as IDataParameter;
			parm.DbType = DbType.Date;
			parm.Value = value;
		}
		public override DbType SqlType {
			get { return DbType.Date; }
		}
		public override bool Equals(object x, object y) {
			if (x==y) return true;
			if (x==null || y==null) return false;

			DateTime date1 = (DateTime) x;
			DateTime date2 = (DateTime) y;

			return date1.Day == date2.Day
				&& date1.Month == date2.Month 
				&& date1.Year == date2.Year;
		}
		public override string Name {
			get { return "date"; }
		}
		public override string ToXML(object val) {
			return ((DateTime)val).ToShortDateString();
		}
		public override object DeepCopyNotNull(object value) {
			DateTime old = (DateTime) value;
			return new DateTime(old.Year, old.Month, old.Day);
		}
		public override bool HasNiceEquals {
			get { return true; }
		}
		public object StringToObject(string xml) {
			return DateTime.Parse(xml);
		}
		public string ObjectToSQLString(object value) {
			return "'" + value.ToString() + "'";
		}
	}
}
