using Microsoft.Playwright;
using Newtonsoft.Json;
using NUnit.Framework;
using PlaywrightTest.Helper;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;

namespace PlaywrightTests
{
    [TestFixture]
    public class PlaywrightTestSingle
    {
        private BrowserManager? _browserManager;
        private IBrowser? _browser;
        private IPlaywright? _playwright;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            try
            {
                _browserManager = new BrowserManager();

                // using var playwright = await Playwright.CreateAsync();

                // Remove "using" to prevent premature disposal
                _playwright = await Playwright.CreateAsync();
                string? user = TestContext.Parameters.Get("LT_USERNAME", null);
                string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY", null);
                string? platform = TestContext.Parameters.Get("PLATFORM", null);
                string? browserType = TestContext.Parameters.Get("BROWSER_NAME", null);
                string? browserVersion = TestContext.Parameters.Get("BROWSER_VERSION", null);
                string? cdpUrlBase = TestContext.Parameters.Get("CDP_URL_BASE", null);

                if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(cdpUrlBase))
                {
                    throw new ArgumentException("Required parameters are missing in the run settings.");
                }

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
                            { "accessKey", accessKey }
                        }
                    }
                };

                string capabilitiesJson = JsonConvert.SerializeObject(capabilities);
                string cdpUrl = $"{cdpUrlBase}?capabilities={Uri.EscapeDataString(capabilitiesJson)}";

                _browser = browserType?.ToLower() switch
                {
                    "chromium" or "chrome" => await _playwright.Chromium.ConnectAsync(cdpUrl),
                    "firefox" => await _playwright.Firefox.ConnectAsync(cdpUrl),
                    "webkit" => await _playwright.Webkit.ConnectAsync(cdpUrl),
                    _ => throw new ArgumentException($"Unsupported browser type: {browserType}")
                };

                var context = await _browser.NewContextAsync(new BrowserNewContextOptions
                {
                    ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                    IgnoreHTTPSErrors = true
                });

                await _browserManager.StartBrowser(context);
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"Error in OneTimeSetup: {ex.Message}");
                throw;
            }
        }

        [Test]
        public async Task RunPlaywrightTest()
        {
            try
            {
                TestContext.WriteLine("Running Playwright test...");
                var page = _browserManager?.GetCurrentTab() ?? 
                    throw new InvalidOperationException("_browserManager is null.");;

                // Perform actions
                await page.GotoAsync("https://duckduckgo.com");
                await page.FillAsync("[name='q']", "LambdaTest");
                await page.Keyboard.PressAsync("Enter");
                await page.WaitForURLAsync(url => url.Contains("q=LambdaTest"), new PageWaitForURLOptions { Timeout = 10000 });

                var title = await page.TitleAsync();
                // Assert.IsTrue(title.Contains("LambdaTest"));
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
                // Close contexts and browser first
                if (_browserManager != null)
                {
                    foreach (var context in _browserManager.BrowserContexts.Values)
                    {
                        await context.CloseAsync().ConfigureAwait(false);
                    }
                    _browserManager.BrowserContexts.Clear();
                }

                // Close the browser
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
