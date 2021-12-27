using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using PassXYZLib;

namespace xunit.PassXYZLib
{
    public class PxEncryptionTest
    {
        [Theory]
        [InlineData("Hello World 11", "12345678901")]
        [InlineData("Hello World 12", "123456789012")]
        [InlineData("Hello World 13", "1234567890123")]
        [InlineData("Hello World 14", "12345678901234")]
        [InlineData("Hello World 15", "123456789012345")]
        public void EncryptStringTest(string secretMessage, string password)
        {
            string encryptedMessage = PxEncryption.EncryptWithPassword(secretMessage, password);
            string decryptedMessage = PxEncryption.DecryptWithPassword(encryptedMessage, password);
            Debug.WriteLine($"EncryptStringTest: PasswdLen={password.Length}, TextLen={encryptedMessage.Length}, {decryptedMessage}");
            Assert.Equal(secretMessage, decryptedMessage);
        }
    }
}
