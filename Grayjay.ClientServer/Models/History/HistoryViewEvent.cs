using System;

namespace Grayjay.ClientServer.Models.History
{
    public class HistoryViewEvent
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string Source { get; set; }
        public string VideoId { get; set; }
        public string Title { get; set; }
        public string ChannelName { get; set; }
        public DateTime StartedAtUtc { get; set; }
        public DateTime? EndedAtUtc { get; set; }
        public long? WatchMs { get; set; }
        public long? StartPositionMs { get; set; }
        public long? EndPositionMs { get; set; }
        public string EndedReason { get; set; }
    }
}
