using System;
#if NET_2_0
using System.Collections.Generic;
#else
using System.Collections;
#endif
using System.IO;
using System.Threading;
using log4net;
using Lucene.Net.Index;
using NHibernate.Search.Engine;
using NHibernate.Search.Storage;

namespace NHibernate.Search.Impl
{
    //TODO introduce the notion of read only IndexReader? We cannot enforce it because Lucene use abstract classes, not interfaces
	/// <summary>
	/// Lucene workspace
	/// This is not intended to be used in a multithreaded environment
	/// <p/>
	/// One cannot execute modification through an IndexReader when an IndexWriter has been acquired on the same underlying directory
	/// One cannot get an IndexWriter when an IndexReader have been acquired and modificed on the same underlying directory
	/// The recommended approach is to execute all the modifications on the IndexReaders, {@link #Dispose()} }, and acquire the
	/// index writers
	/// </summary>
	public class Workspace : IDisposable
	{
		private static ILog log = LogManager.GetLogger(typeof(Workspace));
#if NET_2_0
		private Dictionary<IDirectoryProvider, IndexReader> readers = new Dictionary<IDirectoryProvider, IndexReader>();
		private Dictionary<IDirectoryProvider, IndexWriter> writers = new Dictionary<IDirectoryProvider, IndexWriter>();
		private List<IDirectoryProvider> lockedProviders = new List<IDirectoryProvider>();
#else
		private Hashtable readers = new Hashtable();
		private Hashtable writers = new Hashtable();
		private IList lockedProviders = new ArrayList();
#endif
		private SearchFactory searchFactory;

		public Workspace(SearchFactory searchFactory)
		{
			this.searchFactory = searchFactory;
		}


		public DocumentBuilder GetDocumentBuilder(System.Type entity)
		{
			return searchFactory.GetDocumentBuilder(entity);
		}

		public IndexReader GetIndexReader(System.Type entity)
		{
			//TODO NPEs
			IDirectoryProvider provider = searchFactory.GetDirectoryProvider(entity);
			//one cannot access a reader for update after a writer has been accessed
			if (writers.ContainsKey(provider))
				throw new AssertionFailure("Tries to read for update a index while a writer is accessed" + entity);
			IndexReader reader = null;
#if NET_2_0
			readers.TryGetValue(provider, out reader);
#else
			if (readers.ContainsKey(provider))
				reader = (IndexReader) readers[provider];
#endif
			if (reader != null) return reader;
			LockProvider(provider);
			try
			{
				reader = IndexReader.Open(provider.Directory);
				readers.Add(provider, reader);
			}
			catch (IOException e)
			{
				CleanUp(new SearchException("Unable to open IndexReader for " + entity, e));
			}
			return reader;
		}

		public IndexWriter GetIndexWriter(System.Type entity)
		{
			IDirectoryProvider provider = searchFactory.GetDirectoryProvider(entity);
			//one has to close a reader for update before a writer is accessed
			IndexReader reader = null;
#if NET_2_0
			readers.TryGetValue(provider, out reader);
#else
			if (readers.ContainsKey(provider))
				reader = (IndexReader) readers[provider];
#endif
			if (reader != null)
			{
				try
				{
					reader.Close();
				}
				catch (IOException e)
				{
					throw new SearchException("Exception while closing IndexReader", e);
				}
				readers.Remove(provider);
			}
			IndexWriter writer = null;
#if NET_2_0
			writers.TryGetValue(provider, out writer);
#else
			if (writers.ContainsKey(provider))
				writer = (IndexWriter) writers[provider];
#endif
			if (writer != null) return writer;
			LockProvider(provider);
			try
			{
				writer = new IndexWriter(
					provider.Directory, searchFactory.GetDocumentBuilder(entity).Analyzer, false
					); //have been created at init time
				writers.Add(provider, writer);
			}
			catch (IOException e)
			{
				CleanUp(new SearchException("Unable to open IndexWriter for " + entity, e));
			}
			return writer;
		}

		private void LockProvider(IDirectoryProvider provider)
		{
			//make sure to use a semaphore
			object syncLock = searchFactory.GetLockObjForDirectoryProvider(provider);
			Monitor.Enter(syncLock);
			lockedProviders.Add(provider);
		}

		private void CleanUp(SearchException originalException)
		{
			//release all readers and writers, then release locks
			SearchException raisedException = originalException;
			foreach (IndexReader reader in readers.Values)
			{
				try
				{
					reader.Close();
				}
				catch (IOException e)
				{
					if (raisedException != null)
					{
						log.Error("Subsequent Exception while closing IndexReader", e);
					}
					else
					{
						raisedException = new SearchException("Exception while closing IndexReader", e);
					}
				}
			}
			foreach (IndexWriter writer in writers.Values)
			{
				try
				{
					writer.Close();
				}
				catch (IOException e)
				{
					if (raisedException != null)
					{
						log.Error("Subsequent Exception while closing IndexWriter", e);
					}
					else
					{
						raisedException = new SearchException("Exception while closing IndexWriter", e);
					}
				}
			}
			foreach (IDirectoryProvider provider in lockedProviders)
			{
				object syncLock = searchFactory.GetLockObjForDirectoryProvider(provider);
				Monitor.Exit(syncLock);
			}
			readers.Clear();
			writers.Clear();
			lockedProviders.Clear();
			if (raisedException != null) throw raisedException;
		}

		/// <summary>
		/// release resources consumed in the workspace if any
		/// </summary>
		public void Dispose()
		{
			CleanUp(null);
		}
	}
}