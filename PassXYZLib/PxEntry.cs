using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;
using Xamarin.Forms;

using PureOtp;

using KeePassLib;
using KeePassLib.Interfaces;

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

        public bool IsEncoded { get => !string.IsNullOrEmpty(EncodedKey); }

        private string _value;
        private string shadowValue = string.Empty;
        public string Value
        {
            get => _value;
            set
            {
                _value = value;
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

        private bool _isHide = true;
        public bool IsHide
        {
            get => _isHide;
        }

        public ImageSource ImgSource { get; set; }

        public Field(string key, string value, bool isProtected, string encodedKey = "")
        {
            Key = key;
            EncodedKey = encodedKey;
            IsProtected = isProtected;

            if (IsProtected)
            { 
                // If it is protected, we won't display the value directly.
                shadowValue = value; 
                Value = new string('*', shadowValue.Length);
            }
            else 
            {
                Value = value;
            }

            var lastWord = key.Split(' ').Last();
            ImgSource = FieldIcons.GetImage(lastWord.ToLower());
        }

        public void ShowPassword()
        {
            if (IsProtected && !string.IsNullOrEmpty(shadowValue))
            {
                Value = shadowValue;
                _isHide = false;
            }
        }

        public void HidePassword()
        {
            if (IsProtected && !string.IsNullOrEmpty(shadowValue))
            {
                Value = new string('*', shadowValue.Length);
                _isHide = true;
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
    }

    public static class PwEntryEx
    {
        public static bool IsNotes(this PwEntry entry) 
        {
            string subType = entry.CustomData.Get(PxDefs.PxCustomDataItemSubType);
            if(string.IsNullOrEmpty(subType)) 
            {
                return false;
            }
            else 
            {
                if (subType.Equals(ItemSubType.Notes.ToString()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public static bool IsPxEntry(this PwEntry entry) 
        { 
            return PxDefs.IsPxEntry(entry);
        }

        public static string GetNotes(this PwEntry entry) 
        {
            return entry.Strings.ReadSafe(PwDefs.NotesField);
        }

        /// <summary>
        /// This is an extension method of PwEntry.
        /// Convert ProtectedStringDictionary into a list of fields. TitleField and NotesField
        /// are not included in the list.
        /// TitleField will be used to display the title in UI and NotesField will be displayed at the
        /// bottom of a page with Markdown support.
        /// </summary>
        /// <param name="entry">an instance of PwEntry</param>
		/// <returns>A list of fields</returns>
        public static List<Field> GetFields(this PwEntry entry)
        {
            List<Field> fields = new List<Field>();
            bool isPxEntry = PxDefs.IsPxEntry(entry);

            if (isPxEntry)
            {
                // If this is an instance of PxEntry, we handle it here. We need to convert ProtectedString to Field.
                foreach (var pstr in entry.Strings)
                {
                    if (!pstr.Key.Equals(PwDefs.TitleField) && !pstr.Key.Equals(PwDefs.NotesField))
                    {
                        fields.Add(new Field(PxDefs.DecodeKey(pstr.Key), entry.Strings.ReadSafe(pstr.Key), entry.Strings.GetSafe(pstr.Key).IsProtected, pstr.Key));
                    }
                }
            }
            else
            {
                // If this is an instance of PwEntry, we handle it here.
                if(entry.Strings.Exists(PwDefs.UserNameField))
                {
                    fields.Add(new Field(PwDefs.UserNameField, entry.Strings.ReadSafe(PwDefs.UserNameField), entry.Strings.GetSafe(PwDefs.UserNameField).IsProtected));
                }

                if (entry.Strings.Exists(PwDefs.PasswordField))
                {
                    fields.Add(new Field(PwDefs.PasswordField, entry.Strings.ReadSafe(PwDefs.PasswordField), entry.Strings.GetSafe(PwDefs.PasswordField).IsProtected));
                }

                if (entry.Strings.Exists(PwDefs.UrlField))
                {
                    fields.Add(new Field(PwDefs.UrlField, entry.Strings.ReadSafe(PwDefs.UrlField), entry.Strings.GetSafe(PwDefs.UrlField).IsProtected));
                }

                foreach (var field in entry.Strings) 
                { 
                    if (!PwDefs.IsStandardField(field.Key))
                    {
                        fields.Add(new Field(field.Key, entry.Strings.ReadSafe(field.Key), entry.Strings.GetSafe(field.Key).IsProtected));
                    }
                }
            }

            return fields;
        }
    }

    public static class FieldIcons
    {
        public static Dictionary<string, FontAwesome.Regular.Icon> RegularIcons = new Dictionary<string, FontAwesome.Regular.Icon>()
        {
            { "calendar", FontAwesome.Regular.Icon.CalendarAlt }
        };

        public static Dictionary<string, FontAwesome.Solid.Icon> SolidIcons = new Dictionary<string, FontAwesome.Solid.Icon>()
        {
            { "address", FontAwesome.Solid.Icon.MapMarkerAlt },
            { "card", FontAwesome.Solid.Icon.IdCard },
            { "date", FontAwesome.Solid.Icon.CalendarAlt },
            { "email", FontAwesome.Solid.Icon.Envelope },
            { "mobile", FontAwesome.Solid.Icon.Phone },
            { "name", FontAwesome.Solid.Icon.User },
            { "password", FontAwesome.Solid.Icon.Key },
            { "phone", FontAwesome.Solid.Icon.Phone },
            { "pin", FontAwesome.Solid.Icon.Key },
            { "url", FontAwesome.Solid.Icon.Link },
            { "username", FontAwesome.Solid.Icon.User }
        };

        public static Dictionary<string, FontAwesome.Brand.Icon> BrandIcons = new Dictionary<string, FontAwesome.Brand.Icon>()
        {
            { "alipay", FontAwesome.Brand.Icon.Alipay },
            { "qq", FontAwesome.Brand.Icon.Qq },
            { "wechat", FontAwesome.Brand.Icon.Weixin }
        };

        public static ImageSource GetImage(string key) 
        { 
            if (BrandIcons.ContainsKey(key))
            {
                var brandIconSource = new FontAwesome.Brand.IconSource
                {
                    Icon = BrandIcons[key]
                };
                return brandIconSource;
            }
            else if (RegularIcons.ContainsKey(key)) 
            {
                var regularIconSource = new FontAwesome.Regular.IconSource
                {
                    Icon = RegularIcons[key]
                };
                return regularIconSource;
            }
            else 
            {
                var solidIconSource = new FontAwesome.Solid.IconSource
                {
                    Icon = FontAwesome.Solid.Icon.File
                };

                if (SolidIcons.ContainsKey(key))
                {
                    solidIconSource.Icon = SolidIcons[key];
                }

                return solidIconSource;
            }
        }
    }
}
