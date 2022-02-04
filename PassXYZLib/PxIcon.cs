using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

using Microsoft.Maui;
using Microsoft.Maui.Controls;

using KeePassLib;

namespace PassXYZLib
{
    public enum PxIconType
    {
        PxBuiltInIcon,
        PxEmbeddedIcon
    }

    public class PxIcon : INotifyPropertyChanged
    {
        public PxIconType IconType = PxIconType.PxBuiltInIcon;

        public PwUuid Uuid = PwUuid.Zero;

        public string Type => IconType.ToString();

        private string _name = null;
        public string Name
        {
            get => _name;
            set => _ = SetProperty(ref _name, value);
        }

        private string _filename = null;
        public string FileName
        {
            get => _filename;
            set => _ = SetProperty(ref _filename, value);
        }

        private ImageSource _imgSource = null;
        public ImageSource ImgSource
        {
            get => _imgSource;
            set => SetProperty(ref _imgSource, value);
        }

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

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
