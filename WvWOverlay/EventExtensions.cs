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
}
