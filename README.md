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

### BrowserStack Automate camera rotation screenshot script

Use `/home/runner/work/opensourceproject/opensourceproject/app/browserstack-automate-camera-rotate-screenshot.js` to:

1. Open your app on BrowserStack App Automate.
2. Tap a camera button.
3. Rotate the device to landscape.
4. Save a screenshot locally.

Required environment variables:

- `BROWSERSTACK_USERNAME`
- `BROWSERSTACK_ACCESS_KEY`
- `BROWSERSTACK_APP` (for example `bs://<app-id>`)

Optional environment variables:

- `DEVICE_NAME` (default: `Samsung Galaxy S23`)
- `PLATFORM_NAME` (default: `Android`)
- `PLATFORM_VERSION` (default: unset)
- `CAMERA_BUTTON_USING` (default: `accessibility id`)
- `CAMERA_BUTTON_VALUE` (default: `Open Camera`)
- `SCREENSHOT_OUTPUT` (default: `browserstack-camera-rotated.png`)

Run:

`node /home/runner/work/opensourceproject/opensourceproject/app/browserstack-automate-camera-rotate-screenshot.js`
