// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="FunctionNode.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

#region U S I N G

using System.Collections.Generic;
using System.Linq.Expressions;
using MathExprEngine.Compilation;
using MathExprEngine.Helpers;
using MathExprEngine.Models;

#endregion

namespace MathExprEngine.Nodes
{
    /// <summary>
    ///     A function node.
    /// </summary>
    /// <seealso cref="T:MathExprEngine.Nodes.NodeBase" />
    internal sealed class FunctionNode : NodeBase
    {
        /// <summary>
        ///     (Immutable) the arguments.
        /// </summary>
        private readonly List<NodeBase> _args;

        /// <summary>
        ///     (Immutable) the name.
        /// </summary>
        private readonly string _name;

        /// <summary>
        ///     Initializes a new instance of the <see cref="FunctionNode" /> class.
        /// </summary>
        /// <param name="name">(Immutable) the name.</param>
        /// <param name="args">(Immutable) the arguments.</param>
        /// <param name="col">The col.</param>
        public FunctionNode(string name, List<NodeBase> args, int col)
        {
            _name = name;
            _args = args;

            Column = col;
        }

        /// <inheritdoc />
        protected override double EvaluateCore(EvaluationContext ctx)
        {
            var impl = NodeSemantics.ResolveFunction(ctx.Functions, _name, Column);

            var evaluated = new double[_args.Count];
            for (var i = 0; i < _args.Count; i++)
                evaluated[i] = _args[i].Evaluate(ctx);

            return NodeSemantics.InvokeFunction(impl, _name, Column, evaluated);
        }

        /// <inheritdoc />
        protected override Expression CompileCore(CompilationScope scope)
        {
            var name = Expression.Constant(_name);
            var column = Expression.Constant(Column);
            var nodeDepth = Expression.Constant(scope.CurrentDepth);
            var resolve = Expression.Call(CompiledRuntime.ResolveFunctionMethod, scope.Functions, name, column);

            var args = new Expression[_args.Count];
            for (var i = 0; i < _args.Count; i++)
                args[i] = _args[i].Compile(scope);

            return Expression.Call(CompiledRuntime.InvokeFunctionMethod,
                resolve, name, column, Expression.NewArrayInit(typeof(double), args),
                scope.BaseDepth, nodeDepth);
        }
    }
}