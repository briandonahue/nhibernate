//------------------------------------------------------------------------------
// <autogenerated>
//     This code was generated by a tool.
//     Runtime Version: v1.1.4322
//
//     Changes to this file may cause incorrect behavior and will be lost if 
//     the code is regenerated.
// </autogenerated>
//------------------------------------------------------------------------------

using System;

namespace NHibernate.DomainModel
{

	/// <summary>
	/// POJO for Foo
	/// </summary>
	/// <remark>
	/// This class is autogenerated
	/// </remark>
	[Serializable]
	public class Foo : FooProxy, ILifecycle
	{
		[Serializable]
			public class Struct
		{
			public string name;
			public int count;

			public override bool Equals(object obj)
			{
				Struct s = (Struct) obj;
				return ( s.name==name || s.name.Equals(name) ) && s.count==count;
			}
	 
			public override int GetHashCode()
			{
				return count;
			}
		}

		#region Fields
		/// <summary>
		/// Holder for key
		/// </summary>
		private string _key;
		/// <summary>
		/// Holds the component
		/// </summary> 
		private FooComponent _component;

		/// <summary>
		/// Gets or sets the component
		/// </summary> 
		public FooComponent component
		{
			get 
			{
				return _component; 
			}
			set 
			{
				_component = value;
			}
		}
	
		/// <summary>
		/// Holder for long
		/// </summary>
		private long _long;
	
		/// <summary>
		/// Holder for integer
		/// </summary>
		private int _integer;
	
		/// <summary>
		/// Holder for float
		/// </summary>
		private float _float;
	
		/// <summary>
		/// Holder for x
		/// </summary>
		private String _x;
	
		/// <summary>
		/// Holder for double
		/// </summary>
		private double _double;
	
		/// <summary>
		/// Holder for date
		/// </summary>
		private DateTime _date;
	
		/// <summary>
		/// Holder for timestamp
		/// </summary>
		private DateTime _timestamp;
	
		/// <summary>
		/// Holder for boolean
		/// </summary>
		private bool _boolean;
	
		/// <summary>
		/// Holder for bool
		/// </summary>
		private bool _bool;
	
		/// <summary>
		/// Holder for null
		/// </summary>
		private int _null;
	
		/// <summary>
		/// Holder for short
		/// </summary>
		private short _short;
	
		/// <summary>
		/// Holder for char
		/// </summary>
		private char _char;
	
		/// <summary>
		/// Holder for zero
		/// </summary>
		private float _zero;
	
		/// <summary>
		/// Holder for int
		/// </summary>
		private int _int;
	
		/// <summary>
		/// Holder for string
		/// </summary>
		private String _string;
	
		/// <summary>
		/// Holder for byte
		/// </summary>
		private byte _byte;
	
		/// <summary>
		/// Holder for yesno
		/// </summary>
		private bool _yesno;
	
		/// <summary>
		/// Holder for blob
		/// </summary>
		private Foo.Struct _blob;
	
		/// <summary>
		/// Holder for nullBlob
		/// </summary>
		private object _nullBlob;
	
		/// <summary>
		/// Holder for status
		/// </summary>
		private FooStatus _status;
	
		/// <summary>
		/// Holder for binary
		/// </summary>
		private byte[] _binary;
		private byte[] _bytes;
	
		/// <summary>
		/// Holder for locale
		/// </summary>
		private String _locale;
	
		/// <summary>
		/// Holder for formula
		/// </summary>
		private String _formula;
	
		/// <summary>
		/// Holder for custom
		/// </summary>
		private string[] _custom;
	
		/// <summary>
		/// Holder for version
		/// </summary>
		private String _version;
	
		/// <summary>
		/// Holder for foo
		/// </summary>
		private FooProxy _foo;
	
		/// <summary>
		/// Holder for dependent
		/// </summary>
		private Fee _dependent;
	
		#endregion

		#region Constructors
		/// <summary>
		/// Default constructor for class Foo
		/// </summary>
		public Foo()
		{
		}
	
		#endregion
	
		#region Properties
		/// <summary>
		/// Get/set for key
		/// </summary>
		public string key
		{
			get
			{
				return this._key;
			}
			set
			{
				this._key = value;
			}
		}
	
		/// <summary>
		/// Get/set for long
		/// </summary>
		public long @long
		{
			get
			{
				return this._long;
			}
			set
			{
				this._long = value;
			}
		}
	
		/// <summary>
		/// Get/set for integer
		/// </summary>
		public int integer
		{
			get
			{
				return this._integer;
			}
			set
			{
				this._integer = value;
			}
		}
	
		/// <summary>
		/// Get/set for float
		/// </summary>
		public float @float
		{
			get
			{
				return this._float;
			}
			set
			{
				this._float = value;
			}
		}
	
		/// <summary>
		/// Get/set for x
		/// </summary>
		public String x
		{
			get
			{
				return this._x;
			}
			set
			{
				this._x = value;
			}
		}
	
		/// <summary>
		/// Get/set for double
		/// </summary>
		public double @double
		{
			get
			{
				return this._double;
			}
			set
			{
				this._double = value;
			}
		}
	
		/// <summary>
		/// Get/set for date
		/// </summary>
		public DateTime date
		{
			get
			{
				return this._date;
			}
			set
			{
				this._date = value;
			}
		}
	
		/// <summary>
		/// Get/set for timestamp
		/// </summary>
		public DateTime timestamp
		{
			get
			{
				return this._timestamp;
			}
			set
			{
				this._timestamp = value;
			}
		}
	
		/// <summary>
		/// Get/set for boolean
		/// </summary>
		public bool boolean
		{
			get
			{
				return this._boolean;
			}
			set
			{
				this._boolean = value;
			}
		}
	
		/// <summary>
		/// Get/set for bool
		/// </summary>
		public bool @bool
		{
			get
			{
				return this._bool;
			}
			set
			{
				this._bool = value;
			}
		}
	
		/// <summary>
		/// Get/set for null
		/// </summary>
		public int @null
		{
			get
			{
				return this._null;
			}
			set
			{
				this._null = value;
			}
		}
	
		/// <summary>
		/// Get/set for short
		/// </summary>
		public short @short
		{
			get
			{
				return this._short;
			}
			set
			{
				this._short = value;
			}
		}
	
		/// <summary>
		/// Get/set for char
		/// </summary>
		public char @char
		{
			get
			{
				return this._char;
			}
			set
			{
				this._char = value;
			}
		}
	
		/// <summary>
		/// Get/set for zero
		/// </summary>
		public float zero
		{
			get
			{
				return this._zero;
			}
			set
			{
				this._zero = value;
			}
		}
	
		/// <summary>
		/// Get/set for int
		/// </summary>
		public int @int
		{
			get
			{
				return this._int;
			}
			set
			{
				this._int = value;
			}
		}
	
		/// <summary>
		/// Get/set for string
		/// </summary>
		public string @string
		{
			get
			{
				return this._string;
			}
			set
			{
				this._string = value;
			}
		}
	
		/// <summary>
		/// Get/set for byte
		/// </summary>
		public byte @byte
		{
			get
			{
				return this._byte;
			}
			set
			{
				this._byte = value;
			}
		}
	
		/// <summary>
		/// Get/set for yesno
		/// </summary>
		public bool yesno
		{
			get
			{
				return this._yesno;
			}
			set
			{
				this._yesno = value;
			}
		}
	
		/// <summary>
		/// Get/set for blob
		/// </summary>
		public Foo.Struct blob
		{
			get
			{
				return this._blob;
			}
			set
			{
				this._blob = value;
			}
		}
	
		/// <summary>
		/// Get/set for nullBlob
		/// </summary>
		public object nullBlob
		{
			get
			{
				return this._nullBlob;
			}
			set
			{
				this._nullBlob = value;
			}
		}
	
		/// <summary>
		/// Get/set for status
		/// </summary>
		public FooStatus status
		{
			get
			{
				return this._status;
			}
			set
			{
				this._status = value;
			}
		}
	
		/// <summary>
		/// Get/set for binary
		/// </summary>
		public byte[] binary
		{
			get
			{
				return this._binary;
			}
			set
			{
				this._binary = value;
			}
		}
		public byte[] bytes
		{
			get
			{
				return this._bytes;
			}
			set
			{
				this._bytes = value;
			}
		}
	
		/// <summary>
		/// Get/set for locale
		/// </summary>
		public String locale
		{
			get
			{
				return this._locale;
			}
			set
			{
				this._locale = value;
			}
		}
	
		/// <summary>
		/// Get/set for formula
		/// </summary>
		public String formula
		{
			get
			{
				return this._formula;
			}
			set
			{
				this._formula = value;
			}
		}
	
		/// <summary>
		/// Get/set for custom
		/// </summary>
		public string[] custom
		{
			get
			{
				return this._custom;
			}
			set
			{
				this._custom = value;
			}
		}
	
		/// <summary>
		/// Get/set for version
		/// </summary>
		public String version
		{
			get
			{
				return this._version;
			}
			set
			{
				this._version = value;
			}
		}
	
		/// <summary>
		/// Get/set for foo
		/// </summary>
		public FooProxy foo
		{
			get
			{
				return this._foo;
			}
			set
			{
				this._foo = value;
			}
		}
	
		/// <summary>
		/// Get/set for dependent
		/// </summary>
		public Fee dependent
		{
			get
			{
				return this._dependent;
			}
			set
			{
				this._dependent = value;
			}
		}
	
		#endregion

		#region ILifecycle Members

		public LifecycleVeto OnUpdate(ISession s)
		{
			return LifecycleVeto.NoVeto;
		}

		public void OnLoad(ISession s, object id)
		{
		}

		public LifecycleVeto OnSave(ISession s)
		{
			_string = "a string";
			_date = new DateTime(123);
			_timestamp = DateTime.Now;
			_integer = -666;
			_long = 696969696969696969L - count++;
			_short = 42;
			_float = 6666.66f;
			//_double = new Double( 1.33e-69 );  // this double is too big for the sap db jdbc driver
			_double = 1.12e-36;
			_boolean = true;
			_byte = 127;
			_int = 2;
			_char = '@';
			_bytes = System.Text.Encoding.ASCII.GetBytes(_string);
			Struct ss = new Struct();
			ss.name="name";
			ss.count = 69;
			blob = ss;
			status=FooStatus.ON;
			binary = System.Text.Encoding.ASCII.GetBytes( _string + "yada yada yada" );
			custom = new string[]
			  {
				  "foo", "bar" 
			  };
			component = new FooComponent("foo", 12, new DateTime[] { _date, _timestamp, DateTime.MinValue, new DateTime() }, new FooComponent("bar", 666, new DateTime[] { new DateTime(123456L), DateTime.MinValue }, null ) );
			component.glarch = new Glarch();
			dependent = new Fee();
			dependent.Fi = "belongs to foo # " + key;
			locale = System.Threading.Thread.CurrentThread.CurrentCulture.Name;
			return LifecycleVeto.NoVeto;
		}

		public LifecycleVeto OnDelete(ISession s)
		{
			return LifecycleVeto.NoVeto;
		}

		#endregion

		public void disconnect() 
		{
			if ( _foo!=null) _foo.disconnect();
			_foo=null;
		}

		public override bool Equals(object obj)
		{
			Foo other = (Foo)obj;
			if ( _bytes!=other._bytes ) 
			{
				if ( _bytes==null || other._bytes==null ) return false;
				if ( _bytes.Length!=other._bytes.Length ) return false;
				for ( int i=0; i< _bytes.Length; i++) 
				{
					if ( _bytes[i] != other._bytes[i] ) return false;
				}
			}
		
			return ( this._bool == other._bool )
				&& ( ( this._boolean == other._boolean ) || ( this._boolean.Equals(other._boolean) ) )
				&& ( ( this._byte == other._byte ) || ( this._byte.Equals(other._byte) ) )
				//&& ( ( this._date == other._date ) || ( this._date.getDate() == other._date.getDate() && this._date.getMonth() == other._date.getMonth() && this._date.getYear() == other._date.getYear() ) )
				&& ( ( this._double == other._double ) || ( this._double.Equals(other._double) ) )
				&& ( ( this._float == other._float ) || ( this._float.Equals(other._float) ) )
				&& ( this._int == other._int )
				&& ( ( this._integer == other._integer ) || ( this._integer.Equals(other._integer) ) )
				&& ( ( this._long == other._long ) || ( this._long.Equals(other._long) ) )
				&& ( this._null == other._null )
				&& ( ( this._short == other._short ) || ( this._short.Equals(other._short) ) )
				&& ( ( this._string == other._string) || ( this._string.Equals(other._string) ) )
				//&& ( ( this._timestamp==other._timestamp) || ( this._timestamp.getDate() == other._timestamp.getDate() && this._timestamp.getYear() == other._timestamp.getYear() && this._timestamp.getMonth() == other._timestamp.getMonth() ) )
				&& ( this._zero == other._zero )
				&& ( ( this._foo == other._foo ) || ( this._foo.key.Equals( other._foo.key ) ) )
				&& ( ( this.blob == other.blob ) || ( this.blob.Equals(other.blob) ) )
				&& ( this.yesno == other.yesno )
				&& ( this.status == other.status )
				&& ( ( this.binary == other.binary ) || this.binary.Equals(other._binary))
				&& ( this.key.Equals(other.key) )
				&& ( this.locale.Equals(other.locale) )
				&& ( ( this.custom == other.custom ) || ( this.custom[0].Equals(other.custom[0]) && this.custom[1].Equals(other.custom[1]) ) );
		}
		public override int GetHashCode()
		{
			return key.GetHashCode() - _string.GetHashCode();
		}
		public FooComponent nullComponent
		{
			get
			{
				return null;
			}
			set
			{
				if (value!=null) throw new Exception("Null component");
			}
		}

		private static int count=0;
	}
}
