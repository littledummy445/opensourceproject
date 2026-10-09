#!/usr/bin/env node

const fs = require('node:fs/promises');

const requiredEnv = ['BROWSERSTACK_USERNAME', 'BROWSERSTACK_ACCESS_KEY', 'BROWSERSTACK_APP'];

function getEnv(name, fallback) {
  const value = process.env[name];
  return value && value.trim() ? value.trim() : fallback;
}

function ensureRequiredEnv() {
  const missing = requiredEnv.filter((key) => !getEnv(key));
  if (missing.length > 0) {
    throw new Error(`Missing required environment variables: ${missing.join(', ')}`);
  }
}

async function request(path, method, body, username, accessKey) {
  const response = await fetch(`https://hub-cloud.browserstack.com${path}`, {
    method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Basic ${Buffer.from(`${username}:${accessKey}`).toString('base64')}`
    },
    body: body ? JSON.stringify(body) : undefined
  });

  const payload = await response.json();
  if (!response.ok || payload.value?.error) {
    throw new Error(`BrowserStack API error for ${method} ${path}: ${JSON.stringify(payload)}`);
  }

  return payload;
}

async function setOrientation(sessionId, orientation, creds) {
  await request(`/wd/hub/session/${sessionId}/orientation`, 'POST', { orientation }, creds.username, creds.accessKey);
}

async function findAndClickCameraButton(sessionId, locator, creds) {
  const element = await request(
    `/wd/hub/session/${sessionId}/element`,
    'POST',
    locator,
    creds.username,
    creds.accessKey
  );

  const elementId =
    element.value?.['element-6066-11e4-a52e-4f735466cecf'] ||
    element.value?.ELEMENT;

  if (!elementId) {
    throw new Error(`Could not resolve element id for locator ${JSON.stringify(locator)}`);
  }

  await request(
    `/wd/hub/session/${sessionId}/element/${elementId}/click`,
    'POST',
    {},
    creds.username,
    creds.accessKey
  );
}

async function wait(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function setSessionStatus(sessionId, status, reason, creds) {
  await request(
    `/wd/hub/session/${sessionId}/execute/sync`,
    'POST',
    {
      script: `browserstack_executor: ${JSON.stringify({
        action: 'setSessionStatus',
        arguments: { status, reason }
      })}`,
      args: []
    },
    creds.username,
    creds.accessKey
  );
}

async function takeScreenshot(sessionId, outputPath, creds) {
  const screenshotResponse = await request(
    `/wd/hub/session/${sessionId}/screenshot`,
    'GET',
    undefined,
    creds.username,
    creds.accessKey
  );

  const screenshotData = screenshotResponse.value;
  if (!screenshotData) {
    throw new Error('BrowserStack returned an empty screenshot payload');
  }

  await fs.writeFile(outputPath, Buffer.from(screenshotData, 'base64'));
}

async function deleteSession(sessionId, creds) {
  await request(`/wd/hub/session/${sessionId}`, 'DELETE', undefined, creds.username, creds.accessKey);
}

async function main() {
  ensureRequiredEnv();

  const creds = {
    username: getEnv('BROWSERSTACK_USERNAME'),
    accessKey: getEnv('BROWSERSTACK_ACCESS_KEY')
  };

  const capabilities = {
    alwaysMatch: {
      platformName: getEnv('PLATFORM_NAME', 'Android'),
      'appium:automationName': getEnv('APPIUM_AUTOMATION_NAME', 'UiAutomator2'),
      'appium:deviceName': getEnv('DEVICE_NAME', 'Samsung Galaxy S23'),
      'appium:platformVersion': getEnv('PLATFORM_VERSION', undefined),
      'appium:app': getEnv('BROWSERSTACK_APP'),
      'appium:autoGrantPermissions': getEnv('AUTO_GRANT_PERMISSIONS', 'true') === 'true',
      'bstack:options': {
        userName: creds.username,
        accessKey: creds.accessKey,
        projectName: getEnv('BROWSERSTACK_PROJECT', 'Camera Automation'),
        buildName: getEnv('BROWSERSTACK_BUILD', 'Camera Rotate Screenshot'),
        sessionName: getEnv('BROWSERSTACK_SESSION', 'Open camera, rotate, and screenshot')
      }
    },
    firstMatch: [{}]
  };

  if (!capabilities.alwaysMatch['appium:platformVersion']) {
    delete capabilities.alwaysMatch['appium:platformVersion'];
  }

  const cameraLocator = {
    using: getEnv('CAMERA_BUTTON_USING', 'accessibility id'),
    value: getEnv('CAMERA_BUTTON_VALUE', 'Open Camera')
  };

  const screenshotOutput = getEnv('SCREENSHOT_OUTPUT', 'browserstack-camera-rotated.png');

  let sessionId;
  try {
    const session = await request('/wd/hub/session', 'POST', { capabilities }, creds.username, creds.accessKey);
    sessionId = session.value?.sessionId || session.sessionId;

    if (!sessionId) {
      throw new Error(`Unable to resolve session id from BrowserStack response: ${JSON.stringify(session)}`);
    }

    await findAndClickCameraButton(sessionId, cameraLocator, creds);
    await wait(Number(getEnv('WAIT_AFTER_CAMERA_OPEN_MS', '3000')));

    await setOrientation(sessionId, 'LANDSCAPE', creds);
    await wait(Number(getEnv('WAIT_AFTER_ROTATE_MS', '2000')));

    await takeScreenshot(sessionId, screenshotOutput, creds);
    await setSessionStatus(sessionId, 'passed', `Screenshot captured at ${screenshotOutput}`, creds);

    console.log(`Screenshot captured: ${screenshotOutput}`);
  } catch (error) {
    if (sessionId) {
      await setSessionStatus(sessionId, 'failed', error.message, creds).catch(() => {});
    }
    throw error;
  } finally {
    if (sessionId) {
      await deleteSession(sessionId, creds).catch(() => {});
    }
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exit(1);
});
