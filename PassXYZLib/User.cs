using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace PassXYZLib
{
    public static class PxDataFile
    {
        private static string _logFilePath = string.Empty;
        /// <summary>
        /// The default log file path uses the stardard .NET setting. The default log file path is a readonly item.
        /// </summary>
        public static string LogFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_logFilePath))
                {
                    return _logFilePath;
                }
                else
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "passxyz_log.txt");
                }
            }
        }

        private static string _dataFilePath = string.Empty;
        /// <summary>
        /// The default data file path uses the stardard .NET setting. The default data file path doesn't need to be set.
        /// In some cases, such as unit test, the data file path can be set. If the data file path is set, 
        /// the data file and key file will be stored in the same folder.
        /// </summary>
        public static string DataFilePath
        {
            get
            {
                if(!string.IsNullOrEmpty(_dataFilePath))
                {
                    return _dataFilePath;
                }
                else
                {
                    string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "data");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
            }
            set
            {
                _dataFilePath = value;
            }
        }

        /// <summary>
        /// The default key file path uses the stardard .NET setting. The default key file path doesn't need to be set.
        /// If the data file path is set, the data file and key file will be stored in the same folder.
        /// </summary>
        public static string KeyFilePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_dataFilePath))
                {
                    return _dataFilePath;
                }
                else
                {
                    string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "key");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    return path;
                }
            }
        }

        /// <summary>
        /// The temporary file path.
        /// </summary>
        public static string TmpFilePath
        {
            get
            {
                string tmpPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "tmp");
                if (!Directory.Exists(tmpPath))
                {
                    Directory.CreateDirectory(tmpPath);
                }
                return tmpPath;
            }
        }

        /// <summary>
        /// The backup file path.
        /// </summary>
        public static string BakFilePath
        {
            get
            {
                string bakPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "bak");
                if (!Directory.Exists(bakPath))
                {
                    Directory.CreateDirectory(bakPath);
                }
                return bakPath;
            }
        }

        /// <summary>
        /// The icon file path.
        /// </summary>
        public static string IconFilePath
        {
            get
            {
                string iconPath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "icons");
                if (!Directory.Exists(iconPath))
                {
                    _ = Directory.CreateDirectory(iconPath);
                }
                return iconPath;
            }
        }

        /// <summary>
        /// Decode the username from filename
        /// </summary>
        /// <param name="fileName">File name used to decode username</param>
        /// <returns>Decoded username</returns>
        public static string? GetUserName(string fileName)
        {
            string trimedName;
            
            if (fileName.StartsWith(PxDefs.head_xyz) || fileName.StartsWith(PxDefs.head_data))
            {
                trimedName = fileName.Substring(PxDefs.head_xyz.Length);
                trimedName = trimedName.Substring(0, trimedName.LastIndexOf(PxDefs.xyz));
                // Debug.WriteLine($"PxDataFile: {fileName}, {trimedName} in GetUserName().");
            }
            else
            {
                Debug.WriteLine($"PxDataFile: {fileName} is not PassXYZ data file in GetUserName().");
                return string.Empty;
            }

            try
            {
                if (trimedName != null)
                {
                    trimedName = Base58CheckEncoding.GetString(trimedName);
                }
            }
            catch (FormatException e)
            {
                Debug.WriteLine($"PxDataFile: {e.Message}");
                trimedName = string.Empty;
            }

            return trimedName;
        }

    }

    public class User : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        /// <summary>
        /// PassXYZ uses the concept of user instead of data file to manage password database.
        /// This is because it is difficult to manage data file in mobile devices. The actual data file is encoded
        /// using base58 encoding with information such as key file or device lock enabled.
        /// </summary>
        public virtual string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        private string _password = string.Empty;
        public virtual string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _password2 = string.Empty;
        public string Password2
        {
            get => _password2;
            set => SetProperty(ref _password2, value);
        }

        private bool _isDeviceLockEnabled = false;
        /// <summary>
        /// Check whether Device Lock is enabled for this user.
        /// <c>true</c> - key file is enabled, <c>false</c> - key file is not enabled
        /// </summary>
        public bool IsDeviceLockEnabled
        { 
            get
            {
                if (!_isDeviceLockEnabled && !string.IsNullOrEmpty(_username))
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(PxDataFile.DataFilePath, GetFileName(true))))
                    {
                        _isDeviceLockEnabled = true;
                    }
                }

                return _isDeviceLockEnabled;
            }
            set
            {
                _isDeviceLockEnabled = value;
            }
        }

        /// <summary>
        /// Check whether the username exist or not. 
        /// <c>true</c> - user exist, <c>false</c> - user does not exist
        /// </summary>
        public bool IsUserExist
        {
            get
            {
                if (string.IsNullOrEmpty(_username))
                {
                    return false;
                }

                var dataFiles = Directory.EnumerateFiles(PxDataFile.DataFilePath, PxDefs.all_xyz);
                foreach (string currentFile in dataFiles)
                {
                    string? userName = PxDataFile.GetUserName(currentFile.Substring(PxDataFile.DataFilePath.Length + 1));
                    if (userName != string.Empty && !string.IsNullOrWhiteSpace(userName))
                    {
                        if(userName.Equals(_username)) { return true; }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// Check whether the key file is existed.
        /// true - key file is available, false - key file is not available
        /// </summary>
        public bool IsKeyFileExist
        {
            get
            {
                if (string.IsNullOrEmpty(_username))
                {
                    return false;
                }

                if (IsDeviceLockEnabled)
                {
                    if (System.IO.File.Exists(System.IO.Path.Combine(PxDataFile.KeyFilePath, KeyFileName)))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// The date/time when this user was last accessed (read).
        /// </summary>
        public DateTime LastAccessTime => File.GetLastWriteTime(this.Path);

        /// <summary>
        /// Data file name. Converted Username to file name
        /// </summary>
        public string FileName => GetFileName(IsDeviceLockEnabled);

        /// <summary>
        /// Data file path
        /// </summary>
        public string Path
        {
            get
            {
                if (string.IsNullOrEmpty(_username))
                {
                    return string.Empty;
                }
                return System.IO.Path.Combine(PxDataFile.DataFilePath, FileName);
            }
        }

        /// <summary>
        /// Get the key file name. The key file name is generated using base58 encoding from username.
        /// If Device Lock is not enabled, the key file name is <c>string.Empty</c>.
        /// </summary>
        public string KeyFileName
        {
            get 
            {
                if (string.IsNullOrEmpty(_username))
                {
                    return string.Empty;
                }

                if (IsDeviceLockEnabled)
                {
                    return PxDefs.head_k4xyz + Base58CheckEncoding.ToBase58String(Username) + PxDefs.k4xyz;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        /// <summary>
        /// Key file path
        /// </summary>
        public string KeyFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(_username))
                {
                    return string.Empty;
                }
                return System.IO.Path.Combine(PxDataFile.KeyFilePath, KeyFileName);
            }
        }

        /// <summary>
        /// Delete the current user
        /// </summary>
        public void Delete()
        {
            File.Delete(Path);
            if (IsDeviceLockEnabled)
            {
                File.Delete(KeyFilePath);
            }
        }

        /// <summary>
        /// Get the data file name from the username. The data file name is generated
        /// using base58 encoding from username.
        /// </summary>
        /// <param name="isDeviceLockEnabled">Device lock enabled or not</param>
        /// <returns>Data file name</returns>
        private string GetFileName(bool isDeviceLockEnabled = false)
        {
            if (string.IsNullOrEmpty(_username))
            {
                return string.Empty;
            }

            if (isDeviceLockEnabled)
            {
                return PxDefs.head_data + Base58CheckEncoding.ToBase58String(_username) + PxDefs.xyz;
            }
            else
            {
                return PxDefs.head_xyz + Base58CheckEncoding.ToBase58String(_username) + PxDefs.xyz;
            }
        }

        /// <summary>
        /// Get a list of existing users from the encoded data files
        /// </summary>
        /// <returns>user list</returns>
        public static List<string> GetUsersList()
        {
            List<string> userList = new List<string>();

            var dataFiles = Directory.EnumerateFiles(PxDataFile.DataFilePath, PxDefs.all_xyz);
            foreach (string currentFile in dataFiles)
            {
                string fileName = currentFile.Substring(PxDataFile.DataFilePath.Length + 1);
                string? userName = PxDataFile.GetUserName(fileName);
                if (userName != string.Empty && !string.IsNullOrWhiteSpace(userName))
                {
                    userList.Add(userName);
                }
            }
            return userList;
        }

        public User()
        {
            IsDeviceLockEnabled = false;
        }

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action? onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
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
