using System;
using System.Collections.Generic;
using System.Text;

namespace PassXYZLib
{
    public class PxFieldValue
    {
        public string Value = string.Empty;
        public bool IsProtected = false;

        public PxFieldValue()
        {
            Value = string.Empty;
            IsProtected = false;
        }

        public PxFieldValue(string newValue, bool isProtected)
        {
            Value = newValue;
            IsProtected = isProtected;
        }
    }
}
