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
    }
}
