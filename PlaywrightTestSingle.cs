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
        [Test]
        public async Task RunPlaywrightTest()
        {
            Console.WriteLine("Starting Playwright test...");

            using var playwright = await Playwright.CreateAsync();

            // Fetch parameters from TestContext.Parameters
            string? user = TestContext.Parameters.Get("LT_USERNAME",
                    null);
            string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY",
                    null);
            string? platform = TestContext.Parameters.Get("PLATFORM",
                    null);
            string? browserName = TestContext.Parameters.Get("BROWSER_NAME",
                    null);
            string? browserVersion = TestContext.Parameters.Get("BROWSER_VERSION",
                    null);
            string? cdpUrlBase = TestContext.Parameters.Get("CDP_URL_BASE",
                    null);

            // Validate mandatory parameters
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(cdpUrlBase))
            {
                throw new ArgumentException("Required parameters are missing in the run settings.");
            }

            // Set capabilities
            Dictionary<string, object?> capabilities = new Dictionary<string, object?>();
            Dictionary<string, string?> ltOptions = new Dictionary<string, string?>
            {
                { "name", "Playwright Test" },
                { "build", "Playwright C-Sharp tests" },
                { "platform", platform },
                { "user", user },
                { "accessKey", accessKey }
            };

            capabilities.Add("browserName", browserName);
            capabilities.Add("browserVersion", browserVersion);
            capabilities.Add("LT:Options", ltOptions);

            string capabilitiesJson = JsonConvert.SerializeObject(capabilities);
            string cdpUrl = cdpUrlBase + "?capabilities=" + Uri.EscapeDataString(capabilitiesJson);

            await using var browser = await playwright.Chromium.ConnectAsync(cdpUrl);
            var page = await browser.NewPageAsync();

            try
            {
                await page.GotoAsync("https://duckduckgo.com", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                // Ensure the element is available before clicking and filling
                await page.WaitForSelectorAsync("[name='q']");
                await page.Locator("[name='q']").ClickAsync();
                await page.FillAsync("[name='q']", "LambdaTest");
                await page.Keyboard.PressAsync("Enter");
                // Wait for the title to contain "LambdaTest"
                await page.WaitForURLAsync(url => url.Contains("q=LambdaTest"), new PageWaitForURLOptions { Timeout = 10000 });
                var title = await page.TitleAsync();

                if (title.Contains("LambdaTest at DuckDuckGo"))
                {
                    // Use the following code to mark the test status.
                    await SetTestStatus("passed", "Title matched", page);
                }
                else {
                    await SetTestStatus("failed", "Title not matched", page);
                }
            }
            catch (Exception err)
            {
                await SetTestStatus("failed", err.Message, page);
            }
                await browser.CloseAsync();
        }
        public static async Task SetTestStatus(string status, string remark, IPage page)
        {
            await page.EvaluateAsync("_ => {}", "lambdatest_action: {\"action\": \"setTestStatus\", \"arguments\": {\"status\":\"" + status + "\", \"remark\": \"" + remark + "\"}}");
        }
    }
}