using System;

namespace NHibernate {
	
	/// <summary>
	/// Represents a flushing strategy.
	/// </summary>
	/// <remarks>
	/// The flush process synchronizes database state with session state by detecting state
	/// changes and executing SQL statements
	/// </remarks>
	public enum FlushMode {
		/// <summary>
		/// The <c>ISession</c> is never flushed unless <c>Flush()</c> is explicitly
		/// called by the application. This mode is very efficient for read only
		/// transactions
		/// </summary>
		Never = 0,
		/// <summary>
		/// The <c>ISession</c> is flushed when <c>Transaction.Commit()</c> is called
		/// </summary>
		Commit = 5,
		/// <summary>
		/// The <c>ISession</c> is sometimes flushed before query execution in order to
		/// ensure that queries never return stale state. This is the default flush mode.
		/// </summary>
		Auto = 10
	}
}
