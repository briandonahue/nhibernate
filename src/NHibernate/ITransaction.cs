using System;

namespace NHibernate {
	/// <summary>
	/// Allows the application to define units of work, while maintaining abstraction from the
	/// underlying transaction implementation
	/// </summary>
	/// <remarks>
	/// A transaction is associated with a <c>ISession</c> and is usually instanciated by a call to
	/// <c>ISession.BeginTransaction()</c>. A single session might span multiple transactions since 
	/// the notion of a session (a conversation between the application and the datastore) is of
	/// coarser granularity than the notion of a transaction. However, it is intended that there be
	/// at most one uncommitted <c>ITransaction</c> associated with a particular <c>ISession</c>
	/// at a time. Implementors are not intended to be threadsafe.
	/// </remarks>
	public interface ITransaction {

		/// <summary>
		/// Flush the associated <c>ISession</c> and end the unit of work.
		/// </summary>
		/// <remarks>
		/// This method will commit the underlying transaction if and only if the transaction
		/// was initiated by this object.
		/// </remarks>
		void Commit();

		/// <summary>
		/// Force the underlying transaction to roll back.
		/// </summary>
		void Rollback();

		/// <summary>
		/// Was the transaction folled back or set to rollback only?
		/// </summary>
		bool WasRolledBack { get; }

		/// <summary>
		/// Was the transaction successfully committed?
		/// </summary>
		/// <remarks>
		/// This method could return <c>false</c> even after successful invocation of <c>Commit()</c>
		/// </remarks>
		bool WasCommitted { get; }
		
	}
}
