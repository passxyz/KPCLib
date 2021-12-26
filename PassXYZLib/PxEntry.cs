using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using PureOtp;

using KeePassLib;
using KeePassLib.Security;

namespace PassXYZLib
{
    public class PxEntry : PwEntry
    {
        public PxEntry(bool bCreateNewUuid, bool bSetTimes) : base(bCreateNewUuid, bSetTimes) { }

        public PxEntry() : base() { }

        /// <summary>
        /// Create a PxEntry instance from a JSON string.
        /// </summary>
        /// <param name="str">JSON data</param>
        /// <param name="password">Password of PwEntry</param>
        public PxEntry(string str, string password = null) : base(true, true)
        {
            PxPlainFields fields = new PxPlainFields(str, password);

            if (fields.Strings.Count > 0)
            {
                foreach (var itemInDict in fields.Strings)
                {
                    PxFieldValue data = itemInDict.Value;
                    Strings.Set(itemInDict.Key, new ProtectedString(data.IsProtected, data.Value));
                }

                if (!string.IsNullOrEmpty(fields.CustomDataType))
                {
                    CustomData.Set(PxDefs.PxCustomDataItemSubType, fields.CustomDataType);
                }
            }
        }

        /// <summary>
        /// Convert PxEntry instance to a JSON string.
        /// </summary>
        public override string ToString()
        {
            var fields = new PxPlainFields(this);
            return fields.ToString();
        }

		#region PxEntrySupportOTP
		// Update progress every 3 seconds
		public const int TimerStep = 3;

		string m_TotpDescription = string.Empty;
		public string TotpDescription
		{
			get { return m_TotpDescription; }
			set { m_TotpDescription = value; }
		}

		Totp m_totp = null;
		public Totp Totp
		{
			get
			{
				if (m_totp == null) { SetupTotp(); }
				return m_totp;
			}

			set { m_totp = value; }
		}

		void SetupTotp()
		{
			if (this.CustomData != null && this.CustomData.Exists(PassXYZLib.PxDefs.PxCustomDataOtpUrl))
			{
				var rawUrl = this.CustomData.Get(PassXYZLib.PxDefs.PxCustomDataOtpUrl);
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
			get
			{
				if (m_totp == null) { SetupTotp(); }
				return m_progress;
			}

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
				if (m_token == string.Empty) { SetupTotp(); }
				UpdateToken();
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

					Debug.WriteLine($"New Token={Token} RemainingSeconds={remaining}, increment={m_increment}");
				});
			}
		}

		public void UpdateToken()
		{
			if (Totp != null)
			{
				Token = Totp.ComputeTotp();
				if (Progress <= 1)
				{
					Progress += m_increment;
				}
			}
		}

		public string GetOtpUrl()
		{
			return CustomData.Get(PassXYZLib.PxDefs.PxCustomDataOtpUrl);
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

			this.CustomData.Set(PassXYZLib.PxDefs.PxCustomDataOtpUrl, url);
		}

		#endregion

	}
}
