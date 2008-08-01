using System;
using System.Collections;
using System.Collections.Generic;
using Iesi.Collections.Generic;
using log4net;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Type;

namespace NHibernate.Cache
{
	/// <summary>
	/// The standard implementation of the Hibernate <see cref="IQueryCache" />
	/// interface.  This implementation is very good at recognizing stale query
	/// results and re-running queries when it detects this condition, recaching
	/// the new results.
	/// </summary>
	public class StandardQueryCache : IQueryCache
	{
		private static readonly ILog log = LogManager.GetLogger(typeof (StandardQueryCache));
		private readonly ICache queryCache;
		private readonly string regionName;
		private readonly UpdateTimestampsCache updateTimestampsCache;

		public StandardQueryCache(Settings settings, IDictionary<string, string> props,
		                          UpdateTimestampsCache updateTimestampsCache, string regionName)
		{
			if (regionName == null)
			{
				regionName = typeof (StandardQueryCache).FullName;
			}
			String prefix = settings.CacheRegionPrefix;
			if (!string.IsNullOrEmpty(prefix))
			{
				regionName = prefix + '.' + regionName;
			}

			log.Info("starting query cache at region: " + regionName);
			queryCache = settings.CacheProvider.BuildCache(regionName, props);
			this.updateTimestampsCache = updateTimestampsCache;
			this.regionName = regionName;
		}

		#region IQueryCache Members

		public ICache Cache
		{
			get { return queryCache; }
		}

		public string RegionName
		{
			get { return regionName; }
		}

		public void Clear()
		{
			queryCache.Clear();
		}

		public bool Put(QueryKey key, ICacheAssembler[] returnTypes, IList result, bool isNaturalKeyLookup,
		                ISessionImplementor session)
		{
			if (isNaturalKeyLookup && result.Count == 0)
			{
				return false;
			}
			else
			{
				long ts = session.Timestamp;

				if (log.IsDebugEnabled)
				{
					log.Debug("caching query results in region: " + regionName);
				}

				IList cacheable = new ArrayList(result.Count + 1);
				cacheable.Add(ts);
				for (int i = 0; i < result.Count; i++)
				{
					if (returnTypes.Length == 1)
					{
						cacheable.Add(returnTypes[0].Disassemble(result[i], session, null));
					}
					else
					{
						cacheable.Add(TypeFactory.Disassemble((object[]) result[i], returnTypes, null, session, null));
					}
				}
				queryCache.Put(key, cacheable);
				return true;
			}
		}

		public IList Get(QueryKey key, ICacheAssembler[] returnTypes, bool isNaturalKeyLookup, ISet<string> spaces,
		                 ISessionImplementor session)
		{
			if (log.IsDebugEnabled)
			{
				log.Debug("checking cached query results in region: " + regionName);
			}
			IList cacheable = (IList)queryCache.Get(key);
			if (cacheable == null)
			{
				log.Debug("query results were not found in cache");
				return null;
			}
			long timestamp = (long)cacheable[0];
			log.Debug("Checking query spaces for up-to-dateness [" + spaces + "]");
			if (!isNaturalKeyLookup && !IsUpToDate(spaces, timestamp))
			{
				log.Debug("cached query results were not up to date");
				return null;
			}

			log.Debug("returning cached query results");
			for (int i = 1; i < cacheable.Count; i++)
			{
				if (returnTypes.Length == 1)
				{
					returnTypes[0].BeforeAssemble(cacheable[i], session);
				}
				else
				{
					TypeFactory.BeforeAssemble((object[])cacheable[i], returnTypes, session);
				}
			}
			IList result = new ArrayList(cacheable.Count - 1);
			for (int i = 1; i < cacheable.Count; i++)
			{
				try
				{
					if (returnTypes.Length == 1)
					{
						result.Add(returnTypes[0].Assemble(cacheable[i], session, null));
					}
					else
					{
						result.Add(TypeFactory.Assemble((object[])cacheable[i], returnTypes, session, null));
					}
				}
				catch (UnresolvableObjectException)
				{
					if (isNaturalKeyLookup)
					{
						//TODO: not really completely correct, since
						//      the UnresolvableObjectException could occur while resolving
						//      associations, leaving the PC in an
						//      inconsistent state
						log.Debug("could not reassemble cached result set");
						queryCache.Remove(key);
						return null;
					}
					else
					{
						throw;
					}
				}
			}
			return result;
		}

		public void Destroy()
		{
			try
			{
				queryCache.Destroy();
			}
			catch (Exception e)
			{
				log.Warn("could not destroy query cache: " + regionName, e);
			}
		}

		#endregion

		protected bool IsUpToDate(ISet<string> spaces, long timestamp)
		{
			return updateTimestampsCache.IsUpToDate(spaces, timestamp);
		}
	}
}