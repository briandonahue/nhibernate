using System;
using System.Reflection;
using Iesi.Collections.Generic;
using log4net;
using NHibernate.Bytecode;
using NHibernate.Classic;
using NHibernate.Engine;
using NHibernate.Intercept;
using NHibernate.Mapping;
using NHibernate.Properties;
using NHibernate.Proxy;
using NHibernate.Type;
using Environment=NHibernate.Cfg.Environment;

namespace NHibernate.Tuple.Entity
{
	/// <summary> An <see cref="IEntityTuplizer"/> specific to the POCO entity mode. </summary>
	public class PocoEntityTuplizer : AbstractEntityTuplizer
	{
		private static readonly ILog log = LogManager.GetLogger(typeof(PocoEntityTuplizer));
		private readonly System.Type mappedClass;
		private readonly System.Type proxyInterface;
		private readonly bool islifecycleImplementor;
		private readonly bool isValidatableImplementor;
		private readonly HashedSet<string> lazyPropertyNames = new HashedSet<string>();
		private readonly IReflectionOptimizer optimizer;

		public PocoEntityTuplizer(EntityMetamodel entityMetamodel, PersistentClass mappedEntity)
			: base(entityMetamodel, mappedEntity)
		{
			mappedClass = mappedEntity.MappedClass;
			proxyInterface = mappedEntity.ProxyInterface;
			islifecycleImplementor = typeof(ILifecycle).IsAssignableFrom(mappedClass);
			isValidatableImplementor = typeof(IValidatable).IsAssignableFrom(mappedClass);

			foreach (Mapping.Property property in mappedEntity.PropertyClosureIterator)
			{
				if (property.IsLazy)
					lazyPropertyNames.Add(property.Name);
			}

			if (hasCustomAccessors || !Cfg.Environment.UseReflectionOptimizer)
			{
				optimizer = null;
			}
			else
			{
				optimizer = Cfg.Environment.BytecodeProvider.GetReflectionOptimizer(mappedClass, getters, setters);
			}
		}

		public override System.Type ConcreteProxyClass
		{
			get { return proxyInterface; }
		}

		public override bool IsInstrumented
		{
			get { return FieldInterceptionHelper.IsInstrumented(MappedClass); }
		}

		public override System.Type MappedClass
		{
			get { return mappedClass; }
		}

		protected internal override IGetter BuildPropertyGetter(Mapping.Property mappedProperty, PersistentClass mappedEntity)
		{
			return mappedProperty.GetGetter(mappedEntity.MappedClass);
		}

		protected internal override ISetter BuildPropertySetter(Mapping.Property mappedProperty, PersistentClass mappedEntity)
		{
			return mappedProperty.GetSetter(mappedEntity.MappedClass);
		}

		protected internal override IInstantiator BuildInstantiator(PersistentClass persistentClass)
		{
			if (optimizer == null)
			{
				return new PocoInstantiator(persistentClass, null);
			}
			else
			{
				return new PocoInstantiator(persistentClass, optimizer.InstantiationOptimizer);
			}
		}

		protected internal override IProxyFactory BuildProxyFactory(PersistentClass persistentClass, IGetter idGetter,
		                                                            ISetter idSetter)
		{
			bool needAccesorCheck = true; // NH specific (look the comment below)

			// determine the id getter and setter methods from the proxy interface (if any)
			// determine all interfaces needed by the resulting proxy
			HashedSet<System.Type> proxyInterfaces = new HashedSet<System.Type>();
			proxyInterfaces.Add(typeof(INHibernateProxy));

			System.Type _mappedClass = persistentClass.MappedClass;
			System.Type _proxyInterface = persistentClass.ProxyInterface;

			if (_proxyInterface != null && !_mappedClass.Equals(_proxyInterface))
			{
				if (!_proxyInterface.IsInterface)
				{
					throw new MappingException("proxy must be either an interface, or the class itself: " + EntityName);
				}
				needAccesorCheck = false; // NH (the proxy is an interface all properties can be overridden)
				proxyInterfaces.Add(_proxyInterface);
			}

			if (_mappedClass.IsInterface)
			{
				needAccesorCheck = false; // NH (the mapped class is an interface all properties can be overridden)
				proxyInterfaces.Add(_mappedClass);
			}

			foreach (Subclass subclass in persistentClass.SubclassIterator)
			{
				System.Type subclassProxy = subclass.ProxyInterface;
				System.Type subclassClass = subclass.MappedClass;
				if (subclassProxy != null && !subclassClass.Equals(subclassProxy))
				{
					if (!subclassProxy.IsInterface)
					{
						throw new MappingException("proxy must be either an interface, or the class itself: " + subclass.EntityName);
					}
					proxyInterfaces.Add(subclassProxy);
				}
			}

			/* 
			 * NH Different Implementation (for Error logging):
			 * - Check if the logger is enabled
			 * - Don't need nothing to check if the mapped-class or proxy is an interface
			 */
			if (log.IsErrorEnabled && needAccesorCheck)
			{
                Environment.BytecodeProvider.ProxyFactoryFactory.Validator.LogPropertyAccessorsErrors(persistentClass);
			}
			/**********************************************************/

			MethodInfo idGetterMethod = idGetter == null ? null : idGetter.Method;
			MethodInfo idSetterMethod = idSetter == null ? null : idSetter.Method;

			MethodInfo proxyGetIdentifierMethod = idGetterMethod == null || _proxyInterface == null ? null : 
				idGetterMethod;
				// TODO H3.2 different behaviour  ReflectHelper.GetMethod(_proxyInterface, idGetterMethod);

			MethodInfo proxySetIdentifierMethod = idSetterMethod == null || _proxyInterface == null ? null : 
				idSetterMethod;
				// TODO H3.2 different behaviour ReflectHelper.GetMethod(_proxyInterface, idSetterMethod);

			IProxyFactory pf = BuildProxyFactoryInternal(persistentClass, idGetter, idSetter);
			try
			{
				pf.PostInstantiate(EntityName, _mappedClass, proxyInterfaces, proxyGetIdentifierMethod, proxySetIdentifierMethod,
				                   persistentClass.HasEmbeddedIdentifier ? (IAbstractComponentType) persistentClass.Identifier.Type: null);
			}
			catch (HibernateException he)
			{
				log.Warn("could not create proxy factory for:" + EntityName, he);
				pf = null;
			}
			return pf;
		}

		protected virtual IProxyFactory BuildProxyFactoryInternal(PersistentClass @class, IGetter getter, ISetter setter)
		{
			return Cfg.Environment.BytecodeProvider.ProxyFactoryFactory.BuildProxyFactory();
		}

		public override void AfterInitialize(object entity, bool lazyPropertiesAreUnfetched, ISessionImplementor session)
		{
			if (IsInstrumented)
			{
				HashedSet<string> lazyProps = lazyPropertiesAreUnfetched && EntityMetamodel.HasLazyProperties ? lazyPropertyNames : null;
				//TODO: if we support multiple fetch groups, we would need
				//      to clone the set of lazy properties!
				FieldInterceptionHelper.InjectFieldInterceptor(entity, EntityName, lazyProps, session);
			}
		}

		public override object[] GetPropertyValues(object entity)
		{
			if (ShouldGetAllProperties(entity) && optimizer != null && optimizer.AccessOptimizer != null)
			{
				return GetPropertyValuesWithOptimizer(entity);
			}
			else
			{
				return base.GetPropertyValues(entity);
			}
		}

		private object[] GetPropertyValuesWithOptimizer(object entity)
		{
			return optimizer.AccessOptimizer.GetPropertyValues(entity);
		}

		public override object[] GetPropertyValuesToInsert(object entity, System.Collections.IDictionary mergeMap, ISessionImplementor session)
		{
			if (ShouldGetAllProperties(entity) && optimizer != null && optimizer.AccessOptimizer != null)
			{
				return GetPropertyValuesWithOptimizer(entity);
			}
			else
			{
				return base.GetPropertyValuesToInsert(entity, mergeMap, session);
			}
		}

		public override bool HasUninitializedLazyProperties(object entity)
		{
			if (EntityMetamodel.HasLazyProperties)
			{
				IFieldInterceptor callback = FieldInterceptionHelper.ExtractFieldInterceptor(entity);
				return callback != null && !callback.IsInitialized;
			}
			else
			{
				return false;
			}
		}

		public override bool IsLifecycleImplementor
		{
			get { return islifecycleImplementor; }
		}

		public override void SetPropertyValues(object entity, object[] values)
		{
			if (!EntityMetamodel.HasLazyProperties && optimizer != null && optimizer.AccessOptimizer != null)
			{
				SetPropertyValuesWithOptimizer(entity, values);
			}
			else
			{
				base.SetPropertyValues(entity, values);
			}
		}

		private void SetPropertyValuesWithOptimizer(object entity, object[] values)
		{
			try
			{
				optimizer.AccessOptimizer.SetPropertyValues(entity, values);
			}
			catch (InvalidCastException e)
			{
				throw new PropertyAccessException(e, "Invalid Cast (check your mapping for property type mismatches);", true,
				                                  entity.GetType());
			}
		}

		public override bool IsValidatableImplementor
		{
			get { return isValidatableImplementor; }
		}

		public override EntityMode EntityMode
		{
			get { return NHibernate.EntityMode.Poco; }
		}
	}
}
