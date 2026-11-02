using System;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace MathExprEngineTests.Helpers.ExpressionTrees
{
    internal sealed class NoOpBinder : CallSiteBinder
    {
        public override Expression Bind(object[] args, ReadOnlyCollection<ParameterExpression> parameters, LabelTarget returnLabel)
        {
            throw new NotSupportedException();
        }
    }
}
