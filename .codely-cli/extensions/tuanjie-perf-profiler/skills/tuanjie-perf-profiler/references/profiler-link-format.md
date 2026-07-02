## Profiler Link Format

ALWAYS refer to profiler frames and samples as links in the following format:

1. **Frame**: `[Frame 18](profiler://frame/17)` where 17 is the frame index used in the URL and 18 is the frame number displayed in the Profiler UI (frame index + 1).
2. **Sample**: `[SampleName](profiler://frame/17/threadName/Main%20Thread/rawIndex/60/name/SampleName)`
   - Parameters: frame index, URL-encoded thread name, RawIndex, URL-encoded sample name
   - Full example: `[UnityEngine.Rendering.DebugUpdater.RuntimeInit() [Invoke]](profiler://frame/17/threadName/Main%20Thread/rawIndex/60/name/UnityEngine.Rendering.DebugUpdater.RuntimeInit()%20%5BInvoke%5D)`
3. **DO NOT** wrap links with quotes or backticks.
