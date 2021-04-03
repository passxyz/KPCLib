using System.Collections.Generic;
using System.Linq;

using Xunit;
using Xunit.Abstractions;

namespace KPCLib.xunit.Orderers
{
    class DisplayNameOrderer : ITestCollectionOrderer
    {
        public IEnumerable<ITestCollection> OrderTestCollections(
            IEnumerable<ITestCollection> testCollections) =>
            testCollections.OrderBy(collection => collection.DisplayName);
    }
}
