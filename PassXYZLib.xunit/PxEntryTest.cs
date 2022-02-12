using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using KPCLib;
using KeePassLib;
using PassXYZLib;

namespace PassXYZLib.xunit
{
    [Collection("PxDatabase collection")]
    public class PxEntryTest
    {
        PxDatabaseFixture passxyz;

        public PxEntryTest(PxDatabaseFixture passXYZFixture)
        {
            this.passxyz = passXYZFixture;
        }

        [Fact]
        public void PxEntryConstructorTest()
        {
            PxEntry entry = new("Name1", "Description1", false);
            Item item = entry;
            Guid guid1 = item.GetUuid();
            Guid guid2 = new(guid1.ToString());
            int result = guid1.CompareTo(guid2);
            Assert.Equal(0, result);
            Debug.WriteLine($"Id={item.Id}, Name={item.Name}, Desc={item.Description}, Time={item.LastModificationTime}");
        }

        [Theory]
        [InlineData("pxtem://{'IsPxEntry':true,'Strings':{'000UserName':{'Value':'','IsProtected':false},'001Password':{'Value':'','IsProtected':true},'002Email':{'Value':'','IsProtected':false},'003URL':{'Value':'','IsProtected':false},'004QQ':{'Value':'','IsProtected':false},'005WeChat':{'Value':'','IsProtected':false},'Notes':{'Value':'','IsProtected':false},'Title':{'Value':'PxEntry 1','IsProtected':false}}}")]
        [InlineData("pxtem://{'IsPxEntry':true,'Strings':{'000UserName':{'Value':'user2','IsProtected':false},'001Password':{'Value':'12345','IsProtected':true},'002Email':{'Value':'user2@passxyz.com','IsProtected':false},'003URL':{'Value':'https://passxyz.github.io','IsProtected':false},'004QQ':{'Value':'1234567890','IsProtected':false},'005WeChat':{'Value':'passxyz','IsProtected':false},'Notes':{'Value':'This is a PxEntry.','IsProtected':false},'Title':{'Value':'PxEntry 2','IsProtected':false}}}")]
        [InlineData("pxtem://{'IsPxEntry':true,'Strings':{'000UserName':{'Value':'','IsProtected':false},'002Email':{'Value':'','IsProtected':false},'003URL':{'Value':'','IsProtected':false},'004QQ':{'Value':'','IsProtected':false},'005WeChat':{'Value':'','IsProtected':false},'Notes':{'Value':'','IsProtected':false},'Title':{'Value':'PxEntry 3','IsProtected':false}}}")]
        public void CreatePxEntryFromJsonTest(string str)
        {
            PxEntry pxEntry = new PxEntry(str);
            Debug.WriteLine($"{pxEntry}");
            Assert.Contains("PxEntry", pxEntry.Description);
        }

        [Theory]
        [InlineData("pxtem://{'IsPxEntry':false,'Strings':{'Password':{'Value':'','IsProtected':true},'Mobile':{'Value':'','IsProtected':false},'Notes':{'Value':'','IsProtected':false},'PIN':{'Value':'','IsProtected':true},'Title':{'Value':'KeePass Entry 1','IsProtected':false},'URL':{'Value':'','IsProtected':false},'UserName':{'Value':'test1','IsProtected':false}}}")]
        [InlineData("pxtem://{'IsPxEntry':false,'Strings':{'Password':{'Value':'12345','IsProtected':true},'Mobile':{'Value':'13678909876','IsProtected':false},'Notes':{'Value':'','IsProtected':false},'PIN':{'Value':'123','IsProtected':true},'Title':{'Value':'KeePass Entry 2','IsProtected':false},'URL':{'Value':'https://github.com','IsProtected':false},'UserName':{'Value':'test2','IsProtected':false}}}")]
        public void CreatePwEntryFromJsonTest(string str)
        {
            PxEntry pxEntry = new PxEntry(str);
            Debug.WriteLine($"{pxEntry}");
            Assert.Contains("PxEntry", pxEntry.Description);
        }

        [Theory]
        [InlineData("12345", @"pxdat://6Dw+zRK+X0dQ5XAv71HBxs0sBZnFFt63KJGAqJTrNMBfqGU9AuWRjuCUhogft5MI4xCsGoo4S/XT75Gg4tPZ1gGGBxX6GxGpYj50X4O131kZ6vh146VPdqCP4+QRysPSg56wEaTLcp4L6fDXXfXQ7Wsv849p7luFQ6cFOFnYPpahvzhzOJbw8Z1bdcU5jrTRHVeWiMpcd33JfnFVsNr2AkkP3UW7KVZV3OOERA4CntBxVbXSpB9jJbu4q/3j6roKPO5e5TIzKPc/dtCp2Kg0xGu/la+o1MMA/N34XgqnyLLWA0xcy5PHDwDs4frnxiqOR7nqamNm6wjQXJ4JvD159mqmHDWfGB9uJosJYJBDyuCWkqMfUBJTZcG95+P9PwTrQ7E3rZwMJGyu2r0WWzgzvDq/aenZZgXkE1AzU6NN/FhrPQKoD1umMwQSLrwu5mKKq3XEQRiOWcnPkeQ1DaRZiDWq6tBjGU+h1wccsa3vxa74+2K0yxVAcumPpRbzREhd40Zay9EutYkwhwMjlrlmcVSvgWMJ8+y8pjjHYYWi1a/96w+B6ODQ/RyzUjmyQaXoVgYGBkneXxHfu8S4TRalBQ==")]
        public void CreatePwEntryFromEncrytedJsonTest(string password, string str)
        {
            PxEntry pxEntry = new PxEntry(str, password);
            Assert.False(pxEntry.IsPxEntry());
            Debug.WriteLine($"{pxEntry}");
        }

        [Theory]
        [InlineData("12345", @"pxdat://Did3addyfdEtHv10kR1Iu7tfdgaIekB0+KQ+u3aIaKazjMStxkMSt74gg0n89ytFaM9F1JfQBigY6lQgqoJuYZpZC7tpavKHdhv4XWafe3ofqsWy2HnwwsHgkfMhre1Z1QDGBjyujef9vBWWsHP6NYmAzWecJXyoq2NqOTotIefRyjfMoRWo/mHKzJfh7j4ZTkA7Sj7C/lxWsLeoC+R2IThp8MtCCX3FtT9xU0Hen+eRcxG8P2MimoETFde6ddi/Vi/Eof+z1WML86z4xui5a/mkbnq6jYIXYroo0Np60WuFKQ3rj4P4b6I6YBGsSc2Yj7SNdjWaDFL17aOwTzzCbYZC6SyMymzwHUp9Ww3lgFnsFa8tBrjrLTIDZP5j+uu6T0CkRHEqtT70Seee3z7uR91THjhrU6LAye62gfCS4peHwI88KuYUgzukQ3NJNjXRtAUZTc9hricFeOjiiHjENUbokAYd8nLXKnFKH5OEw2DOiUgB5d+ot/oL0RUwDSL9QyfehOo1VC+tW9fUKf+ySDpOf4EsA//ST/lfj9kt7O50lreclTbzrWHkYI06eWAFaLwLFWVUtwWK/eSuNVwVESfbanmjpngNELdssUDJ58hBLN0owPkU70AsA6vgEoiTh6wQM1JC4JUAO8Jb+8AZ0KpsqIcnw9umH0JHhp8Q7PfUFY3daoXc1dKi3hUAIYvUkxS31Tp24tcvxcxx7Se3og==")]
        public void CreatePxEntryFromEncrytedJsonTest(string password, string str)
        {
            PxEntry pxEntry = new PxEntry(str, password);
            Assert.True(pxEntry.IsPxEntry());
            Debug.WriteLine($"{pxEntry}");
        }

        [Theory]
        [InlineData("http://passxyz.com")]
        [InlineData("pxdat://passxyz.com")]
        [InlineData("pxtem://passxyz.com")]
        public void CreatePwEntryFailureTest(string str)
        {
            PxEntry pxEntry = new PxEntry(str);
            Assert.Empty(pxEntry.Name);
            Debug.WriteLine($"{pxEntry}");
        }

        [Fact]
        public void JsonSerializerTests()
        {
            PwGroup pg = passxyz.PxDb.RootGroup;
            int count = 0;
            foreach (var entry in pg.Entries)
            {
                count++;
                PxEntry pxEntry1 = new PxEntry(entry);
                PxEntry pxEntry2 = new PxEntry(pxEntry1.ToString(), entry.Strings.ReadSafe("Password"));
                Assert.Equal(pxEntry1.Name, pxEntry2.Name);
                Debug.WriteLine($"{pxEntry1.Name}. {pxEntry2.Name}");
            }
        }
    }
}
