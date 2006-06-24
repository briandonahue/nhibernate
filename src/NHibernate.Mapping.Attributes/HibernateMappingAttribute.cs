// 
// NHibernate.Mapping.Attributes
// This product is under the terms of the GNU Lesser General Public License.
//
//
//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: 1.1.4322.2032
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------
//
//
// This source code was auto-generated by Refly, Version=2.21.1.0 (modified).
//
namespace NHibernate.Mapping.Attributes
{
	
	
	/// <summary>hibernate-mapping is the document root</summary>
	[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple=false)]
	[System.Serializable()]
	public class HibernateMappingAttribute : BaseAttribute
	{
		
		private bool _defaultlazyspecified;
		
		private bool _autoimportspecified;
		
		private string _schema = null;
		
		private CascadeStyle _defaultcascade = CascadeStyle.Unspecified;
		
		private string _namespace = null;
		
		private string _defaultaccess = null;
		
		private bool _autoimport = true;
		
		private string _assembly = null;
		
		private bool _defaultlazy = true;
		
		/// <summary> Default constructor (position=0) </summary>
		public HibernateMappingAttribute() : 
				base(0)
		{
		}
		
		/// <summary> Constructor taking the position of the attribute. </summary>
		public HibernateMappingAttribute(int position) : 
				base(position)
		{
		}
		
		/// <summary>defaults to none used</summary>
		public virtual string Schema
		{
			get
			{
				return this._schema;
			}
			set
			{
				this._schema = value;
			}
		}
		
		/// <summary> </summary>
		public virtual CascadeStyle DefaultCascade
		{
			get
			{
				return this._defaultcascade;
			}
			set
			{
				this._defaultcascade = value;
			}
		}
		
		/// <summary>Default property access setting</summary>
		public virtual string DefaultAccess
		{
			get
			{
				return this._defaultaccess;
			}
			set
			{
				this._defaultaccess = value;
			}
		}
		
		/// <summary>Default property access setting</summary>
		public virtual System.Type DefaultAccessType
		{
			get
			{
				return System.Type.GetType( this.DefaultAccess );
			}
			set
			{
				if(value.Assembly == typeof(int).Assembly)
					this.DefaultAccess = value.FullName.Substring(7);
				else
					this.DefaultAccess = value.FullName + ", " + value.Assembly.GetName().Name;
			}
		}
		
		/// <summary> </summary>
		public virtual bool AutoImport
		{
			get
			{
				return this._autoimport;
			}
			set
			{
				this._autoimport = value;
				_autoimportspecified = true;
			}
		}
		
		/// <summary> Tells if AutoImport has been specified. </summary>
		public virtual bool AutoImportSpecified
		{
			get
			{
				return this._autoimportspecified;
			}
		}
		
		/// <summary>Namespace used to find not-Fully Qualified Type Names</summary>
		public virtual string Namespace
		{
			get
			{
				return this._namespace;
			}
			set
			{
				this._namespace = value;
			}
		}
		
		/// <summary>Assembly used to find not-Fully Qualified Type Names</summary>
		public virtual string Assembly
		{
			get
			{
				return this._assembly;
			}
			set
			{
				this._assembly = value;
			}
		}
		
		/// <summary>Default value of the lazy attribute for persistent classes and collections</summary>
		public virtual bool DefaultLazy
		{
			get
			{
				return this._defaultlazy;
			}
			set
			{
				this._defaultlazy = value;
				_defaultlazyspecified = true;
			}
		}
		
		/// <summary> Tells if DefaultLazy has been specified. </summary>
		public virtual bool DefaultLazySpecified
		{
			get
			{
				return this._defaultlazyspecified;
			}
		}
	}
}
