﻿using System;
using Antlr.Runtime;

namespace NHibernate.Hql.Ast.ANTLR.Tree
{
	/// <summary>
	/// A select expression that was generated by a FROM element.
	/// Author: josh
	/// Ported by: Steve Strong
	/// </summary>
	public class SelectExpressionImpl :FromReferenceNode, ISelectExpression 
	{
		public SelectExpressionImpl(IToken token) : base(token)
		{
		}

		public override void ResolveIndex(IASTNode parent)
		{
			throw new InvalidOperationException();
		}

		public override void SetScalarColumnText(int i)
		{
			Text = FromElement.RenderScalarIdentifierSelect(i);
		}

		public override void Resolve(bool generateJoin, bool implicitJoin, string classAlias, IASTNode parent)
		{
			// Generated select expressions are already resolved, nothing to do.
			return;
		}
	}
}
