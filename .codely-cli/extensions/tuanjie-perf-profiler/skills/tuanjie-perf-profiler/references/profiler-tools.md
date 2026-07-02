## Tool Reference

Every tool below is invoked through the bridge's `execute_custom_tool` command. The skeleton for each call:

```json
{
  "tool_name": "<tool name from the table>",
  "parameters": { ... }
}
```

Each tool returns the standard envelope `{ "success": bool, "message": string, "data": { ... } }`. Time/GC summary tools place their formatted text under `data.text`. File/search tools place structured results under `data.matches` and friends.

### Session

#### `Tuanjie.Profiler.Initialize`
Loads a profiling capture so the other tools have data to analyze. If a profiler window is already open with an in-memory session, the bridge reuses it.

| Parameter      | Type   | Required | Description |
| -------------- | ------ | -------- | ----------- |
| `sessionPath`  | string | optional | Project-relative path to a `.data` capture, or a session file name. Leave empty to auto-pick a unique session, or to receive the list of available captures when more than one exists. |

When more than one capture exists and no `sessionPath` is provided, the response is `success=false` with `data.sessions` listing the candidates. Re-invoke with the chosen `projectRelativePath`.

### Time analysis

#### `Tuanjie.Profiler.GetFrameRangeTopTimeSummary`
Frame-time overview across a contiguous range. Use first to find the spiked frame.

| Parameter         | Type  | Required | Description |
| ----------------- | ----- | -------- | ----------- |
| `startFrameIndex` | int   | yes      | First frame index in the range (0-based). |
| `lastFrameIndex`  | int   | yes      | Last frame index (inclusive). |
| `targetFrameTime` | float | yes      | Target frame time in milliseconds. |

#### `Tuanjie.Profiler.GetFrameTopTimeSamplesSummary`
Top main-thread samples in a single frame, ranked by **total time**.

| Parameter         | Type  | Required | Description |
| ----------------- | ----- | -------- | ----------- |
| `frameIndex`      | int   | yes      | Frame index (0-based). |
| `targetFrameTime` | float | yes      | Target frame time in milliseconds. |

#### `Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary`
Top individual samples ranked by **self time** in the inverted hierarchy. Returns `BottomUpId` values.

| Parameter    | Type | Required | Description |
| ------------ | ---- | -------- | ----------- |
| `frameIndex` | int  | yes      | Frame index (0-based). |

#### `Tuanjie.Profiler.GetSampleTimeSummary`
Drill into a sample by integer `SampleId`.

| Parameter    | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `frameIndex` | int    | yes      | Frame index. |
| `threadName` | string | yes      | Thread name (e.g. `Main Thread`, `Render Thread`). |
| `sampleId`   | int    | yes      | SampleId from a previous tool output. |

#### `Tuanjie.Profiler.GetBottomUpSampleTimeSummary`
Drill into a `BottomUpId` returned by `Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary`.

| Parameter     | Type   | Required | Description |
| ------------- | ------ | -------- | ----------- |
| `frameIndex`  | int    | yes      | Frame index. |
| `threadName`  | string | yes      | Thread name. |
| `bottomUpId`  | int    | yes      | BottomUpId from a previous self-time output. |

#### `Tuanjie.Profiler.GetSampleTimeSummaryByMarkerPath`
Drill into a sample by a `/`-delimited path of MarkerIds.

| Parameter      | Type   | Required | Description |
| -------------- | ------ | -------- | ----------- |
| `frameIndex`   | int    | yes      | Frame index. |
| `threadName`   | string | yes      | Thread name. |
| `markerIdPath` | string | yes      | `/`-delimited integer marker ids, e.g. `12/45/132`. |

#### `Tuanjie.Profiler.GetRelatedSamplesTimeSummary`
List samples on another thread that overlap a given sample's time window.

| Parameter            | Type   | Required | Description |
| -------------------- | ------ | -------- | ----------- |
| `frameIndex`         | int    | yes      | Frame index. |
| `threadName`         | string | yes      | Source thread name. |
| `sampleId`           | int    | yes      | Source SampleId. |
| `relatedThreadName`  | string | yes      | Thread to inspect for overlap (e.g. `Render Thread`). |

### GC analysis

#### `Tuanjie.Profiler.GetOverallGcAllocationsSummary`
GC allocation overview across the entire loaded capture. No parameters.

#### `Tuanjie.Profiler.GetFrameRangeGcAllocationsSummary`
| Parameter         | Type | Required | Description |
| ----------------- | ---- | -------- | ----------- |
| `startFrameIndex` | int  | yes      | First frame index in the range. |
| `lastFrameIndex`  | int  | yes      | Last frame index (inclusive). |

#### `Tuanjie.Profiler.GetFrameGcAllocationsSummary`
| Parameter    | Type | Required | Description |
| ------------ | ---- | -------- | ----------- |
| `frameIndex` | int  | yes      | Frame to inspect. |

#### `Tuanjie.Profiler.GetSampleGcAllocationSummary`
| Parameter    | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `frameIndex` | int    | yes      | Frame index. |
| `threadName` | string | yes      | Thread name. |
| `sampleId`   | int    | yes      | SampleId of the sample to investigate. |

#### `Tuanjie.Profiler.GetSampleGcAllocationSummaryByMarkerPath`
| Parameter      | Type   | Required | Description |
| -------------- | ------ | -------- | ----------- |
| `frameIndex`   | int    | yes      | Frame index. |
| `threadName`   | string | yes      | Thread name. |
| `markerIdPath` | string | yes      | `/`-delimited integer marker ids. |

### Source code

#### `Tuanjie.Profiler.FindScriptFile`
Regex search across `Assets/` and `Packages/`.

| Parameter       | Type   | Required | Description |
| --------------- | ------ | -------- | ----------- |
| `searchPattern` | string | optional | Regex applied to file content. Empty = name filter only, returns short previews. |
| `nameRegex`     | string | optional | Regex applied to project-relative path. Strongly recommended for narrowing scope. Defaults to `*.cs`. |

Returns up to ~200 matches across up to 200 files; the `data.scannedFiles` array shows what was inspected.

#### `Tuanjie.Profiler.GetFileContentLineCount`
| Parameter  | Type   | Required | Description |
| ---------- | ------ | -------- | ----------- |
| `filePath` | string | yes      | Project-relative or absolute path. |

#### `Tuanjie.Profiler.GetFileContent`
| Parameter   | Type   | Required | Description |
| ----------- | ------ | -------- | ----------- |
| `filePath`  | string | yes      | Project-relative or absolute path. |
| `startLine` | int    | optional | 0-based start line. Default 0. |
| `lineCount` | int    | optional | Lines to return. `-1` = all. Default `-1`. |

#### `Tuanjie.Profiler.GetMarkerCode`
Maps a profiler marker (e.g. `MyScript.Update`, `Inl_Light2D Pass`) to candidate `.cs` files. Strips namespace, function brackets, and `Inl_` prefix automatically.

| Parameter    | Type   | Required | Description |
| ------------ | ------ | -------- | ----------- |
| `markerName` | string | yes      | Marker name from the profiler (e.g. `MyScript.Update`). |
