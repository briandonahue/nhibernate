using System;
using System.Data;
using System.Collections;

using NHibernate.Engine;
using NHibernate.Type;
using NHibernate.Util;
using NHibernate.Dialect;


namespace NHibernate.Id {
	/// <summary>
	/// An <c>IIdentifierGenerator</c> that combines a hi/lo algorithm with an underlying
	/// oracle-style sequence that generates hi values.
	/// </summary>
	/// <remarks>
	/// The user may specify a maximum lo value to determine how often new hi values are
	/// fetched. If sequences are not avaliable, <c>TableHiLoGenerator</c> might be an
	/// alternative.
	/// </remarks>
	public class SequenceHiLoGenerator : SequenceGenerator {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(SequenceHiLoGenerator));

		public const string MaxLo = "max_lo";

		private int maxLoValue;
		private int lo;
		private long hi;
		private System.Type returnClass;

		public override void Configure(IType type, IDictionary parms, Dialect.Dialect d) {
			base.Configure(type, parms, d);
			lo = maxLoValue = PropertiesHelper.GetInt(MaxLo, parms, 9);
			returnClass = type.ReturnedClass;
		}

		public override object Generate(ISessionImplementor session, object obj) {
			lock(this) {
				if ( lo==maxLoValue ) {
					long hival = ( (long) base.Generate(session, obj) );
					lo = 0;
					hi = hival * ( maxLoValue+1 );
					log.Debug("new hi value: " + hival);
				}
				return IdentifierGeneratorFactory.CreateNumber( hi + lo++, returnClass );
			}
		}
	}
}
