using System;
using System.Collections.Generic;
using System.Text;

using KeePassLib;
using KeePassLib.Interfaces;

namespace PassXYZLib
{
    public class PxGroup: PwGroup
    {
        public PxGroup(bool bCreateNewUuid, bool bSetTimes) : base(bCreateNewUuid, bSetTimes) { }

        public PxGroup() : base() { }

		/// <summary>
		/// Create a PxGroup instance from a JSON string
		/// </summary>
		/// <param name="str">JSON data</param>
		/// <param name="password">Password of PwGroup</param>
		public PxGroup(string str, string? password = null, bool isJson = true) : base(true, true)
		{
			if (isJson) 
			{
				PxPlainFields fields = new PxPlainFields(str, password);

				if (fields.Strings.Count > 0)
				{
					PxFieldValue data;
					fields.Strings.TryGetValue(PwDefs.TitleField, out data);
					Name = data.Value;
					fields.Strings.TryGetValue(PwDefs.NotesField, out data);
					Notes = data.Value;
				}
			}
			else
			{
				// If the first parameter is not a JSON string, we just set the name and description.
				Name = str;
				Notes = password;
			}
		}

	}

    public static class PwGroupEx 
    {
        public static List<Item> GetItems(this PwGroup group)
        {
            List<Item> itemList = new List<Item>();

            foreach (PwEntry entry in group.Entries)
            {
                entry.SetIcon();
                itemList.Add((Item)entry);
            }

            foreach (PwGroup gp in group.Groups)
            {
                gp.SetIcon();
                itemList.Add((Item)gp);
            }
            return itemList;
        }

		/// <summary>
		/// Find a group by Id.
		/// </summary>
		/// <param name="id">ID identifying the group the caller is looking for.</param>
		/// <param name="bSearchRecursive">If <c>true</c>, the search is recursive.</param>
		/// <returns>Returns reference to found group, otherwise <c>null</c>.</returns>
		public static PwGroup FindGroup(this PwGroup group, string id, bool bSearchRecursive)
		{
			if (group.Id == id) return group;

			if (bSearchRecursive)
			{
				PwGroup pgRec;
				foreach (PwGroup pg in group.Groups)
				{
					pgRec = pg.FindGroup(id, true);
					if (pgRec != null) return pgRec;
				}
			}
			else // Not recursive
			{
				foreach (PwGroup pg in group.Groups)
				{
					if (pg.Id == id)
						return pg;
				}
			}

			return null;
		}

		public static PxPlainFields GetPlainFields(this PwGroup group)
		{
			return new PxPlainFields(group);
		}
	}
}
