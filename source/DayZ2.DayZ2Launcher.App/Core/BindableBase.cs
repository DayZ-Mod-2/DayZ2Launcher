using System.ComponentModel;
using System.Runtime.Serialization;

namespace DayZ2.DayZ2Launcher.App.Core
{
    [DataContract]
    public class BindableBase : INotifyPropertyChanged
    {
        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void PropertyHasChanged(params string[] names)
        {
            if (PropertyChanged != null)
            {
                foreach (string name in names)
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        #endregion
    }
}