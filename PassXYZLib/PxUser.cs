using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

using PassXYZLib.Resources;

namespace PassXYZLib
{
#if PASSXYZ_CLOUD_SERVICE
    public enum PxSyncFileType
    {
        Local,
        Remote,
        Backup,
        Temp
    }

    public abstract class PxFileStatus
    {
        private readonly PxUser _user;
        public PxUser User { get => _user; }
        virtual public PxSyncFileType FileType { get; } = PxSyncFileType.Local;
        virtual public bool IsModified { get; set; } = false;
        virtual public DateTime LastWriteTime { get; set; }
        virtual public long Length { get; set; }
        public PxFileStatus(PxUser user)
        {
            _user = user;
        }
    }

    public class PxRemoteFileStatus : PxFileStatus
    {
        public override PxSyncFileType FileType { get; } = PxSyncFileType.Remote;
        public override DateTime LastWriteTime { get; set; }
        public override long Length { get; set; }
        public PxRemoteFileStatus(PxUser user) : base(user)
        {
        }
    }

    public class PxLocalFileStatus : PxFileStatus
    {
        public override PxSyncFileType FileType { get; } = PxSyncFileType.Local;
        public override DateTime LastWriteTime
        {
            get
            {
                if (User.IsUserExist)
                {
                    var lastWriteTime = File.GetLastWriteTime(User.Path);
                    return Preferences.Get(User.FileName + nameof(LastWriteTime), lastWriteTime);
                }
                else
                {
                    return default;
                }
            }
            set
            {
                if (User.IsUserExist) 
                {
                    Preferences.Set(User.FileName + nameof(LastWriteTime), value);
                }
            }
        }

        public override long Length
        {
            get
            {
                if (User.IsUserExist)
                {
                    var fileInfo = new FileInfo(User.Path);
                    return Preferences.Get(User.FileName + nameof(Length), fileInfo.Length);
                }
                else
                {
                    return 0;
                }
            }

            set
            {
                if (User.IsUserExist)
                {
                    Preferences.Set(User.FileName + nameof(Length), value);
                }
            }
        }

        /// <summary>
        /// Has the remote file been changed?
        /// true  - remote file has been change,
        /// false - remote file is the same as the local one.
        /// </summary>

        public override bool IsModified
        {
            get
            {
                if (User.IsUserExist)
                {
                    if (LastWriteTime == User.RemoteFileStatus.LastWriteTime &&
                        Length == User.RemoteFileStatus.Length)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public PxLocalFileStatus(PxUser user) : base(user)
        {
        }
    }

    public class PxCurrentFileStatus : PxFileStatus
    {
        public override PxSyncFileType FileType { get; } = PxSyncFileType.Local;
        public override DateTime LastWriteTime
        {
            get
            {
                if (User.IsUserExist)
                {
                    return File.GetLastWriteTime(User.Path);
                }
                else
                {
                    return default;
                }
            }
        }

        public override long Length
        {
            get
            {
                if (User.IsUserExist)
                {
                    var fileInfo = new FileInfo(User.Path);
                    return fileInfo.Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// Has the local file changed?
        /// true  - the file is changed locally,
        /// false - the file is not touched
        /// </summary>
        public override bool IsModified
        {
            get
            {
                if (User.IsUserExist)
                {
                    return Preferences.Get(User.FileName, LastWriteTime != User.LocalFileStatus.LastWriteTime || Length != User.LocalFileStatus.Length);
                    //return LastWriteTime != User.LocalFileStatus.LastWriteTime ||Length != User.LocalFileStatus.Length;
                }
                else
                {
                    return false;
                }
            }

            set
            {
                if (User.IsUserExist)
                {
                    Preferences.Set(User.FileName, value);
                }
            }
        }

        public PxCurrentFileStatus(PxUser user) : base(user)
        {
        }
    }

    public class PxBackupFileStatus : PxFileStatus
    {
        public override PxSyncFileType FileType { get; } = PxSyncFileType.Backup;
        public override DateTime LastWriteTime
        {
            get
            {
                string backupFile = System.IO.Path.Combine(PxDataFile.BakFilePath, User.FileName);
                if (File.Exists(backupFile))
                {
                    return File.GetLastWriteTime(backupFile);
                }
                else
                {
                    return default;
                }
            }
        }

        public override long Length
        {
            get
            {
                string backupFile = System.IO.Path.Combine(PxDataFile.BakFilePath, User.FileName);
                if (File.Exists(backupFile))
                {
                    var fileInfo = new FileInfo(System.IO.Path.Combine(PxDataFile.BakFilePath, User.FileName));
                    return fileInfo.Length;
                }
                else
                {
                    return 0;
                }
            }
        }

        public PxBackupFileStatus(PxUser user) : base(user)
        {
        }
    }
#endif // PASSXYZ_CLOUD_SERVICE

    /// <summary>
    /// This is a class extended PassXYZLib.User. It has a dependency on Xamarin.Forms.
    /// </summary>
    public class PxUser : User
    {
        /// <summary>
        /// The DefaultTimeout is set to 120 seconds.
        /// If this is too short, there is problem to apply sync package.
        /// </summary>
        public static int DefaultTimeout = 120;

        /// <summary>
        /// The Timeout value to close the database.
        /// </summary>
        public static int AppTimeout
        {
            get => Preferences.Get(nameof(AppTimeout), DefaultTimeout);

            set => Preferences.Set(nameof(AppTimeout), value);
        }

        /// <summary>
        /// This is the icon for the user. We will use an icon to differentiate the user with Device Lock.
        /// </summary>
        public ImageSource ImgSource => new FontImageSource
        {
            FontFamily = "FontAwesomeSolid",
            Glyph = IsDeviceLockEnabled ? FontAwesomeSolid.UserLock : FontAwesomeSolid.User
        };

        private string _syncStatusIconPath = System.IO.Path.Combine(PxDataFile.IconFilePath, "ic_passxyz_disabled.png");
        public string SyncStatusIconPath
        { 
            get
            {
                return _syncStatusIconPath;
            }
            set 
            {
                _syncStatusIconPath = value;
                OnPropertyChanged("SyncStatusIconPath");
            }
        }

        /// <summary>
        /// Load local users
        /// </summary>
        public static async Task<IEnumerable<PxUser>?> LoadLocalUsersAsync(bool isBusyToLoadUsers)
        {
            if (isBusyToLoadUsers)
            {
                Debug.WriteLine("PxUser: LoadLocalUsersAsync IsBusy");
                return null;
            }

            List<PxUser> localUsers = new List<PxUser>();

            return await Task.Run(() => {
                var dataFiles = Directory.EnumerateFiles(PxDataFile.DataFilePath, PxDefs.all_xyz);
                foreach (string currentFile in dataFiles)
                {
                    string fileName = System.IO.Path.GetFileName(currentFile);
                    PxUser pxUser = new PxUser(fileName);
                    if (!string.IsNullOrWhiteSpace(pxUser.Username))
                    {
                        localUsers.Add(pxUser);
                    }
                }
                Debug.WriteLine($"PxUser: LoadLocalUsersAsync {localUsers.Count}");
                return localUsers;
            });
        }

        /// <summary>
        /// Remove all temporary files
        /// </summary>
        public static async Task RemoveTempFilesAsync()
        {
            await Task.Run(() => {
                var dataFiles = Directory.EnumerateFiles(PxDataFile.TmpFilePath, PxDefs.all_xyz);
                foreach (string currentFile in dataFiles)
                {
                    File.Delete(currentFile);
                    Debug.WriteLine($"PxUser: RemoveTempFiles {currentFile}");
                }
            });
        }

        /// <summary>
        /// Create an instance from filename
        /// </summary>
        /// <param name="fileName">File name used to decode username</param>
        public PxUser(string fileName) : this()
        {
            string trimedName;

            if (fileName.StartsWith(PxDefs.head_xyz) || fileName.StartsWith(PxDefs.head_data))
            {
                trimedName = fileName.Substring(PxDefs.head_xyz.Length);
                trimedName = trimedName.Substring(0, trimedName.LastIndexOf(PxDefs.xyz));
                try
                {
                    if (trimedName != null)
                    {
                        trimedName = Base58CheckEncoding.GetString(trimedName);
                        Username = trimedName;
                    }

                    if(fileName.StartsWith(PxDefs.head_data))
                    {
                        IsDeviceLockEnabled = true;
                    }
                }
                catch (FormatException e)
                {
                    Debug.WriteLine($"PxUser: {e.Message}");
                }
            }
            else
            {
                Debug.WriteLine($"PxUser: {fileName} is not PassXYZ data file.");
            }
        }

        public virtual void Logout() { }

#if PASSXYZ_CLOUD_SERVICE
        public async Task DeleteAsync()
        {
            ICloudServices<PxUser> sftp = PxCloudConfig.GetCloudServices();
            if(await sftp.DeleteFileAsync(FileName))
            {
                Debug.WriteLine("PxUser: DeleteAsync successfully");
            }
            else
            {
                Debug.WriteLine("PxUser: DeleteAsync failure");
            }
            Delete();
            SyncStatus = PxCloudSyncStatus.PxDisabled;
        }

        public async Task DisableSyncAsync()
        {
            ICloudServices<PxUser> sftp = PxCloudConfig.GetCloudServices();
            if (await sftp.DeleteFileAsync(FileName))
            {
                Debug.WriteLine("PxUser: DisableSyncAsync successfully");
            }
            else
            {
                Debug.WriteLine("PxUser: DisableSyncAsync failure");
            }
            SyncStatus = PxCloudSyncStatus.PxLocal;
        }

        public async Task EnableSyncAsync()
        {
            ICloudServices<PxUser> sftp = PxCloudConfig.GetCloudServices();
            await sftp.UploadFileAsync(FileName);
            SyncStatus = PxCloudSyncStatus.PxSynced;
        }

        #region PxUserFileStatus
        public PxFileStatus RemoteFileStatus;
        public PxFileStatus LocalFileStatus;
        public PxFileStatus CurrentFileStatus;
        public PxFileStatus BackupFileStatus;
        private PxCloudSyncStatus _syncStatus = PxCloudSyncStatus.PxDisabled;
        public PxCloudSyncStatus SyncStatus
        {
            get => _syncStatus;
            set
            {
                _syncStatus = value;
                if (_syncStatus == PxCloudSyncStatus.PxSynced || _syncStatus == PxCloudSyncStatus.PxSyncing)
                {
                    LocalFileStatus.LastWriteTime = RemoteFileStatus.LastWriteTime;
                    LocalFileStatus.Length = RemoteFileStatus.Length;
                    CurrentFileStatus.IsModified = false;
                }

                switch (_syncStatus) 
                {
                    case PxCloudSyncStatus.PxSynced:
                        SyncStatusIconPath = System.IO.Path.Combine(PxDataFile.IconFilePath, "ic_passxyz_synced.png");
                        break;
                    case PxCloudSyncStatus.PxSyncing:
                        SyncStatusIconPath = System.IO.Path.Combine(PxDataFile.IconFilePath, "ic_passxyz_sync.png");
                        break;
                    case PxCloudSyncStatus.PxCloud:
                        SyncStatusIconPath = System.IO.Path.Combine(PxDataFile.IconFilePath, "ic_passxyz_cloud.png");
                        break;
                    case PxCloudSyncStatus.PxLocal:
                        SyncStatusIconPath = System.IO.Path.Combine(PxDataFile.IconFilePath, "ic_passxyz_local.png");
                        break;
                    default:
                        SyncStatusIconPath = System.IO.Path.Combine(PxDataFile.IconFilePath, "ic_passxyz_disabled.png");
                        break;
                }
                OnPropertyChanged("SyncStatus");
            }
        }

        public PxUser()
        {
            RemoteFileStatus = new PxRemoteFileStatus(this);
            LocalFileStatus = new PxLocalFileStatus(this);
            CurrentFileStatus = new PxCurrentFileStatus(this);
            BackupFileStatus = new PxBackupFileStatus(this);
        }
        #endregion
#else
        public PxUser()
        { 
        }
#endif // PASSXYZ_CLOUD_SERVICE

    }

    public class PxUserComparer : IEqualityComparer<PxUser>
    {
        bool IEqualityComparer<PxUser>.Equals(PxUser? x, PxUser? y)
        {
            if ( x != null && y != null) 
            {
                return (x.Username.Equals(y.Username));
            }
            else { return false; }
        }

        int IEqualityComparer<PxUser>.GetHashCode(PxUser? obj)
        {
            if (obj is null) 
            {
                return 0;
            }
            else 
            {
                var str = obj.ToString();
                if (string.IsNullOrEmpty(str)) 
                {
                    return 0;
                }
                else 
                {
                    return str.GetHashCode();
                }
            }
        }
    }
}
