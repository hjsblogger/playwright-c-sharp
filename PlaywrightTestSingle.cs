using Microsoft.Playwright;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightTests
{
    [TestFixture]
    public class PlaywrightTestSingle
    {
        private IBrowserContext? _browserContext;
        private IPage? _page;

        [Test]
        public async Task RunPlaywrightTest()
        {
            Console.WriteLine("Starting Playwright test on LambdaTest...");

            using var playwright = await Playwright.CreateAsync();

            // Fetch parameters from TestContext.Parameters
            string? user = TestContext.Parameters.Get("LT_USERNAME", null);
            string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY", null);
            string? platform = TestContext.Parameters.Get("PLATFORM", null);
            Console.WriteLine($"Platform = {platform}");
            string? browserType = TestContext.Parameters.Get("BROWSER_NAME", null);
            Console.WriteLine($"Browser Type = {browserType}");
            string? browserVersion = TestContext.Parameters.Get("BROWSER_VERSION", null);
            string? cdpUrlBase = TestContext.Parameters.Get("CDP_URL_BASE", null);

            // Validate mandatory parameters
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(cdpUrlBase))
            {
                throw new ArgumentException("Required parameters are missing in the run settings.");
            }

            // Set capabilities
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

            Console.WriteLine($"CDP URL: {cdpUrl}");

            // Connect to LambdaTest
            IBrowser? browser = null;

            try
            {
                if (browserType?.ToLower() == "chromium" || browserType?.ToLower() == "chrome")
                {
                    browser = await playwright.Chromium.ConnectAsync(cdpUrl);
                }
                else if (browserType?.ToLower() == "firefox")
                {
                    browser = await playwright.Firefox.ConnectAsync(cdpUrl);
                }
                else if (browserType?.ToLower() == "webkit")
                {
                    browser = await playwright.Webkit.ConnectAsync(cdpUrl);
                }
                else
                {
                    throw new ArgumentException($"Unsupported browser type: {browserType}");
                }

                // Create a new browser context
                _browserContext = await browser.NewContextAsync();

                // Create a new page within the browser context
                _page = await _browserContext.NewPageAsync();

                // Perform actions
                await _page.GotoAsync("https://duckduckgo.com", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                await _page.WaitForSelectorAsync("[name='q']");
                await _page.Locator("[name='q']").ClickAsync();
                await _page.FillAsync("[name='q']", "LambdaTest");
                await _page.Keyboard.PressAsync("Enter");
                await _page.WaitForURLAsync(url => url.Contains("q=LambdaTest"), new PageWaitForURLOptions { Timeout = 10000 });

                var title = await _page.TitleAsync();

                if (title.Contains("LambdaTest at DuckDuckGo"))
                {
                    await SetTestStatus("passed", "Title matched");
                }
                else
                {
                    await SetTestStatus("failed", "Title not matched");
                }
            }
            catch (Exception ex)
            {
                if (_page != null)
                {
                    await SetTestStatus("failed", ex.Message);
                }
                throw;
            }
            finally
            {
                // Clean up
                if (_browserContext != null) await _browserContext.CloseAsync();
                if (browser != null) await browser.CloseAsync();
            }
        }

        private async Task SetTestStatus(string status, string remark)
        {
            if (_page != null)
            {
                await _page.EvaluateAsync("_ => {}", $"lambdatest_action: {{\"action\": \"setTestStatus\", \"arguments\": {{\"status\":\"{status}\", \"remark\": \"{remark}\"}}}}");
            }
        }
    }
}
