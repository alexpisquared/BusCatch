using AsLink;
using BusCatch.Model;
using MVVM.Common;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        BasicGeoposition _prevPosition = new BasicGeoposition { Altitude = 0, Latitude = 40, Longitude = -80 };
        DateTime _prevPosChdTm = DateTime.Now, _nextTimeToCheckCVS = DateTime.MinValue;
        const int _hoursToStayOnCVS = 2;
        Geolocator _geolocator;
        void toggleGeoTracking(bool startRquested)
        {
            if (startRquested)
            {
                if (_geolocator != null) return;

                _prevPosition = new BasicGeoposition { Altitude = 0, Latitude = _stngs.LatestLocnLat, Longitude = _stngs.LatestLocnLon };

#if DEBUG
                _geolocator = new Geolocator { ReportInterval = 1000 };   // every 1 second (other intervals do not woek).
#else
                _geolocator = new Geolocator { MovementThreshold = 50 };  // every 50 meters 
#endif

                _geolocator.PositionChanged += onPositnChanged;
                _geolocator.StatusChanged += onStatusChanged;
            }
            else
            {
                removeMapElementsByTag(_map, "CurLocation");
                removeMapElementsByZIndex(_map, ZIdx.MinKm);
                _map.Children.Remove(_currentGrid);
                _distCurToStopM = 10;

                if (_geolocator != null)
                {
                    _geolocator.StatusChanged -= onStatusChanged;
                    _geolocator.PositionChanged -= onPositnChanged;
                    _geolocator = null;
                }
            }
        }

        async void onPositnChanged(Geolocator s, PositionChangedEventArgs args)
        {
            try
            {            // Debug.WriteLine($"{DateTime.Now:HH:mm:ss.f}  gl_PositnChanged: {args.Position.Coordinate.Point.Position.Altitude:N2} {args.Position.Coordinate.Point.Position.Latitude:N8}:{args.Position.Coordinate.Point.Position.Longitude:N8}   SelectRtStop: {(SelectRtStop == null ? " == null" : SelectRtStop.title)}.");
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, onPositnChangedForUI(args.Position.Coordinate.Point.Position));
            }
            catch (Exception ex) { ex.Pop(); }
        }

        DispatchedHandler onPositnChangedForUI(BasicGeoposition position)
        {
            var now = DateTime.Now;
            return async () =>
            {
                if (position.Altitude != 0) //tu: there IS altitude in the position; strip it since map is on level 0 anyway.
                    position.Altitude = 0;
                _map.AddMovHerMark(position, "CurLocation", _brBlk);

                _map.MapElements.Add(breadcrumb(position, 6, Color.FromArgb(255, 0, 0, 255), (int)ZIdx.MyLocation)); // breadcrumbs

                if (!IsVehTracngVw)
                    return;

                if (/*args != null && */CurrentVS != null && _nextTimeToCheckCVS > now) // if time to change the current VS to this new location's nearest.
                {
                    lineVStoBus0();
                    placeMinKmLine(_map, _prevPosition, position, CurrentVS.Location.Position, ZIdx.MinKm, Colors.Magenta, DateTime.Now - _prevPosChdTm, ref _currentGrid); // placeMinKmLine(_map, _stngs.LatestLocnLat, _stngs.LatestLocnLon, CurrentVS.Lat, CurrentVS.Lon, ZIdx.MinKm, Colors.Red, ref _currentGrid);
                    await zoomTo1stBusses(position);
                }
                else
                {
                    await setNearestStopForTracking(now, _stngs.LatestLocation);
                }

                _prevPosChdTm = now;
                _prevPosition = position;
                _stngs.LatestLocnLat = _prevPosition.Latitude;
                _stngs.LatestLocnLon = _prevPosition.Longitude;

            };
        }

        async Task setNearestStopForTracking(DateTime now, BasicGeoposition myLocation)
        {
            if (!FavVSOC.Any()) { return; }

            CurrentVS = FavVSOC.Select(x => new                                 //tu: anonymous holder of the matching record!!!!!!!!
            {
                favStop = x,
                nearest = new BasicGeoposition() { Latitude = x.Lat, Longitude = x.Lon }
            }).OrderBy(y => GetDistanceTo(y.nearest, myLocation)).First().favStop;

            CurVSs.Clear();
            CurVSs.Add(CurrentVS);

            VLocnPredOC = new ObservableCollection<TTC.Model2015.bodyVehicle>(); // clear currently tracked busses.

            await startVehTrackng($"Launching tracking for {CurrentVS.StopName}.");

            _nextTimeToCheckCVS = now.AddHours(_hoursToStayOnCVS);
        }

        void lineVStoBus0()
        {
            if (CurrentVS == null) return;
            try
            {
                var bus0 = VLocnPredOC.OrderBy(r => r?.Predictn?.seconds).FirstOrDefault();
                if (bus0 != null)
                    placeLine(_map, new BasicGeoposition { Latitude = bus0.lat, Longitude = bus0.lon }, CurrentVS.Location.Position, ZIdx.Bus0, Colors.Orange);
            }
            catch (Exception ex) { ex.Pop(); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
        }

        async Task zoomTo1stBusses(BasicGeoposition here, int x1stBusses = 1)
        {
            try
            {
                if (!IsZ2P) return;
                var pois = VLocnPredOC.OrderBy(r => r?.Predictn?.seconds).Take(x1stBusses).Select(r => new BasicGeoposition { Latitude = r.lat, Longitude = r.lon }).ToList();

                if (CurrentVS != null)
                    pois.Add(new BasicGeoposition { Latitude = CurrentVS.Lat, Longitude = CurrentVS.Lon });

                if (IsTrackingLocation)
                    pois.Add(here);

                if (pois.Count > 0)
                    await _map.TrySetViewBoundsAsync(GeoboundingBox.TryCompute(pois), new Thickness(80), MapAnimationKind.Default);
            }
            catch (Exception ex) { ex.Pop(); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
        }

        async void onStatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            Debug.WriteLine("* Geolocator StatusChanged: {0}.", args.Status);

            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => NotifyUser($"Geolocator Status: {args.Status}"));
        }
        async Task<Geoposition> GetGeolocation(uint? desiredAccuracyInMeters)
        {
            try
            {
                var accessStatus = await Geolocator.RequestAccessAsync();
                switch (accessStatus)
                {
                    case GeolocationAccessStatus.Allowed:
                        //75:_cts = new CancellationTokenSource();            //75:CancellationToken token = _cts.Token;

                        //NotifyUser("Geolocator: Waiting for an update...");

                        var lcr = new Geolocator { DesiredAccuracyInMeters = desiredAccuracyInMeters };
                        var pos = await lcr.GetGeopositionAsync();//.AsTask(token);

                        //NotifyUser("Geolocator: Location updated.");
                        return pos;

                    case GeolocationAccessStatus.Denied: NotifyUser("Access to location is denied."); LetLcnVsblty = Visibility.Visible; break;
                    case GeolocationAccessStatus.Unspecified: NotifyUser("Unspecified error."); break;
                }
            }
            catch (TaskCanceledException) { NotifyUser("Canceled."); }
            catch (Exception ex) { NotifyUser(ex.ToString()); }
            finally { /*  //75:_cts = null;*/ }
            return null;
        }
        //75: CancellationTokenSource _cts = null;
    }
}
