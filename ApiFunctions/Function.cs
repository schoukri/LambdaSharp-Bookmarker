using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.APIGatewayEvents;
using OpenGraphNet;
using LambdaSharp.ApiGateway;
using LambdaSharp.Challenge.Bookmarker.Shared;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Challenge.Bookmarker.ApiFunctions {

    public class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private IAmazonDynamoDB _dynamoDbClient;
        private Table _table;

        public override async Task InitializeAsync(LambdaConfig config) {
            // initialize AWS clients
            _dynamoDbClient = new AmazonDynamoDBClient();

            // read settings
            _table = Table.LoadTable(_dynamoDbClient, config.ReadDynamoDBTableName("BookmarksTable"));
        }

        public AddBookmarkResponse AddBookmark(AddBookmarkRequest request) {
            LogInfo($"Add Bookmark:  Url={request.Url}");
            Uri url;
            if (!Uri.TryCreate(request.Url, UriKind.Absolute, out url)) AbortBadRequest("Url Not Valid");

            // Level 1: generate a short ID that is still unique
            var id = Guid.NewGuid().ToString("D");
            var bookmark = new Bookmark {
                ID = id,
                Url = url,
            };
            _table.PutItemAsync(Document.FromJson(SerializeJson(bookmark)));
            return new AddBookmarkResponse{
                ID = bookmark.ID
            };
         }
        public GetBookmarkResponse GetBookmark(string id) {
            LogInfo($"Get Bookmark: ID={id}");
            var bookmark = RetrieveBookmark(id) ?? throw AbortNotFound("Bookmark not found");
            return new GetBookmarkResponse{
                ID = bookmark.ID,
                Url = bookmark.Url,
                Title = bookmark.Title,
                Description = bookmark.Description,
                ImageUrl = bookmark.ImageUrl,
            };
        }

        public GetBookmarksResponse GetBookmarks(string contains = null, int offset = 0, int limit = 10) {
            var search = _table.Scan(new ScanFilter());
            var bookmarks = new List<Bookmark>();
            do {
                var task = Task.Run<List<Document>>(async () => await search.GetNextSetAsync());
                foreach (var document in task.Result)
                    bookmarks.Add(DeserializeJson<Bookmark>(document.ToJson()));
            } while (!search.IsDone);
            return new GetBookmarksResponse{
                Bookmarks = bookmarks
            };
        }

        public DeleteBookmarkResponse DeleteBookmark(string id) {
            LogInfo($"Delete Bookmark: ID={id}");
            var task = Task.Run<Document>(async () => await _table.DeleteItemAsync(id));
            return new DeleteBookmarkResponse{
                Deleted = true,
            };
        }

        public APIGatewayProxyResponse GetBookmarkPreview(string id) {
            LogInfo($"Get Bookmark Preview: ID={id}");
            var bookmark = RetrieveBookmark(id) ?? throw AbortNotFound("Bookmark not found");
            var url = bookmark.Url.ToString();
            var graph = OpenGraph.MakeGraph(
                siteName: "Bookmark.er",
                type: "website",
                title: bookmark.Title,
                image: bookmark.ImageUrl,
                url: url,
                description: bookmark.Description
            );

            var html = $@"<html>
<head prefix=""{graph.HeadPrefixAttributeValue}"">
    <title>{WebUtility.HtmlEncode(bookmark.Title)}</title>
    {graph.ToString()}
</head>
<body style=""font-family: Helvetica, Arial, sans-serif;"">
    <img style=""float: left; margin: 0px 15px 15px 0px;"" src=""{WebUtility.HtmlEncode(bookmark.ImageUrl)}"" width=150 height=150 />
    <h1>{WebUtility.HtmlEncode(bookmark.Title)}</h1>
    <p>{WebUtility.HtmlEncode(bookmark.Description)}</p>
    <p><a href=""{WebUtility.HtmlEncode(url)}"">{WebUtility.HtmlEncode(url)}</a></p>
</body>
</html>
";
            return new APIGatewayProxyResponse{
                Body = html,
                StatusCode = 200,
                Headers = new Dictionary<string,string>(){
                    {"Content-Type", "text/html"},
                },
            };
        }

        public APIGatewayProxyResponse RedirectToBookmark(string id) {
            LogInfo($"Redirect To Bookmark: ID={id}");
            var bookmark = RetrieveBookmark(id) ?? throw AbortNotFound("Bookmark not found");
            return new APIGatewayProxyResponse{
                StatusCode = 301,
                Headers = new Dictionary<string,string>(){
                    {"Location", bookmark.Url.ToString()},
                },
            };
        }

        private Bookmark RetrieveBookmark(string id) {
            var task = Task.Run<Document>(async () => await _table.GetItemAsync(id));
            var document = task.Result;
            return (document == null)
                ? null
                : DeserializeJson<Bookmark>(document.ToJson());
        }
    }
}
