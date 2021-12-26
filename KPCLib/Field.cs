using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Text;

namespace KPCLib
{
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

        //private ProtectedBinary _binary = null;
        /// <summary>
        /// Binary data in the attachment
        /// </summary>
        //public ProtectedBinary Binary
        //{
        //    get => _binary;
        //    set
        //    {
        //        _binary = value;
        //        OnPropertyChanged("Binary");
        //    }
        //}

        public bool IsHide { get; private set; } = true;

        public Object ImgSource { get; set; }

        public Field(string key, string value, bool isProtected, string encodedKey = "")
        {
            Key = key;
            EncodedKey = encodedKey;
            IsProtected = isProtected;
            Value = value;

            // string lastWord = key.Split(' ').Last();
            // ImgSource = FieldIcons.GetImage(lastWord.ToLower());
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

}
