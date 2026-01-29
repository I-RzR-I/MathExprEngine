// ***********************************************************************
//  Assembly         : RzR.Shared.Entity.MathExprEngineTests
//  Author           : RzR
//  Created On       : 2026-01-18 19:01
// 
//  Last Modified By : RzR
//  Last Modified On : 2026-01-18 19:26
// ***********************************************************************
//  <copyright file="ParserResult.cs" company="RzR SOFT & TECH">
//   Copyright © RzR. All rights reserved.
//  </copyright>
// 
//  <summary>
//  </summary>
// ***********************************************************************

namespace MathExprEngineTests.Models
{
    internal class ParserXDataResult
    {
        public string RawVariable { get; set; }
        public string LineColVariable { get; set; }
        public string Line { get; set; }
        public string Col { get; set; }
    }
}