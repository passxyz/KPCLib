using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using PureOtp;

using KeePassLib;

namespace PassXYZLib
{
    public class PxEntry : PwEntry
    {
        public PxEntry() : base()
        { }

        #region PxEntrySupportOTP
        // Update progress every 3 seconds
        public const int TimerStep = 3;

        string m_TotpDescription = string.Empty;
        public string TotpDescription {
            get { return m_TotpDescription; }
            set { m_TotpDescription = value; }
        }

        Totp m_totp = null;
        public Totp Totp {
            get 
            {
                if(m_totp == null) { SetupTotp(); }
                return m_totp; 
            }
            
            set { m_totp = value; }
        }

        void SetupTotp()
        {
            if (this.CustomData != null && this.CustomData.Exists(PxDefs.PxCustomDataOtpUrl))
            {
                var rawUrl = this.CustomData.Get(PxDefs.PxCustomDataOtpUrl);
                var otp = KeyUrl.FromUrl(rawUrl);
                if (otp is Totp totp)
                {
                    var url = new Uri(rawUrl);
                    m_TotpDescription = url.LocalPath.Substring(1);

                    Totp = totp;
                    Token = totp.ComputeTotp();
                }
            }
        }

        double m_increment = 0.1;

        double m_progress = 0;
        public double Progress
        {
            get { return m_progress; }

            set
            {
                SetProperty(ref m_progress, value, "Progress");
            }
        }

        string m_token = string.Empty;
        public string Token
        {
            get 
            { 
                if(m_token == string.Empty) { SetupTotp(); }
                return m_token;
            }

            set
            {
                SetProperty(ref m_token, value, "Token", () => {
                    var remaining = Totp.RemainingSeconds();
                    Progress = 0;
                    if (remaining > TimerStep)
                    {
                        m_increment = (double)TimerStep / remaining;
                    }

                    Debug.WriteLine($"Token is changed! RemainingSeconds={remaining}, increment={m_increment}");
                });
            }
        }

        public void UpdateToken()
        {
            if(Totp != null) 
            {
                Token = Totp.ComputeTotp();
                if (Progress <= 1)
                {
                    Progress += m_increment;
                }
            }
        }

        /// <summary>
        /// Update the OTP Url.
        /// If it is new, create a PxOtpUrl key in CustomData.
        /// Since CustomData is readonly, it can only be set at this level.
        /// </summary>
        /// <param name="url">OTP Url which cannot be <c>null</c>.</param>
        /// <returns>true - success, false - failure</returns>
        public void UpdateOtpUrl(string url)
        {
            if (url == null) { Debug.Assert(false); throw new ArgumentNullException("url"); }

            this.CustomData.Set(PxDefs.PxCustomDataOtpUrl, url);
        }

        #endregion
    }
}
