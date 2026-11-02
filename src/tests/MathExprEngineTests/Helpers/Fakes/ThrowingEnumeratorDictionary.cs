using System;
using System.Collections;
using System.Collections.Generic;

namespace MathExprEngineTests.Helpers.Fakes
{
    internal sealed class ThrowingEnumeratorDictionary : IDictionary<string, object>
    {
        public object this[string key] { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public ICollection<string> Keys => throw new NotSupportedException();
        public ICollection<object> Values => throw new NotSupportedException();
        public int Count => 1;
        public bool IsReadOnly => true;
        public void Add(string key, object value) => throw new NotSupportedException();
        public void Add(KeyValuePair<string, object> item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public bool Contains(KeyValuePair<string, object> item) => throw new NotSupportedException();
        public bool ContainsKey(string key) => throw new NotSupportedException();
        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) => throw new NotSupportedException();
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => throw new InvalidOperationException("Collection was modified.");
        public bool Remove(string key) => throw new NotSupportedException();
        public bool Remove(KeyValuePair<string, object> item) => throw new NotSupportedException();
        public bool TryGetValue(string key, out object value) => throw new NotSupportedException();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
