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
        private IBrowser? browser;

        [Test]
        public async Task RunPlaywrightTest()
        {
            Console.WriteLine("Starting Playwright test on LambdaTest...");

            using var playwright = await Playwright.CreateAsync();

            // Fetch parameters from TestContext.Parameters
            string? user = TestContext.Parameters.Get("LT_USERNAME", null);
            string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY", null);
            string? platform = TestContext.Parameters.Get("PLATFORM", null);
            Console.WriteLine($"platform = {platform}");
            string? browserType = TestContext.Parameters.Get("BROWSER_NAME", null);
            Console.WriteLine($"browserType = {browserType}");
            string? browserVersion = TestContext.Parameters.Get("BROWSER_VERSION", null);
            string? cdpUrlBase = TestContext.Parameters.Get("CDP_URL_BASE", null);

            // Validate mandatory parameters
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(accessKey)
                || string.IsNullOrEmpty(cdpUrlBase))
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

            capabilities.Add("browserName", browserType);
            capabilities.Add("browserVersion", browserVersion);
            capabilities.Add("LT:Options", ltOptions);

            string capabilitiesJson = JsonConvert.SerializeObject(capabilities);
            string cdpUrl = cdpUrlBase + "?capabilities=" + Uri.EscapeDataString(capabilitiesJson);

            Console.WriteLine($"CDP URL: {cdpUrl}");

            // Connect to LambdaTest
            // Conditional browser instantiation
            if (browserType != null)
            {
                if (browserType.ToLower() == "chromium" || browserType.ToLower() == "chrome")
                {
                    TestContext.WriteLine("Using Chromium for browser type.");
                    browser = await playwright.Chromium.ConnectAsync(cdpUrl);
                }
                else
                {
                    TestContext.WriteLine("Using browser type from playwright[browserType].");
                    var browserTypeInstance = browserType switch
                    {
                        "firefox" => playwright.Firefox,
                        "webkit" => playwright.Webkit,
                        _ => null
                    };
                    browser = await playwright[browserType].ConnectAsync(cdpUrl);

                }
                var context = await browser.NewContextAsync();
                var page = await context.NewPageAsync();

                try
                {
                    await page.GotoAsync("https://duckduckgo.com", new PageGotoOptions { WaitUntil = WaitUntilState.NetworkIdle });
                    await page.WaitForSelectorAsync("[name='q']");
                    await page.Locator("[name='q']").ClickAsync();
                    await page.FillAsync("[name='q']", "LambdaTest");
                    await page.Keyboard.PressAsync("Enter");
                    await page.WaitForURLAsync(url => url.Contains("q=LambdaTest"), new PageWaitForURLOptions { Timeout = 10000 });
                    var title = await page.TitleAsync();

                    if (title.Contains("LambdaTest at DuckDuckGo"))
                    {
                        await SetTestStatus1("passed", "Title matched", page);
                    }
                    else
                    {
                        await SetTestStatus1("failed", "Title not matched", page);
                    }
                }
                catch (Exception err)
                {
                    await SetTestStatus1("failed", err.Message, page);
                }
                finally
                {
                    await context.CloseAsync();
                    await browser.CloseAsync();
                }
            }
        }

        public static async Task SetTestStatus1(string status, string remark, IPage page)
        {
            await page.EvaluateAsync("_ => {}", $"lambdatest_action: {{\"action\": \"setTestStatus\", \"arguments\": {{\"status\":\"{status}\", \"remark\": \"{remark}\"}}}}");
        }
    }
}