using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using NHibernate.DomainModel.NHSpecific;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest 
{
	
	[TestFixture]
	public class BasicClassFixture : TestCase 
	{

		[SetUp]
		public void SetUp() 
		{
			ExportSchema( new string[] { "NHSpecific.BasicClass.hbm.xml"}, true );
		}

		[Test]
		public void TestCRUD() 
		{
			int maxIndex = 22;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);
			index++;

			// make sure the previous insert went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);

			Assert.IsNotNull(bc[index]);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].Int32Array[1] = 15;
			bc[index].StringBag[0] = "Replaced Spot 0";
			bc[index].StringArray[2] = "Replaced Spot 2";
			bc[index].StringList[0] = "Replaced Spot 0";
			bc[index].StringMap["keyZero"] = "Replaced Key 0";

			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].BooleanProperty = false;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			
			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].ByteProperty = Byte.MinValue;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].CharacterProperty = 'b';
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// update a property to make sure it picks up that it is dirty
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].ClassProperty = typeof(System.String);
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			// 12 = french
			bc[index].CultureInfoProperty = new System.Globalization.CultureInfo(12);
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			
			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].DateTimeProperty = DateTime.Parse("2004-02-15 08:00:00 PM");
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].DecimalProperty = 5.55555M;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].Int16Property = Int16.MinValue;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].Int32Property = Int32.MinValue;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].Int64Property = Int64.MinValue;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].SingleProperty = bc[index].SingleProperty * -1;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].StringProperty = "new string property";
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].TicksProperty = DateTime.Now;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].TrueFalseProperty = false;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// make sure the previous updates went through
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);

			bc[index].YesNoProperty = false;
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// VERIFY DELETE
			AssertDelete(id);

		}

		[Test]
		public void TestArrayCRUD() 
		{
			int maxIndex = 4;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);

			index++;

			// modify the array so it is updated - should not be recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].StringArray[0] = "modified string 0";
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// change the array to a new array so it is recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].StringArray = new string[] {"string one", "string two"};
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// verify the delete went through
			AssertDelete(id);
		}


		[Test]
		public void TestPrimitiveArrayCRUD() 
		{
			int maxIndex = 4;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);

			index++;

			// modify the array so it is updated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			for(int i = 0; i < bc[index].Int32Array.Length; i++) 
			{
				bc[index].Int32Array[i] = i+1;
			}

			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;


			// modify the array to a new array so it is recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].Int32Array = new int[] {1,2,3,4,5,6};
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// verify the delete went through
			AssertDelete(id);
		}

		[Test]
		public void TestMapCRUD() 
		{
			int maxIndex = 4;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);

			index++;

			// modify the array so it is updated - should not be recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// remove the last one and update another
			bc[index].StringMap.Remove("keyOne");
			bc[index].StringMap["keyTwo"] = "modified string two";
			bc[index].StringMap["keyThree"] = "added key three";
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// change the List to a new List so it is recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].StringMap = new Hashtable();
			bc[index].StringMap.Add("keyZero", "new list zero");
			bc[index].StringMap.Add("keyOne", "new list one");
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// verify the delete went through
			AssertDelete(id);
		}

		[Test]
		public void TestSetCRUD() 
		{
			int maxIndex = 4;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);

			index++;

			// modify the array so it is updated - should not be recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// remove the last one and add another
			bc[index].StringSet.Remove("zero");
			bc[index].StringSet.Add("two", new object());
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// change the List to a new List so it is recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].StringSet = new Hashtable();
			bc[index].StringSet.Add("zero", new object());
			bc[index].StringSet.Add("one", new object());
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// verify the delete went through
			AssertDelete(id);
		}

		[Test]
		public void TestBagCRUD() 
		{
			int maxIndex = 5;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);

			index++;

			// modify the bag so it is updated - should not be recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// remove the last one and update another
			bc[index].StringBag.RemoveAt(bc[index].StringBag.Count-1);
			bc[index].StringBag[1] = "modified string 1";
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// add an item to the list
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// remove the last one and update another
			bc[index].StringBag.Add("inserted into the bag");
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// change the List to a new List so it is recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].StringBag = new ArrayList();
			bc[index].StringBag.Add("new bag zero");
			bc[index].StringBag.Add("new bag one");
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// verify the delete went through
			AssertDelete(id);
		}

		[Test]
		public void TestListCRUD() 
		{
			int maxIndex = 5;
			ISession[] s = new ISession[maxIndex];
			ITransaction[] t = new ITransaction[maxIndex];
			BasicClass[] bc = new BasicClass[maxIndex];

			int index = 0;
			int id = 1;

			bc[index] = InsertBasicClass(id);

			index++;

			// modify the array so it is updated - should not be recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// remove the last one and update another
			bc[index].StringList.RemoveAt(bc[index].StringList.Count-1);
			bc[index].StringList[2] = "modified string 2";
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// add an item to the list
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// remove the last one and update another
			bc[index].StringList.Add("inserted into the list");
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;

			// change the List to a new List so it is recreated
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			bc[index].StringList = new ArrayList();
			bc[index].StringList.Add("new list zero");
			bc[index].StringList.Add("new list one");
			s[index].Update(bc[index]);

			t[index].Commit();
			s[index].Close();

			index++;
			

			// VERIFY PREVIOUS UPDATE & PERFORM DELETE 
			s[index] = sessions.OpenSession();
			t[index] = s[index].BeginTransaction();

			bc[index] = (BasicClass)s[index].Load(typeof(BasicClass), id);
			AssertPropertiesEqual(bc[index-1], bc[index]);
			
			// test the delete method
			s[index].Delete(bc[index]);
			
			t[index].Commit();
			s[index].Close();

			index++;

			// verify the delete went through
			AssertDelete(id);
		}

		[Test]
		public void TestWrapArrayInListProperty() 
		{
			
			ISession s = sessions.OpenSession();
			BasicClass bc = new BasicClass();

			int id = 1;

			bc.StringList = new string[] { "one", "two" };

			s.Save( bc, id );
			s.Flush();
			s.Close();

			s = sessions.OpenSession();
			bc = (BasicClass)s.Load( typeof(BasicClass), id );

			Assert.AreEqual( 2, bc.StringList.Count, "should have saved to StringList from an array" );
			Assert.IsTrue( bc.StringList.Contains( "one" ), "'one' should be in there" );
			Assert.IsTrue( bc.StringList.Contains( "two" ), "'two' should be in there" );

			s.Delete( bc );
			s.Flush();
			s.Close();
		}

		internal void AssertDelete(int id) 
		{
			ISession s = sessions.OpenSession();

			try 
			{
				BasicClass bc = (BasicClass)s.Load(typeof(BasicClass), id);
			}
			catch(ObjectNotFoundException onfe) 
			{
				// I expect this to be thrown because the object no longer exists...
				Assert.IsNotNull(onfe); //getting ride of 'onfe' is never used compile warning
			}

			IList results =  s.CreateCriteria(typeof(BasicClass))
				.Add(Expression.Expression.Eq("Id", id))
				.List();

			Assert.AreEqual(0, results.Count);

			s.Close();
		}

		internal BasicClass InsertBasicClass(int id) 
		{
			ISession s = sessions.OpenSession();
			ITransaction t = s.BeginTransaction();

			BasicClass bc = new BasicClass();
			InitializeBasicClass(id, ref bc);

			s.Save(bc);

			t.Commit();
			s.Close();

			return bc;

		}


		/// <summary>
		/// Compares the non Collection Properties of the BasicClass
		/// </summary>
		/// <param name="expected">The expected values.</param>
		/// <param name="actual">The Actual values.</param>
		/// <remarks>
		/// Passes to the overload with includeCollections=true
		/// </remarks>
		internal void AssertPropertiesEqual(BasicClass expected, BasicClass actual) 
		{
			AssertPropertiesEqual(expected, actual, true);
		}
	
		/// <summary>
		/// Compares the non Collection Properties of the BasicClass
		/// </summary>
		/// <param name="expected">The expected values.</param>
		/// <param name="actual">The Actual values.</param>
		internal void AssertPropertiesEqual(BasicClass expected, BasicClass actual, bool includeCollections) 
		{
			Assert.AreEqual(expected.Id, actual.Id, "Id");
			Assert.AreEqual(expected.BooleanProperty, actual.BooleanProperty, "BooleanProperty");
			Assert.AreEqual(expected.ByteProperty, actual.ByteProperty, "ByteProperty");
			Assert.AreEqual(expected.CharacterProperty, actual.CharacterProperty, "CharacterProperty");
			Assert.AreEqual(expected.ClassProperty, actual.ClassProperty, "ClassProperty");
			Assert.AreEqual(expected.CultureInfoProperty, actual.CultureInfoProperty, "CultureInfoProperty");
			Assert.AreEqual(expected.DateTimeProperty, actual.DateTimeProperty, "DateTimeProperty");
			Assert.AreEqual(expected.DecimalProperty, actual.DecimalProperty, "DecimalProperty using Assert should be AreEqual");
			Assert.IsTrue(expected.DecimalProperty.Equals(actual.DecimalProperty), "DecimalProperty");
//			Assert.AreEqual(expected.DoubleProperty, actual.DoubleProperty, 0, "DoubleProperty");
			Assert.AreEqual(expected.Int16Property, actual.Int16Property, "Int16Property");
			Assert.AreEqual(expected.Int32Property, actual.Int32Property, "Int32Property");
			Assert.AreEqual(expected.Int64Property, actual.Int64Property, "Int64Property");
			Assert.AreEqual(expected.SingleProperty, actual.SingleProperty, 0, "SingleProperty");
			Assert.AreEqual(expected.StringProperty, actual.StringProperty, "StringProperty");
			Assert.AreEqual(expected.TicksProperty, actual.TicksProperty, "TicksProperty");
			Assert.AreEqual(expected.TrueFalseProperty, actual.TrueFalseProperty, "TrueFalseProperty");
			Assert.AreEqual(expected.YesNoProperty, actual.YesNoProperty, "YesNoProperty");
			
			if(includeCollections) 
			{
				ObjectAssert.AssertEquals(expected.StringArray, actual.StringArray);
				ObjectAssert.AssertEquals(expected.Int32Array, actual.Int32Array);
				ObjectAssert.AssertEquals(expected.StringBag, actual.StringBag, false);
				ObjectAssert.AssertEquals(expected.StringList, actual.StringList);
				ObjectAssert.AssertEquals(expected.StringMap, actual.StringMap, true);
				ObjectAssert.AssertEquals(expected.StringSet, actual.StringSet, false);
			}
		}

	
		private void InitializeBasicClass(int id, ref BasicClass basicClass) 
		{
			basicClass.Id = id;

			
			basicClass.BooleanProperty = true;
			basicClass.ByteProperty = Byte.MaxValue;  
			basicClass.CharacterProperty = 'a';
			basicClass.ClassProperty = typeof(object);
			basicClass.CultureInfoProperty = System.Globalization.CultureInfo.CurrentCulture;
			basicClass.DateTimeProperty = DateTime.Parse("2003-12-01 10:45:21 AM");
			basicClass.DecimalProperty = 5.64351M;
			basicClass.Int16Property = Int16.MaxValue;
			basicClass.Int32Property = Int32.MaxValue;
			basicClass.Int64Property = Int64.MaxValue;
			
			// more MySql problems - it returns 3.40282E38
			// instead of 3.402823E+38 which is Single.MaxValue
			basicClass.SingleProperty = 3.5F; //Single.MaxValue;
			basicClass.StringProperty = "string property";
			basicClass.TicksProperty = DateTime.Now;
			basicClass.TrueFalseProperty = true;
			basicClass.YesNoProperty = true;
			
			basicClass.StringArray = new string[] {"3 string", "2 string", "1 string"};
			basicClass.Int32Array = new int[] {5,4,3,2,1};

			IList stringBag = new ArrayList(3);
			stringBag.Add("string 0");
			stringBag.Add("string 1");
			stringBag.Add("string 2");

			basicClass.StringBag = stringBag;

			IList stringList = new ArrayList(5);
			stringList.Add("new string zero");
			stringList.Add("new string one");
			stringList.Add("new string two");
			stringList.Add("new string three");
			stringList.Add("new string four");

			basicClass.StringList = stringList;

			IDictionary stringMap = new Hashtable();
			stringMap.Add("keyOne", "string one");
			stringMap.Add("keyZero", "string zero");
			stringMap.Add("keyTwo", "string two");

			basicClass.StringMap = stringMap;

			basicClass.AddToStringSet("zero");
			basicClass.AddToStringSet("one");
			basicClass.AddToStringSet("zero");

		}

		
	}

}
