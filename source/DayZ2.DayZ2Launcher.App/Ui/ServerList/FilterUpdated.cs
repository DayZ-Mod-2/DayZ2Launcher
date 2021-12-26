using System;
using DayZ2.DayZ2Launcher.App.Core;

namespace DayZ2.DayZ2Launcher.App.Ui.ServerList
{
    public class FilterUpdated
    {
        public FilterUpdated(Func<Server, bool> filter)
        {
            Filter = filter;
        }

        public Func<Server, bool> Filter { get; set; }
    }
}