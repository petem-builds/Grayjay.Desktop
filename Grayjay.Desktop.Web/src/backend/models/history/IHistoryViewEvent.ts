export interface IHistoryViewEvent {
    id: string;
    url: string;
    source?: string;
    videoId?: string;
    title?: string;
    channelName?: string;
    startedAtUtc: string;
    endedAtUtc?: string;
    watchMs?: number;
    startPositionMs?: number;
    endPositionMs?: number;
    endedReason?: string;
}
