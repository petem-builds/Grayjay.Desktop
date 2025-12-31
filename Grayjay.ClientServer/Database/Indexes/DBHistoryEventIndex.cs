using Grayjay.ClientServer.Models.History;
using System;
using System.Text;
using System.Text.Json;

namespace Grayjay.ClientServer.Database.Indexes
{
    public class DBHistoryEventIndex : DBIndex<HistoryViewEvent>
    {
        public const string TABLE_NAME = "history_events";

        [Indexed]
        public string EventId { get; set; }

        [Indexed]
        public string Url { get; set; }

        [Indexed]
        public string Source { get; set; }

        [Indexed]
        [Order(0, Ordering.Descending)]
        public DateTime StartedAtUtc { get; set; }

        [Indexed]
        public DateTime? EndedAtUtc { get; set; }

        [Indexed]
        public string Title { get; set; }

        [Indexed]
        public string ChannelName { get; set; }

        public DBHistoryEventIndex() { }
        public DBHistoryEventIndex(HistoryViewEvent content)
        {
            FromObject(content);
        }

        public override HistoryViewEvent Deserialize()
        {
            string str = Encoding.UTF8.GetString(Serialized);
            return JsonSerializer.Deserialize<HistoryViewEvent>(str);
        }

        public override void FromObject(HistoryViewEvent content)
        {
            EventId = content.Id;
            Url = content.Url;
            Source = content.Source;
            StartedAtUtc = content.StartedAtUtc;
            EndedAtUtc = content.EndedAtUtc;
            Title = content.Title;
            ChannelName = content.ChannelName;
            Serialized = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(content));
        }
    }
}
