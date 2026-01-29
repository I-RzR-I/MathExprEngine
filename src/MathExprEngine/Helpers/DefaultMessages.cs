// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngine
//  Author           : RzR
//  Created On       : 2026-01-18 20:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 20:59
// ***********************************************************************
//  <copyright file="DefaultMessages.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace MathExprEngine.Helpers
{
    internal static class DefaultMessages
    {
        public const string FuncNameIsMissing = "The function name is required!";
        public const string UnknownFunction = "Unknown function: '{0}'!";
        public const string FunctionError = "Error in function: '{0}'. Error: {1}!";

        public const string FunctionExpectsXArgs = "The function {0} expects {1} arguments";
        public const string FunctionExpectsXArg = "The function {0} expects {1} argument";

        public const string UnexceptedTokenAtTheEnd = "Unexpected token '{0}' after end of expression";

        public const string MathRuleEngineExAtColumn = "Error: {0} at column {1}";
        public const string MathRuleEngineEx = "Error: {0}";
        
        public const string TokenInvalidNumericMultipleDot = "Invalid numeric literal (multiple '.')";
        public const string TokenInvalidNumber = "Invalid number '{0}'";
        public const string TokenUnexpectedCharacter = "Unexpected character '{0}'";

        public const string UnknownBinaryOperator = "Unknown binary operator: '{0}'!";
    }
}

