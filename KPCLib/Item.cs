using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace KPCLib
{
    /// <summary>
    /// A class representing an item. An item can be an entry or a group.
    /// Each item has a unique ID (UUID).
    /// </summary>
    public abstract class Item : INotifyPropertyChanged
    {
        public abstract DateTime LastModificationTime { get; set; }

        public abstract string Name { get; set; }

        public abstract string Description { get;}

        public abstract string Notes { get; set; }

        public abstract bool IsGroup { get; }

        // TODO: do we need this ?
        // public abstract PwUuid CustomIconUuid { get; set; }

        public abstract string Id { get; }

        /// <summary>
        /// UUID of this item.
        /// </summary>
        // TODO: need to decouple this
        //public PwUuid Uuid
        //{
        //    get { return m_uuid; }
        //    set
        //    {
        //        if (value == null) { Debug.Assert(false); throw new ArgumentNullException("value"); }
        //        m_uuid = value;
        //    }
        //}

        virtual public Object ImgSource { get; set; }

        #region INotifyPropertyChanged
        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string propertyName = "",
            Action onChanged = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            onChanged?.Invoke();
            OnPropertyChanged(propertyName);
            return true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            var changed = PropertyChanged;
            if (changed == null)
                return;

            changed.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
