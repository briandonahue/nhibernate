using System;
using System.Collections.Generic;

namespace NHibernate.Cfg.MappingSchema
{
	public partial class HbmKeyManyToOne : AbstractDecoratable, IColumnsMapping, IRelationship, IEntityPropertyMapping
	{

		#region Implementation of IColumnsMapping

		public IEnumerable<HbmColumn> Columns
		{
			get { return column ?? AsColumns(); }
		}

		#endregion

		private IEnumerable<HbmColumn> AsColumns()
		{
			if (string.IsNullOrEmpty(column1))
			{
				yield break;
			}
			else
			{
				yield return new HbmColumn
				{
					name = column1,
				};
			}
		}

		#region Overrides of AbstractDecoratable

		protected override HbmMeta[] Metadatas
		{
			get { return meta ?? new HbmMeta[0]; }
		}

		#endregion

		public HbmRestrictedLaziness? Lazy
		{
			get { return lazySpecified ? lazy : (HbmRestrictedLaziness?)null; }
		}

		#region Implementation of IRelationship

		public string EntityName
		{
			get { return entityname; }
		}

		public string Class
		{
			get { return @class; }
		}

		public HbmNotFoundMode NotFoundMode
		{
			get { return notfound; }
		}

		#endregion

		#region Implementation of IEntityPropertyMapping

		public string Name
		{
			get { return name; }
		}

		public bool IsLazy
		{
			get { return lazy != HbmRestrictedLaziness.False; }
		}

		public string Access
		{
			get { return access; }
		}

		public bool OptimisticLock
		{
			get { return false; }
		}

		#endregion
	}
}