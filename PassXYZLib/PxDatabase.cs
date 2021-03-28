using System;
using System.Diagnostics;
using System.Drawing;

using KeePassLib;
using PassXYZLib.Resources;

namespace PassXYZLib
{
    public class PxDatabase : PwDatabase
    {
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
				if(this.RootGroup.Uuid == this.LastSelectedGroup) 
					{ return this.RootGroup; }
				else
					return this.RootGroup.FindGroup(this.LastSelectedGroup, true); 
			}
			set { this.LastSelectedGroup = value.Uuid; }
		}

		public string CurrentPath 
		{ 
			get {
				var group = this.CurrentGroup;
				string path = this.CurrentGroup.Name + "/";

				while (this.RootGroup.Uuid != group.Uuid)
				{
					group = group.ParentGroup;
					path = group.Name + "/" + path;
				}

				return path;
			}
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
	}
}
