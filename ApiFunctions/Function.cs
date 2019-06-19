using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
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
            var id = Guid.NewGuid().ToString("D");
            var bookmark = new Bookmark {
                ID = id,
                Url = request.Url,
            };
            LogInfo($"Add Bookmark: ID={bookmark.ID}, Url={bookmark.Url}");
            _table.PutItemAsync(Document.FromJson(SerializeJson(bookmark)));
            return new AddBookmarkResponse{
                ID = bookmark.ID
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

        public GetBookmarkResponse GetBookmark(string id) {
            LogInfo($"Get Bookmark: ID={id}");
            var task = Task.Run<Document>(async () => await _table.DeleteItemAsync(id));
            var document = task.Result;
            if (document == null) throw AbortNotFound("Bookmark not found");
            var bookmark = DeserializeJson<Bookmark>(document.ToJson());
            return new GetBookmarkResponse{
                ID = bookmark.ID,
                Url = bookmark.Url,
                Title = bookmark.Title,
                Description = bookmark.Description,
                ImageUrl = bookmark.ImageUrl,
            };
        }

        public DeleteBookmarkResponse DeleteBookmark(string id) {
            LogInfo($"Delete Bookmark: ID={id}");
            var task = Task.Run<Document>(async () => await _table.DeleteItemAsync(id));
            return new DeleteBookmarkResponse{
                Deleted = true,
            };
         }

        private async Task<T> GetRecord<T>(string id) {
            var record = await _table.GetItemAsync(id);
            return (record == null)
                ? default(T)
                : DeserializeJson<T>(record.ToJson());
        }
    }
}
