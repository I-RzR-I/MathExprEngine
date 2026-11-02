using System.Collections.Generic;
using System.Linq;

namespace MathExprEngineTests.Helpers.Parity
{
    internal static class DynamicDataRows
    {
        public static IEnumerable<object[]> ByCompiledPath(IEnumerable<string> ids)
        {
            return ByPaths(ids, ParityAssert.CompiledPaths);
        }

        public static IEnumerable<object[]> ByEveryPath(IEnumerable<string> ids)
        {
            return ByPaths(ids, ParityAssert.AllPaths);
        }

        public static IEnumerable<object[]> Ids(IEnumerable<string> ids)
        {
            return ids.Select(k => new object[] { k });
        }

        private static IEnumerable<object[]> ByPaths(IEnumerable<string> ids, IEnumerable<ParityPath> paths)
        {
            foreach (var id in ids)
                foreach (var p in paths)
                    yield return new object[] { id, p.ToString() };
        }
    }
}
