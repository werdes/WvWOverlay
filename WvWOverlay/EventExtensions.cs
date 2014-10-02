using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WvWOverlay
{
    public static class EventExtensions
    {
        static public void RaiseEvent(this EventHandler @event, object sender, EventArgs e)
        {
            if (@event != null)
                @event(sender, e);
        }

        static public void RaiseEvent<T>(this EventHandler<T> @event, object sender, T e)
            where T : EventArgs
        {
            if (@event != null)
                @event(sender, e);
        }
    }

    public class RegionSelectedEventArgs : EventArgs
    {
        public RegionSelectedEventArgs(Model.XML.Region oRegion)
        {
            Region = oRegion;
        }

        public Model.XML.Region Region { get; set; }
    }

    public class MatchSelectedEventArgs : EventArgs
    {
        public MatchSelectedEventArgs(Model.API.matches_match oMatch, Model.API.world oWorld)
        {
            Match = oMatch;
            World = oWorld;
        }

        public Model.API.matches_match Match { get; set; }
        public Model.API.world World { get; set; }

    }

    public class ObjectiveItemDoubleclickEventArgs : EventArgs
    {
        public ObjectiveItemDoubleclickEventArgs(Model.API.objective oObjective, TimeSpan oRemaining, TimeSpan oTimeHeld)
        {
            Objective = oObjective;
            Ri_Remaining = oRemaining;
            Time_Held = oTimeHeld;
        }

        public TimeSpan Ri_Remaining { get; set; }
        public TimeSpan Time_Held { get; set; }
        public Model.API.objective Objective { get; set; }
    }

    public class ObjectiveSiegeTimeSelectedEventArgs : EventArgs
    {
        public int Minutes;

        public ObjectiveSiegeTimeSelectedEventArgs(int nMinutes)
        {
            Minutes = nMinutes;
        }
    }

    public class SiegeTimerEventArgs : EventArgs
    {
        public Model.SiegeTimer SiegeTimer { get; set; }

        public SiegeTimerEventArgs(Model.SiegeTimer oSiegeTimer)
        {
            SiegeTimer = oSiegeTimer;
        }
    }
}
