﻿using System;
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
        public void ListGroupsTests()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;
            foreach (var group in pg.Groups)
            {
                Debug.WriteLine($"Name={group.Name}, Note={group.Notes}");
            }
        }

        [Fact]
        public void ListEntriesTests()
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

        [Fact]
        public void DeleteEmptyEntryTest()
        {
            try 
            { 
                passxyz.PxDb.DeleteEntry(null);
                Assert.True(false);
            }
            catch (System.ArgumentNullException e) 
            {
                Debug.WriteLine($"{e}");
            }            
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        /// <summary>
        /// Delete the first entry in the list.
        /// </summary>
        public void DeleteEntryTests(bool permanent)
        {
            PwGroup rootGroup = passxyz.PxDb.RootGroup;

            var entry1 = rootGroup.Entries.GetAt(0);
            var uuid = entry1.Uuid;
            Debug.WriteLine($"Entry {entry1.Strings.ReadSafe("Title")} is deleted.");
            passxyz.PxDb.DeleteEntry(entry1, permanent);
            var entry2 = rootGroup.FindEntry(uuid, true);
            if(permanent) { Assert.Null(entry2); }
            else { Assert.NotNull(entry2); }
            
        }


        [Fact]
        public void DeleteEmptyGroupTest()
        {
            try
            {
                passxyz.PxDb.DeleteGroup(null);
                Assert.True(false);
            }
            catch (System.ArgumentNullException e)
            {
                Debug.WriteLine($"{e}");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        /// <summary>
        /// Delete a group.
        /// </summary>
        public void DeleteGroupTests(bool permanent)
        {
            PwGroup rootGroup = passxyz.PxDb.RootGroup;

            var gp1 = rootGroup.Groups.GetAt(0);
            var uuid = gp1.Uuid;
            Debug.WriteLine($"Deleting '{gp1.Name}'");
            passxyz.PxDb.DeleteGroup(gp1, permanent);
            var gp2 = rootGroup.FindGroup(uuid, true);
            if (permanent) { Assert.Null(gp2); }
            else { Assert.NotNull(gp2); }
        }

        [Theory]
        [InlineData("General/G1/G21/G21E1")]
        [InlineData("General/G1/G21/")]
        [InlineData("General/G1/G21")]
        /// <summary>
        /// Find group or entry test.
        /// </summary>
        public void FindByPathTests(string path)
        {
            passxyz.PxDb.CurrentGroup = passxyz.PxDb.RootGroup;
            if (path.EndsWith("/"))
            {
                Debug.WriteLine($"{passxyz.PxDb.FindByPath<PwGroup>(path)}");
            }
            else 
            {
                Debug.WriteLine($"{passxyz.PxDb.FindByPath<PwEntry>(path)}");
            }
        }

        [Theory]
        [InlineData("General/G1/G21/")]
        /// <summary>
        /// Find an entry using a group path.
        /// </summary>
        public void FindEntryByPathTests(string path)
        {
            passxyz.PxDb.CurrentGroup = passxyz.PxDb.RootGroup;
            Debug.WriteLine($"{passxyz.PxDb.FindByPath<PwEntry>(path)}");
            Assert.Null(passxyz.PxDb.FindByPath<PwEntry>(path));
        }

        [Theory]
        [InlineData("General/G1/G21/G21E1")]
        /// <summary>
        /// Find group using an entry pass.
        /// </summary>
        public void FindGroupByPathTests(string path)
        {
            passxyz.PxDb.CurrentGroup = passxyz.PxDb.RootGroup;
            Debug.WriteLine($"{passxyz.PxDb.FindByPath<PwGroup>(path)}");
            Assert.Null(passxyz.PxDb.FindByPath<PwGroup>(path));
        }

        [Fact]
        public void FindByPathDefaultTests() 
        {
            Assert.Null(passxyz.PxDb.FindByPath<PwEntry>());
            Assert.Equal(passxyz.PxDb.FindByPath<PwGroup>().ToString(), passxyz.PxDb.CurrentGroup.ToString());
        }

        [Fact]
        public void CurrentGroupTests()
        {
            Debug.WriteLine($"{passxyz.PxDb.CurrentGroup}");
            Assert.NotNull(passxyz.PxDb.CurrentGroup);
        }
    }

    public class PxLibInfoTests 
    { 
        [Fact]
        public void PxLibVersion() 
        {
            Debug.WriteLine($"{PxLibInfo.Version}");
            Assert.Equal(PxLibInfo.Version, new System.Version("1.0.1.1"));
        }

        [Fact]
        public void PxLibName()
        {
            Debug.WriteLine($"{PxLibInfo.Name}");
            Assert.NotNull(PxLibInfo.Name);
        }
    }
}
