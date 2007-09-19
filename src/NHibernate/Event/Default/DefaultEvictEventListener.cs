using System;
using log4net;
using NHibernate.Engine;
using NHibernate.Impl;
using NHibernate.Persister.Entity;
using NHibernate.Proxy;

namespace NHibernate.Event.Default
{
	/// <summary> 
	/// Defines the default evict event listener used by hibernate for evicting entities
	/// in response to generated flush events.  In particular, this implementation will
	/// remove any hard references to the entity that are held by the infrastructure
	/// (references held by application or other persistent instances are okay) 
	/// </summary>
	[Serializable]
	public class DefaultEvictEventListener : IEvictEventListener
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(DefaultEvictEventListener));

		public void OnEvict(EvictEvent @event)
		{
			IEventSource source = @event.Session;
			object obj = @event.Entity;

			if (obj is INHibernateProxy)
			{
				LazyInitializer li = NHibernateProxyHelper.GetLazyInitializer((INHibernateProxy)obj);
				object id = li.Identifier;
				IEntityPersister persister = source.Factory.GetEntityPersister(li.PersistentClass);
				if (id == null)
				{
					throw new ArgumentException("null identifier");
				}
				EntityKey key = new EntityKey(id, persister);
				source.RemoveProxy(key);
				if (!li.IsUninitialized)
				{
					object entity = source.RemoveEntity(key);
					if (entity != null)
					{
						EntityEntry e = @event.Session.RemoveEntry(entity);
						DoEvict(entity, key, e.Persister, @event.Session);
					}
				}
				li.Session = null;
			}
			else
			{
				EntityEntry e = source.RemoveEntry(obj);
				if (e != null)
				{
					EntityKey key = new EntityKey(e.Id, e.Persister);
					source.RemoveEntity(key);
					DoEvict(obj, key, e.Persister, source);
				}
			}
		}

		protected internal void DoEvict(object obj, EntityKey key, IEntityPersister persister, IEventSource session)
		{

			if (log.IsDebugEnabled)
			{
				log.Debug("evicting " + MessageHelper.InfoString(persister));
			}

			// remove all collections for the entity from the session-level cache
			if (persister.HasCollections)
			{
				new EvictVisitor(session).Process(obj, persister);
			}

			Cascades.Cascade(session, persister, obj, Cascades.CascadingAction.ActionEvict, CascadePoint.CascadeOnEvict);
		}
	}
}
