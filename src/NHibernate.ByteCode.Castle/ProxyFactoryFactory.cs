using System;
using Castle.DynamicProxy;
using NHibernate.Bytecode;
using NHibernate.Intercept;
using NHibernate.Proxy;

namespace NHibernate.ByteCode.Castle
{
	public class ProxyFactoryFactory : IProxyFactoryFactory
	{
		#region IProxyFactoryFactory Members

		public IProxyFactory BuildProxyFactory()
		{
			return new ProxyFactory();
		}

		public IProxyValidator ProxyValidator
		{
			get { return new DynProxyTypeValidator(); }
		}

		#endregion
	}
}