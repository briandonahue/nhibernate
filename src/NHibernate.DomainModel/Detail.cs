using System;
using System.Collections;

namespace NHibernate.DomainModel
{
	/// <summary>
	/// Summary description for Detail.
	/// </summary>
	[Serializable]
	public class Detail
	{
		private Master _master;
		private int _i;
		private IDictionary _details; //set in mapping
		private int _x;

		public Master Master 
		{
			get { return _master;}
			set { _master = value; }
		}
		
		public int I 
		{
			get { return _i;}
			set {_i = value; }
		}
	

		public IDictionary SubDetails 
		{
			get { return _details;}
			set { _details = value; }
		}

		public int X 
		{
			get { return _x;}
			set { _x = value; }
		}
	}
}
