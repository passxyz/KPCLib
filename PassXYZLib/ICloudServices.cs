using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace PassXYZLib
{
#if PASSXYZ_CLOUD_SERVICE
    public enum PxCloudType
    {
        OneDrive,
        WebDav,
        SFTP,
        FTP,
        SMB
    }

    public class PxCloudConfigData : INotifyPropertyChanged
    {
        private string _username = PxCloudConfig.Username;
        public string Username
        {
            get
            {
                return _username;
            }

            set
            {
                _username = value;
                OnPropertyChanged("Username");
            }
        }
        private string _password = PxCloudConfig.Password;
        public string Password
        {
            get
            {
                return _password;
            }

            set
            {
                _password = value;
                OnPropertyChanged("Password");
            }
        }
        private string _hostname = PxCloudConfig.Hostname;
        public string Hostname 
        {
            get
            {
                return _hostname;
            }

            set
            {
                _hostname = value;
                OnPropertyChanged("Hostname");
            }
        }
        private int _port = PxCloudConfig.Port;
        /// <summary>
        /// Gets connection port.
        /// </summary>
        /// <value>
        /// The connection port. The default value is 22.
        /// </value>
        public int Port
        {
            get
            {
                return _port;
            }

            set
            {
                _port = value;
                OnPropertyChanged("Port");
            }
        }
        private string _remoteHomePath = PxCloudConfig.RemoteHomePath;
        public string RemoteHomePath 
        {
            get
            {
                return _remoteHomePath;
            }

            set
            {
                _remoteHomePath = value;
                OnPropertyChanged("RemoteHomePath");
            }
        }
        private bool _isEnabled = PxCloudConfig.IsEnabled;
        public bool IsEnabled 
        {
            get
            {
                return _isEnabled;
            }

            set
            {
                _isEnabled = value;
                OnPropertyChanged("IsEnabled");
            }
        }
        private string _configMessage = AppResources.message_id_cloud_config;
        public string ConfigMessage 
        {
            get => _configMessage;
            set 
            {
                _configMessage = value;
                OnPropertyChanged("ConfigMessage");
            }
        }

        public bool IsConfigured
        {
            get
            {
                return !string.IsNullOrWhiteSpace(Username)
                    && !string.IsNullOrWhiteSpace(Password)
                    && !string.IsNullOrWhiteSpace(Hostname)
                    && !string.IsNullOrWhiteSpace(RemoteHomePath);
            }
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

    public static class PxCloudConfig
    {
        public static PxCloudType CurrentServiceType { get => PxCloudType.SFTP; }

        public static string Username 
        {
            get 
            { 
                return Preferences.Get(nameof(PxCloudConfig) + nameof(Username), "");
            }
            set 
            {
                Preferences.Set(nameof(PxCloudConfig) + nameof(Username), value);
            }
        }
        public static string Password 
        {
            get
            {
                return Preferences.Get(nameof(PxCloudConfig) + nameof(Password), "");
            }
            set
            {
                Preferences.Set(nameof(PxCloudConfig) + nameof(Password), value);
            }
        }
        public static string Hostname 
        {
            get
            {
                return Preferences.Get(nameof(PxCloudConfig) + nameof(Hostname), "");
            }
            set
            {
                Preferences.Set(nameof(PxCloudConfig) + nameof(Hostname), value);
            }
        }
        /// <summary>
        /// Gets connection port.
        /// </summary>
        /// <value>
        /// The connection port. The default value is 22.
        /// </value>
        public static int Port
        {
            get
            {
                return Preferences.Get(nameof(PxCloudConfig) + nameof(Port), 22);
            }
            set
            {
                Preferences.Set(nameof(PxCloudConfig) + nameof(Port), value);
            }
        }
        public static string RemoteHomePath 
        {
            get
            {
                return Preferences.Get(nameof(PxCloudConfig) + nameof(RemoteHomePath), "");
            }
            set
            {
                Preferences.Set(nameof(PxCloudConfig) + nameof(RemoteHomePath), value);
            }
        }
        public static bool IsEnabled 
        {
            get
            {
                return Preferences.Get(nameof(PxCloudConfig) + nameof(IsEnabled), false);
            }
            set
            {
                Preferences.Set(nameof(PxCloudConfig) + nameof(IsEnabled), value);
            }
        }
        public static bool IsConfigured => !string.IsNullOrWhiteSpace(Username)
                    && !string.IsNullOrWhiteSpace(Password)
                    && !string.IsNullOrWhiteSpace(Hostname)
                    && !string.IsNullOrWhiteSpace(RemoteHomePath);

        public static void SetConfig(PxCloudConfigData configData)
        {
            Username = configData.Username;
            Password = configData.Password;
            Hostname = configData.Hostname;
            Port = configData.Port;
            RemoteHomePath = configData.RemoteHomePath;
            IsEnabled = configData.IsEnabled;
        }

        private static PxSFtp pxSFtp = null;
        public static ICloudServices<PxUser> GetCloudServices()
        {
            if (pxSFtp == null)
            {
                pxSFtp = new PxSFtp();
            }
            return pxSFtp;
        }
    }

    public class PxFileInfo : INotifyPropertyChanged
    {
        public string IconPath { get; set; }
        public PxSyncFileType FileType { get; set; } = PxSyncFileType.Local;
        public string FileTypeComments { get; set; }
        private DateTime _lastWriteTime;
        public DateTime LastWriteTime
        {
            get => _lastWriteTime;
            set
            {
                _lastWriteTime = value;
                OnPropertyChanged("LastWriteTime");
            }
        }
        private long _length;
        public long Length
        {
            get => _length;
            set
            {
                _length = value;
                OnPropertyChanged("Length");
            }
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

    public interface ICloudServices<T>
    {
        Task LoginAsync();
        bool IsConnected();
        Task<string> DownloadFileAsync(string filename, bool isMerge = false);
        Task UploadFileAsync(string filename);
        Task<bool> DeleteFileAsync(string filename);
        Task<IEnumerable<T>> LoadRemoteUsersAsync();
        Task<IEnumerable<T>> SynchronizeUsersAsync();
        void Logout();
        bool IsSynchronized { get; }
    }
#endif // PASSXYZ_CLOUD_SERVICE
}
