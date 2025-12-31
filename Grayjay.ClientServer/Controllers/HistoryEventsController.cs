using Grayjay.ClientServer.Models.History;
using Grayjay.ClientServer.States;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using System.Text.Json;
using System.Linq;

namespace Grayjay.ClientServer.Controllers
{
    [Route("[controller]/[action]")]
    public class HistoryEventsController : ControllerBase
    {
        public class HistoryEventsPage
        {
            public HistoryViewEvent[] Events { get; set; }
            public bool HasMore { get; set; }
        }

        [HttpGet]
        public HistoryEventsPage HistoryEventsLoad(int pageSize = 50, long? before = null, long? after = null, string url = null)
        {
            var beforeUtc = before.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(before.Value).UtcDateTime : (DateTime?)null;
            var afterUtc = after.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(after.Value).UtcDateTime : (DateTime?)null;

            var (events, hasMore) = StateHistoryEvents.GetEvents(pageSize, beforeUtc, afterUtc, url);
            return new HistoryEventsPage
            {
                Events = events.ToArray(),
                HasMore = hasMore
            };
        }

        [HttpGet]
        public IActionResult Export(string format = "jsonl", int limit = 10000, long? before = null, long? after = null, string url = null)
        {
            var beforeUtc = before.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(before.Value).UtcDateTime : (DateTime?)null;
            var afterUtc = after.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(after.Value).UtcDateTime : (DateTime?)null;
            var events = StateHistoryEvents.GetEventsForExport(limit, beforeUtc, afterUtc, url);

            if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var csv = BuildCsv(events);
                return File(Encoding.UTF8.GetBytes(csv), "text/csv", "history_events.csv");
            }

            var jsonl = BuildJsonl(events);
            return File(Encoding.UTF8.GetBytes(jsonl), "application/jsonl", "history_events.jsonl");
        }

        private static string BuildJsonl(IEnumerable<HistoryViewEvent> events)
        {
            var builder = new StringBuilder();
            foreach (var item in events)
            {
                builder.AppendLine(JsonSerializer.Serialize(item));
            }
            return builder.ToString();
        }

        private static string BuildCsv(IEnumerable<HistoryViewEvent> events)
        {
            var builder = new StringBuilder();
            builder.AppendLine("id,video_url,source,video_id,title,channel_name,started_at_utc,ended_at_utc,watch_ms,start_position_ms,end_position_ms,ended_reason");
            foreach (var item in events)
            {
                builder.AppendLine(string.Join(",",
                    EscapeCsv(item.Id),
                    EscapeCsv(item.Url),
                    EscapeCsv(item.Source),
                    EscapeCsv(item.VideoId),
                    EscapeCsv(item.Title),
                    EscapeCsv(item.ChannelName),
                    EscapeCsv(item.StartedAtUtc.ToString("O")),
                    EscapeCsv(item.EndedAtUtc?.ToString("O")),
                    EscapeCsv(item.WatchMs?.ToString()),
                    EscapeCsv(item.StartPositionMs?.ToString()),
                    EscapeCsv(item.EndPositionMs?.ToString()),
                    EscapeCsv(item.EndedReason)));
            }
            return builder.ToString();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "";
            var needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r');
            var escaped = value.Replace("\"", "\"\"");
            return needsQuotes ? $"\"{escaped}\"" : escaped;
        }
    }
}
