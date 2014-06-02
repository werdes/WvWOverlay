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
    }

    namespace API
    {
        public class matches_json
        {
            public DateTime retrieve_time { get; set; }
            public Dictionary<string, List<match>> region { get; set; }
        }

        public class match
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
            public string red_owner_id { get; set; }
            public string blue_owner_id { get; set; }
            public string green_owner_id { get; set; }
        }

        public class world
        {
            public short world_id { get; set; }
            public string name { get; set; }
            public string color { get; set; }
            public int score { get; set; }
            public short ppt { get; set; }
        }
    }
}
