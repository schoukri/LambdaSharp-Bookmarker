using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using LambdaSharp;
using LambdaSharp.ApiGateway;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2.DocumentModel;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Challenge.Bookmarker.ApiFunctions {

    public class Function : ALambdaApiGatewayFunction {

        //--- Fields ---
        private IAmazonDynamoDB _dynamoDbClient;
        private Table _table;

        private List<Bookmark> _bookmarks = new List<Bookmark>();

        //--- Methods ---
        // public override Task InitializeAsync(LambdaConfig config)
        //     => Task.CompletedTask;


        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {

            // initialize AWS clients
            _dynamoDbClient = new AmazonDynamoDBClient();

            // read settings
            _table = Table.LoadTable(_dynamoDbClient, config.ReadDynamoDBTableName("BookmarksTable"));
        }

        public AddBookmarkResponse AddBookmark(AddBookmarkRequest request) {
            var bookmark = new Bookmark {
                ID = request.Url,
                Url = request.Url
            };

            // _table.PutItemAsync(Document.FromJson(SerializeJson(bookmark)));

            return new AddBookmarkResponse{
                ID = bookmark.Url,
            };
         }

        private async Task<T> GetRecord<T>(string id) {
            var record = await _table.GetItemAsync(id);
            return (record == null)
                ? default(T)
                : DeserializeJson<T>(record.ToJson());
        }

        private async Task PutRecord<T>(T record)
            => await _table.PutItemAsync(Document.FromJson(SerializeJson(record)));


        // public GetBookmarksResponse GetBookmarks(string contains = null, int offset = 0, int limit = 10) { ... }

        // public GetBookmarkResponse GetBookmark(string id) { ... }

        // public DeleteBookmarkResponse DeleteBookmark(string id) { ... }
    }
}
