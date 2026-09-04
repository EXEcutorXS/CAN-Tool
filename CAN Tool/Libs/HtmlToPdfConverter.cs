using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using System.Windows;

namespace CAN_Tool.Libs
{
    // Печатает HTML-строку в PDF через движок Edge/Chromium (WebView2 Runtime, уже стоит в
    // Windows 10/11 вместе с Edge). WebView2 рисует в offscreen-хвостах compositor'а, а не в
    // видимое окно, но WPF-контролу для инициализации всё равно нужен показанный (Show()) HWND-
    // родитель - поэтому используем окно за пределами экрана, а не Visibility.Hidden/Collapsed.
    public static class HtmlToPdfConverter
    {
        public static async Task ConvertAsync(string html, string pdfPath)
        {
            var window = new Window
            {
                Width = 1,
                Height = 1,
                WindowStyle = WindowStyle.None,
                ShowInTaskbar = false,
                ShowActivated = false,
                Left = -32000,
                Top = -32000,
            };

            var webView = new WebView2();
            window.Content = webView;
            window.Show();

            try
            {
                await webView.EnsureCoreWebView2Async();

                var navigationDone = new TaskCompletionSource<bool>();
                void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
                {
                    webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                    navigationDone.TrySetResult(e.IsSuccess);
                }
                webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
                webView.CoreWebView2.NavigateToString(html);
                await navigationDone.Task;

                var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                printSettings.ShouldPrintBackgrounds = true; // иначе пастельные заливки/тени карточек не попадут в PDF
                printSettings.MarginTop = 0.4;
                printSettings.MarginBottom = 0.4;
                printSettings.MarginLeft = 0.4;
                printSettings.MarginRight = 0.4;

                await webView.CoreWebView2.PrintToPdfAsync(pdfPath, printSettings);
            }
            finally
            {
                window.Close();
            }
        }
    }
}
