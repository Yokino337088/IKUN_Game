## Golden Path: Spike Analysis in ~6 Tool Calls

### Turn 1 — Frame Triage
- Call `Tuanjie.Profiler.GetFrameRangeTopTimeSummary` over the full capture to identify the max-CPU frame.
- Skip Frame 0 (index 0) for steady-state analysis — see `references/profiler-assumptions.md` for initialization frame guidance.

### Turn 2 — Bottleneck Identification (batch both calls)
For the spiked frame, call both in the same step:
- `Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary(frameIndex)` — identifies the LEAF bottleneck directly (actual executing code, not wrappers)
- `Tuanjie.Profiler.GetFrameGcAllocationsSummary(frameIndex)` — always pair with self-time to catch GC-driven spikes

### Turn 3 — Verify and Understand
Using the SampleId from Turn 2 (never construct a path string — always use the integer SampleId):
- `Tuanjie.Profiler.GetSampleTimeSummary(frameIndex, threadName, sampleId)` to see parent context and confirm Editor vs Runtime overhead.
- If the tool output says "Sample Self Time is significant on its own", that IS the bottleneck — stop drilling.

### Turn 4 — Code Investigation (only if sample maps to user script)
- `Tuanjie.Profiler.GetMarkerCode(markerName)` for markers matching `TypeName.MethodName()` pattern.
- Use `Tuanjie.Profiler.GetFileContentLineCount` before `Tuanjie.Profiler.GetFileContent` to avoid loading large files unnecessarily.
- Focus on the function named by the profiler sample. After reading script code, cross-reference with child samples.
- Call out the specific child samples that support your analysis in the output.

## Decision Rules

**High Self Time (leaf found):** Self-time > 10% of frame in `Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary` → this IS the bottleneck. Go directly to `Tuanjie.Profiler.GetMarkerCode`. Do NOT traverse children.

**Low Self Time (pass-through marker):** Total-time high but self-time near zero → call `Tuanjie.Profiler.GetSampleTimeSummary` with the SampleId to list all top children in one call. Pick the deepest child that accounts for >50% of the parent's time.

**URP / RenderGraph markers:**
- `UniversalRenderTotal`, `RenderCameraStack`, `RecordAndExecuteRenderGraph` → pass-through wrappers; skip them and jump to `Inl_` markers or specific pass names.
- `RecordRenderGraph` → CPU-side pass setup complexity; look at child counts and script passes.
- `Execute` → draw call overhead or GPU submission.
- `Inl_SomeName` → call `Tuanjie.Profiler.GetMarkerCode("SomeName")` (strip the `Inl_` prefix; the tool also strips it for you).

**Multi-thread:** If Main Thread self-time does not explain the frame budget overrun, call `Tuanjie.Profiler.GetBottomUpSampleTimeSummary` without a thread filter, or `Tuanjie.Profiler.GetRelatedSamplesTimeSummary` to check Render Thread overlap.

**Parallel investigation:** When multiple independent systems are each slow (>10% of frame), batch their `Tuanjie.Profiler.GetSampleTimeSummary` and `Tuanjie.Profiler.GetMarkerCode` calls in the same reasoning step.

**Fast frame selected:** If the requested frame is within budget, tell the user and suggest selecting a frame near a visible spike, adjusting the target frame time, or using the Profile Analyzer package.

## ID Types — Do Not Mix
Each tool returns a specific ID type; they are not interchangeable:
- `SampleId` — use with `Tuanjie.Profiler.GetSampleTimeSummary`, `Tuanjie.Profiler.GetSampleGcAllocationSummary`, `Tuanjie.Profiler.GetRelatedSamplesTimeSummary`
- `BottomUpId` — use only with `Tuanjie.Profiler.GetBottomUpSampleTimeSummary` (returned by `Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary`)
- `RawIndex` — stable within a frame; use only in `profiler://` links, not as a tool parameter
- `Marker Id Path` — `/`-delimited MarkerId integers; use only with `Tuanjie.Profiler.GetSampleTimeSummaryByMarkerPath` / `Tuanjie.Profiler.GetSampleGcAllocationSummaryByMarkerPath`

If `Tuanjie.Profiler.GetSampleTimeSummary` fails with a SampleId from `Tuanjie.Profiler.GetFrameSelfTimeSamplesSummary`, that tool returns a `BottomUpId`. Call `Tuanjie.Profiler.GetBottomUpSampleTimeSummary` instead, then use the regular `SampleId` from its output for subsequent calls.

## SampleId Reliability
Tool outputs include `SampleId: N` and `RawIndex: N` for every sample. Always use the integer SampleId directly for follow-up calls. Never construct a marker path string manually — this causes "Input string format" errors.

If a SampleId fails with "not found in Frame X on thread Y", retry on a different thread (Render Thread, Worker Thread) before reporting failure.

## Engine Marker Filter — Do Not Search Source for These
Do NOT call `Tuanjie.Profiler.FindScriptFile` or `Tuanjie.Profiler.GetMarkerCode` for engine-internal markers. Use `Tuanjie.Profiler.GetSampleTimeSummary` to find the first user-script child instead:
- Markers starting with: `PostLateUpdate.`, `PreLateUpdate.`, `FixedUpdate.`, `Update.`, `Gfx.`, `Render.`, `Physics.`
- `EditorLoop`, `PlayerLoop`, `BehaviourUpdate`, `LateBehaviourUpdate`, `Initialize Mono`, `Initialize Graphics`
- Any marker without a `.` separator is likely an engine system marker, not a user script.

## Investigation Tips (when no obvious leaf bottleneck is found)
- Add `Profiler.BeginSample`/`EndSample` custom markers to narrow cost within a large function.
- Check for string concatenation, LINQ, or `new` allocations inside `Update()` loops causing `GC.Alloc`.
- Use Tuanjie's Profile Analyzer package for multi-frame statistical analysis.
- Use Profile Analyzer's "Compare" mode between good and bad frames.
