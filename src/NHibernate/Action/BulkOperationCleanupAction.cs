using System;
using System.Collections;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using NHibernate.Engine;
using NHibernate.Persister.Entity;

namespace NHibernate.Action
{
	/// <summary>
	/// Implementation of BulkOperationCleanupAction.
	/// </summary>
	[Serializable]
	public class BulkOperationCleanupAction: IExecutable
	{
		private readonly ISessionImplementor session;
		private readonly HashedSet<string> affectedEntityNames= new HashedSet<string>();
		private readonly HashedSet<string> affectedCollectionRoles = new HashedSet<string>();
		private readonly List<string> spaces;

		public BulkOperationCleanupAction(ISessionImplementor session, IQueryable[] affectedQueryables)
		{
			this.session = session;
			List<string> tmpSpaces = new List<string>();
			for (int i = 0; i < affectedQueryables.Length; i++)
			{
				if (affectedQueryables[i].HasCache)
				{
					affectedEntityNames.Add(affectedQueryables[i].EntityName);
				}
				ISet<string> roles = session.Factory.GetCollectionRolesByEntityParticipant(affectedQueryables[i].EntityName);
				if (roles != null)
				{
					affectedCollectionRoles.AddAll(roles);
				}
				for (int y = 0; y < affectedQueryables[i].QuerySpaces.Length; y++)
				{
					tmpSpaces.Add(affectedQueryables[i].QuerySpaces[y]);
				}
			}
			spaces = new List<string>(tmpSpaces);
		}

		/// <summary>
		/// Create an action that will evict collection and entity regions based on queryspaces (table names).  
		/// </summary>
		public BulkOperationCleanupAction(ISessionImplementor session, ISet<string> querySpaces)
		{
			//from H3.2 TODO: cache the autodetected information and pass it in instead.
			this.session = session;

			ISet<string> tmpSpaces = new HashedSet<string>(querySpaces);
			ISessionFactoryImplementor factory = session.Factory;
			IDictionary acmd = factory.GetAllClassMetadata();
			foreach (DictionaryEntry entry in acmd)
			{
				string entityName = ((System.Type) entry.Key).FullName;
				IEntityPersister persister = factory.GetEntityPersister(entityName);
				string[] entitySpaces = persister.QuerySpaces;

				if (AffectedEntity(querySpaces, entitySpaces))
				{
					if (persister.HasCache)
					{
						affectedEntityNames.Add(persister.EntityName);
					}
					ISet<string> roles = session.Factory.GetCollectionRolesByEntityParticipant(persister.EntityName);
					if (roles != null)
					{
						affectedCollectionRoles.AddAll(roles);
					}
					for (int y = 0; y < entitySpaces.Length; y++)
					{
						tmpSpaces.Add(entitySpaces[y]);
					}
				}
			}
			spaces = new List<string>(tmpSpaces);
		}

		private bool AffectedEntity(ISet<string> querySpaces, string[] entitySpaces)
		{
			if (querySpaces == null || (querySpaces.Count == 0))
			{
				return true;
			}

			for (int i = 0; i < entitySpaces.Length; i++)
			{
				if (querySpaces.Contains(entitySpaces[i]))
				{
					return true;
				}
			}
			return false;
		}

		#region IExecutable Members

		public object[] PropertySpaces
		{
			get { return spaces.ToArray(); }
		}

		public void BeforeExecutions()
		{
			// nothing to do
		}

		public void Execute()
		{
			// nothing to do
		}

		public bool HasAfterTransactionCompletion()
		{
			return true;
		}

		public void AfterTransactionCompletion(bool success)
		{
			EvictEntityRegions();
			EvictCollectionRegions();
		}

		private void EvictCollectionRegions()
		{
			if (affectedCollectionRoles != null)
			{
				foreach (string roleName in affectedCollectionRoles)
				{
					session.Factory.EvictCollection(roleName);
				}
			}
		}

		private void EvictEntityRegions()
		{
			if(affectedEntityNames!=null)
			{
				foreach (string entityName in affectedEntityNames)
				{
					session.Factory.EvictEntity(entityName);
				}
			}
		}

		#endregion

		public virtual void Init()
		{
			EvictEntityRegions();
			EvictCollectionRegions();
		}
	}
}
