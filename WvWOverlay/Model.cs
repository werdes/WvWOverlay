using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            public int score { get; set; }
            public short ppt { get; set; }
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
            private TimeSpan remaining;
            private TimeSpan held;

            public int id { get; set; }
            public string name { get; set; }
            public string cardinal { get; set; }
            public short points { get; set; }
            public DateTime capture_time { get; set; }
            public TimeSpan ri_remaining
            {
                get { return remaining; }
                set
                {
                    string cIn = value.ToString();
                    remaining = TimeSpan.ParseExact(cIn, "mm\\:ss\\:hh", System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat);

                }
            }
            public TimeSpan time_held { get; set; }
            public owner current_owner { get; set; }
            public owner previous_owner { get; set; }
            public guild current_guild { get; set; }
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
}
