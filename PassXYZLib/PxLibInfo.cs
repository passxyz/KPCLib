using System;
using System.Reflection;

namespace PassXYZLib
{
    public class PxLibInfo
    {
        public static Version? Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public static string? Name
        {
            get {return Assembly.GetExecutingAssembly().FullName; }
        }
    }
}
