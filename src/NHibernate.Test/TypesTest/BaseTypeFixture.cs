using System;
using System.Data;

using DotNetMock.Framework.Data;

using NUnit.Framework;

namespace NHibernate.Test.TypesTest
{
	/// <summary>
	/// Summary description for BaseTypeFixture.
	/// </summary>
	public class BaseTypeFixture
	{
		protected IDataReader reader = null;

		protected const int BooleanTypeColumnIndex = 0;
		protected const int ByteTypeColumnIndex = 1;
		protected const int Int32TypeColumnIndex = 2;
		protected const int DecimalTypeColumnIndex = 3;
		protected const int GuidTypeColumnIndex = 4;
		[SetUp]
		public void SetUp() 
		{
			MockDataReader mockReader = new MockDataReader();
			
			DataTable dataTable = new DataTable();
			dataTable.Columns.Add(new DataColumn("BooleanType", typeof(bool)));
			dataTable.Columns.Add(new DataColumn("ByteType", typeof(byte)));
			dataTable.Columns.Add(new DataColumn("Int32Type", typeof(int)));
			dataTable.Columns.Add(new DataColumn("DecimalType", typeof(decimal)));
			dataTable.Columns.Add(new DataColumn("GuidType", typeof(Guid)));
			mockReader.SetSchemaTable(dataTable);

			object[,] rowValues = new object[2,5];
			rowValues[0,BooleanTypeColumnIndex] = true;
			rowValues[0,ByteTypeColumnIndex] = 5;
			rowValues[0,Int32TypeColumnIndex] = 0;
			rowValues[0,DecimalTypeColumnIndex] = 5.64351M;
			rowValues[0,GuidTypeColumnIndex] = new Guid("{01234567-abcd-abcd-abcd-0123456789ab}");

			rowValues[1,BooleanTypeColumnIndex] = false;
			rowValues[1,ByteTypeColumnIndex] = 6;
			rowValues[1,Int32TypeColumnIndex] = 1;
			rowValues[1,DecimalTypeColumnIndex] = 5.6435101M;
			rowValues[1,GuidTypeColumnIndex] = new Guid("{76543210-dcba-dcba-dcba-ba9876543210}");
			
			mockReader.SetRows(rowValues);

			reader = mockReader;
			
		}

	}
}
