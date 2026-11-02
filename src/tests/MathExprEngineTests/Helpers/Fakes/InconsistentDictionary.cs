using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace MathExprEngineTests.Helpers.Fakes
{
    internal sealed class InconsistentDictionary : IDictionary<string, object>
    {
        private readonly List<KeyValuePair<string, object>> _pairs;

        public InconsistentDictionary(params KeyValuePair<string, object>[] pairs) => _pairs = pairs.ToList();

        public object this[string key] { get => 999.0; set => throw new NotSupportedException(); }
        public ICollection<string> Keys => _pairs.Select(p => p.Key).ToList();
        public ICollection<object> Values => _pairs.Select(p => p.Value).ToList();
        public int Count => _pairs.Count;
        public bool IsReadOnly => true;
        public void Add(string key, object value) => throw new NotSupportedException();
        public void Add(KeyValuePair<string, object> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(KeyValuePair<string, object> item) => true;
        public bool ContainsKey(string key) => true;
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => _pairs.CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => _pairs.GetEnumerator();
        public bool Remove(string key) => throw new NotSupportedException();
        public bool Remove(KeyValuePair<string, object> item) => throw new NotSupportedException();
        public bool TryGetValue(string key, out object value) { value = 999.0; return true; }
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
