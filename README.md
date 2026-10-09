## Open source project

This repository now includes a full UWP calculator app with:

- Standard calculator operations (+, -, ×, ÷, parentheses)
- Handwriting mode via `InkCanvas` with expression recognition
- Calculation history panel
- Memory buttons (`MC`, `MR`, `M+`, `M-`)

### Project location

- `/home/runner/work/opensourceproject/opensourceproject/uwp-calculator`

### Build and run on Windows

1. Open `/home/runner/work/opensourceproject/opensourceproject/uwp-calculator/HandwritingCalculatorUwp.csproj` in Visual Studio 2022 on Windows.
2. Ensure **Universal Windows Platform development** workload is installed.
3. Select target architecture (`x64` recommended).
4. Build and run (`F5`).

> Note: UWP apps require Windows tooling and cannot be executed inside this Linux-based CI environment.
