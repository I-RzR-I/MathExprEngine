using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using MathExprEngineTests.Helpers.Formatting;

namespace MathExprEngineTests.Helpers.ExpressionTrees
{
    internal sealed class AllowListVisitor : ExpressionVisitor
    {
        private readonly HashSet<ParameterExpression> _allowedParameters = new HashSet<ParameterExpression>();
        private readonly HashSet<ParameterExpression> _blockVariables = new HashSet<ParameterExpression>();
        private readonly HashSet<string> _allowedMethods = new HashSet<string>(CompiledTreeAllowList.AllowedMethods, StringComparer.Ordinal);
        private readonly ParameterExpression _functions;
        private readonly ParameterExpression _slots;
        private int _lambdaCount;

        public AllowListVisitor(LambdaExpression root)
        {
            _functions = root.Parameters.Count > 0 ? root.Parameters[0] : null;
            _slots = root.Parameters.Count > 1 ? root.Parameters[1] : null;

            foreach (var parameter in root.Parameters)
                _allowedParameters.Add(parameter);
        }

        public List<string> Violations { get; } = new List<string>();

        public HashSet<string> CalledMethods { get; } = new HashSet<string>(StringComparer.Ordinal);

        public HashSet<ExpressionType> SeenNodeTypes { get; } = new HashSet<ExpressionType>();

        private void Fail(Expression node, string reason)
        {
            Violations.Add(node.NodeType + ": " + reason);
        }

        public override Expression Visit(Expression node)
        {
            if (node == null)
                return null;

            SeenNodeTypes.Add(node.NodeType);
            Check(node);

            return base.Visit(node);
        }

        private static bool IsDouble(Expression e)
        {
            return e.Type == typeof(double);
        }

        private void CheckArithmetic(BinaryExpression b)
        {
            if (b.Method != null) Fail(b, "operator Method must be null, was " + b.Method);
            if (b.Conversion != null) Fail(b, "Conversion must be null");
            if (!IsDouble(b) || !IsDouble(b.Left) || !IsDouble(b.Right)) Fail(b, "operands and result must be double");
        }

        private void Check(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Lambda:
                    if (++_lambdaCount > 1) Fail(node, "nested lambda");
                    break;

                case ExpressionType.Parameter:
                    if (!_allowedParameters.Contains((ParameterExpression)node))
                        Fail(node, "parameter '" + ((ParameterExpression)node).Name + "' is neither a root lambda parameter nor a Block variable");
                    break;

                case ExpressionType.Constant:
                    var constant = (ConstantExpression)node;
                    if (constant.Value == null)
                        Fail(node, "null constant");
                    else if (!(constant.Type == typeof(double) || constant.Type == typeof(int) || constant.Type == typeof(string)) ||
                             constant.Value.GetType() != constant.Type)
                        Fail(node, "constant of forbidden type " + constant.Type + " / " + constant.Value.GetType());
                    break;

                case ExpressionType.Add:
                case ExpressionType.Subtract:
                case ExpressionType.Multiply:
                    CheckArithmetic((BinaryExpression)node);
                    break;

                case ExpressionType.Divide:
                    var divide = (BinaryExpression)node;
                    CheckArithmetic(divide);
                    var divisor = divide.Right as ConstantExpression;
                    if (divisor == null || !(divisor.Value is double) || (double)divisor.Value != 100.0)
                        Fail(node, "raw Divide is only allowed by Constant(100.0) (postfix %); '/' must use NodeSemantics.Divide");
                    break;

                case ExpressionType.Negate:
                    var negate = (UnaryExpression)node;
                    if (negate.Method != null) Fail(node, "operator Method must be null");
                    if (!IsDouble(negate) || !IsDouble(negate.Operand)) Fail(node, "operand and result must be double");
                    break;

                case ExpressionType.LessThan:
                case ExpressionType.GreaterThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                    var comparison = (BinaryExpression)node;
                    if (comparison.Method != null) Fail(node, "operator Method must be null");
                    if (comparison.IsLiftedToNull) Fail(node, "IsLiftedToNull must be false");
                    if (!IsDouble(comparison.Left) || !IsDouble(comparison.Right)) Fail(node, "operands must be double");
                    break;

                case ExpressionType.Conditional:
                    var conditional = (ConditionalExpression)node;
                    if (conditional.Test.Type != typeof(bool)) Fail(node, "Test must be bool");
                    if (conditional.Type != typeof(double)) Fail(node, "Type must be double");
                    break;

                case ExpressionType.Call:
                    var call = (MethodCallExpression)node;
                    if (call.Object != null) Fail(node, "instance call");
                    if (!call.Method.IsStatic) Fail(node, "non-static method");
                    if (call.Method.IsGenericMethod) Fail(node, "generic method");
                    var key = ReflectionFormat.MethodKey(call.Method);
                    CalledMethods.Add(key);
                    if (!_allowedMethods.Contains(key)) Fail(node, "method not allow-listed: " + key);
                    if (key == CompiledTreeAllowList.ResolveFunctionKey && !ReferenceEquals(call.Arguments[0], _functions))
                        Fail(node, "ResolveFunction's first argument must be the root 'functions' lambda parameter");
                    break;

                case ExpressionType.NewArrayInit:
                    if (node.Type != typeof(double[])) Fail(node, "only double[] array initialisers are allowed");
                    break;

                case ExpressionType.ArrayIndex:
                    var index = (BinaryExpression)node;
                    if (!ReferenceEquals(index.Left, _slots)) Fail(node, "array must be the 'slots' lambda parameter");
                    var position = index.Right as ConstantExpression;
                    if (position == null || !(position.Value is int)) Fail(node, "index must be Constant(int)");
                    break;

                case ExpressionType.Block:
                    var block = (BlockExpression)node;
                    if (block.Type != typeof(double)) Fail(node, "Block result must be double");
                    foreach (var variable in block.Variables)
                    {
                        if (variable.Type != typeof(Func<double[], double>) && variable.Type != typeof(double[]))
                            Fail(node, "Block variable of forbidden type " + variable.Type);
                        _allowedParameters.Add(variable);
                        _blockVariables.Add(variable);
                    }

                    break;

                case ExpressionType.Assign:
                    var assign = (BinaryExpression)node;
                    var target = assign.Left as ParameterExpression;
                    if (target == null || !_blockVariables.Contains(target)) Fail(node, "Assign target must be a Block variable");
                    break;

                default:
                    Fail(node, "forbidden node type");
                    break;
            }
        }
    }
}
