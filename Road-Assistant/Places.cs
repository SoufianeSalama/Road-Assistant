using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Road_Assistant
{
    class Places
    {
        public string Id { get; set; }

        [JsonProperty(PropertyName = "soort")]
        public string Soort { get; set; }

        [JsonProperty(PropertyName = "latitude")]
        public string Latitude { get; set; }

        [JsonProperty(PropertyName = "longitude")]
        public string Longitude { get; set; }
    }
}
