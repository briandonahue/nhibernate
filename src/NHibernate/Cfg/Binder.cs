using System;
using System.Collections;
using System.Text;
using System.Xml;

using NHibernate.Engine;
using NHibernate.Loader;
using NHibernate.Mapping;
using NHibernate.Persister;
using NHibernate.Type;
using NHibernate.Util;

namespace NHibernate.Cfg {
	
	internal class Binder {
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(Binder));

		private static XmlNamespaceManager nsmgr;
		private static readonly string nsPrefix = "hbm";
		internal static Dialect.Dialect dialect;

		public static void BindClass(XmlNode node, PersistentClass model, Mappings mapping) 
		{
			
			string className = node.Attributes["name"] == null ? null : node.Attributes["name"].Value;
			
			// class
			try 
			{
				model.PersistentClazz = ReflectHelper.ClassForName(className);
			} 
			catch ( Exception cnfe ) 
			{
				throw new MappingException( "persistent class not found", cnfe);
			}

			//proxy interface
			XmlAttribute proxyNode = node.Attributes["proxy"];
			if (proxyNode!=null) 
			{
				try 
				{
					model.ProxyInterface = ReflectHelper.ClassForName( proxyNode.Value );
				} 
				catch (Exception cnfe) 
				{
					throw new MappingException(cnfe);
				}
			}
			
			//discriminator
			XmlAttribute discriminatorNode = node.Attributes["discriminator-value"];
			model.DiscriminatorValue = (discriminatorNode==null)
				? model.Name
				: discriminatorNode.Value;
			
			//dynamic update
			XmlAttribute dynamicNode = node.Attributes["dynamic-update"];
			model.DynamicUpdate = (dynamicNode==null)
				? false :
				"true".Equals( dynamicNode.Value );
			
			//dynamic insert
			XmlAttribute insertNode = node.Attributes["dynamic-insert"];
			model.DynamicInsert = (insertNode==null) ? 
				false : 
				"true".Equals( insertNode.Value	);

			//import
			
			// we automattically want to add an import of the Assembly Qualified Name (includes version, 
			// culture, public-key) to the className supplied in the hbm.xml file.  The most common use-case
			// will have it contain the "FullClassname, AssemblyName", it might contain version, culture, 
			// public key, etc...) but should not assume it does.
			mapping.AddImport( model.PersistentClazz.AssemblyQualifiedName, StringHelper.GetFullClassname(className) );

			// if we are supposed to auto-import the Class then add an import to get from the Classname
			// to the Assembly Qualified Class Name
			if (mapping.IsAutoImport) 
			{
				mapping.AddImport( model.PersistentClazz.AssemblyQualifiedName, StringHelper.GetClassname(className) );
			}
		}

		public static void BindSubclass(XmlNode node, Subclass model, Mappings mappings) 
		{

			BindClass(node, model, mappings);

			if ( model.Persister==null ) 
			{
				model.RootClazz.Persister = typeof(EntityPersister);
			}

			model.Table = model.Superclass.Table;

			log.Info("Mapping subclass: " + model.Name + " -> " + model.Table.Name);

			// properties
			PropertiesFromXML(node, model, mappings);
		}

		public static void BindJoinedSubclass(XmlNode node, Subclass model, Mappings mappings) 
		{

			BindClass(node, model, mappings);

			// joined subclass
			if ( model.Persister==null ) 
			{
				model.RootClazz.Persister = typeof(NormalizedEntityPersister);
			}

			//table
			XmlAttribute tableNameNode = node.Attributes["table"];
			string tableName = (tableNameNode==null)
				? StringHelper.Unqualify( model.PersistentClazz.Name )
				: tableNameNode.Value;

			//schema
			XmlAttribute schemaNode = node.Attributes["schema"];
			string schema = schemaNode==null ? mappings.SchemaName : schemaNode.Value;
			Table mytable = mappings.AddTable(schema, tableName);
			model.Table = mytable;

			log.Info("Mapping joined-subclass: " + model.Name + " -> " + model.Table.Name );

			XmlNode keyNode = node.SelectSingleNode(nsPrefix + ":key", nsmgr);
			Value key = new Value(mytable);
			model.Key = key;
			BindValue( keyNode, key, false, model.Name );

			model.Key.Type = model.Identifier.Type;
			model.CreatePrimaryKey(dialect);
			ForeignKey fk = mytable.CreateForeignKey( model.Key.ConstraintColumns );
			fk.ReferencedClass = model.Superclass.PersistentClazz;

			// properties
			PropertiesFromXML(node, model, mappings);
		}

		public static void BindRootClass(XmlNode node, RootClass model, Mappings mappings) 
		{

			BindClass(node, model, mappings);

			//TABLENAME
			XmlAttribute tableNameNode = node.Attributes["table"];
			string tableName = (tableNameNode==null)
				? StringHelper.Unqualify( model.PersistentClazz.Name )
				: tableNameNode.Value;

			XmlAttribute schemaNode = node.Attributes["schema"];
			string schema = schemaNode==null ? mappings.SchemaName : schemaNode.Value;
			Table table = mappings.AddTable(schema, tableName);
			model.Table = table;

			log.Info("Mapping class: " + model.Name + " -> " + model.Table.Name );

			//persister
			XmlAttribute persisterNode = node.Attributes["persister"];
			if ( persisterNode==null ) 
			{
				//persister = typeof(EntityPersister);
			} 
			else 
			{
				try 
				{
					model.Persister = ReflectHelper.ClassForName( persisterNode.Value );
				} 
				catch (Exception) 
				{
					throw new MappingException("could not find persister class: " + persisterNode.Value );
				}
			}

			//mutable
			XmlAttribute mutableNode = node.Attributes["mutable"];
			model.IsMutable = (mutableNode==null) || mutableNode.Value.Equals("true");

			//WHERE
			XmlAttribute whereNode = node.Attributes["where"];
			if (whereNode!=null) model.Where = whereNode.Value;

			//POLYMORPHISM
			XmlAttribute polyNode = node.Attributes["polymorphism"];
			model.IsExplicitPolymorphism = (polyNode!=null) && polyNode.Value.Equals("explicit");

			foreach(XmlNode subnode in node.ChildNodes) 
			{
				string name = subnode.LocalName; //Name;
				string propertyName = GetPropertyName(subnode);

				//I am only concerned with elements that are from the nhibernate namespace
				if(subnode.NamespaceURI!=Configuration.MappingSchemaXMLNS) continue;

				switch( name ) 
				{
					case "id":
						Value id = new Value(table);
						model.Identifier = id;
						
						if ( propertyName==null) 
						{
							BindValue(subnode, id, false, RootClass.DefaultIdentifierColumnName);
							if ( id.Type==null ) throw new MappingException("must specify an identifier type: " + model.PersistentClazz.Name );
							model.IdentifierProperty = null;
						} 
						else 
						{
							BindValue(subnode, id, false, propertyName);
							id.SetTypeByReflection( model.PersistentClazz, propertyName);
							Mapping.Property prop = new Mapping.Property(id);
							BindProperty(subnode, prop, mappings);
							model.IdentifierProperty = prop;
						}

						if ( id.Type.ReturnedClass.IsArray ) 
							throw new MappingException("illegal use of an array as an identifier (arrays don't reimplement equals)"); //is this true in .net?

						MakeIdentifier(subnode, id, mappings);
						break;

					case "composite-id":
						Component compId = new Component(model);
						model.Identifier = compId;
						if (propertyName==null) 
						{
							BindComponent(subnode, compId, null, model.Name + ".id", false, mappings);
							model.HasEmbeddedIdentifier = compId.IsEmbedded;
							model.IdentifierProperty = null;
						} 
						else 
						{
							System.Type reflectedClass = ReflectHelper.GetGetter( model.PersistentClazz, propertyName ).ReturnType;
							BindComponent(subnode, compId, reflectedClass, model.Name + StringHelper.Dot + propertyName, false, mappings);
							Mapping.Property prop = new Mapping.Property(compId);
							BindProperty(subnode, prop, mappings);
							model.IdentifierProperty = prop;
						}
						MakeIdentifier(subnode, compId, mappings);
						break;

					case "version":
					case "timestamp":
						//version
						Value val = new Value(table);
						BindValue(subnode, val, false, propertyName);
						if ( val.Type==null ) val.Type = ( ("version".Equals(name)) ? NHibernate.Int32 : NHibernate.Timestamp );
						Mapping.Property timestampProp = new Mapping.Property(val);
						BindProperty(subnode, timestampProp, mappings);
						model.Version = timestampProp;
						model.AddProperty(timestampProp);
						break;

					case "discriminator":
						Value discrim = new Value(table);
						model.Discriminator = discrim;
						BindValue(subnode, discrim, false, RootClass.DefaultDiscriminatorColumnName);
						if ( discrim.Type==null ) 
						{
							discrim.Type = NHibernate.String;
							foreach(Column col in discrim.ColumnCollection) 
							{
								col.Type = NHibernate.String;
							}
						}
						model.Polymorphic = true;
						if ( subnode.Attributes["force"] != null && "true".Equals( subnode.Attributes["force"].Value ) ) 
						{
							model.IsForceDiscriminator = true;
						}
						break;

					case "jcs-cache":
						model.Cache = Configuration.CreateCache( 
							subnode.Attributes["usage"].Value,
							model.PersistentClazz.Name, 
							model );

						break;
				}
			}

			model.CreatePrimaryKey(dialect);

			PropertiesFromXML(node, model, mappings);
		}

		public static void BindColumns(XmlNode node, Value model, bool isNullable, bool autoColumn, string defaultColumnName) 
		{
			//COLUMN(S)
			XmlAttribute columnNode = node.Attributes["column"];
			if ( columnNode==null ) 
			{
				int count=0;
				foreach(XmlNode subnode in node.SelectNodes(nsPrefix + ":column", nsmgr)) 
				{
					Table table = model.Table;
					Column col = new Column( model.Type, count++ );
					BindColumn(subnode, col, isNullable);
					col.Name = (subnode.Attributes["name"]==null) ? String.Empty : subnode.Attributes["name"].Value;
					if (table!=null) table.AddColumn(col); //table=null -> an association, fill it in later
					model.AddColumn(col);
					//column index
					XmlAttribute indexNode = subnode.Attributes["index"];
					if ( indexNode!=null && table!=null ) 
					{
						table.GetIndex( indexNode.Value ).AddColumn(col);
					}
					XmlAttribute uniqueNode = subnode.Attributes["unique-key"];
					if ( uniqueNode!=null && table!=null ) 
					{
						table.GetUniqueKey( uniqueNode.Value ).AddColumn(col);
					}
				}
			} 
			else 
			{
				Column col = new Column( model.Type, 0 );
				BindColumn(node, col, isNullable);
				col.Name = columnNode.Value;
				Table table = model.Table;
				if (table!=null) table.AddColumn(col); //table=null -> an association - fill it in later
				model.AddColumn(col);
			}

			if ( autoColumn && model.ColumnSpan==0 ) 
			{
				Column col = new Column( model.Type, 0 );
				BindColumn(node, col, isNullable);
				col.Name = defaultColumnName;
				model.Table.AddColumn(col);
				model.AddColumn(col);
			}
		}

		/// <remarks>
		/// Does _not_ automatically make a column if none is specifed by XML
		/// </remarks>
		public static void BindValue(XmlNode node, Value model, bool isNullable) 
		{
			//TYPE
			model.Type = GetTypeFromXML(node);
			BindColumns(node, model, isNullable, false, null);
		}

		/// <remarks>
		/// automatically makes a column with the default name if none is specifed by XML
		/// </remarks>
		public static void BindValue(XmlNode node, Value model, bool isNullable, string defaultColumnName) 
		{
			model.Type = GetTypeFromXML(node);
			XmlAttribute formulaNode = node.Attributes["formula"];
			if (formulaNode != null)
			{
				Formula f = new Formula();
				f.FormulaString = formulaNode.InnerText;
				model.Formula = f;
			}
			else
			{
				BindColumns(node, model, isNullable, true, defaultColumnName);
			}
		}

		public static void BindProperty(XmlNode node, Mapping.Property model, Mappings mappings) 
		{
			model.Name = GetPropertyName(node);
			IType type = model.Value.Type;
			if (type==null) throw new MappingException("could not determine a property type for: " + model.Name );
			
			XmlAttribute accessNode = node.Attributes["access"];
			if( accessNode!=null) 
			{
				model.PropertyAccessorName = accessNode.Value;
			}
			else 
			{
				model.PropertyAccessorName = mappings.DefaultAccess;
			}
			XmlAttribute cascadeNode = node.Attributes["cascade"];
			model.Cascade = (cascadeNode==null) ? mappings.DefaultCascade : cascadeNode.Value;
			
			XmlAttribute updateNode = node.Attributes["update"];
			model.IsUpdateable = (updateNode==null) ? true : "true".Equals( updateNode.Value );

			XmlAttribute insertNode = node.Attributes["insert"];
			model.IsInsertable = (insertNode==null) ? true : "true".Equals( insertNode.Value );

			if ( log.IsDebugEnabled ) 
			{
				string msg = "Mapped property: " + model.Name;
				string columns = Columns( model.Value );
				if ( columns.Length > 0  ) msg += " -> " + columns;
				if ( model.Type !=null ) msg += ", type: " + model.Type.Name;
				log.Debug(msg);
			}

		}
		
		private static string Columns(Value val) 
		{
			StringBuilder columns = new StringBuilder();
			bool first = true;
			foreach(Column col in val.ColumnCollection)
			{
				if (first)
					first = false;
				else
					columns.Append(", ");
				columns.Append(col.Name);
			}
			return columns.ToString();
		}

		/// <remarks>
		/// Called for all collections
		/// </remarks>
		public static void BindCollection(XmlNode node, Mapping.Collection model, string prefix, Mappings mappings) 
		{
			//ROLENAME
			string propertyName = node.Attributes["name"].Value;
			model.Role = prefix + StringHelper.Dot + propertyName;

			XmlAttribute inverseNode = node.Attributes["inverse"];
			if ( inverseNode!=null ) model.IsInverse = StringHelper.BooleanValue( inverseNode.Value );

			XmlAttribute orderNode = node.Attributes["order-by"];
			if ( orderNode!=null ) 
			{
				model.OrderBy = orderNode.Value;
			}
			XmlAttribute whereNode = node.Attributes["where"];
			if ( whereNode!=null ) 
			{
				model.Where = whereNode.Value;
			}

			XmlNode oneToManyNode = node.SelectSingleNode(nsPrefix + ":one-to-many", nsmgr);
			if ( oneToManyNode!=null ) 
			{
				model.IsOneToMany = true;
				model.OneToMany = new OneToMany( model.Owner );
				BindOneToMany( oneToManyNode, model.OneToMany );
				//we have to set up the table later!! yuck
			} 
			else 
			{
				//TABLE
				XmlAttribute tableNode = node.Attributes["table"];
				string tableName;
				if ( tableNode!=null) 
				{
					tableName = tableNode.Value;
				} 
				else 
				{
					tableName = propertyName;
				}
				XmlAttribute schemaNode = node.Attributes["schema"];
				string schema = schemaNode==null ? mappings.SchemaName : schemaNode.Value;
				model.Table = mappings.AddTable(schema, tableName);

				log.Info("Mapping collection: " + model.Role + " -> " + model.Table.Name );
			}
			//laziness
			XmlAttribute lazyNode = node.Attributes["lazy"];
			if (lazyNode!=null) 
			{
				model.IsLazy = StringHelper.BooleanValue( lazyNode.Value );
			}

			//sort
			XmlAttribute sortedAtt = node.Attributes["sort"];
			// unsorted, natural, comparator.class.name
			if (sortedAtt==null || sortedAtt.Value.Equals("unsorted") ) 
			{
				model.IsSorted = false;
			} 
			else 
			{
				model.IsSorted = true;
				string className = sortedAtt.Value;
				if ( !className.Equals("natural") ) 
				{
					try 
					{
						model.Comparer = (IComparer) Activator.CreateInstance( ReflectHelper.ClassForName(className) );
					} 
					catch (Exception) 
					{
						throw new MappingException("could not instantiate comparer class: " + className);
					}
				}
			}

			//ORPHAN DELETE (used for programmer error detection)
			XmlAttribute cascadeAtt = node.Attributes["cascade"];
			if ( cascadeAtt!=null && cascadeAtt.Value.Equals("all-delete-orphan") ) model.OrphanDelete = true;

			//set up second pass
			if (model is List) 
			{
				mappings.AddSecondPass( new ListSecondPass(node, mappings, (List) model) );
			} 
			else if (model is Map) 
			{
				mappings.AddSecondPass( new MapSecondPass(node, mappings, (Map) model) );
			} 
			else if (model is Set) 
			{
				mappings.AddSecondPass( new SetSecondPass(node, mappings, (Set) model) );
			}
			else if (model is IdentifierCollection) 
			{
				mappings.AddSecondPass( new IdentifierCollectionSecondPass(node, mappings, (IdentifierCollection) model) );
			}
			else 
			{
				mappings.AddSecondPass( new CollectionSecondPass(node, mappings, model) );
			}
		}

		public static void BindIntegerValue(XmlNode node, IntegerValue model, string defaultColumnName, bool isNullable) 
		{

			BindValue(node, model, isNullable, defaultColumnName);

			if (model.ColumnCollection.Count > 1)
				log.Error("This shouldn't happen, check BindIntegerValue");
			foreach(Column col in model.ColumnCollection) 
			{
				col.Type = NHibernate.Int32;
				col.TypeIndex = 0;
				break;
			}
		}

		public static void BindManyToOne(XmlNode node, ManyToOne model, string defaultColumnName, bool isNullable) 
		{
			BindColumns(node, model, isNullable, true, defaultColumnName);
			InitOuterJoinFetchSettings(node, model);

			XmlAttribute typeNode = node.Attributes["class"];
			
			if ( typeNode!=null ) 
			{
				try 
				{
					model.Type = 
						TypeFactory.ManyToOne( ReflectHelper.ClassForName( typeNode.Value)); 
				} 
				catch 
				{
					throw new MappingException("could not find class: " + typeNode.Value);
				}
			}
		}

		public static void BindAny(XmlNode node, Any model, bool isNullable) 
		{
			model.IdentifierType = GetTypeFromXML(node);

			XmlAttribute metaAttribute = node.Attributes["meta-type"];
			if (metaAttribute!=null) 
			{
				IType metaType = TypeFactory.HueristicType( metaAttribute.Value );
				if ( metaType==null ) throw new MappingException("could not interpret meta-type");
				model.MetaType = metaType;
			}

			BindColumns(node, model, isNullable, false, null);
		}

		public static void BindOneToOne(XmlNode node, OneToOne model, bool isNullable) 
		{
			BindColumns(node, model, isNullable, false, null);
			InitOuterJoinFetchSettings(node, model);

			XmlAttribute constrNode = node.Attributes["constrained"];
			bool constrained = constrNode!=null && constrNode.Value.Equals("true");
			model.IsConstrained = constrained;

			model.ForeignKeyType = (constrained ? ForeignKeyType.ForeignKeyFromParent : ForeignKeyType.ForeignKeyToParent);

			XmlAttribute typeNode = node.Attributes["class"];			
			
			if (typeNode!=null) 
			{
				try 
				{
					model.Type = 
						TypeFactory.OneToOne( ReflectHelper.ClassForName( typeNode.Value ), model.ForeignKeyType);
					
				} 
				catch (Exception) 
				{
					throw new MappingException("could not find class: " + typeNode.Value);
				}
			}
		}

		public static void BindOneToMany(XmlNode node, OneToMany model) 
		{
			try 
			{
				model.Type = (EntityType) NHibernate.Entity( 
					ReflectHelper.ClassForName( node.Attributes["class"].Value) );
			} 
			catch (Exception e) 
			{
				throw new MappingException("associated class not found", e);
			}
		}

		public static void BindColumn(XmlNode node, Column model, bool isNullable) 
		{
			XmlAttribute lengthNode = node.Attributes["length"];
			if ( lengthNode!=null ) model.Length = int.Parse( lengthNode.Value );
			
			XmlAttribute nullNode = node.Attributes["not-null"];
			model.IsNullable = (nullNode!=null) ? !StringHelper.BooleanValue( nullNode.Value) : isNullable;

			XmlAttribute unqNode = node.Attributes["unique"];
			model.IsUnique = unqNode!=null && StringHelper.BooleanValue( unqNode.Value );

			XmlAttribute typeNode = node.Attributes["sql-type"];
			model.SqlType = (typeNode==null) ? null : typeNode.Value;
		}

		/// <remarks>
		/// Called for arrays and primitive arrays
		/// </remarks>
		public static void BindArray(XmlNode node, Mapping.Array model, string prefix, Mappings mappings) 
		{
			
			BindCollection(node, model, prefix, mappings);

			XmlAttribute att = node.Attributes["element-class"];

			if ( att!=null ) 
			{
				try 
				{
					model.ElementClass = ReflectHelper.ClassForName( att.Value );
				} 
				catch (Exception e) 
				{
					throw new MappingException(e);
				}
			} 
			else 
			{
				foreach(XmlNode subnode in node.ChildNodes) 
				{
					string name = subnode.LocalName; //.Name;

					//I am only concerned with elements that are from the nhibernate namespace
					if(subnode.NamespaceURI!=Configuration.MappingSchemaXMLNS) continue;

					switch(name) 
					{
						case "element":
							IType type = GetTypeFromXML(subnode);

							model.ElementClass = type.ReturnedClass;

							break;

						case "one-to-many":
						case "many-to-many":
						case "composite-element":
							try 
							{
								
								model.ElementClass = ReflectHelper.ClassForName( subnode.Attributes["class"].Value);
							} 
							catch (Exception e) 
							{
								throw new MappingException(e);
							}
							break;
					}
				}
			}
		}

		public static void BindComponent(XmlNode node, Component model, System.Type reflectedClass, string path, bool isNullable, Mappings mappings) 
		{

			XmlAttribute classNode = node.Attributes["class"];
			XmlAttribute dynaclassNode = node.Attributes["dynaclass"];
			
			string className;
			
			if (dynaclassNode!=null) 
			{
				className = dynaclassNode.Value;
				model.IsEmbedded = false;
			} 
			else if (classNode!=null) 
			{
				className = classNode.Value;
				
				try 
				{
					model.ComponentClass = ReflectHelper.ClassForName(className); 
				} 
				catch (Exception e) 
				{
					throw new MappingException("component class not found", e);
				}
				model.IsEmbedded = false;
			} 
			else if (reflectedClass!=null) 
			{
				model.ComponentClass = reflectedClass;
				className = model.ComponentClass.Name;
				model.IsEmbedded = false;
			} 
			else 
			{
				// an "embedded" component (ids only)
				model.ComponentClass = model.Owner.PersistentClazz;
				className = model.Owner.Name;
				model.IsEmbedded = true;
			}

			foreach(XmlNode subnode in node.ChildNodes) 
			{
				//I am only concerned with elements that are from the nhibernate namespace
				if(subnode.NamespaceURI!=Configuration.MappingSchemaXMLNS) continue;

				string name = subnode.LocalName; //.Name;
				string propertyName = GetPropertyName(subnode);
				string subpath = path + StringHelper.Dot + propertyName;

				CollectionType collectType = CollectionType.CollectionTypeFromString(name);
				Value value = null;
				if ( collectType!=null ) 
				{
					Mapping.Collection collection = collectType.Create( subnode, path, model.Owner, mappings );
					mappings.AddCollection(collection);
					value = new Value( model.Table );
					BindValue(subnode, value, isNullable);
					value.Type = collection.Type;
				} 
				else if ( "many-to-one".Equals(name) || "key-many-to-one".Equals(name) ) 
				{
					value = new ManyToOne( model.Table);
					BindManyToOne(subnode, (ManyToOne) value, propertyName, isNullable);
				} 
				else if ( "one-to-one".Equals(name) ) 
				{
					value = new OneToOne( model.Table, model.Owner.Identifier );
					BindOneToOne(subnode, (OneToOne) value, isNullable);
				} 
				else if ( "any".Equals(name) ) 
				{
					value = new Any( model.Table );
					BindAny(subnode, (Any) value, isNullable);
				} 
				else if ( "property".Equals(name) || "key-property".Equals(name) ) 
				{
					value = new Value( model.Table );
					if ( value.Formula!=null ) throw new MappingException("properties of components do not support formula: " + propertyName);
					BindValue(subnode, value, isNullable, propertyName);
				} 
				else if ( "component".Equals(name) || "nested-composite-element".Equals(name) ) 
				{
					System.Type subreflectedClass = (model.ComponentClass==null) ?
						null :
						ReflectHelper.GetGetter( model.ComponentClass, propertyName ).ReturnType;
					value = ( model.Owner!=null ) ?
						new Component( model.Owner ) : // a class component
						new Component( model.Table ); // a composite element
					BindComponent(subnode, (Component) value, subreflectedClass, subpath, isNullable, mappings);
				} 
				else if ( "parent".Equals(name) ) 
				{
					model.ParentProperty = propertyName;
				}

				if ( value!=null ) 
				{
					System.Type componentClass = model.ComponentClass;
					if (componentClass!=null) value.SetTypeByReflection(componentClass, propertyName);
					value.CreateForeignKey();
					Mapping.Property prop = new Mapping.Property(value);
					BindProperty(subnode, prop, mappings);
					model.AddProperty(prop);
				}
			}

			int span = model.PropertySpan;
			string[] names = new string[span];
			IType[] types = new IType[span];
			Cascades.CascadeStyle[] cascade = new Cascades.CascadeStyle[span];
			OuterJoinLoaderType[] joinedFetch = new OuterJoinLoaderType[span];
			Property.IGetter[] getters = new Property.IGetter[span];
			Property.ISetter[] setters = new Property.ISetter[span];
			bool foundCustomAccessor = false;
			
			int i=0;
			foreach(Mapping.Property prop in model.PropertyCollection) 
			{
				names[i] = prop.Name;
				types[i] = prop.Type;
				cascade[i] = prop.CascadeStyle;
				joinedFetch[i] = prop.Value.OuterJoinFetchSetting;
				
				// the setters is new code added for field access
				setters[i] = prop.GetSetter( model.ComponentClass );
				getters[i] = prop.GetGetter( model.ComponentClass );
				if( !prop.IsBasicPropertyAccessor ) foundCustomAccessor = true;
				i++;
			}

			model.Type = 
				(IType) new ComponentType( model.ComponentClass, names, getters, setters, foundCustomAccessor, types, joinedFetch, cascade, model.ParentProperty, model.IsEmbedded );
		}

		private static IType GetTypeFromXML(XmlNode node) 
		{
			IType type;
			XmlAttribute typeNode = node.Attributes["type"];

			if (typeNode==null) typeNode = node.Attributes["id-type"]; //for an any
			if (typeNode==null) 
			{
				return null; //we will have to use reflection
			} 
			else 
			{
				type = TypeFactory.HueristicType( typeNode.Value );
				if (type==null) throw new MappingException("could not interpret type: " + typeNode.Value );
			}
			return type;
		}

		private static void InitOuterJoinFetchSettings(XmlNode node, Association model) 
		{
			XmlAttribute jfNode = node.Attributes["outer-join"];
			if ( jfNode==null ) 
			{
				model.OuterJoinFetchSetting = OuterJoinLoaderType.Auto;
			} 
			else 
			{
				string eoj = jfNode.Value;
				if ( "auto".Equals(eoj) ) 
				{
					model.OuterJoinFetchSetting = OuterJoinLoaderType.Auto;
				} 
				else 
				{
					model.OuterJoinFetchSetting = ("true".Equals(eoj)) ?
						OuterJoinLoaderType.Eager : OuterJoinLoaderType.Lazy;
				}
			}
		}

		private static void MakeIdentifier(XmlNode node, Value model, Mappings mappings) 
		{
			//GENERATOR

			XmlNode subnode = node.SelectSingleNode(nsPrefix + ":generator", nsmgr);
			if ( subnode!=null ) 
			{
				if (subnode.Attributes["class"] == null)
					throw new MappingException("no class given for generator");

				model.IdentifierGeneratorStrategy = subnode.Attributes["class"].Value;

				IDictionary parms = new Hashtable();

				if ( mappings.SchemaName!=null ) 
				{
					parms.Add( "schema", mappings.SchemaName );
				}

				parms.Add( "target_table", model.Table.Name);

				foreach(Column col in model.ColumnCollection)
				{
					parms.Add( "target_column", col);
					break;
				}

				//foreach(XmlNode childNode in subnode.SelectNodes("param")) {
				foreach(XmlNode childNode in subnode.SelectNodes(nsPrefix + ":param", nsmgr)) 
				{
					parms.Add(
						childNode.Attributes["name"].Value,
						childNode.FirstChild.Value
						);
				}

				model.IdentifierGeneratorProperties = parms;
			}

			model.Table.SetIdentifierValue(model);

			//unsaved-value
			XmlAttribute nullValueNode = node.Attributes["unsaved-value"];
			if (nullValueNode!=null) 
				model.NullValue = nullValueNode.Value;
			else
				model.NullValue = "null";
		}

		protected static void PropertiesFromXML(XmlNode node, PersistentClass model, Mappings mappings) 
		{
			string path = model.Name;
			Table table = model.Table;

			foreach(XmlNode subnode in node.ChildNodes) 
			{
				//I am only concerned with elements that are from the nhibernate namespace
				if(subnode.NamespaceURI!=Configuration.MappingSchemaXMLNS) continue;

				string name = subnode.LocalName; //.Name;
				string propertyName = GetPropertyName(subnode);

				CollectionType collectType = CollectionType.CollectionTypeFromString(name);
				Value value = null;
				if (collectType!=null) 
				{
					Mapping.Collection collection = collectType.Create(subnode, path, model, mappings);
					mappings.AddCollection(collection);
					value = new Value(table);
					BindValue(subnode, value, true);
					value.Type = collection.Type;
				} 
				else if ( "many-to-one".Equals(name) ) 
				{
					value = new ManyToOne(table);
					BindManyToOne(subnode, (ManyToOne) value, propertyName, true);
				} 
				else if ( "any".Equals(name) ) 
				{
					value = new Any(table);
					BindAny(subnode, (Any) value, true);
				}
				else if ( "one-to-one".Equals(name) ) 
				{
					value = new OneToOne(table, model.Identifier );
					BindOneToOne(subnode, (OneToOne) value, true);
				} 
				else if ( "property".Equals(name) ) 
				{
					value = new Value(table);
					BindValue(subnode, value, true, propertyName);
				} 
				else if ( "component".Equals(name) ) 
				{
					string subpath = path + StringHelper.Dot + propertyName;
					System.Type reflectedClass = ReflectHelper.GetGetter( model.PersistentClazz, propertyName ).ReturnType;
					value = new Component(model);
					BindComponent(subnode, (Component) value, reflectedClass, subpath, true, mappings);
				} 
				else if ( "subclass".Equals(name) ) 
				{
					Subclass subclass = new Subclass(model);
					BindSubclass( subnode, subclass, mappings );
					model.AddSubclass(subclass);
					mappings.AddClass(subclass);
				} 
				else if ( "joined-subclass".Equals(name) ) 
				{
					Subclass subclass = new Subclass(model);
					BindJoinedSubclass( subnode, subclass, mappings);
					model.AddSubclass(subclass);
					mappings.AddClass(subclass);
				}
				if ( value!=null) 
				{
					value.SetTypeByReflection( model.PersistentClazz, propertyName );
					value.CreateForeignKey();
					Mapping.Property prop = new Mapping.Property(value);
					BindProperty(subnode, prop, mappings);
					model.AddProperty(prop);
				}
			}
		}

		public static void BindSetSecondPass(XmlNode node, Mapping.Set model, IDictionary persistentClasses, Mappings mappings) 
		{
			BindCollectionSecondPass(node, model, persistentClasses, mappings);
		
			if ( !model.IsOneToMany ) model.CreatePrimaryKey();
		}

		/// <remarks>
		/// Called for Lists, arrays, primitive arrays
		/// </remarks>>
		public static void BindListSecondPass(XmlNode node, Mapping.List model, IDictionary classes, Mappings mappings) 
		{
			BindCollectionSecondPass(node, model, classes, mappings);

			XmlNode subnode = node.SelectSingleNode(nsPrefix + ":index", nsmgr);
			IntegerValue iv = new IntegerValue( model.Table );
			BindIntegerValue( subnode, iv, IndexedCollection.DefaultIndexColumnName, model.IsOneToMany );
			model.Index = iv;

			if ( !model.IsOneToMany ) model.CreatePrimaryKey();

			AddIndexForIndexedCollection( model );
		}

		public static void BindIdentifierCollectionSecondPass(XmlNode node, IdentifierCollection model, IDictionary persitentClasses, Mappings mappings) 
		{
			BindCollectionSecondPass(node, model, persitentClasses, mappings);

			XmlNode subnode = node.SelectSingleNode(nsPrefix + ":collection-id", nsmgr);
			Value id = new Value(model.Table);
			BindValue(subnode, id, false, IdentifierCollection.DefaultIdentifierColumnName);
			model.Identifier = id;
			MakeIdentifier(subnode, id, mappings);
			if ( !model.IsOneToMany ) model.CreatePrimaryKey();

		}

		//map binding

		public static void BindMapSecondPass(XmlNode node, Mapping.Map model, IDictionary classes, Mappings mappings) 
		{
			BindCollectionSecondPass(node, model, classes, mappings);

			foreach(XmlNode subnode in node.ChildNodes) 
			{
				//I am only concerned with elements that are from the nhibernate namespace
				if(subnode.NamespaceURI!=Configuration.MappingSchemaXMLNS) continue;

				string name = subnode.LocalName; //.Name;

				if ( "index".Equals(name) ) 
				{
					Value value = new Value( model.Table );
					BindValue(subnode, value, model.IsOneToMany, IndexedCollection.DefaultIndexColumnName);
					model.Index = value;
					if ( model.Index.Type==null ) throw new MappingException("map index element must specify a type");
				} 
				else if ( "index-many-to-many".Equals(name) ) 
				{
					ManyToOne mto = new ManyToOne( model.Table );
					BindManyToOne(subnode, mto, IndexedCollection.DefaultIndexColumnName, model.IsOneToMany);
					model.Index = mto;
				} 
				else if ( "composite-index".Equals(name) ) 
				{
					Component component = new Component( model.Table );
					BindComponent(subnode, component, null, model.Role + ".index", model.IsOneToMany, mappings);
					model.Index = component;
				}
				else if ( "index-many-to-any".Equals(name) ) 
				{
					Any any = new Any( model.Table );
					BindAny( subnode, any, model.IsOneToMany );
					model.Index = any;
				} 
			}
			if ( !model.IsInverse ) model.Index.CreateForeignKey();

			if ( !model.IsOneToMany ) model.CreatePrimaryKey();

			AddIndexForIndexedCollection( model );
		}

		private static void AddIndexForIndexedCollection(IndexedCollection model)
		{
			//TODO: Find out why this is commented out
//			 if ( model.IsOneToMany ) {
//				Index uniqueKey = model.Table.Index("x");
//				Iterator cols = model.getKey().getColumnIterator();
//				while ( cols.hasNext() ) uniqueKey.addColumn( (Column) cols.next() );
//				cols = ( (IndexedCollection) model ).getIndex().getColumnIterator();
//				while ( cols.hasNext() ) uniqueKey.addColumn( (Column) cols.next() );
//			}
		}

		/// <remarks>
		/// Called for all collections
		/// </remarks>
		public static void BindCollectionSecondPass(XmlNode node, Mapping.Collection model, IDictionary persistentClasses, Mappings mappings) 
		{
			if ( model.IsOneToMany ) 
			{
				System.Type assocClass = model.OneToMany.Type.PersistentClass;
				PersistentClass persistentClass = (PersistentClass) persistentClasses[assocClass];
				if ( persistentClass==null) throw new MappingException(
												"Association references unmapped class: " + assocClass.Name);
				model.Table = persistentClass.Table;

				log.Info("mapping collection: " + model.Role + " -> " + model.Table.Name);
			}

			foreach(XmlNode subnode in node.ChildNodes) 
			{
				//I am only concerned with elements that are from the nhibernate namespace
				if(subnode.NamespaceURI!=Configuration.MappingSchemaXMLNS) continue;

				string name = subnode.LocalName; //.Name;

				if ( "key".Equals(name) || "generated-key".Equals(name) ) 
				{
					Value key = new Value( model.Table );
					BindValue(subnode, key, model.IsOneToMany, Mapping.Collection.DefaultKeyColumnName);
					key.Type = model.Owner.Identifier.Type;
					if ( key.Type.ReturnedClass.IsArray ) throw new MappingException(
															  "illegal use of an array as an identifier (arrays don't reimplement equals)");
					model.Key = key;
				} 
				else if ( "element".Equals(name) ) 
				{
					Value elt = new Value( model.Table );
					model.Element = elt;
					BindValue(subnode, elt, true, Mapping.Collection.DefaultElementColumnName);
				} 
				else if ( "many-to-many".Equals(name) ) 
				{
					ManyToOne element = new ManyToOne( model.Table );
					model.Element = element;
					BindManyToOne(subnode, element, Mapping.Collection.DefaultElementColumnName, true);
				} 
				else if ( "composite-element".Equals(name) ) 
				{
					Component element = new Component( model.Table );
					model.Element = element;
					BindComponent(subnode, element, null, model.Role + ".element", true, mappings);
				}
				else if ( "many-to-any".Equals(name) ) 
				{
					Any element = new Any( model.Table );				
					model.Element = element;
					BindAny(subnode, element, true);
				} 
				else if ( "jcs-cache".Equals(name) )
				{
					model.Cache = Configuration.CreateCache( 
						subnode.Attributes["usage"].Value,
						model.Role, 
						model.Owner );
				}
			}

			if ( !model.IsInverse ) 
			{
				if ( !model.IsOneToMany ) // no foreign key for a one-to-many
				{
					model.Element.CreateForeignKey();
				}

				model.Key.CreateForeignKeyOfClass( model.Owner.PersistentClazz );
			}
		}

		public static void BindRoot(XmlDocument doc, Mappings model) 
		{
			XmlNode hmNode = doc.DocumentElement;
			XmlAttribute schemaNode = hmNode.Attributes["schema"];
			model.SchemaName = (schemaNode==null) ? null : schemaNode.Value;
			XmlAttribute dcNode = hmNode.Attributes["default-cascade"];
			model.DefaultCascade = (dcNode==null) ? "none" : dcNode.Value ;
			XmlAttribute daNode = hmNode.Attributes["default-access"];
			model.DefaultAccess = (daNode==null) ? "property" : daNode.Value;
			XmlAttribute aiNode = hmNode.Attributes["auto-import"];
			model.IsAutoImport = (aiNode==null) ? true : "true".Equals( aiNode.Value );

			nsmgr = new XmlNamespaceManager(doc.NameTable);
			// note that the prefix has absolutely nothing to do with what the user
			// selects as their prefix in the document.  It is the prefix we use to 
			// build the XPath and the nsmgr takes care of translating our prefix into
			// the user defined prefix...
			nsmgr.AddNamespace(nsPrefix, Configuration.MappingSchemaXMLNS);
			
			foreach(XmlNode n in hmNode.SelectNodes(nsPrefix + ":class", nsmgr) ) 
			{
				RootClass rootclass = new RootClass();
				Binder.BindRootClass(n, rootclass, model);
				model.AddClass(rootclass);
			}

			foreach(XmlNode n in hmNode.SelectNodes(nsPrefix + ":query", nsmgr) ) 
			{
				string qname = n.Attributes["name"].Value;
				string query = n.FirstChild.Value;
				log.Debug("Named query: " + qname + " -> " + query);
				model.AddQuery(qname, query);
			}

			foreach(XmlNode n in hmNode.SelectNodes(nsPrefix + ":import", nsmgr) ) 
			{
				string className = n.Attributes["class"].Value;
				XmlAttribute renameNode = n.Attributes["rename"];
				string rename = (renameNode==null) ? StringHelper.GetClassname(className) : renameNode.Value;
				log.Debug("Import: " + rename + " -> " + className);
				model.AddImport(className, rename);
			}
		}

		private static string GetPropertyName(XmlNode node) 
		{
			if (node.Attributes!=null) 
			{
				XmlAttribute propertyNameNode = node.Attributes["name"];
				return (propertyNameNode==null) ? null : propertyNameNode.Value;
			}
			return null;
		}

		public abstract class SecondPass 
		{
			internal XmlNode node;
			internal Mappings mappings;
			internal Mapping.Collection collection;

			public SecondPass(XmlNode node, Mappings mappings, Mapping.Collection collection) 
			{
				this.node = node;
				this.collection = collection;
				this.mappings = mappings;
			}

			public void DoSecondPass(IDictionary persistentClasses)
			{
				if ( log.IsDebugEnabled ) log.Debug("Second pass for collection: " + collection.Role );
				secondPass(persistentClasses);
				if ( log.IsDebugEnabled ) 
				{
					string msg = "Mapped collection key: " + Columns( collection.Key );
					if ( collection.IsIndexed ) msg+= ", index: " + Columns( ( (IndexedCollection) collection ).Index );
					if ( collection.IsOneToMany ) 
					{
						msg += ", one-to-many: " + collection.OneToMany.Type.Name;
					}
					else 
					{
						msg += ", element: " + Columns( collection.Element );
						msg += ", type: " + collection.Element.Type.Name;
					}
					log.Debug(msg);
				}			

			}

			public abstract void secondPass(IDictionary persistentClasses);
		}

		private class CollectionSecondPass : SecondPass 
		{
			public CollectionSecondPass(XmlNode node, Mappings mappings, Mapping.Collection collection)
				: base(node, mappings, collection) 
			{ 
			}
			public override void secondPass(IDictionary persistentClasses) 
			{
				Binder.BindCollectionSecondPass(node, collection, persistentClasses, mappings);
			}
		}

		private class IdentifierCollectionSecondPass : SecondPass 
		{
			public IdentifierCollectionSecondPass(XmlNode node, Mappings mappings, Mapping.IdentifierCollection collection) 
				: base(node, mappings, collection)
			{
			}

			public override void secondPass(IDictionary persistentClasses)
			{
				Binder.BindIdentifierCollectionSecondPass( node, (Mapping.IdentifierCollection) collection, persistentClasses, mappings);
			}
		}

		private class MapSecondPass : SecondPass 
		{
			public MapSecondPass(XmlNode node, Mappings mappings, Mapping.Map collection)
				: base(node, mappings, collection) 
			{ 
			}
			public override void secondPass(IDictionary persistentClasses) 
			{
				Binder.BindMapSecondPass(node, (Map) collection, persistentClasses, mappings);
			} 
		}

		private class SetSecondPass : SecondPass 
		{
			public SetSecondPass(XmlNode node, Mappings mappings, Mapping.Set collection)
				: base(node, mappings, collection) 
			{ 
			}
			public override void secondPass(IDictionary persistentClasses) 
			{
				Binder.BindSetSecondPass(node, (Set) collection, persistentClasses, mappings);
			} 
		}

		private class ListSecondPass : SecondPass 
		{
			public ListSecondPass(XmlNode node, Mappings mappings, Mapping.List collection)
				: base(node, mappings, collection) 
			{ 
			}
			public override void secondPass(IDictionary persistentClasses) 
			{
				Binder.BindListSecondPass(node, (List) collection, persistentClasses, mappings);
			} 
		}

		private abstract class CollectionType 
		{
			private string xmlTag;
			public abstract Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings);

			public CollectionType(string xmlTag) 
			{
				this.xmlTag = xmlTag;
			}

			public override string ToString() 
			{
				return xmlTag;
			}

			private static CollectionType MAP = new CollectionTypeMap("map");
			private class CollectionTypeMap : CollectionType 
			{
				public CollectionTypeMap(string xmlTag) : base(xmlTag) { }
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings) 
				{
					Map map = new Map(owner);
					Binder.BindCollection(node, map, prefix, mappings);
					return map;
				}
			}

			private static CollectionType SET = new CollectionTypeSet("set");
			private class CollectionTypeSet : CollectionType 
			{
				public CollectionTypeSet(string xmlTag) : base(xmlTag) {}
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings) 
				{
					Set setCollection = new Set(owner);
					Binder.BindCollection(node, setCollection, prefix, mappings);
					return setCollection;
				}
			}

			private static CollectionType LIST = new CollectionTypeList("list");
			private class CollectionTypeList : CollectionType 
			{
				public CollectionTypeList(string xmlTag) : base(xmlTag) { }
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings) 
				{
					List list = new List(owner);
					Binder.BindCollection(node, list, prefix, mappings);
					return list;
				}
			}		

			private static CollectionType BAG = new CollectionTypeBag("bag");
			private class CollectionTypeBag : CollectionType 
			{
				public CollectionTypeBag(string xmlTag) : base(xmlTag) {}
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings)
				{
					Bag bag = new Bag(owner);
					Binder.BindCollection(node, bag, prefix, mappings);
					return bag;
				}

			}

			private static CollectionType IDBAG = new CollectionTypeIdBag("idbag");
			private class CollectionTypeIdBag : CollectionType 
			{
				public CollectionTypeIdBag(string xmlTag) : base(xmlTag) {}
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings)
				{
					IdentifierBag bag = new IdentifierBag(owner);
					Binder.BindCollection(node, bag, prefix, mappings);
					return bag;
				}

			}

			private static CollectionType ARRAY = new CollectionTypeArray("array");
			private class CollectionTypeArray : CollectionType 
			{
				public CollectionTypeArray(string xmlTag) : base(xmlTag) { }
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings) 
				{
					Mapping.Array array = new Mapping.Array(owner);
					Binder.BindArray(node, array, prefix, mappings);
					return array;
				}
			}		

			private static CollectionType PRIMITIVE_ARRAY = new CollectionTypePrimitiveArray("primitive-array");
			private class CollectionTypePrimitiveArray : CollectionType 
			{
				public CollectionTypePrimitiveArray(string xmlTag) : base(xmlTag) { }
				public override Mapping.Collection Create(XmlNode node, string prefix, PersistentClass owner, Mappings mappings) 
				{
					PrimitiveArray array = new PrimitiveArray(owner);
					Binder.BindArray(node, array, prefix, mappings);
					return array;
				}
			}	
			
			private static Hashtable Instances = new Hashtable();
			static CollectionType() 
			{
				Instances.Add(MAP.ToString(), MAP);
				Instances.Add(BAG.ToString(), BAG);
				Instances.Add(IDBAG.ToString(), IDBAG);
				Instances.Add(SET.ToString(), SET);
				Instances.Add(LIST.ToString(), LIST);
				Instances.Add(ARRAY.ToString(), ARRAY);
				Instances.Add(PRIMITIVE_ARRAY.ToString(), PRIMITIVE_ARRAY);
			}

			public static CollectionType CollectionTypeFromString(string xmlTagName) 
			{
				return (CollectionType) Instances[xmlTagName];
			}
		}
	}
}
