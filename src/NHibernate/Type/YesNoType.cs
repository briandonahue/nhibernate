using System;

namespace NHibernate.Type
{
	/// <summary>
	/// YesNoType.
	/// </summary>
	public class YesNoType : CharBooleanType {

		protected override sealed string TrueString {
			get { return "Y"; }
		}

		protected override sealed string FalseString {
			get { return "N"; }
		}

		public override string Name {
			get { return "yes_no"; }
		}
	}
}