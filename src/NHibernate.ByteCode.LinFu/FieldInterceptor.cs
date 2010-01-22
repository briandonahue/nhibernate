using Iesi.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Intercept;

namespace NHibernate.ByteCode.LinFu
{
    public class FieldInterceptor : AbstractFieldInterceptor
    {
        public FieldInterceptor(ISessionImplementor session, ISet<string> uninitializedFields, string entityName) : base(session, uninitializedFields, entityName)
        {
        }

    }
}