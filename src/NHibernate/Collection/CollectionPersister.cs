using System;
using System.Collections;
using System.Data;
using Iesi.Collections;
using log4net;
using NHibernate.Cache;
using NHibernate.Cfg;
using NHibernate.Engine;
using NHibernate.Id;
using NHibernate.Loader;
using NHibernate.Mapping;
using NHibernate.Metadata;
using NHibernate.SqlCommand;
using NHibernate.Type;
using NHibernate.Util;
using Array = NHibernate.Mapping.Array;

namespace NHibernate.Collection
{
	/// <summary>
	/// Plugs into an instance of <c>PersistentCollection</c>, in order to implement
	/// persistence of that collection while in a particular role.
	/// </summary>
	/// <remarks>
	/// May be considered an immutable view of the mapping object
	/// </remarks>
	public sealed class CollectionPersister : ICollectionMetadata
	{
		private static readonly ILog log = LogManager.GetLogger( typeof( CollectionPersister ) );

		private readonly SqlString sqlDeleteString;
		private readonly SqlString sqlInsertRowString;
		private readonly SqlString sqlUpdateRowString;
		private readonly SqlString sqlDeleteRowString;

		private readonly string sqlOrderByString;
		private readonly string sqlOrderByStringTemplate;
		private readonly string sqlWhereString;
		private readonly string sqlWhereStringTemplate;

		private readonly bool hasOrder;
		private readonly bool hasWhere;
		private readonly bool hasOrphanDelete;
		private readonly IType keyType;
		private readonly IType indexType;
		private readonly IType elementType;
		private readonly string[ ] keyColumnNames;
		private readonly string[ ] indexColumnNames;
		private readonly string[ ] elementColumnNames;
		private readonly string[ ] rowSelectColumnNames;

		private readonly string[ ] indexColumnAliases;
		private readonly string[ ] elementColumnAliases;
		private readonly string[ ] keyColumnAliases;

		private readonly IType rowSelectType;
		private readonly bool primitiveArray;
		private readonly bool array;
		private readonly bool isOneToMany;
		private readonly string qualifiedTableName;
		private readonly bool hasIndex;
		private readonly bool isLazy;
		private readonly bool isInverse;
		private readonly System.Type elementClass;
		private readonly ICacheConcurrencyStrategy cache;
		private readonly PersistentCollectionType collectionType;
		private readonly OuterJoinLoaderType enableJoinedFetch;
		private readonly System.Type ownerClass;

		private readonly IIdentifierGenerator identifierGenerator;
		private readonly string unquotedIdentifierColumnName;
		private readonly IType identifierType;
		private readonly bool hasIdentifier;
		private readonly string identifierColumnName;
		private readonly string identifierColumnAlias;

		private readonly ICollectionInitializer loader;

		private readonly string role;

		private readonly Dialect.Dialect dialect;
		private readonly ISessionFactoryImplementor factory;

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="datastore"></param>
		/// <param name="factory"></param>
		public CollectionPersister( Mapping.Collection collection, Configuration datastore, ISessionFactoryImplementor factory )
		{
			this.factory = factory;
			this.dialect = factory.Dialect;
			collectionType = collection.Type;
			role = collection.Role;
			ownerClass = collection.OwnerClass;
			Alias alias = new Alias( "__" );

			sqlOrderByString = collection.OrderBy;
			hasOrder = sqlOrderByString != null;
			sqlOrderByStringTemplate = hasOrder ? Template.RenderOrderByStringTemplate( sqlOrderByString, dialect ) : null;

			sqlWhereString = collection.Where;
			hasWhere = sqlWhereString != null;
			sqlWhereStringTemplate = hasWhere ? Template.RenderWhereStringTemplate( sqlWhereString, dialect ) : null;

			hasOrphanDelete = collection.OrphanDelete;

			cache = collection.Cache;

			keyType = collection.Key.Type;
			int span = collection.Key.ColumnSpan;
			keyColumnNames = new string[span];
			string[ ] keyAliases = new string[span];
			int k = 0;
			foreach( Column col in collection.Key.ColumnCollection )
			{
				keyColumnNames[ k ] = col.GetQuotedName( dialect );
				keyAliases[ k ] = col.Alias( dialect );
				k++;
			}
			keyColumnAliases = alias.ToAliasStrings( keyAliases, dialect );
			ISet distinctColumns = new HashedSet();
			CheckColumnDuplication( distinctColumns, collection.Key.ColumnCollection );

			isOneToMany = collection.IsOneToMany;
			primitiveArray = collection.IsPrimitiveArray;
			array = collection.IsArray;

			Table table;
			ICollection iter;

			if( isOneToMany )
			{
				EntityType type = collection.OneToMany.Type;
				elementType = type;
				PersistentClass associatedClass = datastore.GetClassMapping( type.PersistentClass );
				span = associatedClass.Identifier.ColumnSpan;
				iter = associatedClass.Key.ColumnCollection;
				table = associatedClass.Table;
				enableJoinedFetch = OuterJoinLoaderType.Eager;
			}
			else
			{
				table = collection.Table;
				elementType = collection.Element.Type;
				span = collection.Element.ColumnSpan;
				enableJoinedFetch = collection.Element.OuterJoinFetchSetting;
				iter = collection.Element.ColumnCollection;
				CheckColumnDuplication( distinctColumns, collection.Element.ColumnCollection );

			}

			qualifiedTableName = table.GetQualifiedName( dialect, factory.DefaultSchema );
			string[ ] aliases = new string[span];
			elementColumnNames = new string[span];
			int j = 0;
			foreach( Column col in iter )
			{
				elementColumnNames[ j ] = col.GetQuotedName( dialect );
				aliases[ j ] = col.Alias( dialect );
				j++;
			}

			elementColumnAliases = alias.ToAliasStrings( aliases, dialect );

			IType selectColumns;
			string[ ] selectType;

			if( hasIndex = collection.IsIndexed )
			{
				IndexedCollection indexedCollection = ( IndexedCollection ) collection;

				indexType = indexedCollection.Index.Type;
				int indexSpan = indexedCollection.Index.ColumnSpan;
				indexColumnNames = new string[indexSpan];

				string[ ] indexAliases = new string[indexSpan];
				int i = 0;
				foreach( Column indexCol in indexedCollection.Index.ColumnCollection )
				{
					indexAliases[ i ] = indexCol.Alias( dialect );
					indexColumnNames[ i ] = indexCol.GetQuotedName( dialect );
					i++;
				}
				selectType = indexColumnNames;
				selectColumns = indexType;
				indexColumnAliases = alias.ToAliasStrings( indexAliases, dialect );
				CheckColumnDuplication( distinctColumns, indexedCollection.Index.ColumnCollection );

			}
			else
			{
				indexType = null;
				indexColumnNames = null;
				indexColumnAliases = null;
				selectType = elementColumnNames;
				selectColumns = elementType;
			}

			hasIdentifier = collection.IsIdentified;

			if( hasIdentifier )
			{
				if( isOneToMany )
				{
					throw new MappingException( "one-to-many collections with identifiers are not supported." );
				}
				IdentifierCollection idColl = ( IdentifierCollection ) collection;
				identifierType = idColl.Identifier.Type;

				Column col = null;
				foreach( Column column in idColl.Identifier.ColumnCollection )
				{
					col = column;
					break;
				}

				identifierColumnName = col.GetQuotedName( dialect );
				selectType = new string[ ] {identifierColumnName};
				selectColumns = identifierType;
				identifierColumnAlias = alias.ToAliasString( col.Alias( dialect ), dialect );
				unquotedIdentifierColumnName = identifierColumnAlias;
				identifierGenerator = idColl.Identifier.CreateIdentifierGenerator( dialect );
				CheckColumnDuplication( distinctColumns, idColl.Identifier.ColumnCollection );
			}
			else
			{
				identifierType = null;
				identifierColumnName = null;
				identifierColumnAlias = null;
				unquotedIdentifierColumnName = null;
				identifierGenerator = null;
			}

			rowSelectColumnNames = selectType;
			rowSelectType = selectColumns;

			// TODO: refactor AddColumn method in insert to AddColumns
			sqlDeleteString = GenerateSqlDeleteString();
			sqlInsertRowString = GenerateSqlInsertRowString();
			sqlUpdateRowString = GenerateSqlUpdateRowString();
			sqlDeleteRowString = GenerateSqlDeleteRowString();

			isLazy = collection.IsLazy;

			isInverse = collection.IsInverse;

			if( collection.IsArray )
			{
				elementClass = ( ( Array ) collection ).ElementClass;
			}
			else
			{
				// for non-arrays, we don't need to know the element class
				elementClass = null;
			}
			loader = CreateCollectionQuery( factory );
		}

		/// <summary>
		/// 
		/// </summary>
		public ICollectionInitializer Initializer
		{
			get { return loader; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="factory"></param>
		/// <returns></returns>
		public ICollectionInitializer CreateCollectionQuery( ISessionFactoryImplementor factory )
		{
			return isOneToMany ?
				( ICollectionInitializer ) new OneToManyLoader( this, factory ) :
				( ICollectionInitializer ) new CollectionLoader( this, factory );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="coll"></param>
		/// <param name="s"></param>
		public void Cache( object id, PersistentCollection coll, ISessionImplementor s )
		{
			if( cache != null )
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Caching collection: " + role + "#" + id );
				}
				cache.Put( id, coll.Disassemble( this ), s.Timestamp );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public ICacheConcurrencyStrategy CacheConcurrencyStrategy
		{
			get { return cache; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool HasCache
		{
			get { return cache != null; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="owner"></param>
		/// <param name="s"></param>
		/// <returns></returns>
		public PersistentCollection GetCachedCollection( object id, object owner, ISessionImplementor s )
		{
			if( cache == null )
			{
				return null;
			}
			else
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Searching for collection in cache: " + role + "#" + id );
				}
				object cached = cache.Get( id, s.Timestamp );
				if( cached == null )
				{
					return null;
				}
				else
				{
					return collectionType.AssembleCachedCollection( s, this, cached, owner );
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void Softlock( object id )
		{
			if( cache != null )
			{
				cache.Lock( id );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		public void ReleaseSoftlock( object id )
		{
			if( cache != null )
			{
				cache.Release( id );
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public PersistentCollectionType CollectionType
		{
			get { return this.collectionType; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public string GetSQLWhereString( string alias )
		{
			if( sqlWhereStringTemplate != null )
			{
				return StringHelper.Replace( sqlWhereStringTemplate, Template.PlaceHolder, alias );
			}
			else
			{
				return null;
			}

		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public string GetSQLOrderByString( string alias )
		{
			if( sqlOrderByStringTemplate != null )
			{
				return StringHelper.Replace( sqlOrderByStringTemplate, Template.PlaceHolder, alias );
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		public OuterJoinLoaderType EnableJoinFetch
		{
			get { return enableJoinedFetch; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool HasOrdering
		{
			get { return hasOrder; }
		}

		/// <summary>
		/// 
		/// </summary>
		public bool HasWhere
		{
			get { return hasWhere; }
		}

		/// <summary>
		/// 
		/// </summary>
		public SqlString SqlDeleteString
		{
			get { return sqlDeleteString; }
		}

		/// <summary>
		/// 
		/// </summary>
		public SqlString SqlInsertRowString
		{
			get { return sqlInsertRowString; }
		}

		/// <summary>
		/// 
		/// </summary>
		public SqlString SqlUpdateRowString
		{
			get { return sqlUpdateRowString; }
		}

		/// <summary>
		/// 
		/// </summary>
		public SqlString SqlDeleteRowString
		{
			get { return sqlDeleteRowString; }
		}

		/// <summary></summary>
		public IType KeyType
		{
			get { return keyType; }
		}

		/// <summary></summary>
		public IType IndexType
		{
			get { return indexType; }
		}

		/// <summary></summary>
		public IType ElementType
		{
			get { return elementType; }
		}

		/// <summary></summary>
		public System.Type ElementClass
		{
			// needed by arrays
			get { return elementClass; }
		}


		/// <summary>
		/// Gets just the Identifier of the Element for the Collection.
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="owner"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		/// <remarks>
		/// This was created in addition to ReadElement because ADO.NET does not allow
		/// for 2 IDataReaders to be open against a single IDbConnection at one time.  
		/// 
		/// When a Collection is loaded it was recursively opening IDbDataReaders to resolve
		/// the Element for the Collection while the IDbDataReader was open that contained the
		/// record for the Collection.
		/// </remarks>		
		public object ReadElementIdentifier( IDataReader rs, object owner, ISessionImplementor session )
		{
			return ElementType.Hydrate( rs, elementColumnAliases, session, owner );
		}

		/// <summary>
		/// Reads the Element from the IDataReader.  The IDataReader will probably only contain
		/// the id of the Element.
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="owner"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		/// <remarks>See ReadElementIdentifier for an explanation of why this method will be depreciated.</remarks>
		public object ReadElement( IDataReader rs, object owner, ISessionImplementor session )
		{
			object element = ElementType.NullSafeGet( rs, elementColumnAliases, session, owner );
			return element;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		public object ReadIndex( IDataReader rs, ISessionImplementor session )
		{
			object index = IndexType.NullSafeGet( rs, indexColumnAliases, session, null );
			if( index == null )
			{
				throw new HibernateException( "null index column for collection: " + role );
			}
			return index;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="rs"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		public object ReadIdentifier( IDataReader rs, ISessionImplementor session )
		{
			object id = IdentifierType.NullSafeGet( rs, unquotedIdentifierColumnName, session, null );
			if( id == null )
			{
				throw new HibernateException( "null identifier column for collection: " + role );
			}
			return id;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dr"></param>
		/// <param name="session"></param>
		/// <returns></returns>
		public object ReadKey( IDataReader dr, ISessionImplementor session )
		{
			return KeyType.NullSafeGet( dr, keyColumnAliases, session, null );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="st"></param>
		/// <param name="elt"></param>
		/// <param name="writeOrder"></param>
		/// <param name="session"></param>
		public void WriteElement( IDbCommand st, object elt, bool writeOrder, ISessionImplementor session )
		{
			ElementType.NullSafeSet( st, elt, ( writeOrder ? 0 : keyColumnNames.Length + ( hasIndex ? indexColumnNames.Length : 0 ) + ( hasIdentifier ? 1 : 0 ) ), session );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="st"></param>
		/// <param name="idx"></param>
		/// <param name="writeOrder"></param>
		/// <param name="session"></param>
		public void WriteIndex( IDbCommand st, object idx, bool writeOrder, ISessionImplementor session )
		{
			IndexType.NullSafeSet( st, idx, keyColumnNames.Length + ( writeOrder ? elementColumnNames.Length : 0 ), session );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="st"></param>
		/// <param name="idx"></param>
		/// <param name="writeOrder"></param>
		/// <param name="session"></param>
		public void WriteIdentifier( IDbCommand st, object idx, bool writeOrder, ISessionImplementor session )
		{
			IdentifierType.NullSafeSet( st, idx, ( writeOrder ? elementColumnNames.Length : keyColumnNames.Length ), session );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="st"></param>
		/// <param name="idx"></param>
		/// <param name="session"></param>
		private void WriteRowSelect( IDbCommand st, object idx, ISessionImplementor session )
		{
			rowSelectType.NullSafeSet( st, idx, ( HasIdentifier ? 0 : keyColumnNames.Length ), session );
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="st"></param>
		/// <param name="id"></param>
		/// <param name="writeOrder"></param>
		/// <param name="session"></param>
		public void WriteKey( IDbCommand st, object id, bool writeOrder, ISessionImplementor session )
		{
			if( id == null )
			{
				throw new NullReferenceException( "Null key for collection: " + role );
			}
			KeyType.NullSafeSet( st, id, ( writeOrder ? elementColumnNames.Length : 0 ), session );
		}

		/// <summary></summary>
		public bool IsPrimitiveArray
		{
			get { return primitiveArray; }
		}

		/// <summary></summary>
		public bool IsArray
		{
			get { return array; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public string SelectClauseFragment( string alias )
		{
			SelectFragment frag = new SelectFragment( factory.Dialect )
				.SetSuffix( String.Empty )
				.AddColumns( alias, elementColumnNames, elementColumnAliases );
			if( hasIndex )
			{
				frag.AddColumns( alias, indexColumnNames, indexColumnAliases );
			}
			if( hasIdentifier )
			{
				frag.AddColumn( alias, identifierColumnName, identifierColumnAlias );
			}
			// TODO: fix this once the interface is changed from a String to a SqlString
			// this works for now because there are no parameters in the select string.
			return frag.ToSqlStringFragment( false )
				.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="alias"></param>
		/// <returns></returns>
		public SqlString MultiselectClauseFragment( string alias )
		{
			SelectFragment frag = new SelectFragment( dialect )
				.SetSuffix( String.Empty )
				.AddColumns( alias, elementColumnNames, elementColumnAliases )
				.AddColumns( alias, keyColumnNames, keyColumnAliases );
			if( hasIndex )
			{
				frag.AddColumns( alias, indexColumnNames, indexColumnAliases );
			}
			if( hasIdentifier )
			{
				frag.AddColumn( alias, identifierColumnName, identifierColumnAlias );
			}

			return frag.ToSqlStringFragment( false );
		}

		private SqlString GenerateSqlDeleteString()
		{
			if( isOneToMany )
			{
				SqlUpdateBuilder update = new SqlUpdateBuilder( factory );
				update.SetTableName( qualifiedTableName )
					.AddColumns( keyColumnNames, "null" );
				if( hasIndex )
				{
					update.AddColumns( indexColumnNames, "null" );
				}
				if( hasWhere )
				{
					update.AddWhereFragment( sqlWhereString );
				}
				update.SetIdentityColumn( keyColumnNames, keyType );

				return update.ToSqlString();
			}
			else
			{
				SqlDeleteBuilder delete = new SqlDeleteBuilder( factory );
				delete.SetTableName( qualifiedTableName )
					.SetIdentityColumn( keyColumnNames, keyType );
				if( hasWhere )
				{
					delete.AddWhereFragment( sqlWhereString );
				}

				return delete.ToSqlString();
			}

		}

		private SqlString GenerateSqlInsertRowString()
		{
			if( isOneToMany )
			{
				SqlUpdateBuilder update = new SqlUpdateBuilder( factory );
				update.SetTableName( qualifiedTableName )
					.AddColumns( keyColumnNames, keyType );
				if( hasIndex )
				{
					update.AddColumns( indexColumnNames, indexType );
				}
				update.SetIdentityColumn( elementColumnNames, elementType );
				return update.ToSqlString();

			}
			else
			{
				SqlInsertBuilder insert = new SqlInsertBuilder( factory );
				insert.SetTableName( qualifiedTableName )
					.AddColumn( keyColumnNames, keyType );
				if( hasIndex )
				{
					insert.AddColumn( indexColumnNames, indexType );
				}
				if( hasIdentifier )
				{
					insert.AddColumn( new string[ ] {identifierColumnName}, identifierType );
				}
				insert.AddColumn( elementColumnNames, elementType );

				return insert.ToSqlString();
			}

		}

		private SqlString GenerateSqlUpdateRowString()
		{
			if( isOneToMany )
			{
				return null;
			}
			else
			{
				SqlUpdateBuilder update = new SqlUpdateBuilder( factory );
				update.SetTableName( qualifiedTableName )
					.AddColumns( elementColumnNames, elementType );
				if( hasIdentifier )
				{
					update.AddWhereFragment( rowSelectColumnNames, rowSelectType, " = " );
				}
				else
				{
					update.AddWhereFragment( keyColumnNames, keyType, " = " )
						.AddWhereFragment( rowSelectColumnNames, rowSelectType, " = " );
				}

				return update.ToSqlString();

			}
		}

		private SqlString GenerateSqlDeleteRowString()
		{
			if( isOneToMany )
			{
				SqlUpdateBuilder update = new SqlUpdateBuilder( factory );
				update.SetTableName( qualifiedTableName )
					.AddColumns( keyColumnNames, "null" );

				if( hasIndex )
				{
					update.AddColumns( indexColumnNames, "null" );
				}

				if( hasIdentifier )
				{
					update.AddWhereFragment( rowSelectColumnNames, rowSelectType, " = " );
				}
				else
				{
					update.AddWhereFragment( keyColumnNames, keyType, " = " );
					update.AddWhereFragment( rowSelectColumnNames, rowSelectType, " = " );
				}

				return update.ToSqlString();

			}
			else
			{
				SqlDeleteBuilder delete = new SqlDeleteBuilder( factory );
				delete.SetTableName( qualifiedTableName );
				if( hasIdentifier )
				{
					delete.AddWhereFragment( rowSelectColumnNames, rowSelectType, " = " );
				}
				else
				{
					delete.AddWhereFragment( keyColumnNames, keyType, " = " )
						.AddWhereFragment( rowSelectColumnNames, rowSelectType, " = " );
				}

				return delete.ToSqlString();

			}

		}

		/// <summary></summary>
		public string[ ] IndexColumnNames
		{
			get { return indexColumnNames; }
		}

		/// <summary></summary>
		public string[ ] ElementColumnNames
		{
			get { return elementColumnNames; }
		}

		/// <summary></summary>
		public string[ ] KeyColumnNames
		{
			get { return keyColumnNames; }
		}

		/// <summary></summary>
		public bool IsOneToMany
		{
			get { return isOneToMany; }
		}

		/// <summary></summary>
		public bool HasIndex
		{
			get { return hasIndex; }
		}

		/// <summary></summary>
		public bool IsLazy
		{
			get { return isLazy; }
		}

		/// <summary></summary>
		public bool IsInverse
		{
			get { return isInverse; }
		}

		/// <summary></summary>
		public string QualifiedTableName
		{
			get { return qualifiedTableName; }
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="session"></param>
		public void Remove( object id, ISessionImplementor session )
		{
			if( !isInverse )
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Deleting collection: " + role + "#" + id );
				}

				IDbCommand st = session.Batcher.PrepareBatchCommand( SqlDeleteString );

				try
				{
					WriteKey( st, id, false, session );
					session.Batcher.AddToBatch( -1 );
				} 
					// TODO: change to SqlException
				catch( Exception e )
				{
					session.Batcher.AbortBatch( e );
					throw;
				}
				if( log.IsDebugEnabled )
				{
					log.Debug( "done deleting collection" );
				}

			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="id"></param>
		/// <param name="session"></param>
		public void Recreate( PersistentCollection collection, object id, ISessionImplementor session )
		{
			if( !isInverse )
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Inserting collection: " + role + "#" + id );
				}

				ICollection entries = collection.Entries();
				if( entries.Count > 0 )
				{
					IDbCommand st = session.Batcher.PrepareBatchCommand( SqlInsertRowString );

					int i = 0;
					try
					{
						foreach( object entry in entries )
						{
							if( collection.EntryExists( entry, i ) )
							{
								collection.PreInsert( this, entry, i ); //TODO: (Big): this here screws up batch - H2.0.3 comment
								WriteKey( st, id, false, session );
								collection.WriteTo( st, this, entry, i, false );
								session.Batcher.AddToBatch( 1 );
							}
							i++;
						}
					} 
						//TODO: change to SqlException
					catch( Exception e )
					{
						session.Batcher.AbortBatch( e );
						throw;
					}
					if( log.IsDebugEnabled )
					{
						log.Debug( "done inserting collection" );
					}
				}
				else
				{
					if( log.IsDebugEnabled )
					{
						log.Debug( "collection was empty" );
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="id"></param>
		/// <param name="session"></param>
		public void DeleteRows( PersistentCollection collection, object id, ISessionImplementor session )
		{
			if( !isInverse )
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Deleting rows of collection: " + role + "#" + id );
				}

				ICollection entries = collection.GetDeletes( elementType );
				if( entries.Count > 0 )
				{
					IDbCommand st = session.Batcher.PrepareBatchCommand( SqlDeleteRowString );
					try
					{
						foreach( object entry in entries )
						{
							if( !hasIdentifier )
							{
								WriteKey( st, id, false, session );
							}
							WriteRowSelect( st, entry, session );
							session.Batcher.AddToBatch( -1 );
						}
					} 
						// TODO: change to SqlException
					catch( Exception e )
					{
						session.Batcher.AbortBatch( e );
						throw;
					}

					if( log.IsDebugEnabled )
					{
						log.Debug( "done deleting collection rows" );
					}
				}
				else
				{
					if( log.IsDebugEnabled )
					{
						log.Debug( "no rows to delete" );
					}
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="collection"></param>
		/// <param name="session"></param>
		public void Update( object id, PersistentCollection collection, ISessionImplementor session )
		{
			IDbCommand st = null;
			ICollection entries = collection.Entries();
			int i = 0;
			try
			{
				foreach( object entry in entries )
				{
					if( collection.NeedsUpdating( entry, i, elementType ) )
					{
						if( st == null )
						{
							st = session.Batcher.PrepareBatchCommand( SqlUpdateRowString );
						}
						if( !hasIdentifier )
						{
							WriteKey( st, id, true, session );
						}
						collection.WriteTo( st, this, entry, i, true );
						session.Batcher.AddToBatch( 1 );
					}
					i++;
				}
			} 
				// TODO: change to SqlException
			catch( Exception e )
			{
				session.Batcher.AbortBatch( e );
				throw;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="id"></param>
		/// <param name="collection"></param>
		/// <param name="session"></param>
		public void UpdateOneToMany( object id, PersistentCollection collection, ISessionImplementor session )
		{
			IDbCommand rmvst = null;
			int i = 0;
			ICollection entries = collection.Entries();
			try
			{
				foreach( object entry in entries )
				{
					if( collection.NeedsUpdating( entry, i, elementType ) )
					{
						if( rmvst == null )
						{
							rmvst = session.Batcher.PrepareBatchCommand( SqlDeleteRowString );
						}
						WriteKey( rmvst, id, false, session );
						WriteIndex( rmvst, collection.GetIndex( entry, i ), false, session );
						session.Batcher.AddToBatch( -1 );
					}
					i++;
				}
			} 
				// TODO: change to SqlException
			catch( Exception e )
			{
				session.Batcher.AbortBatch( e );
				throw;
			}

			// finish all the removes first to take care of possible unique constraints
			// and so that we can take advantage of batching
			IDbCommand insst = null;
			i = 0;
			entries = collection.Entries();
			try
			{
				foreach( object entry in entries )
				{
					if( collection.NeedsUpdating( entry, i, elementType ) )
					{
						if( insst == null )
						{
							insst = session.Batcher.PrepareBatchCommand( SqlInsertRowString );
						}
						WriteKey( insst, id, false, session );
						collection.WriteTo( insst, this, entry, i, false );
						session.Batcher.AddToBatch( 1 );
					}
					i++;
				}
			} 
				//TODO: change to SqlException
			catch( Exception e )
			{
				session.Batcher.AbortBatch( e );
				throw;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="id"></param>
		/// <param name="session"></param>
		public void UpdateRows( PersistentCollection collection, object id, ISessionImplementor session )
		{
			if( !isInverse )
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Updating rows of collection: " + role + "#" + id );
				}

				// update all the modified entries
				if( isOneToMany )
				{
					UpdateOneToMany( id, collection, session );
				}
				else
				{
					Update( id, collection, session );
				}

				if( log.IsDebugEnabled )
				{
					log.Debug( "done updating rows" );
				}
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="collection"></param>
		/// <param name="id"></param>
		/// <param name="session"></param>
		public void InsertRows( PersistentCollection collection, object id, ISessionImplementor session )
		{
			if( !isInverse )
			{
				if( log.IsDebugEnabled )
				{
					log.Debug( "Inserting rows of collection: " + role + "#" + id );
				}

				// insert all the new entries
				ICollection entries = collection.Entries();
				IDbCommand st = null;
				int i = 0;
				try
				{
					foreach( object entry in entries )
					{
						if( collection.NeedsInserting( entry, i, elementType ) )
						{
							collection.PreInsert( this, entry, i ); //TODO: (Big): this here screws up batching! H2.0.3 comment
							if( st == null )
							{
								st = session.Batcher.PrepareBatchCommand( SqlInsertRowString );
							}
							WriteKey( st, id, false, session );
							collection.WriteTo( st, this, entry, i, false );
							session.Batcher.AddToBatch( 1 );
						}
						i++;
					}
				}
					//TODO: change to SqlException
				catch( Exception e )
				{
					session.Batcher.AbortBatch( e );
					throw;
				}

				if( log.IsDebugEnabled )
				{
					log.Debug( "done inserting rows" );
				}
			}
		}

		/// <summary></summary>
		public string Role
		{
			get { return role; }
		}

		/// <summary></summary>
		public System.Type OwnerClass
		{
			get { return ownerClass; }
		}

		/// <summary></summary>
		public IIdentifierGenerator IdentifierGenerator
		{
			get { return identifierGenerator; }
		}

		/// <summary></summary>
		public IType IdentifierType
		{
			get { return identifierType; }
		}

		/// <summary></summary>
		public bool HasIdentifier
		{
			get { return hasIdentifier; }
		}

		/// <summary></summary>
		public bool HasOrphanDelete
		{
			get { return hasOrphanDelete; }
		}

		private void CheckColumnDuplication( ISet distinctColumns, ICollection columns )
		{
			foreach( Column col in columns )
			{
				if( distinctColumns.Contains( col.Name ) )
				{
					throw new MappingException(
						"Repeated column in mapping for collection: " +
							role +
							" column: " +
							col.Name
						);
				}
				else
				{
					distinctColumns.Add( col.Name );
				}
			}
		}

	}
}