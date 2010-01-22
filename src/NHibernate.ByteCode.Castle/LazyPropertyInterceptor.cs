using Castle.Core.Interceptor;

namespace NHibernate.ByteCode.Castle
{
	public class LazyPropertyInterceptor : global::Castle.Core.Interceptor.IInterceptor
	{
		public void Intercept(IInvocation invocation)
		{
			invocation.Proceed();
		}
	}
}