using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using PureOtp;

using KeePassLib;
using KeePassLib.Interfaces;

namespace PassXYZLib
{
    public class PxEntry : PwEntry, IDeepCloneable<PxEntry>
    {
        public PxEntry() : base()
        { }

        /// <summary>
        /// Construct a new, empty password entry. Member variables will be initialized
        /// to their default values.
        /// </summary>
        /// <param name="bCreateNewUuid">If <c>true</c>, a new UUID will be created
        /// for this entry. If <c>false</c>, the UUID is zero and you must set it
        /// manually later.</param>
        /// <param name="bSetTimes">If <c>true</c>, the creation, last modification
        /// and last access times will be set to the current system time.</param>
        public PxEntry(bool bCreateNewUuid, bool bSetTimes) : base(bCreateNewUuid, bSetTimes) { }

        public PxEntry(PwEntry entry) 
        {
            Uuid = entry.Uuid; // PwUuid is immutable
            ParentGroup = entry.ParentGroup;
            LocationChanged = entry.LocationChanged;

            Strings = entry.Strings.CloneDeep();
            Binaries = entry.Binaries.CloneDeep();
            AutoType = entry.AutoType.CloneDeep();
            History = entry.History.CloneDeep();

            IconId = entry.IconId;
            CustomIconUuid = entry.CustomIconUuid;

            ForegroundColor = entry.ForegroundColor;
            BackgroundColor = entry.BackgroundColor;

            CreationTime = entry.CreationTime;
            LastModificationTime = entry.LastModificationTime;
            LastAccessTime = entry.LastAccessTime;
            ExpiryTime = entry.ExpiryTime;
            Expires = entry.Expires;
            UsageCount = entry.UsageCount;

            OverrideUrl = entry.OverrideUrl;

            Tags = new List<string>(entry.Tags);

            CustomData = entry.CustomData.CloneDeep();
        }

#if DEBUG
        // For display in debugger
        public override string ToString()
        {
            return ("PxEntry '" + Strings.ReadSafe(PwDefs.TitleField) + "'");
        }
#endif

        /// <summary>
        /// Clone the current entry. The returned entry is an exact value copy
        /// of the current entry (including UUID and parent group reference).
        /// All mutable members are cloned.
        /// </summary>
        /// <returns>Exact value clone. All references to mutable values changed.</returns>
        public new PxEntry CloneDeep()
        {
            PxEntry peNew = new PxEntry(false, false);

            peNew.Uuid = Uuid; // PwUuid is immutable
            peNew.ParentGroup = ParentGroup;
            peNew.LocationChanged = LocationChanged;

            peNew.Strings = Strings.CloneDeep();
            peNew.Binaries = Binaries.CloneDeep();
            peNew.AutoType = AutoType.CloneDeep();
            peNew.History = History.CloneDeep();

            peNew.IconId = IconId;
            peNew.CustomIconUuid = CustomIconUuid;

            peNew.ForegroundColor = ForegroundColor;
            peNew.BackgroundColor = BackgroundColor;

            peNew.CreationTime = CreationTime;
            peNew.LastModificationTime = LastModificationTime;
            peNew.LastAccessTime = LastAccessTime;
            peNew.ExpiryTime = ExpiryTime;
            peNew.Expires = Expires;
            peNew.UsageCount = UsageCount;

            peNew.OverrideUrl = OverrideUrl;

            peNew.Tags = new List<string>(Tags);

            peNew.CustomData = CustomData.CloneDeep();

            return peNew;
        }

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
