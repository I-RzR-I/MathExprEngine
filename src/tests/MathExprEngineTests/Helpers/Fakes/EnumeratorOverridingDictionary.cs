using System;
using System.Collections.Generic;

namespace MathExprEngineTests.Helpers.Fakes
{
    internal sealed class EnumeratorOverridingDictionary : Dictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
    {
        public EnumeratorOverridingDictionary() : base(StringComparer.OrdinalIgnoreCase) => this["x"] = 999.0;

        IEnumerator<KeyValuePair<string, object>> IEnumerable<KeyValuePair<string, object>>.GetEnumerator()
        {
            yield return new KeyValuePair<string, object>("x", 1.0);
        }
    }
}
