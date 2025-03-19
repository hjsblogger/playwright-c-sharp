/* Reference links */
/*********************
https://www.youtube.com/watch?v=g8bAG-6N2-k
https://stackoverflow.com/questions/77798928/configuring-playwright-using-runsettings
https://playwright.dev/dotnet/docs/test-runners#customizing-browsercontext-options
https://playwright.dev/dotnet/docs/test-runners#base-classes-for-playwright
https://stackoverflow.com/questions/77798928/configuring-playwright-using-runsettings
https://playwright.dev/dotnet/docs/test-runners#using-the-runsettings-file
https://www.lambdatest.com/support/docs/capabilities-for-playwright/
*********************/

/* This can be read as an environment variable */
#define USE_PLAYWRIGHT_ALL_PARALLELISM

using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;
using Newtonsoft.Json;
using PlaywrightTest.Helper;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

/* Parallelism with [Parallelizable(ParallelScope.Self)] */
/* IBrowserContext is reused in this case, not an ideal way to perform parallelism */
#if USE_PLAYWRIGHT_SELF_PARALLELISM

namespace PlaywrightTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.Self)]
    public class PlaywrightTestSingle
    {
        private BrowserManager? _browserManager;
        private IBrowser? _browser;
        /* This object can be used to launch or connect to Chromium, returning instances of */
        private IPlaywright? _playwright;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            try
            {
                _browserManager = new BrowserManager();

                /*  Removed "using" to prevent premature disposal */
                /* using var playwright = await Playwright.CreateAsync(); */
                _playwright = await Playwright.CreateAsync();

                /* Custom run setting options */
                /* https://playwright.dev/dotnet/docs/test-runners#using-the-runsettings-file */
                string? user = TestContext.Parameters.Get("LT_USERNAME", null);
                string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY", null);
                string? platform = TestContext.Parameters.Get("PLATFORM", null);
                string? browserType = TestContext.Parameters.Get("BROWSER_NAME", null);
                string? browserVersion = TestContext.Parameters.Get("BROWSER_VERSION", null);
                string? cdpUrlBase = TestContext.Parameters.Get("CDP_URL_BASE", null);

                /* Null check for all the required parameters */
                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(accessKey)
                    || string.IsNullOrEmpty(cdpUrlBase))
                {
                    throw new ArgumentException("Required parameters are missing in the run settings.");
                }

                /* Dictionary of LambdaTest Capabilities */
                var capabilities = new Dictionary<string, object?>
                {
                    { "browserName", browserType },
                    { "browserVersion", browserVersion },
                    { "LT:Options", new Dictionary<string, string?>
                        {
                            { "name", "Playwright Test" },
                            { "build", "Playwright C-Sharp tests" },
                            { "platform", platform },
                            { "user", user },
                            { "accessKey", accessKey },
                        }
                    }
                };

                string capabilitiesJson = JsonConvert.SerializeObject(capabilities);
                string cdpUrl = $"{cdpUrlBase}?capabilities={Uri.EscapeDataString(capabilitiesJson)}";

                _browser = browserType?.ToLower() switch
                {
                    "chromium" or "chrome" => await _playwright.Chromium.ConnectAsync(cdpUrl),
                    "firefox" or "pw-firefox" => await _playwright.Firefox.ConnectAsync(cdpUrl),
                    "webkit" or "pw-webkit" => await _playwright.Webkit.ConnectAsync(cdpUrl),
                    _ => throw new ArgumentException($"Unsupported browser type: {browserType}")
                };

                /* Create a new browser context */
                var context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                    IgnoreHTTPSErrors = true
                });

                /* Launch the browser with the supplied context */
                await _browserManager.StartBrowser(context);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error in OneTimeSetup: {ex.Message}");
                throw;
            }
        }

        [Test]
        public async Task RunPlaywrightTest_1()
        {
            try
            {
                TestContext.WriteLine("Running Playwright test...");
                var page = _browserManager?.GetCurrentTab() ?? 
                    throw new InvalidOperationException("_browserManager is null.");

                /* Test Scenario */
                /* https://github.com/LambdaTest/playwright-sample/blob/main/ */
                /* playwright-csharp/PlaywrightTestSingle.cs */
                /* replica of https://github.com/LambdaTest/playwright-sample/blob/main/playwright-csharp */
                /* /PlaywrightTestSingle.cs#L37 */
                await page.GotoAsync("https://duckduckgo.com");
                await page.FillAsync("[name='q']", "LambdaTest");
                await page.Keyboard.PressAsync("Enter");
                await page.WaitForURLAsync(url => url.Contains("q=LambdaTest"),
                        new PageWaitForURLOptions { Timeout = 10000 });

                var title = await page.TitleAsync();
                if (title.Contains("LambdaTest"))
                {
                    Console.WriteLine("Test Passed: Title matched.");
                    await SetTestStatus("passed", "Title matched");
                }
                else
                {
                    Console.WriteLine("Test Failed: Title did not match.");
                    await SetTestStatus("failed", "Title did not match");
                }
            }
            catch (PlaywrightException ex)
            {
                TestContext.WriteLine($"Error during test execution: {ex.Message}");
                throw;
            }
        }

        [Test]
        public async Task RunPlaywrightTest_2()
        {
            try
            {
                TestContext.WriteLine("Running Playwright test...");
                var page = _browserManager?.GetCurrentTab() ?? 
                    throw new InvalidOperationException("_browserManager is null.");

                /* Test Scenario */
                /* https://github.com/LambdaTest/playwright-sample/blob/main/ */
                /* playwright-csharp/PlaywrightTestSingle.cs */
                /* replica of https://github.com/LambdaTest/playwright-sample/blob/main/playwright-csharp */
                /* /PlaywrightTestSingle.cs#L37 */
                await page.GotoAsync("https://duckduckgo.com");
                await page.FillAsync("[name='q']", "LambdaTest");
                await page.Keyboard.PressAsync("Enter");
                await page.WaitForURLAsync(url => url.Contains("q=LambdaTest"),
                        new PageWaitForURLOptions { Timeout = 10000 });

                var title = await page.TitleAsync();
                if (title.Contains("LambdaTest"))
                {
                    Console.WriteLine("Test Passed: Title matched.");
                    await SetTestStatus("passed", "Title matched");
                }
                else
                {
                    Console.WriteLine("Test Failed: Title did not match.");
                    await SetTestStatus("failed", "Title did not match");
                }
            }
            catch (PlaywrightException ex)
            {
                TestContext.WriteLine($"Error during test execution: {ex.Message}");
                throw;
            }
        }

        [OneTimeTearDown]
        public async Task TearDown()
        {
            try
            {
                /* Closure of contexts and browser */
                if (_browserManager != null)
                {
                    foreach (var context in _browserManager.BrowserContexts.Values)
                    {
                        await context.CloseAsync().ConfigureAwait(false);
                    }
                    _browserManager.BrowserContexts.Clear();
                }

                /* Finally release the resources used by the instantiated browser */
                if (_browser != null)
                {
                    await _browser.CloseAsync().ConfigureAwait(false);
                    _browser = null;
                }

                // Dispose Playwright after everything else
                _playwright?.Dispose();
            }
            catch (PlaywrightException ex)
            {
                TestContext.WriteLine($"Error during TearDown: {ex.Message}");
            }
        }

        private async Task SetTestStatus(string status, string remark)
        {
            var page = _browserManager?.GetCurrentTab() ?? 
                throw new InvalidOperationException("_browserManager is null.");
            await page.EvaluateAsync("_ => {}", $"lambdatest_action: {{\"action\": \"setTestStatus\", \"arguments\": {{\"status\":\"{status}\", \"remark\": \"{remark}\"}}}}");
        }
    }
}

#elif USE_PLAYWRIGHT_ALL_PARALLELISM

namespace PlaywrightTests
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class PlaywrightTestSingle
    {
        private IPlaywright? _playwright;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            try
            {
                TestContext.WriteLine("Initializing Playwright in OneTimeSetup...");
                _playwright = await Playwright.CreateAsync();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error in OneTimeSetup: {ex.Message}");
                throw;
            }
        }

        [Test]
        public async Task RunPlaywrightTest_1()
        {
            await RunTestScenario("RunPlaywrightTest_1");
        }

        [Test]
        public async Task RunPlaywrightTest_2()
        {
            await RunTestScenario("RunPlaywrightTest_2");
        }

        private async Task RunTestScenario(string testName)
        {
            IBrowser? browser = null;
            BrowserManager? browserManager = null;

            try
            {
                TestContext.WriteLine($"Starting {testName}...");

                /* Get LambdaTest parameters from runsettings */
                string? user = TestContext.Parameters.Get("LT_USERNAME", null);
                string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY", null);
                string? platform = TestContext.Parameters.Get("PLATFORM", null);
                string? browserType = TestContext.Parameters.Get("BROWSER_NAME", null);
                string? browserVersion = TestContext.Parameters.Get("BROWSER_VERSION", null);
                string? cdpUrlBase = TestContext.Parameters.Get("CDP_URL_BASE", null);

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(accessKey)
                    || string.IsNullOrEmpty(cdpUrlBase))
                {
                    throw new ArgumentException("Required parameters are missing in the run settings.");
                }

                /* LambdaTest capabilities from https://www.lambdatest.com/capabilities-generator */
                var capabilities = new Dictionary<string, object?>
                {
                    { "browserName", browserType },
                    { "browserVersion", browserVersion },
                    { "LT:Options", new Dictionary<string, string?>
                        {
                            { "name", testName }, // Unique name per test
                            { "build", "Playwright C-Sharp tests" },
                            { "platform", platform },
                            { "user", user },
                            { "accessKey", accessKey },
                        }
                    }
                };

                string capabilitiesJson = JsonConvert.SerializeObject(capabilities);
                string cdpUrl = $"{cdpUrlBase}?capabilities={Uri.EscapeDataString(capabilitiesJson)}";

                /* Connect to browser using shared _playwright instance */
                /* Allowed browsers - https://www.lambdatest.com/support/docs/capabilities-for-playwright/ */
                browser = browserType?.ToLower() switch
                {
                    "chromium" or "chrome" => await _playwright.Chromium.ConnectAsync(cdpUrl),
                    "firefox" or "pw-firefox" => await _playwright.Firefox.ConnectAsync(cdpUrl),
                    "webkit" or "pw-webkit" => await _playwright.Webkit.ConnectAsync(cdpUrl),
                    _ => throw new ArgumentException($"Unsupported browser type: {browserType}")
                };

                /* Context creation for the test */
                var context = await browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                    IgnoreHTTPSErrors = true
                });

                /* Instantiate the browser */
                browserManager = new BrowserManager();
                await browserManager.StartBrowser(context);

                /* Get the current tab */
                var page = browserManager.GetCurrentTab();

                /* Rudimentary test scenario */
                /* Test Scenario */
                /* https://github.com/LambdaTest/playwright-sample/blob/main/ */
                /* playwright-csharp/PlaywrightTestSingle.cs */
                /* replica of https://github.com/LambdaTest/playwright-sample/blob/main/playwright-csharp */
                /* /PlaywrightTestSingle.cs#L37 */
                await page.GotoAsync("https://duckduckgo.com", 
                    new PageGotoOptions { WaitUntil = WaitUntilState.Load });
                await page.FillAsync("[name='q']", "LambdaTest");
                await page.Keyboard.PressAsync("Enter");
                await page.WaitForURLAsync(url => url.Contains("q=LambdaTest"),
                    new PageWaitForURLOptions { Timeout = 10000 });

                var title = await page.TitleAsync();
                if (title.Contains("LambdaTest"))
                {
                    TestContext.WriteLine($"{testName} Passed: Title matched.");
                    await SetTestStatus(page, "passed", "Title matched");
                }
                else
                {
                    TestContext.WriteLine($"{testName} Failed: Title did not match.");
                    await SetTestStatus(page, "failed", "Title did not match");
                }
            }
            catch (PlaywrightException ex)
            {
                TestContext.WriteLine($"Error in {testName}: {ex.Message}");
                throw;
            }
            finally
            {
                /* Cleanup of resources */
                if (browserManager != null)
                {
                    foreach (var context in browserManager.BrowserContexts.Values)
                    {
                        await context.CloseAsync().ConfigureAwait(false);
                    }
                }
                if (browser != null)
                {
                    await browser.CloseAsync().ConfigureAwait(false);
                }
            }
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            try
            {
                TestContext.WriteLine("Disposing Playwright in OneTimeTearDown...");
                _playwright?.Dispose();
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error in OneTimeTearDown: {ex.Message}");
                throw;
            }
        }

        private async Task SetTestStatus(IPage page, string status, string remark)
        {
            await page.EvaluateAsync("_ => {}", $"lambdatest_action: {{\"action\": \"setTestStatus\", \"arguments\": {{\"status\":\"{status}\", \"remark\": \"{remark}\"}}}}");
        }
    }
}
#endif