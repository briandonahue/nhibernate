using System;
using System.Collections;

using NUnit.Framework;

using NHibernate.Criterion;
using NHibernate.Transform;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Test.Criteria.Lambda
{

	[TestFixture]
	public class CriteriaFixture : LambdaFixtureBase
	{

		[Test]
		public void Equality()
		{
			ICriteria expected =
				CreateTestCriteria(typeof(Person))
					.Add(Restrictions.Eq("Name", "test name"));

			ICriteria actual =
				CreateTestCriteria(typeof(Person))
					.Add<Person>(p => p.Name == "test name");

			AssertCriteriaAreEqual(expected, actual);
		}

	}

}