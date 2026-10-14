namespace AdventurersGuild.Desktop;

using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Services;
using Microsoft.AspNetCore.Components.WebView;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

public partial class MainWindow : Window
{
    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll")]
    private static extern bool ReleaseCapture();

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;

    private void StartWindowDrag()
    {
        var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        ReleaseCapture();
        SendMessage(hwnd, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
    }

    public MainWindow()
    {
        InitializeComponent();

        var services = new ServiceCollection();
        services.AddWpfBlazorWebView();
        services.AddMudServices();

        FileSystem.Current = new SystemFileSystem();

        services.AddSingleton<DataLoader>();
        services.AddSingleton<LocationRepository>();
        services.AddSingleton<CreatureRepository>();

        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<DataLoader>().EnsureLoaded();

        Resources.Add("services", sp);
    }

    private void OnBlazorInitialized(object? sender, BlazorWebViewInitializedEventArgs e)
    {
        e.WebView.CoreWebView2.WebMessageReceived += (s, args) =>
        {
            var msg = args.TryGetWebMessageAsString();

            Dispatcher.Invoke(() =>
            {
                switch (msg)
                {
                    case "close-app":
                        Close();
                        break;
                    case "minimize-app":
                        WindowState = WindowState.Minimized;
                        break;
                    case "toggle-maximize":
                        WindowState = WindowState == WindowState.Maximized
                            ? WindowState.Normal
                            : WindowState.Maximized;
                        break;
                    case "start-drag":
                        StartWindowDrag();
                        break;
                }
            });
        };
    }
}