using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PassXYZLib
{
    public static class PxDataFile
    {
        public static string DataFilePath
        {
            get
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "data");
                if(!Directory.Exists(path)) 
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        public static string KeyFilePath
        {
            get
            { 
                string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "key");
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }
                return path;
            }
        }

        /// <summary>
        /// Decode the username from filename
        /// </summary>
        /// <param name="fileName">File name used to decode username</param>
        /// <returns>Decoded username</returns>
        public static string GetUserName(string fileName)
        {
            string trimedName;
            
            if (fileName.StartsWith(PxDefs.head_xyz) || fileName.StartsWith(PxDefs.head_data))
            {
                trimedName = fileName.Substring(PxDefs.head_xyz.Length);
                trimedName = trimedName.Substring(0, trimedName.LastIndexOf(PxDefs.xyz));
                Debug.WriteLine($"PxDataFile: {fileName}, {trimedName} in GetUserName().");
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

        /// <summary>
        /// Get a list of existing users from the encoded data files
        /// </summary>
        /// <returns>user list</returns>
        public static List<string> GetUsersList()
        {
            List<string> userList = new List<string>();

            var dataFiles = Directory.EnumerateFiles(DataFilePath, PxDefs.all_xyz);
            foreach (string currentFile in dataFiles)
            {
                string fileName = currentFile.Substring(DataFilePath.Length + 1);
                string userName = GetUserName(fileName);
                if (userName != string.Empty && !string.IsNullOrWhiteSpace(userName))
                {
                    userList.Add(userName);
                }
            }
            return userList;
        }
    }

    public class User
    {
        private string _username;
        public string Username 
        {
            get => _username;
            set 
            {
                _username = value;

                // Check whether Device Lock is enabled, but key file may not exist.
                if (System.IO.File.Exists(System.IO.Path.Combine(PxDataFile.DataFilePath, GetFileName(true))))
                {
                    IsDeviceLockEnabled = true;
                }
                else
                {
                    IsDeviceLockEnabled = false;
                }
            }
        }

        public string Password { get; set; }

        /// <summary>
        /// Check whether Device Lock is enabled for this user.
        /// <c>true</c> - key file is enabled, <c>false</c> - key file is not enabled
        /// </summary>
        public bool IsDeviceLockEnabled { get; set; }

        /// <summary>
        /// Check whether the key file is existed.
        /// true - key file is available, false - key file is not available
        /// </summary>
        public bool IsKeyFileExist
        {
            get
            {
                if (IsDeviceLockEnabled)
                {
                    var keyFilePath = System.IO.Path.Combine(PxDataFile.KeyFilePath, KeyFileName);
                    if (System.IO.File.Exists(keyFilePath))
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
                return System.IO.Path.Combine(PxDataFile.DataFilePath, FileName);
            }
        }

        /// <summary>
        /// Get the key file name. The key file name is generated using base58 encoding from username.
        /// If Device Lock is not enabled, return <c>string.Empty</c>.
        /// </summary>
        public string KeyFileName
        {
            get 
            {
                if(IsDeviceLockEnabled) 
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
        /// Get the data file name from the username. The data file name is generated
        /// using base58 encoding from username.
        /// </summary>
        /// <param name="isDeviceLockEnabled">Device lock enabled or not</param>
        /// <returns>Data file name</returns>
        private string GetFileName(bool isDeviceLockEnabled = false)
        {
            if (isDeviceLockEnabled)
            {
                return PxDefs.head_data + Base58CheckEncoding.ToBase58String(_username) + PxDefs.xyz;
            }
            else
            {
                return PxDefs.head_xyz + Base58CheckEncoding.ToBase58String(_username) + PxDefs.xyz;
            }
        }

        public User()
        {
            IsDeviceLockEnabled = false;
        }
    }
}
