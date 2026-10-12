namespace AdventurersGuild.Desktop;

public partial class App : System.Windows.Application
{
    public App()
    {
        DispatcherUnhandledException += (s, e) =>
        {
            System.Windows.MessageBox.Show(
                e.Exception.ToString(),
                "Dispatcher Exception",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            e.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as System.Exception;
            System.Windows.MessageBox.Show(
                ex?.ToString() ?? "Unknown",
                "AppDomain Exception",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        };
    }
}