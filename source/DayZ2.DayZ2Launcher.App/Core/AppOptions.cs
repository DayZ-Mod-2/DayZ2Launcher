using System.Runtime.Serialization;

namespace DayZ2.DayZ2Launcher.App.Core
{
    [DataContract]
    public class AppOptions : BindableBase
    {
        [DataMember] private bool _lowPingRate;

        public bool LowPingRate
        {
            get { return _lowPingRate; }
            set
            {
                _lowPingRate = value;
                PropertyHasChanged("LowPingRate");
                UserSettings.Current.Save();
            }
        }
    }
}