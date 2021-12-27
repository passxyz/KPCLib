using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using KPCLib;
using KeePassLib;

namespace xunit.KPCLib
{
    public class ItemTest
    {
        [Fact]
        public void PwEntryTest()
        {
            Item item = new PwEntry(true, true);
            Debug.WriteLine($"Id={item.Id}, Name={item.Name}, Desc={item.Description}, Time={item.LastModificationTime}");
            Assert.NotNull(item.Id);
            Assert.True(!item.IsGroup);
        }

        [Fact]
        public void PwGroupTest() 
        {
            Item item = new PwGroup(true, true);
            Debug.WriteLine($"Id={item.Id}, Name={item.Name}, Desc={item.Description}, Time={item.LastModificationTime}");
            Assert.NotNull(item.Id);
            Assert.True(item.IsGroup);
        }
    }
}
