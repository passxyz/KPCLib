using System.Diagnostics;
using System.Text;

using PureOtp;

using KeePassLib;
using KeePassLib.Security;
using KeePassLib.Utility;
using Image = SkiaSharp.SKBitmap;

using PassXYZLib.Resources;
// using PassXYZ.Vault.Resx;

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
        public PxEntry(string str, string? password = null, bool isJson = true) : base(true, true)
        {
            if (isJson)
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
                    else if (fields.IsPxEntry)
                    {
                        CustomData.Set(PxDefs.PxCustomDataItemSubType, ItemSubType.PxEntry.ToString());
                    }
                }
            }
            else 
            {
                // If the first parameter is not a JSON string, we just set the name and description.
                Name = str;
                Notes = password;
            }
        }

		/// <summary>
		/// Create a PxEntry instance from a PwEntry.
		/// </summary>
		/// <param name="entry">a PwEntry instance</param>
		public PxEntry(PwEntry entry)
		{
			Uuid = entry.Uuid;
			AssignProperties(entry, false, true, true);
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

        Totp? m_totp = null;
        public Totp? Totp
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
                SetProperty(ref m_progress, value, nameof(Progress));
            }
        }

        string? m_token = string.Empty;
        public string? Token
        {
            get
            {
                if (m_token == string.Empty) { SetupTotp(); }
                UpdateToken();
                return m_token;
            }

            set
            {
                SetProperty(ref m_token, value, nameof(Token), () => {
                    var remaining = (Totp == null) ? 0 : Totp.RemainingSeconds();
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

    public static class FieldIcons
    {
        public static Dictionary<string, string> RegularIcons = new Dictionary<string, string>()
        {
            { "calendar", FontAwesomeRegular.CalendarAlt }
        };

        public static Dictionary<string, string> SolidIcons = new Dictionary<string, string>()
        {
            { "address", FontAwesomeSolid.MapMarkerAlt },
            { "地址", FontAwesomeSolid.MapMarkerAlt },
            { "地点", FontAwesomeSolid.MapMarkerAlt },
            { "card", FontAwesomeSolid.IdCard },
            { "账号", FontAwesomeSolid.IdCard },
            { "身份证号", FontAwesomeSolid.IdCard },
            { "date", FontAwesomeSolid.CalendarAlt },
            { "日期", FontAwesomeSolid.CalendarAlt },
            { "签发日期", FontAwesomeSolid.CalendarAlt },
            { "有效期至", FontAwesomeSolid.CalendarAlt },
            { "email", FontAwesomeSolid.Envelope },
            { "邮件地址", FontAwesomeSolid.Envelope },
            { "邮件", FontAwesomeSolid.Envelope },
            { "mobile", FontAwesomeSolid.Phone },
            { "手机号码", FontAwesomeSolid.Phone },
            { "name", FontAwesomeSolid.User },
            { "姓名", FontAwesomeSolid.User },
            { "password", FontAwesomeSolid.Key },
            { "密码", FontAwesomeSolid.Key },
            { "支付密码", FontAwesomeSolid.Key },
            { "交易密码", FontAwesomeSolid.Key },
            { "网银密码", FontAwesomeSolid.Key },
            { "取款密码", FontAwesomeSolid.Key },
            { "U盾密码", FontAwesomeSolid.Key },
            { "phone", FontAwesomeSolid.Phone },
            { "pin", FontAwesomeSolid.Key },
            { "url", FontAwesomeSolid.Link },
            { "链接地址", FontAwesomeSolid.Link },
            { "username", FontAwesomeSolid.User }
        };

        public static Dictionary<string, string> BrandIcons = new Dictionary<string, string>()
        {
            { "alipay", FontAwesomeBrands.Alipay },
            { "qq", FontAwesomeBrands.Qq },
            { "wechat", FontAwesomeBrands.Weixin }
        };

        public static ImageSource GetImage(string key) 
        { 
            if (BrandIcons.ContainsKey(key))
            {
                var brandIconSource = new FontImageSource
                {
                    FontFamily = "FontAwesomeBrands",
                    Glyph = BrandIcons[key]
                };
                return brandIconSource;
            }
            else if (RegularIcons.ContainsKey(key)) 
            {
                var regularIconSource = new FontImageSource
                {
                    FontFamily = "FontAwesomeRegular",
                    Glyph = RegularIcons[key]
                };
                return regularIconSource;
            }
            else 
            {
                var solidIconSource = new FontImageSource
                {
                    FontFamily = "FontAwesomeSolid",
                    Glyph = FontAwesomeSolid.File
                };

                if (SolidIcons.ContainsKey(key))
                {
                    solidIconSource.Glyph = SolidIcons[key];
                }

                return solidIconSource;
            }
        }
    }

    public enum BinaryDataClass
    {
        Unknown = 0,
        Text,
        RichText,
        Excel,
        Image,
        PDF,
        WebDocument
    }

    public static class BinaryDataClassifier
    {
        private static readonly string[] m_vTextExtensions = new string[] {
            "txt", "csv", "c", "cpp", "h", "hpp", "css", "js", "bat"
        };

        private static readonly string[] m_vRichTextExtensions = new string[] {
            "rtf", "doc", "docx"
        };

        private static readonly string[] m_vExcelExtensions = new string[] {
            "xls", "xlsx"
        };

        private static readonly string[] m_vPdfExtensions = new string[] {
            "pdf"
        };

        private static readonly string[] m_vImageExtensions = new string[] {
            "bmp", "emf", "exif", "gif", "ico", "jpeg", "jpe", "jpg",
            "png", "tiff", "tif", "wmf"
        };

        private static readonly string[] m_vWebExtensions = new string[] {
            "htm", "html"
        };

        public static BinaryDataClass ClassifyUrl(string strUrl)
        {
            Debug.Assert(strUrl != null);
            if (strUrl == null) throw new ArgumentNullException("strUrl");

            string str = strUrl.Trim().ToLower();

            foreach (string strPdfExt in m_vPdfExtensions)
            {
                if (str.EndsWith("." + strPdfExt))
                    return BinaryDataClass.PDF;
            }

            foreach (string strTextExt in m_vTextExtensions)
            {
                if (str.EndsWith("." + strTextExt))
                    return BinaryDataClass.Text;
            }

            foreach (string strRichTextExt in m_vRichTextExtensions)
            {
                if (str.EndsWith("." + strRichTextExt))
                    return BinaryDataClass.RichText;
            }

            foreach (string strImageExt in m_vImageExtensions)
            {
                if (str.EndsWith("." + strImageExt))
                    return BinaryDataClass.Image;
            }

            foreach (string strWebExt in m_vWebExtensions)
            {
                if (str.EndsWith("." + strWebExt))
                    return BinaryDataClass.WebDocument;
            }

            foreach (string strExcelExt in m_vExcelExtensions)
            {
                if (str.EndsWith("." + strExcelExt))
                    return BinaryDataClass.Excel;
            }

            return BinaryDataClass.Unknown;
        }

        public static BinaryDataClass ClassifyData(byte[] pbData)
        {
            Debug.Assert(pbData != null);
            if (pbData == null) throw new ArgumentNullException("pbData");

            try
            {
                Image img = GfxUtil.LoadImage(pbData);
                if (img != null)
                {
                    img.Dispose();
                    return BinaryDataClass.Image;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"BinaryDataClass: Exception={e.ToString()}");
            }

            return BinaryDataClass.Unknown;
        }

        public static BinaryDataClass Classify(string strUrl, byte[] pbData)
        {
            BinaryDataClass bdc = ClassifyUrl(strUrl);
            if (bdc != BinaryDataClass.Unknown) return bdc;

            // We don't have classify data by content.
            // return ClassifyData(pbData);
            return BinaryDataClass.Unknown;
        }

        public static StrEncodingInfo GetStringEncoding(byte[] pbData,
            out uint uStartOffset)
        {
            Debug.Assert(pbData != null);
            if (pbData == null) throw new ArgumentNullException("pbData");

            uStartOffset = 0;

            List<StrEncodingInfo> lEncs = new List<StrEncodingInfo>(StrUtil.Encodings);
            lEncs.Sort(BinaryDataClassifier.CompareBySigLengthRev);

            foreach (StrEncodingInfo sei in lEncs)
            {
                byte[] pbSig = sei.StartSignature;
                if ((pbSig == null) || (pbSig.Length == 0)) continue;
                if (pbSig.Length > pbData.Length) continue;

                byte[] pbStart = MemUtil.Mid<byte>(pbData, 0, pbSig.Length);
                if (MemUtil.ArraysEqual(pbStart, pbSig))
                {
                    uStartOffset = (uint)pbSig.Length;
                    return sei;
                }
            }

            if ((pbData.Length % 4) == 0)
            {
                byte[] z3 = new byte[] { 0, 0, 0 };
                int i = MemUtil.IndexOf<byte>(pbData, z3);
                if ((i >= 0) && (i < (pbData.Length - 4))) // Ignore last zero char
                {
                    if ((i % 4) == 0) return StrUtil.GetEncoding(StrEncodingType.Utf32BE);
                    if ((i % 4) == 1) return StrUtil.GetEncoding(StrEncodingType.Utf32LE);
                    // Don't assume UTF-32 for other offsets
                }
            }

            if ((pbData.Length % 2) == 0)
            {
                int i = Array.IndexOf<byte>(pbData, 0);
                if ((i >= 0) && (i < (pbData.Length - 2))) // Ignore last zero char
                {
                    if ((i % 2) == 0) return StrUtil.GetEncoding(StrEncodingType.Utf16BE);
                    return StrUtil.GetEncoding(StrEncodingType.Utf16LE);
                }
            }

            try
            {
                UTF8Encoding utf8Throw = new UTF8Encoding(false, true);
                utf8Throw.GetString(pbData);
                return StrUtil.GetEncoding(StrEncodingType.Utf8);
            }
            catch (Exception) { }

            return StrUtil.GetEncoding(StrEncodingType.Default);
        }

        private static int CompareBySigLengthRev(StrEncodingInfo a, StrEncodingInfo b)
        {
            Debug.Assert((a != null) && (b != null));

            int na = 0, nb = 0;
            if ((a != null) && (a.StartSignature != null))
                na = a.StartSignature.Length;
            if ((b != null) && (b.StartSignature != null))
                nb = b.StartSignature.Length;

            return -(na.CompareTo(nb));
        }
    }
}
