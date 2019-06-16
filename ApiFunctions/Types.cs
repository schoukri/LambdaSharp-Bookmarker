using System.Collections.Generic;
using Amazon.Lambda.Core;
using Newtonsoft.Json;

namespace LambdaSharp.Challenge.Bookmarker.ApiFunctions {

    public class Bookmark {

        //--- Properties ---
        [JsonRequired]
        public string ID { get; set; }

        [JsonRequired]
        public string Url { get; set; }
    }

    public class AddBookmarkRequest {

        //--- Properties ---
        [JsonRequired]
        public string Url { get; set; }
    }

    public class AddBookmarkResponse {

        //--- Properties ---
        [JsonRequired]
        public string ID { get; set; }
    }

    public class GetBookmarksResponse {

        //--- Properties ---
        [JsonRequired]
        public List<Bookmark> Bookmarks = new List<Bookmark>();
    }

    public class GetBookmarkResponse {

        //--- Properties ---
        [JsonRequired]
        public string ID { get; set; }

        [JsonRequired]
        public string Url { get; set; }
    }

    public class DeleteBookmarkResponse {

        //--- Properties ---
        [JsonRequired]
        public bool Deleted;
    }
}
