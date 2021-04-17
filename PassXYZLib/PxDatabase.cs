using System;
using System.IO;
using System.Diagnostics;
using System.Drawing;

using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;
using PassXYZLib.Resources;

namespace PassXYZLib
{
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
					return null;
				}
				else 
				{
					var group = CurrentGroup;
					string path = group.Name + "/";

					while (RootGroup.Uuid != group.Uuid)
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
				var pxKeyProvider = new PassXYZ.Services.PxKeyProvider(userName, false);
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
		/// Delete a group.
		/// </summary>
		/// <param name="pg">Group to be added. Must not be <c>null</c>.</param>
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
				catch (Exception ex)
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
	}
}
