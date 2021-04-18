using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using PureOtp;
using PassXYZLib;

namespace KPCLib.xunit
{
    public class PxEntryTests
    {
        [Fact]
        public void PxEntryOtpInitTests() 
        {
            const string secret = "MyTestSecret";
            var entry = new PxEntry();
            if(entry.Totp == null) 
            {
                entry.Totp = new PureOtp.Totp(Encoding.UTF8.GetBytes(secret));
            }
            if (entry.Token == String.Empty) { entry.Token = entry.Totp.ComputeTotp(); }
            var isGood = entry.Totp.VerifyTotp(entry.Token, out var timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay);
            Debug.WriteLine($"Token={entry.Token}");
            Assert.True(isGood);
        }

        [Fact]
        /// <summary>
        /// This test computes a token using a secret.
        /// The result can be verified at the below website:
        /// https://totp.danhersam.com/
        /// </summary>
        public void PxEntryTokenGenerateTests() 
        {
            var entry = new PxEntry();
            if (entry.Totp == null)
            {
                entry.UpdateOtpUrl("otpauth://totp/Google%3Apxentry_test%40gmail.com?secret=JBSWY3DPEHPK3PXP&issuer=Google");
                var Totp = entry.Totp;
                Debug.WriteLine($"Token={entry.Token}");
                Assert.NotNull(Totp);
            }
        }
    }
}
