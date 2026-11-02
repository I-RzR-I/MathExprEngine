// ***********************************************************************
//  Assembly          : RzR.Shared.Entity.MathExprEngine
//  Author            : RzR
//  Created On        : 2026-10-22 20:10
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-10-31 15:37
//  ***********************************************************************
//  <copyright file="DefaultMessages.cs" company="RzR SOFT & TECH">
//      Copyright (c) RzR. All rights reserved.
//  </copyright>
//  <contact>
//      https://iamrzr.dev/contact
//  </contact>
//  <summary></summary>
//  ***********************************************************************

namespace MathExprEngine.Helpers
{
    internal static class DefaultMessages
    {
        public const string FuncNameIsMissing = "The function name is required!";
        public const string UnknownFunction = "Unknown function: {0}!";
        public const string FunctionError = "Error in function: {0}. Error: {1}!";

        public const string FunctionExpectsXArgs = "The function {0} expects {1} arguments";
        public const string FunctionExpectsXArg = "The function {0} expects {1} argument";

        public const string UnexceptedTokenAtTheEnd = "Unexpected token '{0}' after end of expression";

        public const string MathRuleEngineExAtColumn = "Error: {0} at column {1}";
        public const string MathRuleEngineEx = "Error: {0}";

        public const string TokenInvalidNumericMultipleDot = "Invalid numeric literal (multiple '.')";
        public const string TokenInvalidNumber = "Invalid number {0}";
        public const string TokenUnexpectedCharacter = "Unexpected character {0}";

        public const string UnknownBinaryOperator = "Unknown binary operator: {0}!";

        public const string SyntaxExAtColumnWithMessage = "Error: Syntax error at column {0}. Message: {1}";
        public const string SyntaxEx = "Error: {0}";

        public const string FunctionExAtColumnWithMessage = "Error: Function error at column {0}. Message: {1}";
        public const string FunctionEx = "Error: {0}";

        public const string FunctionExpectsAtLeastXArgs = "The function {0} expects at least {1} argument(s)";
        public const string FunctionDomainError = "The function {0} is not defined for the argument value {1}";

        public const string ExpressionIsRequired =
            "The expression is required and cannot be null, empty or white-space.";

        public const string ExpressionTooLong = "The expression length ({0}) exceeds the configured maximum ({1})";

        public const string TooManyTokens =
            "The expression produced too many tokens ({0}); the configured maximum is {1}";

        public const string NodeBudgetExceeded = "The expression is too complex: it produced more than {0} nodes";
        public const string ParseDepthExceeded = "The expression nesting depth exceeds the configured maximum ({0})";

        public const string EvaluationDepthExceeded =
            "The expression evaluation depth exceeds the configured maximum ({0})";

        public const string EvaluationStackExhausted =
            "Expression evaluation exhausted the available stack space at depth {0}";

        public const string ParseStackExhausted =
            "The expression nesting is too deep for the available stack (configured maximum depth {0})";

        public const string CompiledHelperMissing = "The compiled-expression helper '{0}.{1}' could not be resolved";

        public const string FunctionTooManyArgs =
            "The function '{0}' was called with more than the configured maximum of {1} arguments";

        public const string NonFiniteResult = "The expression produced a non-finite value (NaN or Infinity)";
        public const string DivisionByZero = "Division by zero";
        public const string ModuloByZero = "Modulo by zero";

        public const string UnknownUnaryOperator = "Unknown unary operator '{0}'";
        public const string UndefinedVariable = "Undefined variable '{0}'.";

        public const string ParameterNullValue =
            "The parameter '{0}' value is null and cannot be converted to a number";

        public const string ParameterNotConvertible =
            "The parameter '{0}' value of type '{1}' cannot be converted to a number";

        public const string ExpectedTokenFoundToken = "Expected {0}, found {1}";
        public const string ExpectedColonInConditional = "Expected ':' in conditional operator";
        public const string MissingClosingParenAfterArgs = "Missing closing ')' after function arguments";
        public const string MissingClosingParen = "Missing closing ')'";

        public const string UnexpectedIdentifier =
            "Unexpected identifier '{0}' (functions must be called like name(...))";

        public const string UnexpectedToken = "Unexpected token '{0}'";

        public const string ParametersModifiedDuringCopy =
            "The parameters dictionary was modified by another thread while it was being copied for evaluation.";

        public const string CompileHeightExceeded =
            "The expression is too tall to compile: its height exceeds the compile cap ({0})";

        public const string TestOptionsOverrideReturnedNull =
            "The test options override returned null instead of an options instance";
    }
}