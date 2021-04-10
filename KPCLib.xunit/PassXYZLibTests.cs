using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Xunit;
using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;
using KeePassLib.Utility;
using PassXYZLib;

namespace KPCLib.xunit
{
    public class PassXYZLibFixture : IDisposable
    {
        const string TEST_DB = "pass_d_E8f4pEk.xyz";
        const string TEST_DB_KEY = "12345";

        public PassXYZLibFixture()
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

        public string Username { 
            get { return PxDefs.GetUserNameFromDataFile(TEST_DB); }
        }
    }

    [CollectionDefinition("PassXYZLib collection")]
    public class PassXYZLibCollection : ICollectionFixture<PassXYZLibFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("PassXYZLib collection")]
    public class PassXYZLibTests 
    {
        PassXYZLibFixture passxyz;

        public PassXYZLibTests(PassXYZLibFixture passXYZLibFixture)
        {
            this.passxyz = passXYZLibFixture;
        }

        [Fact]
        public void IsOpenDbTest()
        {
            Debug.WriteLine($"{passxyz.PxDb}, Username is {passxyz.Username}.");
            Assert.True((passxyz.PxDb.IsOpen));
        }

        [Fact]
        public void CurrentGroupTests()
        {
            var currentGroup = passxyz.PxDb.CurrentGroup;
            Debug.WriteLine($"{currentGroup}");
            Assert.NotNull(currentGroup);
        }

        [Fact]
        public void CurrentPathTests()
        {
            var currentPath = passxyz.PxDb.CurrentPath;
            Debug.WriteLine($"Current path is {currentPath}.");
            Assert.NotNull(currentPath);
        }

        [Theory]
        [InlineData("/test1/WebDAV")]
        /// <summary>
        /// Change a protect field
        /// </summary>
        /// <param name="path">Destination path. Must not be <c>null</c>.</param>
        public void ChangeProtectedFieldTests(string path) 
        {
            var entry = passxyz.PxDb.FindByPath<PwEntry>(path);

            Debug.WriteLine($"Current path is {entry}.");
            // Update the existing protected field
            PxDefs.UpdatePxEntry(entry, PxDefs.PasswordField, "123456", true);
            Assert.True(entry.Strings.Get(PxDefs.FindEncodeKey(entry.Strings, PxDefs.PasswordField)).IsProtected);
            // Add a new protected field "PIN"
            PxDefs.UpdatePxEntry(entry, "PIN", "1234", true);
            Assert.True(entry.Strings.Get(PxDefs.FindEncodeKey(entry.Strings, "PIN")).IsProtected);
            // Remove a field
            PxDefs.UpdatePxEntry(entry, "002Email", String.Empty, false);
            foreach (KeyValuePair<string, ProtectedString> kvp in entry.Strings)
            {
                Debug.WriteLine($"    {kvp.Key}={kvp.Value.ReadString()}, IsProtected={kvp.Value.IsProtected}");
            }

        }

        // The end of PassXYZLibTests
    }

}