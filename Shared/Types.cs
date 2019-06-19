using Newtonsoft.Json;

namespace LambdaSharp.Challenge.Bookmarker.Shared {

    public class Bookmark {

        //--- Properties ---
        [JsonRequired]
        public string ID { get; set; }

        [JsonRequired]
        public string Url { get; set; }

        [JsonIgnore]
        public string Title { get; set; }

        [JsonIgnore]
        public string Description { get; set; }

        [JsonIgnore]
        public string ImageUrl { get; set; }

    }
}
