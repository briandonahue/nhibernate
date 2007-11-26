using System;
using System.Collections.Generic;
using System.IO;
using Iesi.Collections.Generic;
using log4net;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using NHibernate.Engine;
using NHibernate.Expression;
using NHibernate.Impl;
using System.Collections;
using NHibernate.Search.Engine;
using NHibernate.Search.Impl;
using Directory = Lucene.Net.Store.Directory;

namespace NHibernate.Search.Impl
{
	public class FullTextQueryImpl : AbstractQueryImpl
	{
		private static ILog log = LogManager.GetLogger(typeof (FullTextQueryImpl));
		private Query luceneQuery;
		private System.Type[] classes;
#if NET_2_0
		private ISet<System.Type> classesAndSubclasses;
#else
		private ISet classesAndSubclasses;
#endif
		private int resultSize;
		private int batchSize = 1;

		/// <summary>
		/// classes must be immutable
		/// </summary>
		public FullTextQueryImpl(Query query, System.Type[] classes, ISessionImplementor session)
			: base(query.ToString(), FlushMode.Unspecified, session)
		{
			this.luceneQuery = query;
			this.classes = classes;
		}

#if NET_2_0
		public override IEnumerable Enumerable()
		{
			return Enumerable<object>();
		}

		/// <summary>
		/// Return an interator on the results.
		/// Retrieve the object one by one (initialize it during the next() operation)
		/// </summary>
		public override IEnumerable<T> Enumerable<T>()
		{
			//implement an interator which keep the id/class for each hit and get the object on demand
			//cause I can't keep the searcher and hence the hit opened. I dont have any hook to know when the
			//user stop using it
			//scrollable is better in this area

			SearchFactory searchFactory = SearchFactory.GetSearchFactory(Session);
			//find the directories
			Searcher searcher = FullTextSearchHelper.BuildSearcher(searchFactory, out classesAndSubclasses, classes);
			if (searcher == null)
			{
				return new IteratorImpl<T>(new List<EntityInfo>(), Session).Iterate();
			}
			try
			{
				Query query = FullTextSearchHelper.FilterQueryByClasses(classesAndSubclasses, luceneQuery);
				Hits hits = searcher.Search(query);
				SetResultSize(hits);
				int first = First();
				int max = Max(first, hits);
				IList<EntityInfo> entityInfos = new List<EntityInfo>(max - first + 1);
				for (int index = first; index <= max; index++)
				{
					Document document = hits.Doc(index);
					EntityInfo entityInfo = new EntityInfo();
					entityInfo.clazz = DocumentBuilder.GetDocumentClass(document);
					entityInfo.id = DocumentBuilder.GetDocumentId(searchFactory, document);
					entityInfos.Add(entityInfo);
				}
				return new IteratorImpl<T>(entityInfos, Session).Iterate();
			}
			catch (IOException e)
			{
				throw new HibernateException("Unable to query Lucene index", e);
			}
			finally
			{
				if (searcher != null)
				{
					try
					{
						searcher.Close();
					}
					catch (IOException e)
					{
						log.Warn("Unable to properly close searcher during lucene query: " + QueryString, e);
					}
				}
			}
		}

		private class IteratorImpl<T>
		{
			private IList<EntityInfo> entityInfos;
			private ISession session;

			public IteratorImpl(IList<EntityInfo> entityInfos, ISession session)
			{
				this.entityInfos = entityInfos;
				this.session = session;
			}

			public IEnumerable<T> Iterate()
			{
				foreach (EntityInfo entityInfo in entityInfos)
				{
					yield return (T) session.Load(entityInfo.clazz, entityInfo.id);
				}
			}
		}


		public override IList<T> List<T>()
		{
			ArrayList arrayList = new ArrayList();
			List(arrayList);
			return (T[]) arrayList.ToArray(typeof (T));
		}
#endif


		public override IList List()
		{
			ArrayList arrayList = new ArrayList();
			List(arrayList);
			return arrayList;
		}

		public override void List(IList list)
		{
			SearchFactory searchFactory = SearchFactory.GetSearchFactory(Session);
			//find the directories
			Searcher searcher = FullTextSearchHelper.BuildSearcher(searchFactory, out classesAndSubclasses, classes);
			if (searcher == null)
				return;
			try
			{
				Query query = FullTextSearchHelper.FilterQueryByClasses(classesAndSubclasses, luceneQuery);
				Hits hits = searcher.Search(query);
				SetResultSize(hits);
				int first = First();
				int max = Max(first, hits);
				for (int index = first; index <= max; index++)
				{
					Document document = hits.Doc(index);
					System.Type clazz = DocumentBuilder.GetDocumentClass(document);
					object id = DocumentBuilder.GetDocumentId(searchFactory, document);
					list.Add(this.Session.Load(clazz, id));
					//use load to benefit from the batch-size
					//we don't face proxy casting issues since the exact class is extracted from the index
				}
				//then initialize the objects
				IList excludedObects = new ArrayList();
				foreach (Object element in list)
				{
					try
					{
						NHibernateUtil.Initialize(element);
					}
					catch (ObjectNotFoundException e)
					{
						log.Debug("Object found in Search index but not in database: "
						          + e.PersistentClass + " with id " + e.Identifier);
						excludedObects.Add(element);
					}
				}
				foreach (object excludedObect in excludedObects)
				{
					list.Remove(excludedObect);
				}
			}
			catch (IOException e)
			{
				throw new HibernateException("Unable to query Lucene index", e);
			}
			finally
			{
				if (searcher != null)
				{
					try
					{
						searcher.Close();
					}
					catch (IOException e)
					{
						log.Warn("Unable to properly close searcher during lucene query: " + QueryString, e);
					}
				}
			}
		}

		private int Max(int first, Hits hits)
		{
			if (Selection.MaxRows == RowSelection.NoValue)
				return hits.Length() - 1;
			else if (Selection.MaxRows  + first < hits.Length())
				return first + Selection.MaxRows - 1;
			else return hits.Length() - 1;
		}

		private int First()
		{
			if (Selection.FirstRow != RowSelection.NoValue)
				return Selection.FirstRow;
			else 
				return 0;
		}

		//TODO change classesAndSubclasses by side effect, which is a mismatch with the Searcher return, fix that.

		private void SetResultSize(Hits hits)
		{
			resultSize = hits.Length();
		}

		public int ResultSize
		{
			get { return this.resultSize; }
		}

		private class EntityInfo
		{
			public System.Type clazz;
			public object id;
		}
	}
}