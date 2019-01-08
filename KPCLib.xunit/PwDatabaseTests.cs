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
    public class PwDatabaseTests
    {
        const string TEST_DB = "testdb.kdbx";
        const string TEST_DB_KEY = "12345";

        private PwDatabase OpenDatabaseInternal()
        {
            PwDatabase pwDb = new PwDatabase();
            IOConnectionInfo ioc = IOConnectionInfo.FromPath(TEST_DB);
            CompositeKey cmpKey = new CompositeKey();
            cmpKey.AddUserKey(new KcpPassword(TEST_DB_KEY));
            pwDb.Open(ioc, cmpKey, null);
            return pwDb;
        }

        [Fact]
        public void OpenDatabaseTest()
        {
            PwDatabase pwDb = OpenDatabaseInternal();
            Debug.WriteLine($"Name={pwDb.Name}, Description={pwDb.Description}");
            Assert.True((pwDb.IsOpen));
        }

        [Fact]
        public void ListGroups()
        {
            PwDatabase pwDb = OpenDatabaseInternal();
            PwGroup pg = pwDb.RootGroup;
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void ListEntries()
        {
            PwDatabase pwDb = OpenDatabaseInternal();
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
    }
}
