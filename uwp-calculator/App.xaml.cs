using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace HandwritingCalculatorUwp;

sealed partial class App : Application
{
    public App()
    {
        InitializeComponent();
        Suspending += OnSuspending;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs e)
    {
        var rootFrame = Window.Current.Content as Frame;

        if (rootFrame is null)
        {
            rootFrame = new Frame();
            rootFrame.NavigationFailed += OnNavigationFailed;
            Window.Current.Content = rootFrame;
        }

        if (rootFrame.Content is null)
        {
            rootFrame.Navigate(typeof(MainPage), e.Arguments);
        }

        Window.Current.Activate();
    }

    private static void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
    {
        throw new InvalidOperationException($"Failed to load page: {e.SourcePageType.FullName}");
    }

    private static void OnSuspending(object sender, SuspendingEventArgs e)
    {
    }
}
