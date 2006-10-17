using System;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Impl;

using NUnit.Framework;

namespace NHibernate.Test.HQLFunctionTest
{
	public class BaseFunctionFixture
	{
		protected ISessionFactory factory;
		protected ISessionFactoryImplementor factoryImpl;
		protected Dialect.Dialect dialect;

		[SetUp]
		public virtual void SetUp()
		{
			Configuration cfg = new Configuration();

			factory = cfg.BuildSessionFactory();
			factoryImpl = (ISessionFactoryImplementor)factory;
			dialect = Dialect.Dialect.GetDialect();
		}
	}
}
