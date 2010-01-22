using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.LazyProperties
{
	[TestFixture]
	public class Fixture : BugTestCase
	{
		protected override void OnSetUp()
		{
			using (var s = OpenSession())
			using (s.BeginTransaction())
			{
				var person = new Person
				{
					Name = "ayende"
				};
				s.Save(person);
				s.Transaction.Commit();
			}
		}

		protected override void OnTearDown()
		{
			using (var s = OpenSession())
			using (s.BeginTransaction())
			{
				s.Delete("from Person");
				s.Transaction.Commit();
			}
		}

		[Test]
		public void GetGoodErrorForDirtyReassociatedCollection()
		{
			using (var s = OpenSession())
			using (s.BeginTransaction())
			{
				var person = s.Get<Person>(1);
				Assert.AreEqual("ayende", person.Name);
				s.Transaction.Commit();
			}

		}

	}
}
