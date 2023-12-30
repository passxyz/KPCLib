using System;
using System.Collections.Generic;
using System.Text;

namespace KPCLib
{
    public static class StringExt
    {
        public static string Truncate(this string variable, int Length)
        {
            if (string.IsNullOrEmpty(variable)) return variable;
            return variable.Length <= Length ? variable : variable.Substring(0, Length)+" ...";
        }
    }
}
