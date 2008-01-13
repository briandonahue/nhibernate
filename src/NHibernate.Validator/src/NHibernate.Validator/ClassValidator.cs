namespace NHibernate.Validator
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Reflection;
	using System.Resources;
	using Iesi.Collections;
	using Iesi.Collections.Generic;
	using Interpolator;
	using Mapping;
	using Properties;
	using Proxy;
	using Util;

	/// <summary>
	/// Engine that take a object and check every expressed attribute restrictions
	/// </summary>
	[Serializable]
	public class ClassValidator : IClassValidator
	{
		//TODO: Logging
		//private static Log log = LogFactory.getLog( ClassValidator.class );

		private BindingFlags AnyVisibilityInstanceAndStatic = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

		private Type beanClass;

		private DefaultMessageInterpolatorAggregator defaultInterpolator;

		[NonSerialized] private ResourceManager messageBundle;

		[NonSerialized] private ResourceManager defaultMessageBundle;

		[NonSerialized] private IMessageInterpolator userInterpolator;

		private readonly Dictionary<Type, ClassValidator> childClassValidators;

		private IList<IValidator> beanValidators;

		private IList<IValidator> memberValidators;

		private List<MemberInfo> memberGetters;

		private List<MemberInfo> childGetters;

		private static readonly InvalidValue[] EMPTY_INVALID_VALUE_ARRAY = new InvalidValue[] {};

		private CultureInfo culture;
		

		/// <summary>
		/// Create the validator engine for this bean type
		/// </summary>
		/// <param name="beanClass"></param>
		public ClassValidator(Type beanClass)
			: this(beanClass, (ResourceManager) null,(CultureInfo)null)
		{
		}

		/// <summary>
		/// Create the validator engine for a particular bean class, using a resource bundle
		/// for message rendering on violation
		/// </summary>
		/// <param name="beanClass">bean type</param>
		/// <param name="resourceManager"></param>
		public ClassValidator(Type beanClass, ResourceManager resourceManager, CultureInfo culture)
			: this(beanClass, resourceManager, culture, null, new Dictionary<Type, ClassValidator>())
		{
		}

		/// <summary>
		/// Create the validator engine for a particular bean class, using a custom message interpolator
		/// for message rendering on violation
		/// </summary>
		/// <param name="beanClass"></param>
		/// <param name="interpolator"></param>
		public ClassValidator(Type beanClass, IMessageInterpolator interpolator)
			: this(beanClass, null, null, interpolator, new Dictionary<Type, ClassValidator>())
		{
		}

		/// <summary>
		/// Not a public API
		/// </summary>
		/// <param name="clazz"></param>
		/// <param name="resourceManager"></param>
		/// <param name="culture"></param>
		/// <param name="userInterpolator"></param>
		/// <param name="childClassValidators"></param>
		internal ClassValidator(
			Type clazz,
			ResourceManager resourceManager,
			CultureInfo culture,
			IMessageInterpolator userInterpolator,
			Dictionary<Type, ClassValidator> childClassValidators)
		{
			this.beanClass = clazz;

			this.messageBundle = resourceManager ?? GetDefaultResourceManager();
			this.defaultMessageBundle = GetDefaultResourceManager();
			this.culture = culture;
			this.userInterpolator = userInterpolator;
			this.childClassValidators = childClassValidators;

			//Initialize the ClassValidator
			InitValidator(beanClass, childClassValidators);
		}

		public ClassValidator(Type type, CultureInfo culture)
			: this(type)
		{
			this.culture = culture;
		}

		/// <summary>
		/// Return true if this <see cref="ClassValidator"/> contains rules for apply, false in other case. 
		/// </summary>
		public bool HasValidationRules
		{
			get
			{
				return beanValidators.Count != 0 || memberValidators.Count != 0;
			}
		}

		private ResourceManager GetDefaultResourceManager()
		{
			return new ResourceManager("NHibernate.Validator.Resources.DefaultValidatorMessages",
			                           Assembly.GetExecutingAssembly());
		}

		/// <summary>
		/// Initialize the <see cref="ClassValidator"/> type.
		/// </summary>
		/// <param name="clazz"></param>
		/// <param name="childClassValidators"></param>
		private void InitValidator(Type clazz, IDictionary<Type, ClassValidator> childClassValidators)
		{
			this.beanValidators = new List<IValidator>();
			this.memberValidators = new List<IValidator>();
			this.memberGetters = new List<MemberInfo>();
			this.childGetters = new List<MemberInfo>();
			this.defaultInterpolator = new DefaultMessageInterpolatorAggregator();
			this.defaultInterpolator.Initialize(messageBundle, defaultMessageBundle, culture);

			//build the class hierarchy to look for members in
			childClassValidators.Add(clazz, this);
			ISet<Type> classes = new HashedSet<Type>();
			AddSuperClassesAndInterfaces(clazz, classes);

			foreach(Type currentClass in classes)
			{
				foreach(Attribute classAttribute in currentClass.GetCustomAttributes(false))
				{
					IValidator validator = CreateValidator(classAttribute);

					if (validator != null)
					{
						beanValidators.Add(validator);
					}

					//Note: No need to handle Aggregate annotations, c# use Multiple Attribute declaration.
					//HandleAggregateAnnotations(classAttribute, null);
				}
			}

			//Check on all selected classes
			foreach(Type currentClass in classes)
			{
				foreach(PropertyInfo currentProperty in currentClass.GetProperties())
				{
					CreateMemberValidator(currentProperty);
					CreateChildValidator(currentProperty);
				}

				foreach(FieldInfo currentField in currentClass.GetFields(AnyVisibilityInstanceAndStatic))
				{
					CreateMemberValidator(currentField);
					CreateChildValidator(currentField);
				}
			}
		}

		/// <summary>
		/// apply constraints on a bean instance and return all the failures.
		/// if <see cref="bean"/> is null, an empty array is returned 
		/// </summary>
		/// <param name="bean">object to apply the constraints</param>
		/// <returns></returns>
		public InvalidValue[] GetInvalidValues(object bean)
		{
			return this.GetInvalidValues(bean, new IdentitySet());
		}

		/// <summary>
		/// Not a public API
		/// </summary>
		/// <param name="bean"></param>
		/// <param name="circularityState"></param>
		/// <returns></returns>
		private InvalidValue[] GetInvalidValues(object bean, ISet circularityState)
		{
			if (bean == null || circularityState.Contains(bean))
			{
				return EMPTY_INVALID_VALUE_ARRAY; //Avoid circularity
			}
			else
			{
				circularityState.Add(bean);
			}

			if (!beanClass.IsInstanceOfType(bean))
			{
				throw new ArgumentException("not an instance of: " + bean.GetType());
			}

			List<InvalidValue> results = new List<InvalidValue>();

			//Bean Validation
			foreach(IValidator validator in beanValidators)
			{
				if (!validator.IsValid(bean))
				{
					results.Add(new InvalidValue(Interpolate(validator), beanClass, null, bean, bean));
				}
			}

			//Property & Field Validation
			for(int i = 0; i < memberValidators.Count; i++)
			{
				MemberInfo member = memberGetters[i];

				if (IsPropertyInitialized(bean, member.Name))
				{
					object value = GetMemberValue(bean, member);

					IValidator validator = memberValidators[i];

					if (!validator.IsValid(value))
					{
						results.Add(new InvalidValue(Interpolate(validator), beanClass, member.Name, value, bean));
					}
				}
			}

			//Child validation
			for(int i = 0; i < childGetters.Count; i++)
			{
				MemberInfo member = childGetters[i];

				if (IsPropertyInitialized(bean, member.Name))
				{
					object value = GetMemberValue(bean, member);

					if (value != null && NHibernateUtil.IsInitialized(value))
					{
						MakeChildValidation(value,bean,member,circularityState,results);
					}
				}
			}
			return results.ToArray();
		}

		/// <summary>
		/// Validate the child validation to objects and collections
		/// </summary>
		/// <param name="value">value to validate</param>
		/// <param name="bean"></param>
		/// <param name="member"></param>
		/// <param name="circularityState"></param>
		/// <param name="results"></param>
		private void MakeChildValidation(object value, object bean, MemberInfo member,ISet circularityState, List<InvalidValue> results)
		{
			if (value is IEnumerable)
			{
				MakeChildValidation((IEnumerable) value, bean, member, circularityState, results);
			}
			else
			{
				//Simple Value, Not a Collection
				InvalidValue[] invalidValues = GetClassValidator(value)
					.GetInvalidValues(value, circularityState);

				foreach(InvalidValue invalidValue in invalidValues)
				{
					invalidValue.AddParentBean(bean, member.Name);
					results.Add(invalidValue);
				}
			}
		}

		/// <summary>
		/// Validate the child validation to collections
		/// </summary>
		/// <param name="value"></param>
		/// <param name="bean"></param>
		/// <param name="member"></param>
		/// <param name="circularityState"></param>
		/// <param name="results"></param>
		private void MakeChildValidation(IEnumerable value, object bean, MemberInfo member,ISet circularityState, List<InvalidValue> results)
		{
			if(IsGenericDictionary(value.GetType())) //Generic Dictionary
			{
				int index = 0;
				foreach (object item in value) 
				{
					if (item == null) 
					{
						index++;
						continue;
					}

					IGetter ValueProperty = new BasicPropertyAccessor().GetGetter(item.GetType(), "Value");
					IGetter KeyProperty = new BasicPropertyAccessor().GetGetter(item.GetType(), "Key");

					InvalidValue[] invalidValuesKey = GetClassValidator(ValueProperty.Get(item)).GetInvalidValues(ValueProperty.Get(item), circularityState);
					String indexedPropName = string.Format("{0}[{1}]", member.Name, index);
					
					foreach (InvalidValue invalidValue in invalidValuesKey) 
					{
						invalidValue.AddParentBean(bean, indexedPropName);
						results.Add(invalidValue);
					}

					InvalidValue[] invalidValuesValue = GetClassValidator(KeyProperty.Get(item)).GetInvalidValues(KeyProperty.Get(item), circularityState);
					indexedPropName = string.Format("{0}[{1}]", member.Name, index);

					foreach (InvalidValue invalidValue in invalidValuesValue) 
					{
						invalidValue.AddParentBean(bean, indexedPropName);
						results.Add(invalidValue);
					}

					index++;
				}
			}
			else //Generic collection
			{
				int index = 0;
				foreach(object item in value)
				{
					if (item == null)
					{
						index++;
						continue;
					}

					InvalidValue[] invalidValues = GetClassValidator(item).GetInvalidValues(item, circularityState);

					String indexedPropName = string.Format("{0}[{1}]", member.Name, index);

					index++;

					foreach(InvalidValue invalidValue in invalidValues)
					{
						invalidValue.AddParentBean(bean, indexedPropName);
						results.Add(invalidValue);
					}
				}
			}
		}

		/// <summary>
		/// Get the ClassValidator for the <see cref="Type"/> of the <see cref="value"/>
		/// parametter  from <see cref="childClassValidators"/>. If doesn't exist, a 
		/// new <see cref="ClassValidator"/> is returned.
		/// </summary>
		/// <param name="value">object to get type</param>
		/// <returns></returns>
		private ClassValidator GetClassValidator(object value)
		{
			Type clazz = value.GetType();

			ClassValidator classValidator = childClassValidators[clazz];

			return classValidator ?? new ClassValidator(clazz);
		}

		/// <summary>
		/// Check if the property is initialized. If the named property does not exist
		/// or is not persistent, this method always return <value>true</value>
		/// </summary>
		/// <param name="proxy">proxy The potential proxy</param>
		/// <param name="propertyName">the name of a persistent attribute of the object</param>
		/// <returns>
		/// true if the named property of the object is not listed as uninitialized
		/// false if the object is an uninitialized proxy, or the named property is uninitialized
		/// </returns>
		private bool IsPropertyInitialized(object proxy, string propertyName)
		{
			object entity;
			if ( proxy is INHibernateProxy ) 
			{
				ILazyInitializer li = ((INHibernateProxy)proxy).HibernateLazyInitializer;
				if ( li.IsUninitialized ) 
					return false;
				else 
					entity = li.GetImplementation();
			}
			else 
			{
				entity = proxy;
			}

			//Note: Always true at NHibernate implementation
			//if (FieldInterceptionHelper.IsInstrumented(entity)) 
			//{
			//    IFieldInterceptor interceptor = FieldInterceptionHelper.ExtractFieldInterceptor(entity);
			//    return interceptor == null || interceptor.IsInitializedField(propertyName);
			//} 
			//else
			//{
			//    return true;
			//}

			return true;
		}

		/// <summary>
		/// Get the message of the <see cref="IValidator"/> and 
		/// interpolate it.
		/// </summary>
		/// <param name="validator"></param>
		/// <returns></returns>
		private string Interpolate(IValidator validator)
		{
			String message = defaultInterpolator.GetAttributeMessage(validator);

			if (userInterpolator != null)
			{
				return userInterpolator.Interpolate(message, validator, defaultInterpolator);
			}
			else
			{
				return defaultInterpolator.Interpolate(message, validator, null);
			}
		}

		/// <summary>
		/// Create a <see cref="IValidator{A}"/> from a <see cref="ValidatorClassAttribute"/> attribute.
		/// If the attribute is not a <see cref="ValidatorClassAttribute"/> type return null.
		/// </summary>
		/// <param name="attribute">attribute</param>
		/// <returns>the validator for the attribute</returns>
		private IValidator CreateValidator(Attribute attribute)
		{
			try
			{
				ValidatorClassAttribute validatorClass = null;
				object[] AttributesInTheAttribute = attribute.GetType().GetCustomAttributes(typeof(ValidatorClassAttribute), false);

				if (AttributesInTheAttribute.Length > 0)
				{
					validatorClass = (ValidatorClassAttribute) AttributesInTheAttribute[0];
				}

				if (validatorClass == null)
				{
					return null;
				}

				IValidator beanValidator = (IValidator) Activator.CreateInstance(validatorClass.Value);
				beanValidator.Initialize(attribute);
				defaultInterpolator.AddInterpolator(attribute, beanValidator);
				return beanValidator;
			}
			catch(Exception ex)
			{
				throw new ArgumentException("could not instantiate ClassValidator", ex);
			}
		}

		/// <summary>
		/// Create a Validator from a property or field.
		/// </summary>
		/// <param name="member"></param>
		private void CreateMemberValidator(MemberInfo member)
		{
			object[] memberAttributes = member.GetCustomAttributes(false);

			foreach(Attribute memberAttribute in memberAttributes)
			{
				IValidator propertyValidator = CreateValidator(memberAttribute);

				if (propertyValidator != null)
				{
					memberValidators.Add(propertyValidator);
					memberGetters.Add(member);
				}
			}
		}

		/// <summary>
		/// Create the validator for the children, who got the <see cref="ValidAttribute"/>
		/// on the fields or properties
		/// </summary>
		/// <param name="member"></param>
		private void CreateChildValidator(MemberInfo member)
		{
			if (!member.IsDefined(typeof(ValidAttribute), false)) return;

			KeyValuePair<Type, Type> clazzDictionary;
			Type clazz = null;

			childGetters.Add(member);

			if (IsGenericDictionary(GetType(member)))
			{
				clazzDictionary = GetGenericTypesOfDictionary(member);
				if(!childClassValidators.ContainsKey(clazzDictionary.Key))
					new ClassValidator(clazzDictionary.Key, messageBundle, culture, userInterpolator, childClassValidators);
				if (!childClassValidators.ContainsKey(clazzDictionary.Value))
					new ClassValidator(clazzDictionary.Value, messageBundle, culture, userInterpolator, childClassValidators);

				return;
			}
			else
			{
				clazz = GetTypeOfMember(member);
			} 
			
			if (!childClassValidators.ContainsKey(clazz))
			{
				new ClassValidator(clazz, messageBundle, culture, userInterpolator, childClassValidators);
			}
		}

		/// <summary>
		/// Get the Generic Arguments of a <see cref="IDictionary{TKey,TValue}"/>
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		private KeyValuePair<Type, Type> GetGenericTypesOfDictionary(MemberInfo member)
		{
			Type clazz = GetType(member);

			return new KeyValuePair<Type, Type> (clazz.GetGenericArguments()[0], clazz.GetGenericArguments()[1]);
		}

		/// <summary>
		/// Get the type of the a Field or Property. 
		/// If is a: Generic Collection or a Array, return the type of the elements.
		/// TODO: Refactor this method to some Utils.
		/// </summary>
		/// <param name="member">MemberInfo, represent a property or field</param>
		/// <returns>type of the member or collection member</returns>
		private Type GetTypeOfMember(MemberInfo member)
		{
			Type clazz = GetType(member);

			if (clazz.IsArray) // Is Array
			{
				return clazz.GetElementType();
			} 
			else if (IsEnumerable(clazz)  && clazz.IsGenericType) //Is Collection Generic  
			{
				return clazz.GetGenericArguments()[0];
			}

			return clazz; //Single type, not a collection/array
		}

		/// <summary>
		/// Indicates if a <see cref="Type"/> is <see cref="IEnumerable"/>
		/// </summary>
		/// <param name="clazz"></param>
		/// <returns>is enumerable or not</returns>
		private bool IsEnumerable(Type clazz)
		{
			return clazz.GetInterface(typeof(IEnumerable).FullName) == null ? false : true;
		}

		private bool IsGenericDictionary(Type clazz)
		{
			if(clazz.IsInterface&&clazz.IsGenericType)
				return typeof(IDictionary<,>).Equals(clazz.GetGenericTypeDefinition());
			else 
				return clazz.GetInterface(typeof(IDictionary<,>).Name) == null ? false : true;
		}

		/// <summary>
		/// Get the <see cref="Type"/> of a <see cref="MemberInfo"/>.
		/// TODO: works only with properties and fields.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		private Type GetType(MemberInfo member)
		{
			switch(member.MemberType)
			{
				case MemberTypes.Field:
					return ((FieldInfo) member).FieldType;

				case MemberTypes.Property:
					return ((PropertyInfo) member).PropertyType;
				default:
					throw new ArgumentException("The argument must be a property or field","member");
			}
		}


		/// <summary>
		/// Get the value of some Property or Field.
		/// TODO: refactor this to some Utils.
		/// </summary>
		/// <param name="bean"></param>
		/// <param name="member"></param>
		/// <returns></returns>
		private object GetMemberValue(object bean, MemberInfo member)
		{
			FieldInfo fi = member as FieldInfo;
			if (fi != null)
				return fi.GetValue(bean);

			PropertyInfo pi = member as PropertyInfo;
			if (pi != null)
				return pi.GetValue(bean, ReflectHelper.AnyVisibilityInstance | BindingFlags.GetProperty, null, null, null);
			
			return null;
		}

		/// <summary>
		/// Add recursively the inheritance tree of types (Classes and Interfaces)
		/// to the parameter <paramref name="classes"/>
		/// </summary>
		/// <param name="clazz">Type to be analyzed</param>
		/// <param name="classes">Collections of types</param>
		private void AddSuperClassesAndInterfaces(Type clazz, ISet<Type> classes)
		{
			//iterate for all SuperClasses
			for(Type currentClass = clazz; currentClass != null; currentClass = currentClass.BaseType)
			{
				if (!classes.Add(clazz))
				{
					return; //Base case for the recursivity
				}

				Type[] interfaces = currentClass.GetInterfaces();

				foreach(Type @interface in interfaces)
				{
					AddSuperClassesAndInterfaces(@interface, classes);
				}
			}
		}

		/// <summary>
		/// Assert a valid Object. A <see cref="InvalidStateException"/> 
		/// will be throw in a Invalid state.
		/// </summary>
		/// <param name="bean">Object to be asserted</param>
		public void AssertValid(object bean)
		{
			InvalidValue[] values = GetInvalidValues(bean);
			if (values.Length > 0)
			{
				throw new InvalidStateException(values);
			}
		}

		/// <summary>
		/// Apply constraints of a particular property value of a bean type and return all the failures.
		/// The InvalidValue objects returns return null for InvalidValue#getBean() and InvalidValue#getRootBean()
		/// Note: this is not recursive.
		/// </summary>
		/// <param name="propertyName"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public InvalidValue[] GetPotentialInvalidValues(string propertyName, object value)
		{
			List<InvalidValue> results = new List<InvalidValue>();

			for (int i = 0; i < memberValidators.Count; i++) 
			{
				MemberInfo getter = memberGetters[i];
				if (getter.Name.Equals(propertyName)) 
				{
					IValidator validator = memberValidators[i];
					if (!validator.IsValid(value)) 
						results.Add(new InvalidValue(Interpolate(validator), beanClass, propertyName, value, null));
				}
			}

			return results.ToArray();
		}

		/// <summary>
		/// Apply the registred constraints rules on the hibernate metadata (to be applied on DB schema)
		/// </summary>
		/// <param name="persistentClass">hibernate metadata</param>
		public void Apply(PersistentClass persistentClass)
		{
			foreach (IValidator validator in beanValidators)
			{
				if (validator is IPersistentClassConstraint)
					((IPersistentClassConstraint)validator).Apply(persistentClass);
			}

			for (int i = 0; i < memberValidators.Count;i++ ) 
			{
				IValidator validator = memberValidators[i];
				MemberInfo getter = memberGetters[i];

				string propertyName = getter.Name;

				if(	validator is IPropertyConstraint )
				{
					try
					{
						Property property = FindPropertyByName(persistentClass, propertyName);
						if(property != null)
							((IPropertyConstraint)validator).Apply(property);
					}
					catch(MappingException ex)
					{
					}
				}
			}
		}

		private Property FindPropertyByName(PersistentClass associatedClass, string propertyName)
		{
			Property property = null;
			Property idProperty = associatedClass.IdentifierProperty;
			string idName = idProperty != null ? idProperty.Name : null;
			try
			{
				//if it's a Id
				if (propertyName == null || propertyName.Length == 0 || propertyName.Equals(idName))
					property = idProperty;
				else //if it's a property
				{
					if (propertyName.IndexOf(idName + ".") == 0) 
					{
						property = idProperty;
						propertyName = propertyName.Substring(idName.Length + 1);
					}
					
					foreach(string element in new StringTokenizer(propertyName, ".", false))
					{
						if (property == null)
							property = associatedClass.GetProperty(element);
						else
						{
							if (property.IsComposite) 
								property = ((Component) property.Value).GetProperty(element);
							else
								return null;
						}
					}
				}
			}
			catch(MappingException ex)
			{
				try 
				{
					//if we do not find it try to check the identifier mapper
					if (associatedClass.IdentifierMapper == null) return null;
					StringTokenizer st = new StringTokenizer(propertyName, ".", false);

					foreach(string element in st)
					{
						if (property == null) 
						{
							property = associatedClass.IdentifierMapper.GetProperty(element);
						} 
						else 
						{
							if (property.IsComposite) 
								property = ((Component)property.Value).GetProperty(element);
							else
								return null;
						}
					}
				} 
				catch (MappingException ee) 
				{
					return null;
				}
				
			}

			return property;
		}
	}
}