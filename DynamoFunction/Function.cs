using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.Lambda.Serialization.Json;
using LambdaSharp;
using LambdaSharp.Challenge.Bookmarker.Shared;
using OpenGraphNet;
using OpenGraphNet.Metadata;



// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace LambdaSharp.Challenge.Bookmarker.DynamoFunction {

    public class Function : ALambdaFunction<DynamoDBEvent, string> {
        //--- Fields ---
        private IAmazonDynamoDB _dynamoDbClient;
        private Table _table;

        //--- Methods ---
        public override async Task InitializeAsync(LambdaConfig config) {
            // initialize AWS clients
            _dynamoDbClient = new AmazonDynamoDBClient();

            // read settings
            _table = Table.LoadTable(_dynamoDbClient, config.ReadDynamoDBTableName("BookmarksTable"));
        }

        public override async Task<string> ProcessMessageAsync(DynamoDBEvent evt) {
            LogInfo($"# Kinesis Records = {evt.Records.Count}");
            for(var i = 0; i < evt.Records.Count; ++i) {
                var record = evt.Records[i];
                LogInfo($"Record #{i}");
                LogInfo($"AwsRegion = {record.AwsRegion}");
                LogInfo($"DynamoDB.ApproximateCreationDateTime = {record.Dynamodb.ApproximateCreationDateTime}");
                LogInfo($"DynamoDB.Keys.Count = {record.Dynamodb.Keys.Count}");
                LogInfo($"DynamoDB.Keys = {string.Join(", ", record.Dynamodb.Keys)}");
                LogInfo($"DynamoDB.SequenceNumber = {record.Dynamodb.SequenceNumber}");
                LogInfo($"DynamoDB.UserIdentity.PrincipalId = {record.UserIdentity?.PrincipalId}");
                LogInfo($"EventID = {record.EventID}");
                LogInfo($"EventName = {record.EventName}");
                LogInfo($"EventSource = {record.EventSource}");
                LogInfo($"EventSourceArn = {record.EventSourceArn}");
                LogInfo($"EventVersion = {record.EventVersion}");
                if (record.EventName != "INSERT") continue;
                var id = record.Dynamodb.NewImage["ID"].S;
                var url = record.Dynamodb.NewImage["Url"].S;
                if (!Regex.IsMatch(url, "^https?://", RegexOptions.IgnoreCase)) {
                    LogWarn($"SKIPPED: url is not valid: {url}");
                    continue;
                }
                var graph = OpenGraph.ParseUrl(url);
                var html = graph.OriginalHtml;
                var bookmark = new Bookmark {
                    ID = id,
                    Url = url,
                    Title = graph.Title,
                    Description = graph.Metadata["og:description"].Value(),
                    //Description = graph.Metadata["og:description"][0].Value,
                    ImageUrl = graph.Image.ToString(),
                };
                LogInfo($"Updated Bookmark:\n{SerializeJson(bookmark)}");
                _table.PutItemAsync(Document.FromJson(SerializeJson(bookmark)));
            }
            return "Ok";
        }

    }
}
