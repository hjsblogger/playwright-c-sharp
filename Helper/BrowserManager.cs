using Microsoft.Playwright;
// using System.Collections.Generic;
// using System.Threading.Tasks;

namespace PlaywrightTest.Helper
{
    public class BrowserManager
    {
        public Dictionary<string, IBrowserContext> BrowserContexts { get; private set; } = new();
        public Dictionary<string, IPage> BrowserTabs { get; private set; } = new();
        public Dictionary<string, List<string>> TabsMap { get; private set; } = new();
        public string CurrentBrowserKey { get; private set; } = "default";
        public string CurrentTabKey { get; private set; } = "default";

        public async Task StartBrowser(IBrowserContext context)
        {
            // Initialize browser contexts
            BrowserContexts = new Dictionary<string, IBrowserContext> { { "default", context } };

            /* Create a new page within the default context */
            var page = await context.NewPageAsync().ConfigureAwait(false);
            BrowserTabs = new Dictionary<string, IPage> { { "default", page } };

            // Set current browser and tab keys
            CurrentBrowserKey = "default";
            CurrentTabKey = "default";

            // Map tabs to contexts
            TabsMap = new Dictionary<string, List<string>>
            {
                { "default", new List<string> { "default" } }
            };
        }

        public IPage GetCurrentTab() => BrowserTabs[CurrentTabKey];

        public IBrowserContext GetCurrentContext() => BrowserContexts[CurrentBrowserKey];
    }
}