using MVVM.Common;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using TTC.Svc;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace BusCatch
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
#if DebugNoUI
            establishEtaUpdateTimeV();
            if (Debugger.IsAttached) return;
#else
#endif
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(Microsoft.ApplicationInsights.WindowsCollectors.Metadata | Microsoft.ApplicationInsights.WindowsCollectors.Session);
            InitializeComponent();
            Suspending += OnSuspending;
            Resuming += OnResuming;
        }

#if DebugNoUI
        async void establishEtaUpdateTimeV()
        {
            await establishLocationsUpdateTime();
            await Task.Delay(30000);
            await establishLocationsUpdateTime();
        }

        async Task establishLocationsUpdateTime()
        {
            var rex0 = await WebSvc.GetVehLocations("ttc", "504", 0);
            for (int i = 0; i < 20; i++)
            {
                var rex1 = await WebSvc.GetVehLocations("ttc", "504", 0);
                Debug.WriteLine($"\t{rex0.vehicle[0].lat,12} =?= {rex1.vehicle[0].lat,12}");
                //if (i > 40 && rex0.predictions.direction[0].prediction[0].seconds != rex1.predictions.direction[0].prediction[0].seconds) { Debug.WriteLine(i); break; }

                await Task.Delay(999);
            }

        }
        async Task establishEtaUpdateTime() // predicitions are always fresh. locations
        {
            var rex0 = await WebSvc.GetPredictions("ttc", "504", "23893", false);
            for (int i = 0; i < 20; i++)
            {
                var rex1 = await WebSvc.GetPredictions("ttc", "504", "23893", false);
                Debug.WriteLine($"{(rex0.predictions.direction[0].prediction[0].epochTime - rex1.predictions.direction[0].prediction[0].epochTime),6}\t{rex0.predictions.direction[0].prediction[0].seconds,4} =?= {rex1.predictions.direction[0].prediction[0].seconds,4}");
                if (i > 40 && rex0.predictions.direction[0].prediction[0].seconds != rex1.predictions.direction[0].prediction[0].seconds) { Debug.WriteLine(i); break; }

                await Task.Delay(999);
            }

        }
#endif

        void OnResuming(object sender, object e) { }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected /*async */override void OnLaunched(LaunchActivatedEventArgs e)
        {

#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }

            //await new TtcViewModel().TestMethod1();
            //return;
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                rootFrame.Navigate(typeof(MainPage), e.Arguments);
            }
            // Ensure the current window is active
            Window.Current.Activate();
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity

            await ViewModelDispatcher.TtcViewModel.OnNavigatedFrom();

            deferral.Complete();
        }
    }
}


///todo:
/// - announce time to go moment.
/// - improve measurement of walking to the stop time by using pedestrian path to destination feature.
/// - 
/// Description:
/// - Automated next bus tracking for the nearest favorite bus stop.
/// 
/// "Tap on the TA to see the list of its routes"
/// - route  busstop list: align to bottom/?/top
/// 
/// May 2017
///   