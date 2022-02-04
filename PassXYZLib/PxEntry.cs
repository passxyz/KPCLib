using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

using Newtonsoft.Json;

using Markdig;

using KeePassLib;
using KeePassLib.Collections;
using KeePassLib.Interfaces;
using KeePassLib.Security;
using KeePassLib.Utility;
using Image = SkiaSharp.SKBitmap;

using PassXYZLib.Resources;
// using PassXYZ.Vault.Resx;

namespace PassXYZLib
{
    /// <summary>
    /// A class representing a field in PwEntry. A field is stored in ProtectedStringDictionary.
    /// We convert key value pair into a field so that it can be used by the user interface.
    /// </summary>
    public class Field : INotifyPropertyChanged
    {
        private string _key;
        /// <summary>
        /// This is the key used by Field. This Key should be decoded for PxEntry.
        /// </summary>
        public string Key
        {
            get => _key;
            set
            {
                _key = value;
                OnPropertyChanged("Key");
            }
        }

        /// <summary>
        /// The EncodeKey is used by PxEntry. For PwEntry, this is an empty string.
        /// </summary>
        public string EncodedKey = string.Empty;

        public bool IsEncoded => !string.IsNullOrEmpty(EncodedKey);

        private string _value;
        private string _shadowValue = string.Empty;
        /// <summary>
        /// This is the value Field for display.
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if(IsProtected)
                {
                    _shadowValue = value;
                    _value = new string('*', _shadowValue.Length);
                }
                else
                {
                    _value = value;
                }
                OnPropertyChanged("Value");
            }
        }

        /// <summary>
        /// This is the value Field for editing purpose.
        /// </summary>
        public string EditValue
        {
            get => IsProtected ? _shadowValue : _value;

            set
            {
                if (IsProtected)
                {
                    _shadowValue = value;
                    _value = new string('*', _shadowValue.Length);
                }
                else
                {
                    _value = value;
                }
                OnPropertyChanged("Value");
            }
        }

        private bool _isProtected = false;
        public bool IsProtected
        {
            get => _isProtected;
            set
            {
                _isProtected = value;
                OnPropertyChanged("IsProtected");
            }
        }

        private bool _isBinaries = false;
        /// <summary>
        /// Whether this field is an attachment
        /// </summary>
        public bool IsBinaries
        {
            get => _isBinaries;
            set
            {
                _isBinaries = value;
                OnPropertyChanged("IsProtected");
            }
        }

        private ProtectedBinary _binary = null;
        /// <summary>
        /// Binary data in the attachment
        /// </summary>
        public ProtectedBinary Binary
        {
            get => _binary;
            set
            {
                _binary = value;
                OnPropertyChanged("Binary");
            }
        }

        public bool IsHide { get; private set; } = true;

        public ImageSource ImgSource { get; set; }

        public Field(string key, string value, bool isProtected, string encodedKey = "")
        {
            Key = key;
            EncodedKey = encodedKey;
            IsProtected = isProtected;
            Value = value;

            string lastWord = key.Split(' ').Last();
            ImgSource = FieldIcons.GetImage(lastWord.ToLower());
        }

        public object ShowContextAction { get; set; }

        public void ShowPassword()
        {
            if (IsProtected && !string.IsNullOrEmpty(_shadowValue))
            {
                _value = _shadowValue;
                IsHide = false;
                OnPropertyChanged("Value");
            }
        }

        public void HidePassword()
        {
            if (IsProtected && !string.IsNullOrEmpty(_shadowValue))
            {
                _value = new string('*', _shadowValue.Length);
                IsHide = true;
                OnPropertyChanged("Value");
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }

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
