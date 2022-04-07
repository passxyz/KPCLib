using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Xunit;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Serialization;
using KeePassLib.Utility;
using PassXYZLib;

namespace PassXYZLib.xunit
{
    public class PxDatabaseFixture : IDisposable
    {
        const string TEST_DB = "utdb.kdbx";
        const string TEST_DB_KEY = "12345";

        public PxDatabaseFixture() 
        {
            PxDb = new PxDatabase();
            PxDb.Open(TEST_DB, TEST_DB_KEY);
        }

        public void Dispose() 
        {
            PxDb.Close();
        }

        public PxDatabase PxDb { get; private set; }
    }

    [CollectionDefinition("PxDatabase collection")]
    public class PxDatabaseCollection : ICollectionFixture<PxDatabaseFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("PxDatabase collection")]
    [TestCaseOrderer("xunit.Orderers.AlphabeticalOrderer", "xunit.PassXYZLib")]
    public class PxDatabaseTests
    {
        PxDatabaseFixture passxyz;

        public PxDatabaseTests(PxDatabaseFixture passXYZFixture) 
        {
            this.passxyz = passXYZFixture;
        }

        [Fact]
        public void OpenNonExistDBTest()
        {
            User user = new();
            user.Username = "nonexist";
            user.Password = "password";
            PxDatabase db = new();
            try { db.Open(user); }
            catch (System.IO.FileNotFoundException ex) 
            {
                Assert.IsType<System.IO.FileNotFoundException>(ex);
                Debug.WriteLine($"{ex}");
            }
            Assert.False(db.IsOpen);
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
            Assert.Contains("utdb", pg.Name);
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
        public void Z_DeleteEmptyEntryTest()
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
        //[InlineData(false)]
        /// <summary>
        /// Delete the first entry in the list.
        /// </summary>
        public void Z_DeleteEntryTests(bool permanent)
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
        public void Z_DeleteEmptyGroupTest()
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
        public void Z_DeleteGroupTests(bool permanent)
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
        [InlineData("/G1/G21")]
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
                if(path.StartsWith("/")) { Assert.Null(passxyz.PxDb.FindByPath<PwGroup>(path)); }
                else Debug.WriteLine($"{passxyz.PxDb.FindByPath<PwEntry>(path)}");
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
        //[InlineData("/utdb/General/G1/G21")]
        //[InlineData("../..")]
        //[InlineData("..")]
        /// <summary>
        /// Find group using an entry pass.
        /// </summary>
        public void FindGroupByPathTests(string path)
        {
            PwGroup group;

            if (path.StartsWith("/")) {
                group = passxyz.PxDb.FindByPath<PwGroup>(path);
                Assert.NotNull(group);
            }
            else if(path.StartsWith(".."))
            {
                passxyz.PxDb.CurrentGroup = passxyz.PxDb.FindByPath<PwGroup>("/utdb/General/G1/G21");
                Debug.WriteLine($"Current group is: {passxyz.PxDb.CurrentGroup}");
                group = passxyz.PxDb.FindByPath<PwGroup>(path);
            }
            else 
            {
                passxyz.PxDb.CurrentGroup = passxyz.PxDb.RootGroup;
                group = passxyz.PxDb.FindByPath<PwGroup>(path);
                Debug.WriteLine($"Cannot find group {path}");
                Assert.Null(group);
            }
        }

        [Fact]
        public void FindByPathDefaultTests() 
        {
            Assert.Null(passxyz.PxDb.FindByPath<PwEntry>());
            Assert.Null(passxyz.PxDb.FindByPath<PwGroup>());
        }

        [Fact]
        public void CurrentGroupTests()
        {
            Debug.WriteLine($"{passxyz.PxDb.CurrentGroup}");
            Assert.NotNull(passxyz.PxDb.CurrentGroup);
        }

        [Fact]
        public void CurrentPathTests()
        {
            Debug.WriteLine($"Current path is {passxyz.PxDb.CurrentPath}.");
            Assert.NotNull(passxyz.PxDb.CurrentPath);
        }

        [Theory]
        [InlineData("/utdb")]
        [InlineData("/utdb/General/G1")]
        /// <summary>
        /// IsParentGroup test cases
        /// source:      "/utdb/Windows/W1/W2/W3/W4/W5"
        /// destination: "/utdb/General/G1/G21"
        /// </summary>
        /// <param name="path">Source path. Must not be <c>null</c>.</param>	
        public void IsParentGroupG1Tests(string path)
        {
            var dstPath = "/utdb/General/G1";
            var dstGroup = passxyz.PxDb.FindByPath<PwGroup>(dstPath);
            var srcGroup = passxyz.PxDb.FindByPath<PwGroup>(path);
            if (passxyz.PxDb.IsParentGroup(srcGroup, dstGroup))
            {
                Debug.WriteLine($"{path} is the parent of {dstPath}.");
                Assert.Equal("/utdb", path);
            }
            else
            {
                Debug.WriteLine($"{path} is not the parent of {dstPath}.");
                Assert.Equal("/utdb/General/G1", path);
            }
        }

        [Theory]
        [InlineData("/utdb/Windows/W1")]
        [InlineData("/utdb/General")]
        /// <summary>
        /// IsParentGroup test cases
        /// source:      "/utdb/Windows/W1/W2/W3/W4/W5"
        /// destination: "/utdb/General/G1/G21"
        /// </summary>
        /// <param name="path">Source path. Must not be <c>null</c>.</param>	
        public void IsParentGroupG21Tests(string path)
        {
            var dstPath = "/utdb/General/G1/G21";
            var dstGroup = passxyz.PxDb.FindByPath<PwGroup>(dstPath);
            var srcGroup = passxyz.PxDb.FindByPath<PwGroup>(path);
            if(passxyz.PxDb.IsParentGroup(srcGroup, dstGroup))
            {
                Debug.WriteLine($"{path} is the parent of {dstPath}.");
                Assert.Equal("/utdb/General", path);
            }
            else 
            {
                Debug.WriteLine($"{path} is not the parent of {dstPath}.");
                Assert.Equal("/utdb/Windows/W1", path);
            }
        }

        [Theory]
        [InlineData("/utdb/General")]
        [InlineData("/utdb")]
        /// <summary>
        /// MoveEntry test cases
        /// Test case 1: srcEntry: "/utdb/General",   dstGroup: "/utdb/General"
        /// Test case 2: srcEntry: "/utdb/TestEntry", dstGroup: "/utdb"
        /// </summary>
        /// <param name="path">Destination path. Must not be <c>null</c>.</param>
        public void MoveEntryTests(string path)
        {
            string srcPath;

            if (path == "/utdb/General") 
            {
                srcPath = "/utdb/General";
            }
            else
            {
                srcPath = "/utdb/TestEntry";
            }

            var srcEntry = passxyz.PxDb.FindByPath<PwEntry>(srcPath);
            var dstGroup = passxyz.PxDb.FindByPath<PwGroup>(path);

            if(passxyz.PxDb.MoveEntry(srcEntry, dstGroup))
            {
                Debug.WriteLine($"Moved entry {srcPath} to {path}.");
                Assert.Equal("/utdb/General", path);
            }
            else
            {
                Debug.WriteLine($"Cannot move {srcPath} to {path}.");
                Assert.Equal("/utdb", path);
            }
        }

        [Theory]
        [InlineData("/utdb/Windows/W1/W2/W3/")]
        [InlineData("/utdb/General/G1/G21/")]
        [InlineData("/utdb/General/")]
        /// <summary>
        /// MoveGroup test cases
        /// Test case 1: srcGroup: "/utdb/Windows/W1/W2/W3/W4/W5/", dstGroup: "/utdb/Windows/W1/W2/W3/"
        ///              Move sub-group to the parent group, this is a successful case
        /// Test case 2: srcGroup: "/utdb/General/", dstGroup: "/utdb/General/G1/G21/"
        ///              Move parent group to the sub-group, this is a failure case
        /// Test case 2: srcGroup: "/utdb/General/", dstGroup: "/utdb/General/"
        ///              Move group to the same location, this is a failure case
        /// </summary>
        /// <param name="path">Destination path. Must not be <c>null</c>.</param>
        public void MoveGroupTests(string path)
        {
            string srcPath;

            if (path == "/utdb/General/")
            {
                srcPath = "/utdb/General/";
            }
            else if (path == "/utdb/General/G1/G21/")
            {
                srcPath = "/utdb/General/";
            }
            else
            {
                srcPath = "/utdb/Windows/W1/W2/W3/W4/W5/";
            }

            var srcGroup = passxyz.PxDb.FindByPath<PwGroup>(srcPath);
            var dstGroup = passxyz.PxDb.FindByPath<PwGroup>(path);

            if (passxyz.PxDb.MoveGroup(srcGroup, dstGroup))
            {
                Debug.WriteLine($"Moved entry {srcPath} to {path}.");
                Assert.Equal("/utdb/Windows/W1/W2/W3/", path);
            }
            else
            {
                Debug.WriteLine($"Cannot move {srcPath} to {path}.");
            }
        }

        [Theory]
        [InlineData("kpclib")]
        public void A_EntrySearchTests(string keyword)
        {
            var entries = passxyz.PxDb.SearchEntries(keyword);
            Assert.True(entries.Any());
        }
        
        [Fact]
        public void GetAllEntriesByLastModificationTimeTest() 
        {
            var entries = passxyz.PxDb.GetAllEntries();
            // descending or ascending
            IEnumerable<PwEntry> entriesByDate =
                from e in entries
                orderby e.LastModificationTime descending
                select e;

            Debug.WriteLine($"Found {entriesByDate.Count()} entries.");
            foreach (var entry in entriesByDate) 
            {
                Debug.WriteLine($"{entry.Name} {entry.LastModificationTime}");
            }
        }
        // Add new test cases here
    }


    public class PxLibInfoTests 
    { 
        [Fact]
        public void PxLibVersion() 
        {
            Debug.WriteLine($"{PxLibInfo.Version}");
            Assert.Equal(PxLibInfo.Version, new System.Version("2.0.1.0"));
        }

        [Fact]
        public void PxLibName()
        {
            Debug.WriteLine($"{PxLibInfo.Name}");
            Assert.NotNull(PxLibInfo.Name);
        }
    }
}
