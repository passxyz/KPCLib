using System;
using System.Diagnostics;

using Xunit;

using PassXYZLib;

namespace xunit.PassXYZLib
{
    public class UserFixture : IDisposable
    {
        public User user;

        public UserFixture()
        {
            user = new User
            {
                Username = "test1"
            };
            PxDb = new PxDatabase();
        }

        public void Dispose()
        {
            PxDb.Close();
        }
        public PxDatabase PxDb { get; private set; }
    }

    [CollectionDefinition("User collection")]
    public class UserCollection : ICollectionFixture<UserFixture>
    {
        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }

    [Collection("User collection")]
    public class UserTests
    {
        readonly UserFixture userFixture;

        public UserTests(UserFixture fixture)
        {
            this.userFixture = fixture;
        }

        [Fact]
        public void DataPathTest()
        {
            userFixture.user.Username = "test1";
            Debug.Print($"DataFilePath={PxDataFile.DataFilePath}");
            Debug.Print($"FileName={userFixture.user.FileName}, LastAccessTime: {userFixture.user.LastAccessTime}");
            Assert.NotNull(PxDataFile.DataFilePath);
            Assert.True(!userFixture.user.IsDeviceLockEnabled);
        }

        [Fact]
        public void KeyPathTest()
        {
            userFixture.user.Username = "test1";
            Debug.Print($"DataFilePath={PxDataFile.KeyFilePath}");
            Assert.NotNull(PxDataFile.KeyFilePath);
        }

        [Fact]
        public void GetUserNameTest()
        {
            User.GetUsersList();
        }

        [Fact]
        public void FileNameTest()
        {
            userFixture.user.Username = "kpclibpy";
            PxDataFile.DataFilePath = System.IO.Directory.GetCurrentDirectory();
            Debug.Print($"FileName={userFixture.user.FileName}");
            if(userFixture.user.IsKeyFileExist) 
            {
                Debug.WriteLine($"FileNameTest: Found key file {userFixture.user.KeyFileName}");
            }
            Assert.NotNull(userFixture.user.FileName);
        }

        /// <summary>
        /// Testing a KeePass database with key file.
        /// </summary>
        [Fact]
        public void KeePassKeyFileTest()
        {
            PxDataFile.DataFilePath = System.IO.Directory.GetCurrentDirectory();
            userFixture.user.Username = "user1";
            userFixture.user.Password = "123123";
            userFixture.PxDb.Open(userFixture.user);
            Debug.WriteLine($"Database: {userFixture.PxDb.Name}");
            Assert.True(userFixture.PxDb.IsOpen);
        }
    }
}
