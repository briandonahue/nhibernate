using System;
using System.Collections;

namespace NHibernate.DomainModel.NHSpecific
{
	/// <summary>
	/// Summary description for Parent.
	/// </summary>
	public class Parent
	{
		private int _id;
		private string _adultName;
		private Iesi.Collections.ISet _children;
		private Iesi.Collections.ISet _adultFriends;


		public Parent()
		{
			_adultFriends = new Iesi.Collections.SortedSet( new ParentComparer() );
		}

		public int Id 
		{
			get {return _id;}
			set {_id = value;}
		}

		public string AdultName 
		{
			get {return _adultName;}
			set {_adultName = value;}
		}

		public Iesi.Collections.ISet Children 
		{
			get {return _children;}
			set {_children = value;}
		}

		public Iesi.Collections.ISet AdultFriends 
		{
			get {return _adultFriends;}
			set {_adultFriends = value;}
		}

		public void AddFriend(Parent friend) 
		{
			_adultFriends.Add( friend );
		}

	}
}
