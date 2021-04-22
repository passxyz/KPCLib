using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using PureOtp;
using PassXYZLib;

namespace KPCLib.xunit
{
    public class PxEntryTests
    {
        [Fact]
        public void PxEntryInitTests() 
        {
            var entry = new PxEntry();
            Assert.NotNull(entry);
        }
    }
}
