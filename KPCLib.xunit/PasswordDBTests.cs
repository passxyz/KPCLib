using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;
using PassXYZLib;

namespace KPCLib.xunit
{
    public class PasswordDBFixture : IDisposable
    {
        const string TEST_DB = "pass_d_E8f4pEk.xyz";
        const string TEST_DB_KEY = "12345";

        public PasswordDBFixture()
        {
            PxDb = PasswordDb.Instance;
            PxDb.Open(TEST_DB, TEST_DB_KEY);
        }

        public void Dispose()
        {
            PxDb.Close();
        }

        public PasswordDb PxDb { get; private set; }
        public KPCLibLogger Logger { get; private set; }
    }

    [CollectionDefinition("PasswordDB collection")]
    public class PasswordDBCollection : ICollectionFixture<PasswordDBFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("PasswordDB collection")]
    public class PasswordDBTests
    {
        PasswordDBFixture passxyz;

        public PasswordDBTests(PasswordDBFixture passXYZFixture)
        {
            this.passxyz = passXYZFixture;
        }

        [Fact]
        public void IsOpenDbTest()
        {
            Debug.WriteLine($"{passxyz.PxDb}");
            Assert.True((passxyz.PxDb.IsOpen));
        }

        [Fact]
        public void ListItemsTests()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;

            List<Item> itemList = pg.GetItems();
            foreach (var item in itemList)
            {
                Debug.WriteLine($"\t{item.Name} : {item.ImgSource}");
            }
            Assert.NotNull(itemList);
        }

        [Theory]
        [InlineData("http://github.com")]
        [InlineData("http://www.baidu.com")]
        [InlineData("http://www.youdao.com")]
        [InlineData("http://www.qq.com")]
        [InlineData("http://www.hp.com")]
        [InlineData("http://www.163.com")]
        [InlineData("http://www.bing.com")]
        public void CustomIconTests(string url) 
        {
            var entry = new PwEntry();
            entry.AddNewIcon(url);
            PwCustomIcon icon = passxyz.PxDb.GetPwCustomIcon(entry.CustomIconUuid);
            entry.Name = icon.Name;
            Debug.WriteLine($"{icon.Name} is stored at {passxyz.PxDb.CurrentPath}");
            passxyz.PxDb.CurrentGroup.AddEntry(entry, true);
            passxyz.PxDb.Save(null);
            Assert.False(entry.CustomIconUuid.Equals(PwUuid.Zero));
        }

    }
}
