using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using KeePassLib;

namespace PassXYZLib
{
    /// <summary>
    /// Extension methods of PwUuid.
    /// </summary>
    public static class PxUuid
    {
        public static Guid GetGuid(this PwUuid uuid)
        {
            return new Guid(uuid.UuidBytes);
        }
    }
}
