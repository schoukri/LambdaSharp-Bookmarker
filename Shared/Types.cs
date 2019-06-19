using System.ComponentModel;
using Newtonsoft.Json;

namespace LambdaSharp.Challenge.Bookmarker.Shared {

    public class Bookmark {

        //--- Properties ---
        [JsonRequired]
        public string ID { get; set; }

        [JsonRequired]
        public string Url { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Title { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string Description { get; set; }

        [DefaultValue("")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate)]
        public string ImageUrl { get; set; }

    }
}
