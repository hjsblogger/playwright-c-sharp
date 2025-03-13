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
        private IBrowser? _browser;
        private IBrowserContext? _browserContext;
        private IPage? _page;
        private IPlaywright? _playwright;
        private string? _cdpUrl;

        // public async Task StartBrowser()
        // {
        //     CurrentFixture.BrowserContexts = new Dictionary<string, IBrowserContext>{{"default", CurrentFixture.Context}};
        //     CurrentFixture.Page = await CurrentFixture.Context.NewPageAsync().ConfigureAwait(false);
        //     CurrentFixture.BrowserTabs = new Dictionary<string, IPage>{{"default", CurrentFixture.Page}};
        //     CurrentFixture.CurrentBrowserKey = "default";
        //     CurrentFixture.CurrentTabKey = "default";
        //     CurrentFixture.TabsMap = new Dictionary<string, List<string>>{{"default",
        //             new List<string>{"default"} }};
        //     CurrentFixture.TabsMap = new Dictionary<string, List<string>>{{"default",
        //             new List<string>{"default"} }};

        // }

        [OneTimeSetUp]
        public async Task OneTimeSetUp()
        {
            Console.WriteLine("Initializing resources for Playwright tests...");

            _playwright = await Playwright.CreateAsync();

            // Fetch parameters from TestContext.Parameters
            string? user = TestContext.Parameters.Get("LT_USERNAME", null);
            string? accessKey = TestContext.Parameters.Get("LT_ACCESS_KEY", null);
            string? platform = TestContext.Parameters.Get("PLATFORM", null);
            string? browserType = TestContext.Parameters.Get("BROWSER_NAME", null);
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
            _cdpUrl = $"{cdpUrlBase}?capabilities={Uri.EscapeDataString(capabilitiesJson)}";

            // Console.WriteLine($"CDP URL: {_cdpUrl}");

            // Connect to the browser based on browser type
            if (browserType?.ToLower() == "chromium" || browserType?.ToLower() == "chrome")
            {
                _browser = await _playwright.Chromium.ConnectAsync(_cdpUrl);
            }
            else if (browserType?.ToLower() == "firefox")
            {
                _browser = await _playwright.Firefox.ConnectAsync(_cdpUrl);
            }
            else if (browserType?.ToLower() == "webkit")
            {
                _browser = await _playwright.Webkit.ConnectAsync(_cdpUrl);
            }
            else
            {
                throw new ArgumentException($"Unsupported browser type: {browserType}");
            }
        }

        [Test]
        public async Task RunPlaywrightTest()
        {
            Console.WriteLine("Running Playwright test on LambdaTest...");

            // Create a new browser context with overridden options
            string? platform = TestContext.Parameters.Get("PLATFORM", null);
            var contextOptions = GetBrowserNewContextOptions(platform);
            _browserContext = await _browser.NewContextAsync(contextOptions);

            // Create a new page within the browser context
            _page = await _browserContext.NewPageAsync();

            try
            {
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
                // Clean up browser context
                if (_browserContext != null)
                {
                    await _browserContext.CloseAsync();
                }
            }
        }

        private async Task SetTestStatus(string status, string remark)
        {
            if (_page != null)
            {
                await _page.EvaluateAsync("_ => {}", $"lambdatest_action: {{\"action\": \"setTestStatus\", \"arguments\": {{\"status\":\"{status}\", \"remark\": \"{remark}\"}}}}");
            }
        }

        /// <summary>
        /// Overrides BrowserNewContextOptions to customize browser context.
        /// </summary>
        /// <param name="platform">The platform (Windows, macOS, etc.)</param>
        /// <returns>BrowserNewContextOptions</returns>
        private BrowserNewContextOptions GetBrowserNewContextOptions(string? platform)
        {
            var options = new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
                IgnoreHTTPSErrors = true,
                Locale = "en-US",
                UserAgent = "CustomUserAgent",
                Permissions = new[] { "geolocation", "notifications" }
            };

            if (!string.IsNullOrEmpty(platform) && platform.ToLower() == "macos")
            {
                options.DeviceScaleFactor = 2;
            }

            return options;
        }

        [OneTimeTearDown]
        public async Task OneTimeTearDown()
        {
            Console.WriteLine("Cleaning up resources for Playwright tests...");
            if (_browser != null)
            {
                await _browser.CloseAsync();
            }
            if (_playwright != null)
            {
                _playwright.Dispose();
            }
        }
    }
}