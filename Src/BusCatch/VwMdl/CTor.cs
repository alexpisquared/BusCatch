using BusCatch.Model;
using MVVM.Common;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using TTC.Model2015;
using Windows.Devices.Geolocation;
using Windows.Storage.Streams;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        public TtcViewModel()
        {
            var g = Windows.Graphics.Display.DisplayInformation.GetForCurrentView();

#if DEBUG
            var info = string.Format("res scale:{0}  rppvp:{1}  lgcl dpi:{2}"
                , g.ResolutionScale
                , g.RawPixelsPerViewPixel
                , g.LogicalDpi
                );

            ApplicationView.GetForCurrentView().Title = info;
            NotifyUser(info);
#endif

            _mapIconStreamReference_Agency = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/AgencyPin.png"));
            _mapIconStreamReference_Walkng = RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/pedestrian-35x60.png"));
            _mapIconStreamReference_RtStop = g.ResolutionScale == Windows.Graphics.Display.ResolutionScale.Scale100Percent ?
                RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/BusStop_Fav_48x48.png")) :
                RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/BusStop_Fav_96x96.png"));
            FavVSOC = new ObservableCollection<FavVehStop>(_stngs.FavVSLst);
            RouteOC = new ObservableCollection<bodyRouteL>();
            VStopOC = new ObservableCollection<bodyRouteStop>();
            _vlopre = new ObservableCollection<bodyVehicle>();

            ShowRoutsBtnVis = FavVSOC.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

            if (_stngs.Is1stTm)
            {
                _stngs.Is1stTm = false;
                PrivacyPlcVis = Visibility.Visible;
            }

#if DEBUG
            DevOps = true;  // _DevOpsVsblty = Visibility.Visible;
#else
			DevOps = false; // _DevOpsVsblty = Visibility.Collapsed;
#endif

            _dt.Interval = TimeSpan.FromSeconds(_quickUiRefreshSec);
            _dt.Tick += async (s, e) => await dt_Tick(s, e);
            _dt.Start();
        }

        internal async Task RequestLocationAccessAsync()
        {
            var status = await Geolocator.RequestAccessAsync();
            switch (status)
            {
                case GeolocationAccessStatus.Allowed: LetLcnVsblty = Visibility.Collapsed; toggleGeoTracking(true); break;
                case GeolocationAccessStatus.Denied: LetLcnVsblty = Visibility.Visible; break;
                case GeolocationAccessStatus.Unspecified: LetLcnVsblty = Visibility.Collapsed; MsgBrdVsblty = Visibility.Visible; MsgBrdColor = _smRed; MsgBrdText = "GeoLocator: Unspecifed Error!"; break;
            }
        }
        internal async Task OnNavigatedFrom()
        {
            await Task.Delay(9);

            _stngs.LastVStop = CurrentVS;
            _stngs.LastVStops = CurVSs;
            _stngs.FavVSLst = FavVSOC;
        }
    }
}
