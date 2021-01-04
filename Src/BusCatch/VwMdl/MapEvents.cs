using BusCatch.Model;
using MVVM.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TTC.Model2015;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        void onMapElntClk(MapControl map, MapElementClickEventArgs args)
        {
            if (args.MapElements.FirstOrDefault(x => x is MapIcon && ((MapIcon)x).Image == _mapIconStreamReference_Agency) is MapIcon clickedAgency)
            {
                MsgBrdVsblty = Visibility.Collapsed;
                //hides TAs: IsRouteListVw = true;
                RouteLstVsblty = Visibility.Visible;
                _ignoreMapTap = true;

                if (_agncyTag != clickedAgency.Title)
                {
                    _agncyTag = clickedAgency.Title;
                    SelectAgency = AgncyOC.FirstOrDefault(r => r.tag == _agncyTag);
                    loadRoutesForSelectAgency(_agncyTag, _isGtfs = SelectAgency.IsGtfs); // 
                }

                return;
            }

            if (_viewMode == ViewModes.VehTrackg)
            {
                _clickedRtStop = args.MapElements.FirstOrDefault(x => x is MapIcon && ((MapIcon)x).Image == _mapIconStreamReference_RtStop) as MapIcon;
                if (_clickedRtStop != null)
                {
                    _ignoreMapTap = true;

                    //if (_viewMode == ViewModes.RouteList)
                    //  onTglFav(tryGetNearestStop(args.Location));
                    //else
                    {
                        VhStopMenuTtl = _clickedRtStop.Title;

                        var x = FavColWidth + args.Position.X - 40 + 140 > map.ActualWidth ? map.ActualWidth - 140 : FavColWidth + args.Position.X - 40;
                        var y = args.Position.Y - 60 + 200 > map.ActualHeight ? map.ActualHeight - 200 : args.Position.Y - 60;

                        VhStopMenuPsn = new Thickness(x, y, 0, 0);
                        VhStopMenuVis = Visibility.Visible;
                        return;
                    }
                }
            }
        }
        void onMapTappped(MapControl map, MapInputEventArgs args)
        {
            if (_ignoreMapTap) { _ignoreMapTap = false; return; }

            if (_viewMode == ViewModes.RouteList)
                onTglFav(tryGetNearestStop(args.Location));
            else if (VhStopMenuVis == Visibility.Visible)
                VhStopMenuVis = Visibility.Collapsed;
        }
        void onMapHolding(MapControl map, MapInputEventArgs args)
        {
            //var clickedRtStop = tryGetNearestStop(args.Location);
            //if (clickedRtStop == null)
            //  return;

            //VhStopMenuTtl = clickedRtStop.title;
            //VhStopMenuPsn = new Thickness(FavColWidth + args.Position.X - 40, args.Position.Y - 60, 0, 0);
            //VhStopMenuVis = Visibility.Visible;
        }

        void onMapCenterChanged(MapControl map, object args)
        {
            if (_selectRtStop != null) Debug.WriteLine("* onMapCenterChanged: {0:N4} {1:N4} - {2}.", _map.Center.Position.Latitude, _map.Center.Position.Longitude, _selectRtStop == null ? "Ignored" : "Updated");

            _stngs.MapCenterLat = _map.Center.Position.Latitude;
            _stngs.MapCenterLon = _map.Center.Position.Longitude;

            if (CurrentVS != null && _map.ZoomLevel > _autoSaveableZoom) //update fav stop view for later use;  store only usable zoom per vstop.
            {
                CurrentVS.MapCenterLat = _map.Center.Position.Latitude;
                CurrentVS.MapCenterLon = _map.Center.Position.Longitude;
            }
        }
        void onMapZoomLvlChangd(MapControl map, object args)
        {
            ZoomLvl = string.Format("zm: {0:N1}", _map.ZoomLevel);

            _stngs.MapZoom = _map.ZoomLevel;

            VLocnPredOC.ToList().ForEach(cvs => cvs.Zoom = _map.ZoomLevel);

            if (CurrentVS != null && _map.ZoomLevel > _autoSaveableZoom) //update fav stop view for later use;  store only usable zoom per vstop.
            {
                CurrentVS.MapZoomLevel = _map.ZoomLevel;
            }

            BusImgVsblty = _map.ZoomLevel > _autoSaveableZoom ? Visibility.Visible : Visibility.Collapsed; //todo: how to consume on UI side?
        }

        async Task Add2CurVSs(MapIcon clickedRtStop)
        {
            try
            {
                stopAndClearPredictions();
                CurrentVS = FavVSOC.FirstOrDefault(r => r.StopName == clickedRtStop.Title);
                CurVSs.Add(CurrentVS);
                CpuUse = $"Tracking {CurVSs.Count} stops.";
                _isGtfs = CurrentVS.IsGtfs;
                setupVehStopPresets(_CurrentVS);
                await startVehTrackng($"Launching tracking for {CurrentVS?.StopName}");
            }
            catch (Exception ex) { ApplicationView.GetForCurrentView().Title = ex.Message; }
            finally { VhStopMenuVis = Visibility.Collapsed; }
        }
        void RemvFrmCur(MapIcon clickedRtStop)
        {
            try
            {
                stopAndClearPredictions();
                CurrentVS = FavVSOC.FirstOrDefault(r => r.StopName == clickedRtStop.Title);
                CurVSs.Remove(CurrentVS);
                CpuUse = $"Tracking {CurVSs.Count} stops.";
            }
            catch (Exception ex) { ApplicationView.GetForCurrentView().Title = ex.Message; }
            finally { VhStopMenuVis = Visibility.Collapsed; }
        }
        void DeletFavVS(MapIcon clickedRtStop)
        {
            try
            {
                if (clickedRtStop == null)
                    return;

                var x = FavVSOC.FirstOrDefault(r => r.StopName == clickedRtStop.Title);
                if (x == CurrentVS)
                    stopAndClearPredictions();

                CurVSs.Remove(CurrentVS);
                FavVSOC.Remove(x);
                _stngs.FavVSLst = FavVSOC;
                var vs = _map.MapElements.FirstOrDefault(r => ((MapIcon)r).Title == x.StopName);
                if (vs != null)
                {
                    _map.MapElements.Remove(vs);
                    _map.Children.Remove(_currentGrid);
                    removeMapElementsByZIndex(_map, ZIdx.MinKm);
                }

                ShowRoutsBtnVis = FavVSOC.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex) { ApplicationView.GetForCurrentView().Title = ex.Message; }
            finally { VhStopMenuVis = Visibility.Collapsed; }
        }
        async void setupVehStopPresets(FavVehStop value)
        {
            if (_CurrentVS == null)
                return;

            _stngs.LastVStop = value;

            if (_CurrentVS.VStopTag.EndsWith("_ar")) // arrivals are not interesting (Jan2018).
            {
                CurrentVS.VStopTag = CurrentVS.VStopTag.Replace("_ar", "");
                Debug.WriteLine($"_ar removed from {CurrentVS.VStopTag}");
            }

            if (_prevVStop != _CurrentVS.VStopTag) _prevVStop = _CurrentVS.VStopTag;

            placeMinKmLine(_map, _prevPosition, _prevPosition, CurrentVS.Location.Position, ZIdx.MinKm, Colors.DodgerBlue, DateTime.Now - _prevPosChdTm, ref _currentGrid); // placeMinKmLine(_map, _stngs.LatestLocnLat, _stngs.LatestLocnLon, CurrentVS.Lat, CurrentVS.Lon, ZIdx.MinKm, Colors.Red, ref _currentGrid);

            if (_CurrentVS.MapZoomLevel == 12.0012)
                await setMapBounds(_map, _stngs.LatestLocnLat, _stngs.LatestLocnLon, _CurrentVS.Lat, _CurrentVS.Lon);
            else
                await _map.TrySetViewAsync(CurrentVS.MapCenter, CurrentVS.MapZoomLevel);

            _prevAgncy = CurrentVS.AgncyTag;
            _prevRoute = CurrentVS.RouteTag;
            _prevVStop = CurrentVS.VStopTag;

            await loadPlotRouteForCurAgncyRoute(false); //             Task.Run(async () => await webLoadPlotRoute(false));
        }
        bodyRouteStop tryGetNearestStop(Geopoint location)
        {
            var nearestStop = VStopOC.Select(x => new                                 //tu: anonymous holder of the matching record!!!!!!!!
            {
                nearest = x,
                basicGP = new BasicGeoposition() { Latitude = x.lat, Longitude = x.lon }
            })
            .OrderBy(y => GetDistanceTo(y.basicGP, location.Position))
            .FirstOrDefault().nearest;
            nearestStop.IsGtfs = _isGtfs;
            return nearestStop;
        }
    }
}
