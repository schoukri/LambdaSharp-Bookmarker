using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using OpenGraphNet;
using OpenGraphNet.Metadata;
using LambdaSharp.Challenge.Bookmarker.Shared;



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
                LogInfo($"DynamoDB.ApproximateCreationDateTime = {record.Dynamodb.ApproximateCreationDateTime}");
                LogInfo($"EventID = {record.EventID}");
                LogInfo($"EventName = {record.EventName}");
                if (record.EventName != "INSERT") continue;
                var id = record.Dynamodb.NewImage["ID"].S;
                var url = record.Dynamodb.NewImage["Url"].S;
                if (!Regex.IsMatch(url, "^https?://", RegexOptions.IgnoreCase)) {
                    LogWarn($"SKIPPED: url is not valid: {url}");
                    continue;
                }
                var graph = OpenGraph.ParseUrl(url);
                var html = graph.OriginalHtml;
                LogInfo($"HTML: html");
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
