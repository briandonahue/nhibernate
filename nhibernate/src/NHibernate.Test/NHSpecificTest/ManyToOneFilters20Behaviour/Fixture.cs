using System.Collections.Generic;
using NHibernate.Criterion;
using NUnit.Framework;

namespace NHibernate.Test.NHSpecificTest.ManyToOneFilters20Behaviour
{
	[TestFixture]
	public class Fixture : BugTestCase
	{
		private static IList<Parent> joinGraphUsingHql(ISession s)
		{
			const string hql = @"select p from Parent p
                                    join p.Child c";
			return s.CreateQuery(hql).List<Parent>();
		}

		private static IList<Parent> joinGraphUsingCriteria(ISession s)
		{
			return s.CreateCriteria(typeof (Parent)).SetFetchMode("Child", FetchMode.Join).List<Parent>();
		}

		private static Parent createParent()
		{
			return new Parent {Child = new Child()};
		}

		private static void enableFilters(ISession s)
		{
			IFilter f = s.EnableFilter("activeChild");
			f.SetParameter("active", 1);
			IFilter f2 = s.EnableFilter("alwaysValid");
			f2.SetParameter("always", 1);
		}

		protected override void OnTearDown()
		{
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					s.Delete("from Parent");
					tx.Commit();
				}
			}
		}

		[Test]
		public void VerifyAlwaysFilter()
		{
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					Parent p = createParent();
					p.Child.Always = false;
					s.Save(p);
					tx.Commit();
				}
			}

			using (ISession s = OpenSession())
			{
				enableFilters(s);
				IList<Parent> resCriteria = joinGraphUsingCriteria(s);
				IList<Parent> resHql = joinGraphUsingHql(s);

				Assert.AreEqual(0, resCriteria.Count);
				Assert.AreEqual(0, resHql.Count);
			}
		}

		[Test]
		public void VerifyFilterActiveButNotUsedInManyToOne()
		{
			using (ISession s = OpenSession())
			{
				using (ITransaction tx = s.BeginTransaction())
				{
					s.Save(createParent());
					tx.Commit();
				}
			}

			using (ISession s = OpenSession())
			{
				enableFilters(s);
				IList<Parent> resCriteria = joinGraphUsingCriteria(s);
				IList<Parent> resHql = joinGraphUsingHql(s);

				Assert.AreEqual(1, resCriteria.Count);
				Assert.IsNotNull(resCriteria[0].Child);

				Assert.AreEqual(1, resHql.Count);
				Assert.IsNotNull(resHql[0].Child);
			}
		}

	    [Test]
	    public void VerifyQueryWithWhereClause()
	    {
            using (ISession s = OpenSession())
            {
                using (ITransaction tx = s.BeginTransaction())
                {
                    var p = createParent();
                    p.ParentString = "a";
                    p.Child.ChildString = "b";
                    s.Save(p);
                    tx.Commit();
                }
            }
	        IList<Parent> resCriteria;
	        IList<Parent> resHql;
            using (ISession s = OpenSession())
            {
                enableFilters(s);
                resCriteria = s.CreateCriteria(typeof(Parent), "p")
                                .CreateCriteria("Child", "c")
                                .SetFetchMode("Child", FetchMode.Join)
                                .Add(Restrictions.Eq("p.ParentString", "a"))
                                .Add(Restrictions.Eq("c.ChildString", "b"))
                                .List<Parent>();
                resHql = s.CreateQuery(@"select p from Parent p
                                                        join fetch p.Child c
                                                    where p.ParentString='a' and c.ChildString='b'").List<Parent>();
            }
            Assert.AreEqual(1, resCriteria.Count);
            Assert.IsNotNull(resCriteria[0].Child); 
            Assert.AreEqual(1, resHql.Count);
            Assert.IsNotNull(resHql[0].Child);
	    }
	}
}
