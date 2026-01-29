// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-01-18 19:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 19:27
// ***********************************************************************
//  <copyright file="ParseXData.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

#region U S A G E S

using System.Collections.Generic;
using System.Text.RegularExpressions;
using MathExprEngineTests.Models;

#endregion

namespace MathExprEngineTests.Helpers
{
    internal static class ParseXData
    {
        private static readonly Regex _regex = new Regex(@"\{\[(\d+)\];\[(\d+)\]\}");

        public static IEnumerable<ParserXDataResult> Parse(string rasExpression)
        {
            var result = new List<ParserXDataResult>();

            var matches = _regex.Matches(rasExpression);

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var res = new ParserXDataResult
                    {
                        RawVariable = match.Value,
                        LineColVariable = match.Value.Replace("{", "").Replace("}", ""),
                        Line = match.Groups[1].Value,
                        Col = match.Groups[2].Value
                    };

                    result.Add(res);
                }
            }

            return result;
        }
    }
}