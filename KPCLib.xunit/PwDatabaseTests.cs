using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Xunit;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;

namespace KPCLib.xunit
{
    public class KeePassFixture : IDisposable
    {
        const string TEST_DB = "utdb.kdbx";
        const string TEST_DB_KEY = "12345";

        public KeePassFixture()
        {
            Logger = new KPCLibLogger();
            PwDb = new PwDatabase();
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(TEST_DB);
            CompositeKey cmpKey = new CompositeKey();
            cmpKey.AddUserKey(new KcpPassword(TEST_DB_KEY));
            PwDb.Open(ioc, cmpKey, Logger);
        }

        public void Dispose()
        {
            PwDb.Close();
        }

        public PwDatabase PwDb { get; private set; }
        public KPCLibLogger Logger { get; private set; }
    }

    [CollectionDefinition("PwDatabase collection")]
    public class PwDatabaseCollection : ICollectionFixture<KeePassFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("PwDatabase collection")]
    public class PwDatabaseTests
    {
        KeePassFixture keepass;

        public PwDatabaseTests(KeePassFixture fixture)
        {
            this.keepass = fixture;
        }

        [Fact]
        public void IsOpenDbTest()
        {
            Debug.WriteLine($"Name={keepass.PwDb.Name}, Description={keepass.PwDb.Description}");
            Assert.True((keepass.PwDb.IsOpen));
        }

        [Fact]
        public void ListGroups()
        {
            PwGroup pg = keepass.PwDb.RootGroup;
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"    Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void ListEntries()
        {
            PwGroup pg = keepass.PwDb.RootGroup;
            int count = 0;
            foreach (var entry in pg.Entries)
            {
                count++;
                Debug.WriteLine($"{count}. {entry.Uuid}");
                foreach (var kp in entry.Strings)
                {
                    Debug.WriteLine($"    {kp.Key}={kp.Value.ReadString()}");
                }
            }
        }
    }
}
