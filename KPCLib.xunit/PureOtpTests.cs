using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using PureOtp;

namespace xunit.PureOtp
{
    public class PureOtpTests
    {
        [Theory]
        [InlineData("/kpclibpy/Database/Oracle")]
        [InlineData("http://www.google.com/test?secret=JBSWY3DPEHPK3PXP")]
        [InlineData("otpauth://totp/test01%3Abad_url_test%40gmail.com?secret=098")]
        [InlineData("otpauth://totp/Google%3Apxentry_test%40gmail.com")]
        [InlineData("otpauth://totp/Google%3Apxentry_test%40gmail.com?secret=JBSWY3DPEHPK3PXP")]
        [InlineData("otpauth://totp/Google%3Apxentry_test%40gmail.com?secret=JBSWY3DPEHPK3PXP&issuer=Google")]
        public void RawUrlTest(string rawUrl)
        {
            try 
            {
                var otp = KeyUrl.FromUrl(rawUrl);
                if (otp is Totp totp)
                {
                    var url = new Uri(rawUrl);
                    Assert.True(true);
                    Debug.WriteLine($"{rawUrl} is a valid URL.");
                }
                else
                {
                    Debug.WriteLine($"{rawUrl} is an invalid URL.");
                }
            }
            catch (Exception ex) 
            {
                Debug.WriteLine($"{ex}");
                Assert.False(false);
            }
        }
    }
}
