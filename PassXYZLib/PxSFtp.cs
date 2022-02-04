using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

#if PASSXYZ_CLOUD_SERVICE
using Renci.SshNet;
using Renci.SshNet.Common;
using Renci.SshNet.Sftp;
#endif

namespace PassXYZLib
{
#if PASSXYZ_CLOUD_SERVICE
    class PxSFtp : ICloudServices<PxUser>
    {
        public PxSFtp()
        {
        }

        private readonly object _syncIsConnecting = new object();
        private bool _isConnecting = false;
        public bool IsConnecting 
        { 
            get => _isConnecting;
            private set
            {
                lock (_syncIsConnecting)
                {
                    _isConnecting = value;
                }
            }
        }

        private bool _isSynchronized = false;
        public bool IsSynchronized => _isSynchronized;

        private bool _isConnected = false;
        public bool IsConnected()
        {
            return _isConnected;
        }

        private async Task<bool> ConnectAsync(Action<SftpClient> updateAction)
        {
            if (!PxCloudConfig.IsConfigured || !PxCloudConfig.IsEnabled || PassXYZ.Vault.App.IsSshOperationTimeout)
            {
                Debug.WriteLine($"PxSFtp: ConnectAsync, Cloud storage is not configured or connect timeout {PassXYZ.Vault.App.IsSshOperationTimeout}.");
                return false;
            }

            if (IsConnecting)
            {
                Debug.WriteLine("PxSFtp: ConnectAsync, another connection is running ...");
                return false;
            }

            await Task.Run(() =>
            {
                _isConnected = false;
                using (var sftp = new SftpClient(PxCloudConfig.Hostname, PxCloudConfig.Port, PxCloudConfig.Username, PxCloudConfig.Password))
                {
                    try
                    {
                        Debug.WriteLine($"PxSFtp: Trying to connect to {PxCloudConfig.Hostname}.");
                        IsConnecting = true;
                        sftp.Connect();

                        if (sftp.IsConnected)
                        {
                            _isConnected = true;
                            updateAction?.Invoke(sftp);
                        }
                        else
                        {
                            Debug.WriteLine($"PxSFtp: connection error.");
                        }
                    }
                    catch (SshOperationTimeoutException ex)
                    {
                        _isConnected = false;
                        PassXYZ.Vault.App.IsSshOperationTimeout = true;
                        Debug.WriteLine($"PxSFtp: {ex}");
                    }
                    catch (SshAuthenticationException ex)
                    {
                        _isConnected = false;
                        Debug.WriteLine($"PxSFtp: {ex}");
                    }
                    catch (SftpPathNotFoundException ex)
                    {
                        _isConnected = false;
                        Debug.WriteLine($"PxSFtp: {ex}");
                    }
                    catch (SshConnectionException ex)
                    {
                        _isConnected = false;
                        Debug.WriteLine($"PxSFtp: {ex}");
                    }
                    catch (System.Net.Sockets.SocketException ex)
                    {
                        _isConnected = false;
                        Debug.WriteLine($"PxSFtp: {ex}");
                    }
                    finally 
                    {
                        IsConnecting = false;
                    }
                }
            });
            return _isConnected;
        }

        public async Task LoginAsync()
        {
            await ConnectAsync((SftpClient sftp) => {
                Debug.WriteLine($"PxSFtp: LoginAsync {sftp.IsConnected}");
            });
        }

        public async Task<string> DownloadFileAsync(string filename, bool isMerge = true)
        {
            var path = string.Empty;
            if (isMerge)
            {
                path = Path.Combine(PxDataFile.TmpFilePath, filename);
            }
            else
            {
                path = Path.Combine(PxDataFile.DataFilePath, filename);
                string backupPath = Path.Combine(PxDataFile.BakFilePath, filename);
                if (File.Exists(path))
                {
                    File.Copy(path, backupPath, true);
                }
            }

            var remotepath = PxCloudConfig.RemoteHomePath + filename;
            await ConnectAsync((SftpClient sftp) => {
                using (var fileStr = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None,
                            bufferSize: 4096, useAsync: true))
                {
                    if (fileStr != null)
                    {
                        sftp.DownloadFile(remotepath, fileStr);
                    }
                }
            });
            return path;
        }

        public async Task UploadFileAsync(string filename)
        {
            var localPath = Path.Combine(PxDataFile.DataFilePath, filename);
            string remotePath = PxCloudConfig.RemoteHomePath + filename;
            await ConnectAsync((SftpClient sftp) => {
                using (var uploadedFileStream = new FileStream(localPath, FileMode.Open))
                {
                    sftp.UploadFile(uploadedFileStream, remotePath, true, null);
                    Debug.WriteLine($"PxSFtp: UploadFileAsync {sftp.IsConnected}");
                }
            });
        }

        public async Task<bool> DeleteFileAsync(string filename)
        {
            await ConnectAsync((SftpClient sftp) => {
                string remotePath = PxCloudConfig.RemoteHomePath + filename;
                var file = sftp.Get(remotePath);
                file.Delete();
                Debug.WriteLine($"PxSFtp: DeleteFileAsync {sftp.IsConnected}");
            });

            return _isConnected;
        }

        public async Task<IEnumerable<PxUser>> LoadRemoteUsersAsync()
        {
            List<PxUser> remoteUsers = new List<PxUser>();

            await ConnectAsync((SftpClient sftp) => {
                foreach (SftpFile remoteFile in sftp.ListDirectory(PxCloudConfig.RemoteHomePath))
                {
                    if (Path.GetExtension(remoteFile.FullName) == ".xyz")
                    {
                        // If it is a PassXYZ data file, then we add it to the list.
                        PxUser pxUser = new PxUser(Path.GetFileName(remoteFile.FullName))
                        {
                            SyncStatus = PxCloudSyncStatus.PxCloud,
                        };

                        if (!string.IsNullOrWhiteSpace(pxUser.Username))
                        {
                            pxUser.RemoteFileStatus.LastWriteTime = remoteFile.LastWriteTime;
                            pxUser.RemoteFileStatus.Length = remoteFile.Length;
                            remoteUsers.Add(pxUser);
                        }
                    }
                }
            });
            Debug.WriteLine($"PxSFtp: LoadRemoteUsersAsync {remoteUsers.Count}");
            return remoteUsers;
        }

        private Task DownloadAsync(SftpClient client, string remoteFullName, string localFullName)
        {
            var fileStr = new FileStream(localFullName, FileMode.Create, FileAccess.Write, FileShare.None,
                        bufferSize: 4096, useAsync: true);

            return Task.Factory.FromAsync(
                (callback, obj) =>
                    client.BeginDownloadFile(remoteFullName, fileStr, callback, obj),
                result =>
                {
                    client.EndDownloadFile(result);
                    if (result.IsCompleted)
                    {
                        fileStr.Close();
                    }
                },
                null);
        }

        private PxUser GetUserByUsername(IEnumerable<PxUser> pxUsers, string username) 
        {
            var pxUser = from cust in pxUsers
                         where cust.Username == username
                         select cust;
            return pxUser.First();
        }

        public async Task<IEnumerable<PxUser>> SynchronizeUsersAsync()
        {
            IEnumerable<PxUser> pxUsers = null;

            if (!PxCloudConfig.IsConfigured || !PxCloudConfig.IsEnabled || PassXYZ.Vault.App.IsBusyToLoadUsers)
            {
                Debug.WriteLine("PxSFtp: SynchronizeUsersAsync, cloud storage is not configured or IsBusy");
                return pxUsers;
            }

            await Task.Run(async () =>
            {
                IEnumerable<PxUser> remoteUsers = await LoadRemoteUsersAsync();

                if (remoteUsers.Count() == 0)
                {
                    Debug.WriteLine("PxSFtp: cannot retrieve remote users");
                    // Since there is no remote users, we return local users only.
                    pxUsers = await PxUser.LoadLocalUsersAsync();
                    foreach (PxUser localOnlyUser in pxUsers)
                    {
                        localOnlyUser.SyncStatus = PxCloudSyncStatus.PxLocal;
                    }
                    _isSynchronized = true;
                    PassXYZ.Vault.App.IsBusyToLoadUsers = false;
                    return;
                }

                // await PxUser.RemoveTempFilesAsync();
                IEnumerable<PxUser> localUsers = await PxUser.LoadLocalUsersAsync();
                IEnumerable<PxUser> localOnyUsers = localUsers.Except(remoteUsers, new PxUserComparer());
                IEnumerable<PxUser> remoteOnyUsers = remoteUsers.Except(localUsers, new PxUserComparer());
                IEnumerable<PxUser> syncedUsers = localUsers.Intersect(remoteUsers, new PxUserComparer());

                PassXYZ.Vault.App.IsBusyToLoadUsers = true;

                foreach (PxUser localOnlyUser in localOnyUsers)
                {
#if PASSXYZ_CLOUD_SERVICE_UPLOAD_LOCAL_AUTO
                    await UploadFileAsync(localOnlyUser.FileName);
                    localOnlyUser.RemoteFileStatus.Length = localOnlyUser.CurrentFileStatus.Length;
                    localOnlyUser.RemoteFileStatus.LastWriteTime = localOnlyUser.CurrentFileStatus.LastWriteTime;
                    localOnlyUser.SyncStatus = PxCloudSyncStatus.PxSynced;
                    Debug.WriteLine($"PxSFtp: SynchronizeUsersAsync updated local Username={localOnlyUser.Username}, FileName={localOnlyUser.FileName}");
#else
                    localOnlyUser.SyncStatus = PxCloudSyncStatus.PxLocal;
#endif // PASSXYZ_CLOUD_SERVICE_UPLOAD_LOCAL_AUTO
                }
                foreach (PxUser remoteOnlyUser in remoteOnyUsers)
                {
                    // Need to download remote users to local
                    await DownloadFileAsync(remoteOnlyUser.FileName, false);
                    remoteOnlyUser.SyncStatus = PxCloudSyncStatus.PxSynced;
                    Debug.WriteLine($"PxSFtp: SynchronizeUsersAsync downloaded Username={remoteOnlyUser.Username}, FileName={remoteOnlyUser.FileName}");
                }
                foreach (PxUser syncedUser in syncedUsers)
                {
                    // Need to sychronize users
                    // Case 1: Local file is newer than remote file, upload local file to overwrite the remote one
                    bool isMerge = syncedUser.CurrentFileStatus.IsModified;
                    PxUser rUser = GetUserByUsername(remoteUsers, syncedUser.Username);
                    syncedUser.RemoteFileStatus = rUser.RemoteFileStatus;
                    if ((syncedUser.CurrentFileStatus.Length != rUser.RemoteFileStatus.Length) || isMerge)
                    {
                        if (!rUser.LocalFileStatus.IsModified)
                        {
                            // Case 2: If remote file is not changed, upload local file
                            await UploadFileAsync(syncedUser.FileName);
                            syncedUser.SyncStatus = PxCloudSyncStatus.PxSynced;
                            Debug.WriteLine($"PxSFtp: SynchronizeUsersAsync: updated remote file, Username={syncedUser.Username}, FileName={syncedUser.FileName}");
                        }
                        else
                        {
                            // Case 3: Both remote file and local file changed

                            if (isMerge)
                            {
                                // Local file is changed so we may have a conflict here. Download remote file to a temp folder
                                isMerge = true;
                                syncedUser.SyncStatus = PxCloudSyncStatus.PxSyncing;
                                Debug.WriteLine($"PxSFtp: SynchronizeUsersAsync conflict found Username={syncedUser.Username}, FileName={syncedUser.FileName}");
                            }
                            else
                            {
                                // Local file is not modified, we download remote file
                                isMerge = false;
                                syncedUser.SyncStatus = PxCloudSyncStatus.PxSynced;
                                Debug.WriteLine($"PxSFtp: SynchronizeUsersAsync: updated local file, Username={syncedUser.Username}, FileName={syncedUser.FileName}");
                            }
                            await DownloadFileAsync(syncedUser.FileName, isMerge);
                        }
                    }
                    else
                    {
                        syncedUser.SyncStatus = PxCloudSyncStatus.PxSynced;
                    }
                }

                // Add remote only users to the list
                if (remoteOnyUsers.Count() > 0)
                {
                    localUsers.Concat(remoteOnyUsers);
                }
                pxUsers = localUsers;
                _isSynchronized = true;
                PassXYZ.Vault.App.IsBusyToLoadUsers = false;
            });

            Debug.WriteLine("PxSFtp: SynchronizeUsersAsync done");

            return pxUsers;
        }

        public void Logout()
        {
            _isConnected = false;
            Debug.WriteLine("SFTP: Logout");
        }
    }
#endif // PASSXYZ_CLOUD_SERVICE
}
