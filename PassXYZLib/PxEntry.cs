using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using PureOtp;

using KeePassLib;
using KeePassLib.Interfaces;

namespace PassXYZLib
{
    public class PxEntry : PwEntry
    {
        public PxEntry(bool bCreateNewUuid, bool bSetTimes) : base(bCreateNewUuid, bSetTimes) { }
        public PxEntry() : base() { }
    }
}
