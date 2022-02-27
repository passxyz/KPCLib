using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

using SkiaSharp;

using KPCLib;
using KeePassLib;
using KeePassLib.Delegates;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;
using PassXYZLib.Resources;

namespace PassXYZLib
{
	/// <summary>
	/// PxDatabase is a sub-class of PwDatabase. This class has more dependencies than PwDatabase with
	/// Xamarin.Forms and SkiaSharp etc.
	/// </summary>
	public class PxDatabase : PwDatabase
    {
		private PwGroup m_pwCurrentGroup = null;

 		/// <summary>
		/// If this is <c>true</c>, a database is currently open.
		/// </summary>
		public new bool IsOpen
		{
			get { return base.IsOpen; }
		}

		/// <summary>
		/// Modification flag. If true, the class has been modified and the
		/// user interface should prompt the user to save the changes before
		/// closing the database for example.
		/// </summary>
		public new bool Modified
		{
			get { return base.Modified; }
			set { base.Modified = value; }
		}

		public new DateTime SettingsChanged
		{
			get { return base.SettingsChanged; }
			set { base.SettingsChanged = value; }
		}

		public new DateTime NameChanged
		{
			get { return base.NameChanged; }
			set { base.NameChanged = value; }
		}

		public new DateTime DescriptionChanged
		{
			get { return base.DescriptionChanged; }
			set { base.DescriptionChanged = value; }
		}

		public new DateTime DefaultUserNameChanged
		{
			get { return base.DefaultUserNameChanged; }
			set { base.DefaultUserNameChanged = value; }
		}

		/// <summary>
		/// Number of days until history entries are being deleted
		/// in a database maintenance operation.
		/// </summary>
		public new uint MaintenanceHistoryDays
		{
			get { return base.MaintenanceHistoryDays; }
			set { base.MaintenanceHistoryDays = value; }
		}

		public new Color Color
		{
			get { return base.Color; }
			set { base.Color = value; }
		}

		public new DateTime MasterKeyChanged
		{
			get { return base.MasterKeyChanged; }
			set { base.MasterKeyChanged = value; }
		}

		public new long MasterKeyChangeRec
		{
			get { return base.MasterKeyChangeRec; }
			set { base.MasterKeyChangeRec = value; }
		}

		public new long MasterKeyChangeForce
		{
			get { return base.MasterKeyChangeForce; }
			set { base.MasterKeyChangeForce = value; }
		}

		public new bool MasterKeyChangeForceOnce
		{
			get { return base.MasterKeyChangeForceOnce; }
			set { base.MasterKeyChangeForceOnce = value; }
		}

		/// <summary>
		/// Compression algorithm used to encrypt the data part of the database.
		/// </summary>
		public new PwCompressionAlgorithm Compression
		{
			get { return base.Compression; }
			set { base.Compression = value; }
		}

		/// <summary>
		/// This is a dirty-flag for the UI. It is used to indicate when an
		/// icon list update is required.
		/// </summary>
		public new bool UINeedsIconUpdate
		{
			get { return base.UINeedsIconUpdate; }
			set { base.UINeedsIconUpdate = value; }
		}

		public new bool RecycleBinEnabled
		{
			get { return base.RecycleBinEnabled; }
			set { base.RecycleBinEnabled = value; }
		}

		public new DateTime RecycleBinChanged
		{
			get { return base.RecycleBinChanged; }
			set { base.RecycleBinChanged = value; }
		}

		public new DateTime EntryTemplatesGroupChanged
		{
			get { return base.EntryTemplatesGroupChanged; }
			set { base.EntryTemplatesGroupChanged = value; }
		}

		public new int HistoryMaxItems
		{
			get { return base.HistoryMaxItems; }
			set { base.HistoryMaxItems = value; }
		}

		public new long HistoryMaxSize
		{
			get { return base.HistoryMaxSize; }
			set { base.HistoryMaxSize = value; }
		}

		public new bool UseFileTransactions
		{
			get { return base.UseFileTransactions; }
			set { base.UseFileTransactions = value; }
		}

		public new bool UseFileLocks
		{
			get { return base.UseFileLocks; }
			set { base.UseFileLocks = value; }
		}

		public PwGroup CurrentGroup
		{
			get {
				if (!IsOpen) { return null; }

				if(RootGroup.Uuid == LastSelectedGroup || LastSelectedGroup.Equals(PwUuid.Zero))
				{
					LastSelectedGroup = RootGroup.Uuid;
					m_pwCurrentGroup = RootGroup;
				}

				if(m_pwCurrentGroup == null) 
				{ 
					if(!LastSelectedGroup.Equals(PwUuid.Zero)) { m_pwCurrentGroup = RootGroup.FindGroup(LastSelectedGroup, true); }
				}
				return m_pwCurrentGroup;
			}
			set {
				if (value == null) { Debug.Assert(false); throw new ArgumentNullException("value"); }
				LastSelectedGroup = value.Uuid;
				if (RootGroup.Uuid == LastSelectedGroup || LastSelectedGroup.Equals(PwUuid.Zero))
				{
					LastSelectedGroup = RootGroup.Uuid;
					m_pwCurrentGroup = RootGroup;
				}
				else
					m_pwCurrentGroup = RootGroup.FindGroup(LastSelectedGroup, true);
			}
		}

		/// <summary>
		/// This is the string of CurrentGroup
		/// </summary>
		public string CurrentPath
		{ 
			get {
				if(CurrentGroup == null)
				{
					return string.Empty;
				}
				else 
				{
					var group = CurrentGroup;
					string path = group.Name + "/";

					while (!RootGroup.Uuid.Equals(group.Uuid))
					{
						group = group.ParentGroup;
						path = group.Name + "/" + path;
					}

					return path;
				}
			}
		}

		private static string m_DefaultFolder = String.Empty;
		public static string DefaultFolder
		{
			get
			{
				if (m_DefaultFolder == String.Empty) { m_DefaultFolder = Environment.CurrentDirectory; }

				return m_DefaultFolder;
			}

			set 
			{ 
				m_DefaultFolder = value;
				PassXYZ.Utils.Settings.DefaultFolder = m_DefaultFolder;
			}
		}

		/// <summary>
		/// Constructs an empty password manager object.
		/// </summary>
		public PxDatabase() : base()
		{
			Debug.WriteLine("PxDatabase: Created instance");
		}

		~PxDatabase() 
		{
			Debug.WriteLine("PxDatabase: Destory instance");
		}

		/// <summary>
		/// Open database using a filename and password
		/// This data file has a device lock.
		/// Need to set DefaultFolder first. This is the folder to store data files.
		/// </summary>
		/// <param name="filename">The data file name</param>
		/// <param name="password">The password of data file</param>
		public void Open(string filename, string password)
		{
			if (filename == null || filename == String.Empty)
			{ Debug.Assert(false); throw new ArgumentNullException("filename"); }
			if (password == null || password == String.Empty)
			{ Debug.Assert(false); throw new ArgumentNullException("password"); }

			var logger = new KPCLibLogger();

			var file_path = Path.Combine(DefaultFolder, filename);
			IOConnectionInfo ioc = IOConnectionInfo.FromPath(file_path);
			CompositeKey cmpKey = new CompositeKey();
			cmpKey.AddUserKey(new KcpPassword(password));

			if(PxDefs.IsDeviceLockEnabled(filename))
			{
				var userName = PxDefs.GetUserNameFromDataFile(filename);
				User user = new() { Username = userName };
				var pxKeyProvider = new PassXYZ.Services.PxKeyProvider(userName, user.KeyFilePath);
				if (pxKeyProvider.IsInitialized)
				{
					KeyProviderQueryContext ctxKP = new KeyProviderQueryContext(new IOConnectionInfo(), false, false);
					byte[] pbProvKey = pxKeyProvider.GetKey(ctxKP);
					cmpKey.AddUserKey(new KcpCustomKey(pxKeyProvider.Name, pbProvKey, true));
				}
				else
				{
					throw new KeePassLib.Keys.InvalidCompositeKeyException();
				}
			}

			Open(ioc, cmpKey, logger);
		}

		/// <summary>
		/// Open database with user information.
		/// If the device lock is enabled, we need to set DefaultFolder first.
		/// </summary>
		/// <param name="user">an instance of PassXYZLib.User</param>
		public void Open(PassXYZLib.User user)
        {
			if (user == null)
			{ Debug.Assert(false); throw new ArgumentNullException("PassXYZLib.User"); }
			if (user.Password == null || user.Password == String.Empty)
			{ Debug.Assert(false); throw new ArgumentNullException("Password"); }
			if (!user.IsUserExist)
			{ throw new FileNotFoundException("User doesn't exist."); }

			var logger = new KPCLibLogger();

			IOConnectionInfo ioc = IOConnectionInfo.FromPath(user.Path);
			CompositeKey cmpKey = new CompositeKey();
			cmpKey.AddUserKey(new KcpPassword(user.Password));

			if (user.IsDeviceLockEnabled)
			{
				try 
				{
					//PassXYZ.Utils.Settings.DefaultFolder = PxDataFile.KeyFilePath;
					var pxKeyProvider = new PassXYZ.Services.PxKeyProvider(user.Username, user.KeyFilePath);
					if (pxKeyProvider.IsInitialized)
					{
						KeyProviderQueryContext ctxKP = new KeyProviderQueryContext(new IOConnectionInfo(), false, false);
						byte[] pbProvKey = pxKeyProvider.GetKey(ctxKP);
						cmpKey.AddUserKey(new KcpCustomKey(pxKeyProvider.Name, pbProvKey, true));
					}
					else
					{
						throw new KeePassLib.Keys.InvalidCompositeKeyException();
					}
				}
				catch (PassXYZ.Services.InvalidDeviceLockException ex)
                {
					try { cmpKey.AddUserKey(new KcpKeyFile(user.KeyFilePath)); }
					catch (Exception exFile)
					{
						Debug.Write($"{exFile} in {ex}");
					}
				}
			}

			Open(ioc, cmpKey, logger);
		}

		public string GetDeviceLockData(PassXYZLib.User user)
        {
			if (user.IsDeviceLockEnabled)
			{
				try
				{
					PassXYZ.Utils.Settings.DefaultFolder = PxDataFile.KeyFilePath;
					var pxKeyProvider = new PassXYZ.Services.PxKeyProvider(user.Username, false);
					if (pxKeyProvider.IsInitialized)
					{
						return PassXYZ.Services.PxKeyData.ToBase64String(pxKeyProvider.KeyData);
					}
				}
				catch (PassXYZ.Services.InvalidDeviceLockException ex)
				{
					Debug.WriteLine($"{ex}");
					return string.Empty;
				}
			}
			return string.Empty;
		}

		/// <summary>
		/// Change master password
		/// </summary>
		/// <param name="newPassword">new master password</param>
		/// <param name="user">the current user</param>
		/// <returns>true - changed successfully, false - failed to change</returns>
		public bool ChangeMasterPassword(string newPassword, PassXYZLib.User user)
		{
			CompositeKey cmpKey = new CompositeKey();

			cmpKey.AddUserKey(new KcpPassword(newPassword));

			if (user.IsDeviceLockEnabled)
			{
				try
				{
					PassXYZ.Utils.Settings.DefaultFolder = PxDataFile.KeyFilePath;
					var pxKeyProvider = new PassXYZ.Services.PxKeyProvider(user.Username, false);
					if (pxKeyProvider.IsInitialized)
					{
						KeyProviderQueryContext ctxKP = new KeyProviderQueryContext(new IOConnectionInfo(), false, false);
						byte[] pbProvKey = pxKeyProvider.GetKey(ctxKP);
						cmpKey.AddUserKey(new KcpCustomKey(pxKeyProvider.Name, pbProvKey, true));
					}
					else
					{
						throw new KeePassLib.Keys.InvalidCompositeKeyException();
					}
				}
				catch (PassXYZ.Services.InvalidDeviceLockException ex)
				{
					try { cmpKey.AddUserKey(new KcpKeyFile(user.KeyFilePath)); }
					catch (Exception exFile)
					{
						Debug.Write($"{exFile} in {ex}");
						return false;
					}
				}
			}
			MasterKey = cmpKey;
			MasterKeyChanged = DateTime.UtcNow;

			return true;
		}

		/// <summary>
		/// Create a database with user information.
		/// If the device lock is enabled, we need to set DefaultFolder first.
		/// </summary>
		/// <param name="user">an instance of PassXYZLib.User</param>
		public void New(PassXYZLib.User user)
		{
			if (user == null) { Debug.Assert(false); throw new ArgumentNullException("PassXYZLib.User"); }

			if (user.IsDeviceLockEnabled)
			{
				if(!CreateKeyFile(user))
                {
					throw new KeePassLib.Keys.InvalidCompositeKeyException();
				}
			}

			IOConnectionInfo ioc = IOConnectionInfo.FromPath(user.Path);
			CompositeKey cmpKey = new CompositeKey();
			cmpKey.AddUserKey(new KcpPassword(user.Password));

			if (user.IsDeviceLockEnabled)
			{
				PassXYZ.Utils.Settings.DefaultFolder = PxDataFile.KeyFilePath;
				var pxKeyProvider = new PassXYZ.Services.PxKeyProvider(user.Username, false);
				if (pxKeyProvider.IsInitialized)
				{
					KeyProviderQueryContext ctxKP = new KeyProviderQueryContext(new IOConnectionInfo(), false, false);
					byte[] pbProvKey = pxKeyProvider.GetKey(ctxKP);
					cmpKey.AddUserKey(new KcpCustomKey(pxKeyProvider.Name, pbProvKey, true));
				}
				else
				{
					throw new KeePassLib.Keys.InvalidCompositeKeyException();
				}
			}
			New(ioc, cmpKey);

			// Set the database name to the current user name
			Name = user.Username;

			// Set the name of root group to the user name
			RootGroup.Name = user.Username;
		}

		/// <summary>
		/// Create a key file from an PxKeyProvider instance or from the system
		/// </summary>
		/// <param name="kp">a key provider instance. If it is null, the key file is created from the 
		/// current system.</param>
		/// <returns>true - created key file, false - failed to create key file.</returns>
		private bool CreateKeyFile(PassXYZLib.User user, PassXYZ.Services.PxKeyProvider kp = null)
		{
			PassXYZ.Utils.Settings.DefaultFolder = PxDataFile.KeyFilePath;
			PassXYZ.Utils.Settings.User.Username = user.Username;
			PassXYZ.Services.PxKeyProvider pxKeyProvider;
			if (kp == null)
			{
				pxKeyProvider = new PassXYZ.Services.PxKeyProvider();
				return pxKeyProvider.CreateKeyFile(true);
			}
			else
			{
				pxKeyProvider = kp;
				return pxKeyProvider.CreateKeyFile(false);
			}
		}

		/// <summary>
		/// Recreate a key file from a PxKeyData
		/// </summary>
		/// <param name="data">PxKeyData source</param>
		/// <param name="username">username inside PxKeyData source</param>
		/// <returns>true - created key file, false - failed to create key file.</returns>
		public bool CreateKeyFile(string data, string username)
		{
			if (data.StartsWith(PxDefs.PxKeyFile))
			{
				PassXYZ.Utils.Settings.DefaultFolder = PxDataFile.KeyFilePath;
				PassXYZ.Utils.Settings.User.Username = username;

				var msg = data.Substring(PxDefs.PxKeyFile.Length);
				PassXYZ.Services.PxKeyData keyData = PassXYZ.Services.PxKeyData.FromBase64String(msg);
				if (keyData != null)
				{
					PassXYZ.Services.PxKeyProvider pxKeyProvider = new PassXYZ.Services.PxKeyProvider(keyData);
					if (pxKeyProvider.KeyData.Username == username)
					{
						if (pxKeyProvider.CreateKeyFile(false))
						{
							return true;
						}
					}
				}
			}

			return false;
		}

		private void EnsureRecycleBin(ref PwGroup pgRecycleBin)
		{
			if (pgRecycleBin == this.RootGroup)
			{
				Debug.Assert(false);
				pgRecycleBin = null;
			}

			if (pgRecycleBin == null)
			{
				pgRecycleBin = new PwGroup(true, true, PxRes.RecycleBin,
					PwIcon.TrashBin);
				pgRecycleBin.EnableAutoType = false;
				pgRecycleBin.EnableSearching = false;
				pgRecycleBin.IsExpanded = false;
				this.RootGroup.AddGroup(pgRecycleBin, true);

				this.RecycleBinUuid = pgRecycleBin.Uuid;
			}
			else { Debug.Assert(pgRecycleBin.Uuid.Equals(this.RecycleBinUuid)); }
		}

		/// <summary>
		/// Remove RecycleBin before merge. RecycleBin should be kept locally and should not be merged.
		/// </summary>
		/// <param name="pwDb"></param>
		/// <returns></returns>
		private bool DeleteRecycleBin(PwDatabase pwDb)
		{
			if (pwDb == null) { return false; }

			PwGroup pgRecycleBin = pwDb.RootGroup.FindGroup(pwDb.RecycleBinUuid, true);

			if (pgRecycleBin != null)
			{
				pwDb.RootGroup.Groups.Remove(pgRecycleBin);
				pgRecycleBin.DeleteAllObjects(pwDb);
				PwDeletedObject pdo = new PwDeletedObject(pgRecycleBin.Uuid, DateTime.UtcNow);
				pwDb.DeletedObjects.Add(pdo);
				Debug.WriteLine("DeleteRecycleBin successfully.");
				return true;
			}
			else
			{
				Debug.WriteLine("DeleteRecycleBin failure.");
				return false;
			}
		}


		/// <summary>
		/// Find an entry or a group.
		/// </summary>
		/// <param name="path">The path of an entry or a group. If it is null, return the root group</param>
		public T FindByPath<T>(string path = "/") where T : new()
        {
			if (this.IsOpen)
			{ 
				if (path == null) throw new ArgumentNullException("path");

				string[] paths = path.Split('/');
				var lastSelectedGroup = this.CurrentGroup;

				if (path.StartsWith("/"))
				{
					//
					// if the path start with "/", we have to remove "/root" and
					// search from the root group
					//
					if(path.StartsWith("/" + this.RootGroup.Name))
					{
						lastSelectedGroup = this.RootGroup;
						paths = String.Join("/", paths, 2, paths.Length - 2).Split('/');
					}
					else 
					{
						return default(T);
					}
				}

				if(paths.Length > 0) 
				{
					if (typeof(T).Name == "PwGroup") 
					{
						foreach (var item in paths)
						{
							if (!String.IsNullOrEmpty(item))
							{
								if (lastSelectedGroup != null)
								{
									lastSelectedGroup = FindSubgroup(lastSelectedGroup, item);
								}
								else
									break;
							}
						}
						Debug.WriteLine($"Found group: {lastSelectedGroup}");
						return (T)Convert.ChangeType(lastSelectedGroup, typeof(T));
					}
					else if(typeof(T).Name == "PwEntry")
					{
						int i;
						string item;

						for (i = 0; i < paths.Length - 1; i++)
						{
							item = paths[i];
							if (!String.IsNullOrEmpty(item))
							{
								if (lastSelectedGroup != null)
								{
									lastSelectedGroup = FindSubgroup(lastSelectedGroup, item);
								}
								else
									break;
							}
						}
						if(lastSelectedGroup != null) 
						{
							var entry = FindEntry(lastSelectedGroup, paths[paths.Length - 1]);
							Debug.WriteLine($"Found entry: {entry}");
							return (T)Convert.ChangeType(entry, typeof(T));
						}
					}
				}
			}

			return default(T);

			PwEntry FindEntry(PwGroup group, string name) 
			{
				if (group == null) throw new ArgumentNullException("group");
				if (name == null) throw new ArgumentNullException("name");

				foreach (var entry in group.Entries)
                {
					if(entry.Strings.ReadSafe("Title") == name) { return entry; }
                }
				return null;
			}

			PwGroup FindSubgroup(PwGroup group, string name)
			{
				if (group == null) throw new ArgumentNullException("group");
				if (name == null) throw new ArgumentNullException("name");

				if(name == "..") 
				{
					if (this.RootGroup.Uuid != group.Uuid) { return group.ParentGroup; }
					else { return null; }
				}

				foreach (var gp in group.Groups) 
				{ 
					if(gp.Name == name) 
					{ return gp; }
				}

				return null;
			}
		}


		/// <summary>
		/// Find an entry by Uuid
		/// </summary>
		/// <param name="id">The entry uuid</param>	
		/// <returns>Entry found or return <c>null</c> if the entry cannot be found</returns>
		public PwEntry? FindEntryById(string id)
		{
			PwEntry? targetEntry = null;

			EntryHandler eh = delegate (PwEntry pe)
			{
				PwUuid pu = pe.Uuid;
				//string hexStr = pu.ToHexString();
				Guid guid = pu.GetGuid();
				string hexStr = guid.ToString();
				if (hexStr.Equals(id))
				{
					targetEntry = pe;
					return false;
				}

				return true;
			};

			RootGroup.TraverseTree(TraversalMethod.PreOrder, null, eh);

			return targetEntry;
		}

		/// <summary>
		/// Delete a group.
		/// </summary>
		/// <param name="pg">Group to be deleted. Must not be <c>null</c>.</param>
		/// <param name="permanent">Permanent delete or move to recycle bin</param>
		public void DeleteGroup(PwGroup pg, bool permanent = false)
		{
			if (pg == null) throw new ArgumentNullException("pg");

			PwGroup pgParent = pg.ParentGroup;
			if (pgParent == null)  throw new ArgumentNullException("pgParent"); // Can't remove virtual or root group

			PwGroup pgRecycleBin = RootGroup.FindGroup(RecycleBinUuid, true);

			bool bPermanent = false;
			if (RecycleBinEnabled == false) bPermanent = true;
			else if (permanent) bPermanent = true;
			else if (pgRecycleBin == null) { } // if we cannot find it, we will create it later
			else if (pg == pgRecycleBin) bPermanent = true;
			else if (pg.IsContainedIn(pgRecycleBin)) bPermanent = true;
			else if (pgRecycleBin.IsContainedIn(pg)) bPermanent = true;

			pgParent.Groups.Remove(pg);

			if (bPermanent)
			{
				pg.DeleteAllObjects(this);

				PwDeletedObject pdo = new PwDeletedObject(pg.Uuid, DateTime.UtcNow);
				DeletedObjects.Add(pdo);
			}
			else // Recycle
			{
				EnsureRecycleBin(ref pgRecycleBin);

				try { pgRecycleBin.AddGroup(pg, true, true); }
				catch (Exception)
				{
					if (pgRecycleBin.Groups.IndexOf(pg) < 0)
						pgParent.AddGroup(pg, true, true); // Undo removal
				}

				pg.Touch(false);
			}
		}


		/// <summary>
		/// Delete an entry.
		/// </summary>
		/// <param name="pe">The entry to be deleted. Must not be <c>null</c>.</param>	
        /// <param name="permanent">Permanent delete or move to recycle bin</param>
		public void DeleteEntry(PwEntry pe, bool permanent = false)
        {
			if (pe == null) throw new ArgumentNullException("pe");

			PwGroup pgRecycleBin = RootGroup.FindGroup(RecycleBinUuid, true);

			PwGroup pgParent = pe.ParentGroup;
			if (pgParent == null) return; // Can't remove

			pgParent.Entries.Remove(pe);

			bool bPermanent = false;
			if (RecycleBinEnabled == false) bPermanent = true;
			else if (permanent) bPermanent = true;
			else if (pgRecycleBin == null) { } // if we cannot find it, we will create it later
			else if (pgParent == pgRecycleBin) bPermanent = true;
			else if (pgParent.IsContainedIn(pgRecycleBin)) bPermanent = true;

			DateTime dtNow = DateTime.UtcNow;
			if (bPermanent)
			{
				PwDeletedObject pdo = new PwDeletedObject(pe.Uuid, dtNow);
				DeletedObjects.Add(pdo);
			}
			else // Recycle
			{
				EnsureRecycleBin(ref pgRecycleBin);

				pgRecycleBin.AddEntry(pe, true, true);
				pe.Touch(false);
			}
		}

		/// <summary>
		/// Check whether the source group is the parent of destination group
		/// </summary>
		/// <param name="srcGroup">The entry to be moved. Must not be <c>null</c>.</param>	
		/// <param name="dstGroup">New group for the entry</param>
		/// <returns>Success or failure.</returns>
		public bool IsParentGroup(PwGroup srcGroup, PwGroup dstGroup)
		{
			if (srcGroup == null) throw new ArgumentNullException("srcGroup");
			if (dstGroup == null) throw new ArgumentNullException("dstGroup");

			// If the source group is the root group, return true.
			if (srcGroup.Uuid == this.RootGroup.Uuid) return true;

			PwGroup group = dstGroup.ParentGroup;
			while (this.RootGroup.Uuid != group.Uuid)
			{
				if (group.Uuid == srcGroup.Uuid) return true;
				group = group.ParentGroup;
			}
			return false;
		}

		/// <summary>
		/// Move an entry to a new location.
		/// </summary>
		/// <param name="pe">The entry to be moved. Must not be <c>null</c>.</param>	
		/// <param name="group">New group for the entry</param>
		/// <returns>Success or failure.</returns>
		public bool MoveEntry(PwEntry pe, PwGroup group)
        {
			if (pe == null) throw new ArgumentNullException("pe");
			if (group == null) throw new ArgumentNullException("group");

			PwGroup pgParent = pe.ParentGroup;
			if (pgParent == group) return false;

			if (pgParent != null) // Remove from parent
			{
				if (!pgParent.Entries.Remove(pe)) { Debug.Assert(false); }
			}
			group.AddEntry(pe, true, true);
			return true;
		}

		/// <summary>
		/// Move a group to a new location.
		/// The source group cannot be the parent of destination group
		/// </summary>
		/// <param name="srcGroup">The entry to be moved. Must not be <c>null</c>.</param>	
		/// <param name="dstGroup">New group for the entry</param>
		/// <returns>Success or failure.</returns>
		public bool MoveGroup(PwGroup srcGroup, PwGroup dstGroup)
		{
			if (srcGroup == null) throw new ArgumentNullException("srcGroup");
			if (dstGroup == null) throw new ArgumentNullException("dstGroup");

			if (srcGroup == dstGroup) return false;

			PwGroup pgParent = srcGroup.ParentGroup;
			if (pgParent == dstGroup) return false;

			if (IsParentGroup(srcGroup, dstGroup)) return false;

			if (pgParent != null) // Remove from parent
			{
				if (!pgParent.Groups.Remove(srcGroup)) { Debug.Assert(false); }
			}
			dstGroup.AddGroup(srcGroup, true, true);
			return true;
		}

		/// <summary>
		/// Find a list of entries with a defined property, such as OTP Url
		/// The customized properties are stored in CustomData per PxDefs, such as PxCustomDataOtpUrl
		/// </summary>
		/// <param name="name">The property name. Must not be <c>null</c>.</param>	
		/// <returns>a list of entries</returns>
		public IEnumerable<PwEntry> GetEntryListByProperty(string name)
		{
			if (name == null) { Debug.Assert(false); throw new ArgumentNullException("name"); }

			List<PwEntry> resultsList = new List<PwEntry>();

			LinkedList<PwGroup> flatGroupList = RootGroup.GetFlatGroupList();

			foreach (PwEntry entry in RootGroup.Entries)
			{
				if (entry.CustomData != null && entry.CustomData.Exists(name))
				{
					if (!string.IsNullOrWhiteSpace(entry.CustomData.Get(name)))
					{
						resultsList.Add(entry);
					}
				}
			}

			foreach (PwEntry entry in PwGroup.GetFlatEntryList(flatGroupList))
			{
				if (entry.CustomData != null && entry.CustomData.Exists(name))
				{
					if (!string.IsNullOrWhiteSpace(entry.CustomData.Get(name)))
					{
						resultsList.Add(entry);
					}
				}
			}
			return resultsList;
		}

		/// <summary>
		/// Retrieve a list of entries with OTP
		/// </summary>
		/// <returns>a list of entries with OTP Url</returns>
		public IEnumerable<PwEntry> GetOtpEntryList()
		{
			return GetEntryListByProperty(PxDefs.PxCustomDataOtpUrl);
		}

		public IEnumerable<PwEntry> GetAllEntries() 
		{
			List<PwEntry> resultsList = new List<PwEntry>();
			LinkedList<PwGroup> flatGroupList = RootGroup.GetFlatGroupList();

			foreach (PwEntry entry in RootGroup.Entries)
			{
				resultsList.Add(entry);
			}

			foreach (PwEntry entry in PwGroup.GetFlatEntryList(flatGroupList))
			{
				resultsList.Add(entry);
			}
			return resultsList;
		}

		/// <summary>
		/// Search entries using a keyword. If the keyword is null or empty, 
		/// a list of entries by LastModificationTime will be returned.
		/// </summary>
		/// <param name="strSearch">string to be searched</param>
		/// <param name="itemGroup">within this group to be searched</param>
		/// <returns>list of result</returns>
		public IEnumerable<Item> SearchEntries(string strSearch, Item itemGroup = null)
		{
			if (string.IsNullOrEmpty(strSearch)) 
			{
				var entries = GetAllEntries();
				// descending or ascending
				IEnumerable<PwEntry> entriesByLastModificationTime =
					from e in entries
					orderby e.LastModificationTime descending
					select e;
				return entriesByLastModificationTime;
			}

			List<Item> resultsList = new List<Item>();
			string strGroupName = " (\"" + strSearch + "\") ";
            PwGroup pg = new PwGroup(true, true, strGroupName, PwIcon.EMailSearch)
            {
                IsVirtual = true
            };

            SearchParameters sp = new SearchParameters();

			if (strSearch.StartsWith(@"//") && strSearch.EndsWith(@"//") &&
				(strSearch.Length > 4))
			{
				string strRegex = strSearch.Substring(2, strSearch.Length - 4);

				try // Validate regular expression
				{
					Regex rx = new Regex(strRegex, RegexOptions.IgnoreCase);
					rx.IsMatch("ABCD");
				}
				catch (Exception exReg)
				{
					Debug.WriteLine($"SearchEntriesAsync: Exception={exReg.Message}.");
					return resultsList;
				}

				sp.SearchString = strRegex;
				sp.RegularExpression = true;
			}
			else sp.SearchString = strSearch;

			sp.SearchInTitles = sp.SearchInUserNames =
				sp.SearchInUrls = sp.SearchInNotes = sp.SearchInOther =
				sp.SearchInUuids = sp.SearchInGroupNames = sp.SearchInTags = true;
			sp.SearchInPasswords = false;
			sp.RespectEntrySearchingDisabled = true;
			sp.ExcludeExpired = false;

			// Search in root group by default
			PwGroup pgRoot = RootGroup;

			// If a search group is specified, try it.
			if (itemGroup != null)
			{
				if(itemGroup.IsGroup) { pgRoot = itemGroup as PwGroup; }
			}
			pgRoot.SearchEntries(sp, pg.Entries);

			foreach (PwEntry entry in pg.Entries)
			{
				resultsList.Add(entry);
			}
			return resultsList;
		}

		public bool Merge(string path, PwMergeMethod mm)
		{
			var pwImp = new PwDatabase();
			var ioInfo = IOConnectionInfo.FromPath(path);

			var compositeKey = MasterKey;

			KPCLibLogger swLogger = new KPCLibLogger();
			try
			{
				swLogger.StartLogging("Merge: Opening database ...", true);
				pwImp.Open(ioInfo, compositeKey, swLogger);
				swLogger.EndLogging();
			}
			catch (Exception e)
			{
				Debug.WriteLine($"$Failed to open database: {e.Message}.");
				return false;
			}

			// We only merge, if these are the same database with different versions.
			if (RootGroup.EqualsGroup(pwImp.RootGroup, (PwCompareOptions.IgnoreLastBackup | 
				PwCompareOptions.IgnoreHistory | PwCompareOptions.IgnoreParentGroup | 
				PwCompareOptions.IgnoreTimes | PwCompareOptions.PropertiesOnly), MemProtCmpMode.None))
			{
				Debug.WriteLine($"Merge: Root group are the same. Merge method is {mm}.");
			}
			else
			{
				Debug.WriteLine($"Merge: Root group are different DBase={RootGroup}, pwImp={pwImp.RootGroup}.");
				pwImp.Close();
				return false;
			}

			try
			{
				// Need to remove RecycleBin first before merge.
				DeleteRecycleBin(pwImp);

				MergeIn(pwImp, mm, swLogger);
				DescriptionChanged = DateTime.UtcNow;
				Save(swLogger);
				pwImp.Close();
			}
			catch (Exception exMerge)
			{
				Debug.WriteLine($"Merge failed {exMerge}");
				return false;
			}
			return true;
		}

		// The end of PxDatabase
	}

	/// <summary>
	/// PasswordDb is a sub-class of PxDatabase. It is a singleton class.
	/// </summary>
	public sealed class PasswordDb : PxDatabase
	{
		private static PasswordDb instance = null;

		private PasswordDb() { }
		public static PasswordDb Instance 
		{ 
			get 
			{ 
				if(instance == null) 
				{
					instance = new PasswordDb();
				}
				return instance;
			}
		}

		public PwCustomIcon GetPwCustomIcon(PwUuid pwIconId)
		{
			if (pwIconId != PwUuid.Zero) 
			{
				int nIndex = GetCustomIconIndex(pwIconId);
				if (nIndex >= 0)
					return CustomIcons[nIndex];
				else { Debug.Assert(false); }
			}

			return null;
		}

		public enum PxCustomIconSize
		{
			Size = 32,
			Min = 96,
			Max = 216
		}

		/// <summary>
		/// GetCustomIcon - find a custom icon by name
		/// Please refer to the below implementation in PwDatabase.cs. We may move it to here.
		///       public Image GetCustomIcon(PwUuid pwIconId)
		/// </summary>
		/// <param name="name">The custom icon name. This can be supported by KeePass 2.48 or above.</param>	
		/// <returns>custom icon instance</returns>
		public PwCustomIcon GetCustomIcon(string name) 
		{
			int n = CustomIcons.Count;
			for (int i = 0; i < n; ++i)
			{
				PwCustomIcon ci = CustomIcons[i];
				if (ci.Name.Equals(name)) return ci;
			}

			return null;
		}

		/// <summary>
		/// Save SKBitmap as a custom icon and return the uuid.
		/// If the uuid is PwUuid.Zero, the icon is not saved due to error.
		/// </summary>
		/// <param name="img">SKBitmap image. Must not be <c>null</c>.</param>	
		/// <param name="name">The custom icon name. This can be supported by KeePass 2.48 or above.</param>	
		/// <returns>custom icon uuid</returns>
		public PwUuid SaveCustomIcon(SKBitmap img, string name = "") 
		{
			PwUuid uuid = PwUuid.Zero;

			if (img == null) { return PwUuid.Zero; }

			if ((img.Width != img.Height) || (img.Width < (int)PxCustomIconSize.Size))
			{
				return PwUuid.Zero;
			}

			// If the image is not at PxCustomIconSize.Min, we need to resize it.
			//if (img.Width != (int)PxCustomIconSize.Min)
			//{
			//	SKImageInfo resizeInfo = new SKImageInfo((int)PxCustomIconSize.Min, (int)PxCustomIconSize.Min);
			//	using (SKBitmap resizedSKBitmap = img.Resize(resizeInfo, SKFilterQuality.Medium))
			//	{
			//		img = resizedSKBitmap;
			//	}
			//}

			using (var image = SKImage.FromBitmap(img))
			{
				using (var png = image.Encode(SKEncodedImageFormat.Png, 100))
				{
					using (var ms = new MemoryStream())
					{
						png.SaveTo(ms);
                        PwCustomIcon pwci = new PwCustomIcon(new PwUuid(true), ms.ToArray())
                        {
                            Name = name
                        };
                        CustomIcons.Add(pwci);
						uuid = pwci.Uuid;
					}
				}
			}
			return uuid;
		}
	}
}
