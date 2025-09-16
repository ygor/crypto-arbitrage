using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;
using Xunit;

namespace CryptoArbitrage.Tests.ContractTests
{
	public class ExchangeSchemaTests
	{
		private static JsonNode LoadJson(string content) => JsonNode.Parse(content)!;
		private static JsonSchema LoadSchema(string relativePath)
		{
			var baseDir = AppContext.BaseDirectory;
			var path = Path.Combine(baseDir, "specs", relativePath.Replace('/', Path.DirectorySeparatorChar));
			return JsonSchema.FromText(System.IO.File.ReadAllText(path));
		}

		private static string BuildErrorMessage(EvaluationResults eval)
		{
			var details = eval.Details ?? Array.Empty<EvaluationResults>();
			var messages = details
				.SelectMany(d => d.Errors != null ? d.Errors.Select(kv => kv.Value) : Array.Empty<string>());
			return string.Join("; ", messages);
		}

		[Fact]
		public void Coinbase_Subscribe_Payload_SimpleFormat_Matches_Schema()
		{
			var schema = LoadSchema("coinbase/ws/subscribe.schema.json");
			// Test the simplified form: channels as string array
			var payload = new
			{
				type = "subscribe",
				product_ids = new[] { "BTC-USD" },
				channels = new[] { "level2", "heartbeat" }
			};
			var json = JsonSerializer.Serialize(payload);
			var eval = schema.Evaluate(LoadJson(json), new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
			Assert.True(eval.IsValid, BuildErrorMessage(eval));
		}

		[Fact]
		public void Coinbase_Subscribe_Payload_ObjectFormat_Matches_Schema()
		{
			var schema = LoadSchema("coinbase/ws/subscribe.schema.json");
			// Test the object form we now use: channels as object array (more reliable)
			var payload = new
			{
				type = "subscribe",
				product_ids = new[] { "BTC-USD" },
				channels = new object[]
				{
					new { name = "level2", product_ids = new[] { "BTC-USD" } },
					new { name = "heartbeat", product_ids = new[] { "BTC-USD" } }
				}
			};
			var json = JsonSerializer.Serialize(payload);
			var eval = schema.Evaluate(LoadJson(json), new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
			Assert.True(eval.IsValid, BuildErrorMessage(eval));
		}

		[Fact]
		public void Coinbase_L2Update_Fixture_Matches_Schema()
		{
			var schema = LoadSchema("coinbase/ws/l2update.schema.json");
			var fixture = """
			{
			  "type": "l2update",
			  "product_id": "BTC-USD",
			  "time": "2023-10-01T10:00:00Z",
			  "changes": [["buy","50000.10","0.5"], ["sell","50010.00","1.0"]]
			}
			""";
			var eval = schema.Evaluate(LoadJson(fixture), new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
			Assert.True(eval.IsValid, BuildErrorMessage(eval));
		}

		[Fact]
		public void Kraken_v1_Book_Snapshot_Fixture_Matches_Schema()
		{
			var schema = LoadSchema("kraken/ws-v1/book.snapshot.schema.json");
			var fixture = """
			[
			  1234,
			  {
			    "as": [["5541.30000","2.50700000","1534614248.123678"]],
			    "bs": [["5541.20000","1.52900000","1534614248.765567"]]
			  },
			  "book-10",
			  "XBT/USD"
			]
			""";
			var eval = schema.Evaluate(LoadJson(fixture), new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
			Assert.True(eval.IsValid, BuildErrorMessage(eval));
		}

		[Fact]
		public void Kraken_Subscribe_Payload_Matches_Schema()
		{
			var schema = LoadSchema("kraken/ws-v1/subscribe.schema.json");
			// Test our Kraken subscribe message format
			var payload = new
			{
				@event = "subscribe",
				reqid = 12345,
				pair = new[] { "XBT/USD" },
				subscription = new
				{
					name = "book",
					depth = 25
				}
			};
			var json = JsonSerializer.Serialize(payload);
			var eval = schema.Evaluate(LoadJson(json), new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
			Assert.True(eval.IsValid, BuildErrorMessage(eval));
		}

		[Fact]
		public void Kraken_v2_Ticker_Snapshot_Fixture_Matches_Schema()
		{
			var schema = LoadSchema("kraken/ws-v2/ticker.snapshot.schema.json");
			var fixture = """
			{
			  "channel": "ticker",
			  "type": "snapshot",
			  "data": [
			    {
			      "symbol": "ALGO/USD",
			      "bid": 0.10025,
			      "bid_qty": 740.0,
			      "ask": 0.10036,
			      "ask_qty": 1361.4481,
			      "last": 0.10035
			    }
			  ]
			}
			""";
			var eval = schema.Evaluate(LoadJson(fixture), new EvaluationOptions { OutputFormat = OutputFormat.Hierarchical });
			Assert.True(eval.IsValid, BuildErrorMessage(eval));
		}
	}
} 