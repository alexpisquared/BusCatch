using Windows.ApplicationModel;
using MVVM.Common;
using BusCatch.VwMdl;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using System;
using AsLink;

namespace BusCatch
{
    public sealed partial class MainPage : Page //? BackButtonPage 
    {
        TtcViewModel _ttcViewModel;

        public MainPage()
        {
            this.InitializeComponent();

#if SnapShotTime
      ApplicationView.PreferredLaunchViewSize = new Size(1364, 735);
      ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
#else
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.Auto; //tu: keeps same locn and size between launches.
#endif

            _ttcViewModel = ViewModelDispatcher.TtcViewModel;
            _ttcViewModel.Map = pnlMapr.FindName("map1") as MapControl;
            DataContext = _ttcViewModel;
            //onLockL();

            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;

#if DEBUG
            ApplicationView.GetForCurrentView().Title = /*tbVer.Text =*/ $@"Dbg: built {(DateTime.Now - DevOp.BuildTime(typeof(App))).TotalDays:N1} days ago";
#else
            ApplicationView.GetForCurrentView().Title = /*tbVer.Text =*/ $@"Rls: {DevOp.BuildTime(typeof(App))}";
#endif
        }
        void Application_Suspending(object s, SuspendingEventArgs e)
        {
            if (Frame.CurrentSourcePageType == typeof(MainPage))       // Handle global application events only if this page is active
            {
                var deferral = e.SuspendingOperation.GetDeferral();

                //do some stuff here: t5.Text += "s·";

                deferral.Complete();
            }
        }
        async void Application_Resuming(object s, object o)
        {
            if (Frame.CurrentSourcePageType == typeof(MainPage))       // Handle global application events only if this page is active
            {
                await _ttcViewModel.CheckStartNearestCurrentStopTracking();
            }
        }


        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //_ttcViewModel.BackButtonVisibility = BackButtonVisibility_Page = _ttcViewModel.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
            SystemNavigationManager.GetForCurrentView().BackRequested += onBackRequested;
            base.OnNavigatedTo(e);

            //ApplicationData.Current.RoamingSettings.Values["Exn"] = "WWWWWWWWWWWWWWWWWWWWWWW EEEEEEEEEEEEEEEE 3333333333333333333 111111111111111111111 4444444444444444";

            if (!string.IsNullOrEmpty(ApplicationData.Current.RoamingSettings.Values["Exn"]?.ToString()))
                tbError.Text = $"{ApplicationData.Current.RoamingSettings.Values["Exn"]}";

            await _ttcViewModel.RequestLocationAccessAsync();
        }
        protected override async void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            SystemNavigationManager.GetForCurrentView().BackRequested -= onBackRequested;
            await _ttcViewModel.OnNavigatedFrom();
        }

        async void onBackRequested(object sender, BackRequestedEventArgs e)
        {
            if (_ttcViewModel.CanGoBack)
            {
                e.Handled = true;
                await _ttcViewModel.GoBack();
                //_ttcViewModel.BackButtonVisibility = BackButtonVisibility_Page = _ttcViewModel.CanGoBack ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;
            }
        }

        void onSizeToStd1366x768(object sender, Windows.UI.Xaml.RoutedEventArgs e) { Width = 1366; Height = 768; }
        void onMakeVisible(object sender, Windows.UI.Xaml.RoutedEventArgs e) { msgBrd.Visibility = Windows.UI.Xaml.Visibility.Visible; }

        //public AppViewBackButtonVisibility BackButtonVisibility_Page
        //{
        //  get { return SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility; }
        //  set { SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = value; }
        //}


        void onClearExeptionMsg(object sender, RoutedEventArgs e) => ApplicationData.Current.RoamingSettings.Values["Exn"] = tbError.Text = "";


        void onLockP(object sender = null, RoutedEventArgs e = null) { DisplayInformation.AutoRotationPreferences = (tgLockP.IsChecked = DisplayInformation.AutoRotationPreferences != DisplayOrientations.Portrait) ? DisplayOrientations.Portrait : DisplayOrientations.None; }
        void onLockL(object sender = null, RoutedEventArgs e = null) { DisplayInformation.AutoRotationPreferences = (tgLockL.IsChecked = DisplayInformation.AutoRotationPreferences != DisplayOrientations.Landscape) ? DisplayOrientations.Landscape : DisplayOrientations.None; }
    }
}
