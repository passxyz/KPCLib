using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using KeePassLib;
using KeePassLib.Security;
using KeePassLib.Collections;

namespace PassXYZLib
{
    /// <summary>
    /// Contains PassXYZ-global definitions and enums. Extend KeePass to support
    /// more standard fields. The standard fields defined here are language-agnostic
    /// </summary>
    public static class PxDefs
    {
        public const int Data = 0;
        public const int Template = 1;

        /// <summary>
        /// Appname used internally. The external name could be PassXYZ or PassXYZ cloud.
        /// </summary>
        public const string Appname = "PassXYZ";

        /// <summary>
        /// Default identifier string for the title field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string TitleField = PwDefs.TitleField;

        /// <summary>
        /// Default identifier string for the user name field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string UserNameField = PwDefs.UserNameField;

        /// <summary>
        /// Default identifier string for the password field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string PasswordField = PwDefs.PasswordField;

        /// <summary>
        /// Default identifier string for the URL field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string UrlField = PwDefs.UrlField;

        /// <summary>
        /// Default identifier string for the notes field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string NotesField = PwDefs.NotesField;

        /// <summary>
        /// Default identifier string for the Email field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string EmailField = "Email";

        /// <summary>
        /// Default identifier string for the Mobile field. Should not contain
        /// spaces, tabs or other whitespace.
        /// </summary>
        public const string MobileField = "Mobile";

        /// <summary>
        /// The format of PxEntry Key Index
        /// </summary>
        public const string PxEntryKeyIndexFormat = "{0:000}";
        public const int PxEntryMaximumFieldNum = 999;
        public const int PxEntryKeyDigits = 3;

        /// <summary>
        /// Definitions of icons for ItemSubType
        /// </summary>
        public const string EntryIconFile = "ic_entry_items.png";
        public const string GroupIconFile = "passxyz_group.png";
        public const string NotesIconFile = "ic_entry_notes.png";

        /// <summary>
        /// Default identifier string for the Guest. 
        /// The data file of Guest are stored on the user specified location.
        /// </summary>
        public const string UserGuest = "Guest";
        public const string UserGuestFile = "pass_guest.xyz";

        public const string TempFolder = "tmp";
        public const string LogFilename = "passlog";

        /// <summary>
        /// Template file. 
        /// </summary>
        public const string TemplateFile = "pass.xyz";
        public const string TemplateFileSignature = "xamarin.forms";

        public const string BluetoothSharingFolder = "/sdcard/bluetooth";

        public const string KeyFileSignature = "com.PassXYZ.KeyFile;k4xyz";

        /// <summary>
        /// File header used, when export PassXYZ data.
        /// For the naming convention, please refer to design doc 8.1.
        /// </summary>
        public const string head_xyz = "pass_d_";
        public const string head_data = "pass_e_";
        public const string head_k4xyz = "pass_k_";
        public const string head_r4xyz = "pass_r_";
        public const string head_s4xyz = "pass_s_";
        public const string head_t4xyz = "pass_t_";

        /// <summary>
        /// File extensions for PassXYZ. 
        /// </summary>
        public const string kdbx = ".kdbx";
        public const string xyz = ".xyz";
        public const string k4xyz = ".k4xyz";
        public const string r4xyz = ".r4xyz";
        public const string s4xyz = ".s4xyz";
        public const string t4xyz = ".t4xyz";
        public const string txt = ".txt";

        /// <summary>
        /// File extensions for PassXYZ. 
        /// </summary>
        public const string all_kdbx = "*.kdbx";
        public const string all_xyz = "*.xyz";
        public const string all_k4xyz = "*.k4xyz";
        public const string all_r4xyz = "*.r4xyz";
        public const string all_s4xyz = "*.s4xyz";
        public const string all_t4xyz = "*.t4xyz";
        public const string all_txt = "*.txt";

        public const string PxKeyFile = "pxkey://";
        public const string PxJsonData = "pxdat://";
        public const string PxJsonTemplate = "pxtem://";

        public const int QR_CODE_MAX_LEN = 1152;

        /// <summary>
        /// PassXYZ customization data keys, please refer to chapter 17
        /// </summary>
        public const string PxCloudSyncDbKey = "PassXYZ_DB_Cloud";
        public const string PxCloudSyncStatusDbKey = "PassXYZ_DB_CloudStatus";

        public const string PxCloudSyncDisabled = "CloudDisabled";

        public const int PxCloudSyncNotificationID = 1;

        public const string PxCustomDataItemSubType = "PassXYZ_Type";
        public const string PxCustomDataOtpUrl = "PassXYZ_OTP_Url";
        public const string PxCustomDataIconName = "PassXYZ_Icon";
        public const string PxCustomDataCloudStorageName = "PassXYZ_CloudStorageName";

        /// <summary>
        /// Decode the username from data file name. Data file type can be
        /// either PxFileType.PxDataEx or PxFileType.PxData
        /// </summary>
        /// <param name="fileName">File name used to decode username</param>
        /// <param name="fileType">File type</param>
        /// <returns>decoded username</returns>
        public static string GetUserNameFromDataFile(string fileName)
        {
            var userName = GetUserNameFromFileName(fileName, PxFileType.PxDataEx);

            if(userName == string.Empty) { userName = GetUserNameFromFileName(fileName); }

            return userName;
        }

        /// <summary>
        /// Decode the username from filename
        /// </summary>
        /// <param name="fileName">File name used to decode username</param>
        /// <param name="fileType">File type</param>
        /// <returns>decoded username</returns>
        public static string GetUserNameFromFileName(string fileName, PxFileType fileType = PxFileType.PxData)
        {
            string trimedName = null;
            string head = PxFileTypeInfo.GetHead(fileType);
            string tail = PxFileTypeInfo.GetTail(fileType);

            // If it is an file exported from PassXYZ, we can decode the user name.
            if (fileName.StartsWith(head))
            {
                trimedName = fileName.Substring(head.Length);
                trimedName = trimedName.Substring(0, trimedName.LastIndexOf(tail));
                //Debug.WriteLine($"GetUserNameFromFileName: {fileName}, {trimedName}");
            }
            else
            {
                Debug.WriteLine($"GetUserNameFromFileName: {fileName} is not PassXYZ data file.");
                return string.Empty;
            }

            try
            {
                if (trimedName != string.Empty)
                {
                    trimedName = Base58CheckEncoding.GetString(trimedName);
                }
            }
            catch (FormatException e)
            {
                Debug.WriteLine($"GetUserNameFromFileName decode failed with {e.Message}");
                trimedName = string.Empty;
            }

            return trimedName;
        }

        public static bool IsDeviceLockEnabled(string filename)
        {
            if (string.IsNullOrEmpty(filename)) { return false; }

            if (filename.StartsWith(head_data)) return true;
            else return false;
        }

        public static bool IsPxEntry(PwEntry entry)
        {
            if (entry == null) { return false; }

            if (entry.CustomData.Get(PxCustomDataItemSubType) == ItemSubType.PxEntry.ToString())
                return true;
            else
                return false;
        }

        public static bool IsNotes(PwEntry entry)
        {
            if (entry == null) { return false; }

            if (entry.CustomData.Get(PxCustomDataItemSubType) == ItemSubType.Notes.ToString())
                return true;
            else
                return false;
        }

        public static string DecodeKey(string key)
        {
            return key.Substring(PxEntryKeyDigits);
        }

        public static string EncodeKey(string key, uint index) 
        {
            return String.Format(PxEntryKeyIndexFormat, index) + key;
        }

        public static string FindEncodeKey(ProtectedStringDictionary protectdStrings, string key)
        {
            string encodedKey = string.Empty;

            foreach (KeyValuePair<string, ProtectedString> kvp in protectdStrings) 
            {
                if (DecodeKey(kvp.Key) == key) { encodedKey = kvp.Key; }
            }

            return encodedKey;
        }

        /// <summary>
        /// Update a PxEntry with a key/value pair. If the value is empty, we remove
        /// the field.
        /// </summary>
        /// <param name="entry">a PwEntry instance</param>
        /// <param name="key">key</param>
        /// <param name="value">value</param>
		/// <param name="bEnableProtection">If this parameter is <c>true</c>,
		/// the string will be protected in memory (encrypted). If it
		/// is <c>false</c>, the string will be stored as plain-text.</param>
        public static void UpdatePxEntry(PwEntry entry, string key, string value, bool bEnableProtection = false)
        {
            // Fields in PxEntry need to be encoded. TitleField and NotesField should not be encoded.
            if(key == TitleField || key == NotesField) 
            {
                entry.Strings.Set(key, new ProtectedString(false, value));
            }
            else 
            {
                var encodeKey = FindEncodeKey(entry.Strings, key);
                if(encodeKey != string.Empty)
                {
                    entry.Strings.Set(encodeKey, new ProtectedString(bEnableProtection, value));
                }
                else 
                {
                    var index = entry.Strings.UCount - 2;
                    if(index >= PxEntryMaximumFieldNum) { throw new IndexOutOfRangeException("PxEntryMaximumFieldNum"); }
                    entry.Strings.Set(EncodeKey(key, index), new ProtectedString(bEnableProtection, value));
                }

                if(value == string.Empty) 
                {
                    // We remove empty field.
                    entry.Strings.Remove(encodeKey);
                }
            }
        }
        
        public static string GetDatabaseType(string fileName)
        {
            if (fileName.EndsWith(PxDefs.xyz))
            {
                return "PassXYZ";
            }
            else 
            {
                return "KeePass";
            }
        }
        // The end of PxDefs
    }

    public enum ItemType
    {
        PwEntry,
        PwGroup
    }

    public enum ItemSubType
    {
        Group,
        Entry,
        Notes,
        PxEntry,
        None
    }

    /// <summary>
    /// OneDrive synchronization status of database, the default is PxOneDriveLocal
    /// </summary>
    public enum PxCloudSyncStatus
    {
        PxLocal,
        PxCloud,
        PxSyncing,
        PxSynced,
        PxDisabled
    }

    /// <summary>
    /// PwData     - KeePass data file
    /// PxData     - PassXYZ data file
    /// PxDataEx   - PassXYZ data file with a key file
    /// PxKeyFile  - PassXYZ key file
    /// PxSync     - PassXYZ record sync
    /// PxExchange - PassXYZ data sync
    /// PxTemplate - PassXYZ template file
    /// </summary>
    public enum PxFileType
    {
        PwData,
        PxData,
        PxDataEx,
        PxKeyFile,
        PxSync,
        PxExchange,
        PxTemplate,
        PxNone
    }

    public static class PxFileTypeInfo
    {
        public static string GetHead(PxFileType fileType)
        {
            string strHead = null;

            switch(fileType)
            {
                case PxFileType.PwData:
                case PxFileType.PxData:
                    strHead = PxDefs.head_xyz;
                    break;
                case PxFileType.PxDataEx:
                    strHead = PxDefs.head_data;
                    break;
                case PxFileType.PxKeyFile:
                    strHead = PxDefs.head_k4xyz;
                    break;
                case PxFileType.PxSync:
                    strHead = PxDefs.head_r4xyz;
                    break;
                case PxFileType.PxExchange:
                    strHead = PxDefs.head_s4xyz;
                    break;
                case PxFileType.PxTemplate:
                    strHead = PxDefs.head_t4xyz;
                    break;
                default:
                    break;

            }
            return strHead;
        }

        public static string GetTail(PxFileType fileType)
        {
            string strHead = null;

            switch (fileType)
            {
                case PxFileType.PwData:
                    strHead = PxDefs.kdbx;
                    break;
                case PxFileType.PxData:
                case PxFileType.PxDataEx:
                    strHead = PxDefs.xyz;
                    break;
                case PxFileType.PxKeyFile:
                    strHead = PxDefs.k4xyz;
                    break;
                case PxFileType.PxSync:
                    strHead = PxDefs.r4xyz;
                    break;
                case PxFileType.PxExchange:
                    strHead = PxDefs.s4xyz;
                    break;
                case PxFileType.PxTemplate:
                    strHead = PxDefs.t4xyz;
                    break;
                default:
                    break;

            }
            return strHead;
        }
    }
}
