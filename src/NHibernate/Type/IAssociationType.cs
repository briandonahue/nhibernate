using NHibernate.Engine;
using NHibernate.Persister;

namespace NHibernate.Type
{
	/// <summary>
	/// An <see cref="IType"/> that represents some kind of association between entities.
	/// </summary>
	public interface IAssociationType : IType
	{
		/// <summary>
		/// When implemented by a class, gets the type of foreign key directionality 
		/// of this association.
		/// </summary>
		/// <value>The <see cref="ForeignKeyDirection"/> of this association.</value>
		ForeignKeyDirection ForeignKeyDirection { get; }

		/// <summary>
		/// Is the primary key of the owning entity table
		/// to be used in the join?
		/// </summary>
		bool UseLHSPrimaryKey { get; }

		/// <summary>
		/// Get the "persister" for this association - a class or collection persister
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		IJoinable GetAssociatedJoinable( ISessionFactoryImplementor factory );

		/// <summary>
		/// Get the columns referenced by this association.
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		string[] GetReferencedColumns( ISessionFactoryImplementor factory );

		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		System.Type GetAssociatedClass( ISessionFactoryImplementor factory );
	}
}