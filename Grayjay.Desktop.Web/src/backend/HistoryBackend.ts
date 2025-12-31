import { Backend } from "./Backend";
import { IHistoryVideo } from "./models/content/IHistoryVideo";
import { Pager } from "./models/pagers/Pager";
import { IHistoryViewEvent } from "./models/history/IHistoryViewEvent";

export abstract class HistoryBackend {
    static async getHistoricalPosition(url: string): Promise<number> {
        return await Backend.GET("/history/GetHistoricalPosition?url=" + encodeURIComponent(url)) as number;
    }

    static async historyLoad(): Promise<PagerResult<IHistoryVideo>> {
        return await Backend.GET("/history/HistoryLoad") as PagerResult<IHistoryVideo>;
    }

    static async historyLoadSearch(query: string): Promise<PagerResult<IHistoryVideo>> {
        return await Backend.GET("/history/HistoryLoadSearch?query=" + encodeURIComponent(query)) as PagerResult<IHistoryVideo>;
    }

    static async historySearchPager(query: string): Promise<Pager<IHistoryVideo>> {
        return Pager.fromMethods<IHistoryVideo>(() => this.historyLoadSearch(query), this.historyNextPage);
    }

    static async historyNextPage(): Promise<PagerResult<IHistoryVideo>> {
        return await Backend.GET("/history/HistoryNextPage") as PagerResult<IHistoryVideo>;
    }
    static async historyPager(): Promise<Pager<IHistoryVideo>> {
        return Pager.fromMethods<IHistoryVideo>(this.historyLoad, this.historyNextPage);
    }

    static async removeHistory(url: string): Promise<boolean> {
        return await Backend.GET("/history/RemoveHistory?url=" + encodeURIComponent(url)) as boolean;
    }

    static async removeHistoryRange(minutesToRemove?: number): Promise<boolean> {
        return await Backend.GET("/history/RemoveHistoryRange?minutes=" + (minutesToRemove ?? -1).toString()) as boolean;
    }

    static async historyEventsLoad(pageSize: number, before?: number, after?: number, url?: string): Promise<IHistoryEventsPage> {
        const params = new URLSearchParams();
        params.set("pageSize", pageSize.toString());
        if (before !== undefined) {
            params.set("before", before.toString());
        }
        if (after !== undefined) {
            params.set("after", after.toString());
        }
        if (url) {
            params.set("url", url);
        }
        return await Backend.GET(`/historyevents/HistoryEventsLoad?${params.toString()}`) as IHistoryEventsPage;
    }

    static async exportHistoryEvents(format: "csv" | "jsonl"): Promise<void> {
        const content = await Backend.GET_text(`/historyevents/Export?format=${format}`);
        if (!content) {
            return;
        }
        const blob = new Blob([content], { type: format === "csv" ? "text/csv" : "application/jsonl" });
        const url = URL.createObjectURL(blob);
        const link = document.createElement("a");
        link.href = url;
        link.download = `history_events.${format}`;
        link.click();
        URL.revokeObjectURL(url);
    }
}

export interface IHistoryEventsPage {
    events: IHistoryViewEvent[];
    hasMore: boolean;
}
