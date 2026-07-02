## Profiler Marker Naming Conventions

1. **MonoBehaviour scripts** use the format `MonoBehaviourName.Method`
   - Example: `MyScript.Update`, `MyScript.Start`
   - The corresponding filename is `MyScript.cs`

2. **`Inl_` prefix**: Main Thread markers of the Universal Render Pipeline
   - These map to a ScriptableRenderPass implementation
   - Search WITHOUT the `Inl_` prefix: `Inl_Light2D Pass` → search for `Light2D Pass`
   - Use `Tuanjie.Profiler.GetMarkerCode` with the stripped name (the tool strips it for you, but be explicit if searching with `Tuanjie.Profiler.FindScriptFile`)

3. **Pass-through / wrapper markers** — high total time, near-zero self time, safe to skip:
   - `UniversalRenderTotal`, `RenderCameraStack`, `RecordAndExecuteRenderGraph`
   - `EditorLoop` (Editor overhead — not present in player builds)
   - `PlayerLoop`, `BehaviourUpdate`, `LateBehaviourUpdate` (loop dispatchers)
   - Call `Tuanjie.Profiler.GetSampleTimeSummary` on these only to find their expensive children, then move to those children.

4. **RenderGraph markers**:
   - `RecordRenderGraph`: cost here = CPU-side pass setup complexity
   - `Execute`: cost here = draw call overhead, GPU submission
   - `Inl_` markers under these are the actual render passes to investigate

5. **Job system markers**:
   - `WaitForJobGroupID`: Main Thread stall waiting for a job. `Tuanjie.Profiler.GetSampleTimeSummary` shows callstack. Use `Tuanjie.Profiler.GetRelatedSamplesTimeSummary` to check actual worker thread cost.

6. **GC markers**:
   - `GC.Alloc`: Managed allocation. `Tuanjie.Profiler.GetSampleTimeSummary` shows callstack if capture was taken with "Capture Callstacks for GC.Alloc" enabled.
   - If callstack is missing, advise the user to re-capture with callstack collection enabled.

7. **Initialization frames (Frame 1 / index 0)**:
   - Frame 1 (index 0) is dominated by `Initialize Mono`, `Initialize Graphics`, JIT compilation, and asset loading. High allocations and long frame times here are expected and non-actionable.
   - Always skip Frame 1 for steady-state performance analysis. Focus on a representative frame from normal gameplay (typically after the first few seconds of runtime).
