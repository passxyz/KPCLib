using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Xunit;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;
using PassXYZLib;

namespace KPCLib.xunit
{
    public class PassXYZFixture : IDisposable
    {
        const string TEST_DB = "utdb.kdbx";
        const string TEST_DB_KEY = "12345";

        public PassXYZFixture() 
        {
            Logger = new KPCLibLogger();
            PxDb = new PxDatabase();
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(TEST_DB);
            CompositeKey cmpKey = new CompositeKey();
            cmpKey.AddUserKey(new KcpPassword(TEST_DB_KEY));
            PxDb.Open(ioc, cmpKey, Logger);
        }

        public void Dispose() 
        {
            PxDb.Close();
        }

        public PxDatabase PxDb { get; private set; }
        public KPCLibLogger Logger { get; private set; }
    }

    [CollectionDefinition("PxDatabase collection")]
    public class PxDatabaseCollection : ICollectionFixture<PassXYZFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("PxDatabase collection")]
    public class PxDatabaseTests
    {
        PassXYZFixture passxyz;

        public PxDatabaseTests(PassXYZFixture passXYZFixture) 
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
        public void ListGroups()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void ListEntries()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;
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

        private void PrintGroups(PwGroup pg)
        {
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void DeleteEntry()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;

            PrintGroups(passxyz.PxDb.RootGroup);
            var entry = pg.Entries.GetAt(0);
            passxyz.PxDb.DeleteEntry(entry);
            Debug.WriteLine($"Entry {entry.Strings.ReadSafe("Title")} is deleted.");
            PrintGroups(passxyz.PxDb.RootGroup);
        }

        [Fact]
        public void DeleteGroup() 
        {
            PwGroup pg = passxyz.PxDb.RootGroup;
            var gp = pg.Groups.GetAt(0);
            passxyz.PxDb.DeleteGroup(gp);
        }
    }
}
