using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace WvWOverlay.Model
{
    namespace XML
    {
        [Serializable()]
        public class Region
        {
            public short RegionIdentifier { get; set; }
            public string Name { get; set; }
            public string Icon { get; set; }
            public string Slug { get; set; }
        }

        [Serializable()]
        public class Profession
        {
            public short Id { get; set; }
            public string Name { get; set; }
        }

        [Serializable()]
        public class Map
        {
            public short MapID { get; set; }
            public string Identifier { get; set; }
            public string Title { get; set; }
            public short Gw2StatsID { get; set; }
            public string Color { get; set; }
        }

        [Serializable()]
        public class Objective
        {
            public class Coordinate
            {
                public int X { get; set; }
                public int Y { get; set; }
            }

            public enum ObjectiveType
            {
                Castle = 35,
                Keep = 25,
                Tower = 10,
                Camp = 5,
                Ruin = 0
            }


            public short Id { get; set; }
            public ObjectiveType Type { get; set; }
            public string Name { get; set; }
            public Coordinate Coordinates { get; set; }
            public short MapId { get; set; }
        }
    }

    namespace API
    {
        public class matches_json
        {
            public DateTime retrieve_time { get; set; }
            public Dictionary<string, List<matches_match>> region { get; set; }
        }

        public class matches_match
        {
            public bloodlust bloodlust { get; set; }
            public DateTime start_date { get; set; }
            public DateTime end_date { get; set; }
            public DateTime last_update { get; set; }
            public string match_id { get; set; }
            public string unique_id { get; set; }
            public List<world> worlds { get; set; }
        }

        public class bloodlust
        {
            public DateTime update_time { get; set; }
            public string match_id { get; set; }
            public short red_owner_id { get; set; }
            public short blue_owner_id { get; set; }
            public short green_owner_id { get; set; }
        }

        public class world
        {
            public short world_id { get; set; }
            public string name { get; set; }
            public string color { get; set; }
            //public int score { get; set; }

            //String, da bei 0 ->null
            public string ppt { get; set; }
        }

        public class match
        {
            public DateTime retrieve_time { get; set; }
            public string type { get; set; }
            public string match_id { get; set; }
            public bloodlust bloodlust { get; set; }
            public List<map> maps { get; set; }
        }

        public class map
        {
            public string name { get; set; }
            public short map_id { get; set; }
            public short map_owner_id { get; set; }
            public string map_owner_name { get; set; }
            public Dictionary<string, objective> objectives { get; set; }
            public List<objective> objectives_list { get; set; }
        }

        public class objective
        {
            private TimeSpan held;

            public int id { get; set; }
            public string name { get; set; }
            public string cardinal { get; set; }
            public short points { get; set; }
            public DateTime capture_time { get; set; }

            public TimeSpan ri_remaining
            {
                get;
                set;
            }
            public TimeSpan time_held { get; set; }
            public owner current_owner { get; set; }
            public owner previous_owner { get; set; }
            public guild current_guild { get; set; }

            public void SetTimes(DateTime oTime)
            {
                ri_remaining = new TimeSpan();
                TimeSpan oTemp;
                DateTime oTemp2;
                oTemp = (oTime.ToUniversalTime() - capture_time);
                time_held = oTemp;
                if (oTemp.TotalMilliseconds < 1000 * 60 * 5)
                {
                    oTemp2 = new DateTime(1, 1, 1, 0, 5, 0) - oTemp;
                    ri_remaining = new TimeSpan(0, oTemp2.Minute, oTemp2.Second);
                }
            }
        }

        public class owner
        {
            public short world_id { get; set; }
            public string name { get; set; }
            public string color { get; set; }
        }

        public class guild
        {
            public string id { get; set; }
            public string name { get; set; }
        }
    }

    public class mumble_identity
    {
        public string name { get; set; }
        public short profession { get; set; }
        public int map_id { get; set; }
        public long world_id { get; set; }
        public short team_color_id { get; set; }
        public bool commander { get; set; }
    }

    public class SiegeTimer
    {
        public Model.XML.Objective XMLObjective { get; set; }
        public Model.API.objective APIObjective { get; set; }
        public DateTime End { get; set; }

        [XmlIgnore]
        public System.Windows.Threading.DispatcherTimer Timer { get; set; }

        public Model.XML.Map Map { get; set; }

    }
}
