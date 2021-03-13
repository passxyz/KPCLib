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
    public class PxDatabaseTests
    {
        const string TEST_DB = "utdb.kdbx";
        const string TEST_DB_KEY = "12345";

        private PxDatabase OpenDatabaseInternal()
        {
            PxDatabase pwDb = new PxDatabase();
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(TEST_DB);
            CompositeKey cmpKey = new CompositeKey();
            cmpKey.AddUserKey(new KcpPassword(TEST_DB_KEY));
            pwDb.Open(ioc, cmpKey, null);
            return pwDb;
        }

        [Fact]
        public void IsOpenDbTest() 
        {
            PxDatabase pwDb = new PxDatabase();
            Assert.True((pwDb.IsOpen));
        }

        [Fact]
        public void OpenDatabaseTest()
        {
            PxDatabase pwDb = OpenDatabaseInternal();
            Debug.WriteLine($"Name={pwDb.Name}, Description={pwDb.Description}");
            Assert.True((pwDb.IsOpen));
        }

        [Fact]
        public void ListGroups()
        {
            PxDatabase pwDb = OpenDatabaseInternal();
            PwGroup pg = pwDb.RootGroup;
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void ListEntries()
        {
            PxDatabase pwDb = OpenDatabaseInternal();
            PwGroup pg = pwDb.RootGroup;
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
            PxDatabase pxDb = OpenDatabaseInternal();
            PwGroup pg = pxDb.RootGroup;

            PrintGroups(pxDb.RootGroup);
            var entry = pg.Entries.GetAt(0);
            pxDb.DeleteEntry(entry);
            Debug.WriteLine($"Entry {entry.Strings.ReadSafe("Title")} is deleted.");
            PrintGroups(pxDb.RootGroup);
        }

        [Fact]
        public void DeleteGroup() 
        {
            PxDatabase pxDb = OpenDatabaseInternal();
            PwGroup pg = pxDb.RootGroup;
            var gp = pg.Groups.GetAt(0);
            pxDb.DeleteGroup(gp);
        }
    }
}
