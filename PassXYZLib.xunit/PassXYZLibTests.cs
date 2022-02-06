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

namespace PassXYZLib.xunit
{
    public class PassXYZLibFixture : IDisposable
    {
        const string TEST_DB = "pass_d_E8f4pEk.xyz";
        const string TEST_DB_KEY = "12345";

        public PassXYZLibFixture()
        {
            PxDb = new PxDatabase();
            PxDb.Open(TEST_DB, TEST_DB_KEY);
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
        public void ListItemsTests()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;
            foreach (var item in pg.Items)
            {
                if(item.IsGroup)
                {
                    Debug.WriteLine($"{item.Description}\t{item.Name}/");
                }
                else 
                {
                    Debug.WriteLine($"{item.Description}\t{item.Name}");
                }
            }
        }

        [Fact (Skip = "test manually")]
        /// <summary>
        /// This test computes a token using a secret.
        /// The result can be verified at the below website:
        /// https://totp.danhersam.com/
        /// </summary>
        public void TokenGenerateTests()
        {
            var entry = new PxEntry();
            if (entry.Totp == null)
            {
                entry.UpdateOtpUrl("otpauth://totp/Google%3Apxentry_test%40gmail.com?secret=JBSWY3DPEHPK3PXP&issuer=Google");
                var Totp = entry.Totp;
                Debug.WriteLine($"Token={entry.Token}\tProgress={entry.Progress}");
                Assert.NotNull(Totp);
            }

            int i = 0;
            for (i = 0; i < 30; i++)
            {
                Debug.WriteLine($"Token={entry.Token}\tProgress={entry.Progress}");
                System.Threading.Thread.Sleep(3000);
            }
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