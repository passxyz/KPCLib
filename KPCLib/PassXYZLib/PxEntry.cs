using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using PureOtp;

using KeePassLib;
using KeePassLib.Interfaces;

namespace PassXYZLib
{
    public class PxEntry : PwEntry, IDeepCloneable<PxEntry>
    {
        public PxEntry() : base()
        { }

        /// <summary>
        /// Construct a new, empty password entry. Member variables will be initialized
        /// to their default values.
        /// </summary>
        /// <param name="bCreateNewUuid">If <c>true</c>, a new UUID will be created
        /// for this entry. If <c>false</c>, the UUID is zero and you must set it
        /// manually later.</param>
        /// <param name="bSetTimes">If <c>true</c>, the creation, last modification
        /// and last access times will be set to the current system time.</param>
        public PxEntry(bool bCreateNewUuid, bool bSetTimes) : base(bCreateNewUuid, bSetTimes) { }

        public PxEntry(PwEntry entry) 
        {
            Uuid = entry.Uuid; // PwUuid is immutable
            ParentGroup = entry.ParentGroup;
            LocationChanged = entry.LocationChanged;

            Strings = entry.Strings.CloneDeep();
            Binaries = entry.Binaries.CloneDeep();
            AutoType = entry.AutoType.CloneDeep();
            History = entry.History.CloneDeep();

            IconId = entry.IconId;
            CustomIconUuid = entry.CustomIconUuid;

            ForegroundColor = entry.ForegroundColor;
            BackgroundColor = entry.BackgroundColor;

            CreationTime = entry.CreationTime;
            LastModificationTime = entry.LastModificationTime;
            LastAccessTime = entry.LastAccessTime;
            ExpiryTime = entry.ExpiryTime;
            Expires = entry.Expires;
            UsageCount = entry.UsageCount;

            OverrideUrl = entry.OverrideUrl;

            Tags = new List<string>(entry.Tags);

            CustomData = entry.CustomData.CloneDeep();
        }

#if DEBUG
        // For display in debugger
        public override string ToString()
        {
            return ("PxEntry '" + Strings.ReadSafe(PwDefs.TitleField) + "'");
        }
#endif

        /// <summary>
        /// Clone the current entry. The returned entry is an exact value copy
        /// of the current entry (including UUID and parent group reference).
        /// All mutable members are cloned.
        /// </summary>
        /// <returns>Exact value clone. All references to mutable values changed.</returns>
        public new PxEntry CloneDeep()
        {
            PxEntry peNew = new PxEntry(false, false);

            peNew.Uuid = Uuid; // PwUuid is immutable
            peNew.ParentGroup = ParentGroup;
            peNew.LocationChanged = LocationChanged;

            peNew.Strings = Strings.CloneDeep();
            peNew.Binaries = Binaries.CloneDeep();
            peNew.AutoType = AutoType.CloneDeep();
            peNew.History = History.CloneDeep();

            peNew.IconId = IconId;
            peNew.CustomIconUuid = CustomIconUuid;

            peNew.ForegroundColor = ForegroundColor;
            peNew.BackgroundColor = BackgroundColor;

            peNew.CreationTime = CreationTime;
            peNew.LastModificationTime = LastModificationTime;
            peNew.LastAccessTime = LastAccessTime;
            peNew.ExpiryTime = ExpiryTime;
            peNew.Expires = Expires;
            peNew.UsageCount = UsageCount;

            peNew.OverrideUrl = OverrideUrl;

            peNew.Tags = new List<string>(Tags);

            peNew.CustomData = CustomData.CloneDeep();

            return peNew;
        }
    }
}
