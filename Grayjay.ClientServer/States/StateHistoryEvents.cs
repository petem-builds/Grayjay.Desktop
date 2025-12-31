using Grayjay.ClientServer.Database.Indexes;
using Grayjay.ClientServer.Models.History;
using Grayjay.ClientServer.Store;
using Grayjay.Engine.Models.Detail;
using System.Collections.Concurrent;
using System.Linq;
using System;

namespace Grayjay.ClientServer.States
{
    public static class StateHistoryEvents
    {
        private const string EndReasonStop = "stop";
        private const string EndReasonTimeout = "timeout";
        private const string EndReasonCrashRecovered = "crash_recovered";

        private static readonly TimeSpan InactivityTimeout = TimeSpan.FromMinutes(3);

        private static readonly ConcurrentDictionary<object, DBHistoryEventIndex> _eventIdIndex = new ConcurrentDictionary<object, DBHistoryEventIndex>();
        private static readonly ManagedDBStore<DBHistoryEventIndex, HistoryViewEvent> _events =
            new ManagedDBStore<DBHistoryEventIndex, HistoryViewEvent>(DBHistoryEventIndex.TABLE_NAME)
            .WithIndex(x => x.EventId, _eventIdIndex, false, true)
            .Load();

        private static readonly ConcurrentDictionary<string, ActiveEvent> _activeEvents = new ConcurrentDictionary<string, ActiveEvent>();

        public static void CloseDanglingEvents(DateTime appStartUtc)
        {
            var indexes = _events.GetAllIndexes();
            foreach (var index in indexes)
            {
                if (index.EndedAtUtc.HasValue)
                    continue;

                var item = _events.Get(index.ID)?.Object;
                if (item == null)
                    continue;

                item.EndedAtUtc = appStartUtc;
                item.WatchMs = (long)(appStartUtc - item.StartedAtUtc).TotalMilliseconds;
                item.EndedReason = EndReasonCrashRecovered;
                _events.Update(index.ID, item);
            }
        }

        public static void HandleProgress(string playerSessionId, string url, long position, PlatformVideoDetails videoDetails)
        {
            if (string.IsNullOrWhiteSpace(playerSessionId) || string.IsNullOrWhiteSpace(url))
                return;

            var now = DateTime.UtcNow;
            if (_activeEvents.TryGetValue(playerSessionId, out var active))
            {
                if (!string.Equals(active.Url, url, StringComparison.OrdinalIgnoreCase))
                {
                    EndEvent(playerSessionId, active.LastPositionMs, EndReasonStop, now);
                    StartEvent(playerSessionId, url, position, videoDetails, now);
                    return;
                }

                if (now - active.LastUpdatedUtc > InactivityTimeout)
                {
                    EndEvent(playerSessionId, active.LastPositionMs, EndReasonTimeout, now);
                    StartEvent(playerSessionId, url, position, videoDetails, now);
                    return;
                }

                UpdateEvent(playerSessionId, position, now);
                return;
            }

            StartEvent(playerSessionId, url, position, videoDetails, now);
        }

        public static void StartEvent(string playerSessionId, string url, long position, PlatformVideoDetails videoDetails, DateTime startedAtUtc)
        {
            if (string.IsNullOrWhiteSpace(playerSessionId) || string.IsNullOrWhiteSpace(url))
                return;

            var newEvent = new HistoryViewEvent
            {
                Id = Guid.NewGuid().ToString(),
                Url = url,
                Source = videoDetails?.ID?.Platform,
                VideoId = videoDetails?.ID?.Value,
                Title = videoDetails?.Name,
                ChannelName = videoDetails?.Author?.Name,
                StartedAtUtc = startedAtUtc,
                StartPositionMs = position >= 0 ? position : null,
                EndPositionMs = position >= 0 ? position : null
            };

            var index = _events.Insert(newEvent);
            _activeEvents[playerSessionId] = new ActiveEvent(index.ID, url, startedAtUtc, position);
        }

        public static void UpdateEvent(string playerSessionId, long position, DateTime nowUtc)
        {
            if (!_activeEvents.TryGetValue(playerSessionId, out var active))
                return;

            active.LastUpdatedUtc = nowUtc;
            active.LastPositionMs = position;

            var item = _events.Get(active.IndexId)?.Object;
            if (item == null)
                return;

            item.EndPositionMs = position >= 0 ? position : item.EndPositionMs;
            _events.Update(active.IndexId, item);
        }

        public static void EndEvent(string playerSessionId, long position, string reason)
        {
            EndEvent(playerSessionId, position, reason, DateTime.UtcNow);
        }

        public static void EndEvent(string playerSessionId, long position, string reason, DateTime endedAtUtc)
        {
            if (!_activeEvents.TryRemove(playerSessionId, out var active))
                return;

            var item = _events.Get(active.IndexId)?.Object;
            if (item == null)
                return;

            item.EndedAtUtc = endedAtUtc;
            item.EndPositionMs = position >= 0 ? position : item.EndPositionMs;
            item.WatchMs = (long)(endedAtUtc - item.StartedAtUtc).TotalMilliseconds;
            item.EndedReason = reason;
            _events.Update(active.IndexId, item);
        }

        public static (IReadOnlyList<HistoryViewEvent> Events, bool HasMore) GetEvents(int pageSize, DateTime? beforeUtc, DateTime? afterUtc, string url)
        {
            if (pageSize < 1)
                pageSize = 1;

            var events = _events.GetAll()
                .Select(x => x.Object)
                .Where(x => x != null);

            if (!string.IsNullOrWhiteSpace(url))
                events = events.Where(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase));

            if (beforeUtc.HasValue)
                events = events.Where(x => x.StartedAtUtc < beforeUtc.Value);

            if (afterUtc.HasValue)
                events = events.Where(x => x.StartedAtUtc > afterUtc.Value);

            var ordered = events.OrderByDescending(x => x.StartedAtUtc)
                .Take(pageSize + 1)
                .ToList();

            var hasMore = ordered.Count > pageSize;
            if (hasMore)
                ordered = ordered.Take(pageSize).ToList();

            return (ordered, hasMore);
        }

        public static IReadOnlyList<HistoryViewEvent> GetEventsForExport(int limit, DateTime? beforeUtc, DateTime? afterUtc, string url)
        {
            if (limit < 1)
                limit = 1;

            var events = _events.GetAll()
                .Select(x => x.Object)
                .Where(x => x != null);

            if (!string.IsNullOrWhiteSpace(url))
                events = events.Where(x => string.Equals(x.Url, url, StringComparison.OrdinalIgnoreCase));

            if (beforeUtc.HasValue)
                events = events.Where(x => x.StartedAtUtc < beforeUtc.Value);

            if (afterUtc.HasValue)
                events = events.Where(x => x.StartedAtUtc > afterUtc.Value);

            return events.OrderByDescending(x => x.StartedAtUtc)
                .Take(limit)
                .ToList();
        }

        private sealed class ActiveEvent
        {
            public ActiveEvent(long indexId, string url, DateTime lastUpdatedUtc, long lastPositionMs)
            {
                IndexId = indexId;
                Url = url;
                LastUpdatedUtc = lastUpdatedUtc;
                LastPositionMs = lastPositionMs;
            }

            public long IndexId { get; }
            public string Url { get; }
            public DateTime LastUpdatedUtc { get; set; }
            public long LastPositionMs { get; set; }
        }
    }
}
