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
}
