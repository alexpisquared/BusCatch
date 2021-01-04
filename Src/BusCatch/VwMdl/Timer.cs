using AsLink;
using BusCatch.Model;
using MVVM.Common;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TTC.Model2015;
using TTC.WP.Misc;
using Windows.Devices.Geolocation;
using Windows.Devices.Power;
using Windows.System.Power;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Shapes;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        const string _curBus = "currentBus", _exprs = "E";
        bool _firstTime = true;
        int _cntDn = 4;
        DateTimeOffset _prevResChkTime = DateTimeOffset.MinValue;
        TimeSpan _prevu;
        bodyPredictionsDirectionPrediction _1stBus, _2ndBus;

        async Task dt_Tick(object s, object e)
        {
            if (_busyInWebCall) return;

            try
            {
                if (_ining) // a kludge to asynch load prev selection:
                {
                    await setViewModeAddRmvTrk(FavVSOC.Count() == 0 ? ViewModes.AgncySeln : ViewModes.VehTrackg);
                    foreach (var fvs in FavVSOC) addMapIcon(fvs.StopName, _mapIconStreamReference_RtStop, new Geopoint(new BasicGeoposition() { Latitude = fvs.Lat, Longitude = fvs.Lon }));
                }
                else if (DateTime.Now > _nextWebCall)
                {
                    await webCallUpdate();
                }
                else
                {
                    quickUIUpdate();
                }
            }
            catch (Exception ex) { ex.Pop(); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
        }

        void quickUIUpdate()
        {
            try
            {
                Timer_360 = -360 * (_nextWebCall - DateTime.Now).TotalSeconds / _webCallIntervalSec;
                VLocnPredOC.Where(v => v.Predictn != null).ToList().ForEach(vp =>
                {
                    vp.Predictn.seconds -= _quickUiRefreshSec;
                    vp.SmartMinSecLbl = vp.CalcSmartMinSecLbl(vp.Predictn.seconds, vp.Predictn.epochTime, vp.Predictn.isDeparture);
                    vp.secsSinceReport += _quickUiRefreshSec;
                });

                //CPU intence - redrawBlueDebugVectors(Color.FromArgb(255, 0, 0, 255)); //88* moveVectors_NOGO(Color.FromArgb(255, 0, 0, 255));
            }
            catch (Exception ex) { ex.Pop(); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
        }
        async Task webCallUpdate()
        {
            _busyInWebCall = true;
            NotInWebCall = Visibility.Collapsed;

            var period = --_cntDn > 0 ? _webCallIntervalSec / _cntDn : _webCallIntervalSec;
            _nextWebCall = DateTime.Now.AddSeconds(period);

            try
            {
                //doesnot make sence with null: onPositnChangedForUI(); // new on mar 2018
                CpuUse = batteryReport();// cpuLoadReport();
                await webCallUpdate_VLocn_PredOC();          //MsgBrdText = string.Format("IsAll == {0} => {1} busses.", IsAll, VLocnPredOC.Count());
                VLocnPredOC.ToList().ForEach(vp => _map.MapElements.Add(breadcrumb(vp.lat, vp.lon, 6, Color.FromArgb(255, 255, 128, 0), (int)ZIdx.Vector))); // breadcrumbs
                await announceArrivers();
            }
            catch (Exception ex) { ex.Pop(); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
            finally
            {
                //Debug.WriteLine($"--period: {period}");

                _busyInWebCall = false;
                NotInWebCall = Visibility.Visible;
            }
        }

        void redrawBlueDebugVectors(Color vectorColor) // too CPU intence!!!
        {
#if !DEBUG
            removeMapElementsByZIndex(_map, ZIdx.Vector);
#endif

            VLocnPredOC.ToList().ForEach(vp =>
            {
                _map.MapElements.Add(breadcrumb(vp.lat, vp.lon, 6, vectorColor, (int)ZIdx.Vector));
                //var pointB = GeoHelper.GetTargetLocation(vp.lat, vp.lon, -vp.heading, (vp.SecsSinceReportAbs / 60.0));
                //_map.MapElements.Add(newBusProjectionVector(vp.lat, vp.lon, pointB, vectorColor));
                //moveMapChildren_Triangle(pointB.X, pointB.Y, 1, _blu, vp.heading, vp.id.ToString());
            });
        }

        void removeMapChildenByTag(string tag) { _map.Children.Where(r => r is Shape && ((r as Shape).Tag as string) == tag).ToList().ForEach(bus => _map.Children.Remove(bus)); }
        void minimizeCrntBussesToCrumbs() { _map.Children.Where(r => r is Shape && ((r as Shape).Tag as string) == _curBus).ToList().ForEach(bus => { ((Shape)bus).Width = ((Shape)bus).Height = 2; ((Shape)bus).StrokeThickness = 3; ((Shape)bus).Tag = "Old"; }); }

        async Task announceArrivers()
        {
            try
            {
                var totalVehs = _prevVPreds == null ? 0 : _prevVPreds.Count; // VLocnPredOC.Count;
                if (totalVehs < 1)
                {
                    await say(
                        !Connectivity.IsInternet() ? "Not connected to the Internet. Please check your connection and try again." :
                        _prevVStop == null ? "Acquiring location..." :
                        _prevRoute == null ? "Route is not yet decided." :
                        "Seems like no vehicles are on the route at the moment.");
                }
                else
                {
                    var vehsWithPredn = _prevVPreds; // VLocnPredOC.Where(v => v.Predictn != null);
                    if (vehsWithPredn.Count() < 1)
                    {
                        await say(string.Format("Out of {0} vehicles currently on the route, none has ETA for the selected stop.", totalVehs));
                    }
                    else
                    {
                        const int speechOffsetSec = 1;

                        var chosens =
                            vehsWithPredn.Any(r => r.seconds > _jogEtaInSec) ?                           // if close enough to run-catch a bus ?
                            vehsWithPredn.Where(r => r.seconds > _jogEtaInSec).OrderBy(v => v.seconds) : // only within the run-time range     :
                            vehsWithPredn.OrderBy(v => v.seconds);                                       // all

                        _1stBus = chosens.ElementAtOrDefault(0);
                        _2ndBus = chosens.ElementAtOrDefault(1);

                        if (_1stBus == null || _2ndBus == null) return;

                        var secondsTo1stBus = _1stBus.EtaInSecByEpo - speechOffsetSec;
                        var timeto1stMsg = timeToMsg(secondsTo1stBus);

                        var expressesBeyond1stTwo = (!_1stBus.branch.EndsWith(_exprs) && !_2ndBus.branch.EndsWith(_exprs) && (vehsWithPredn.Any(r => r.branch.EndsWith(_exprs)))) ? ($"1st express in {timeToMsg(((int)vehsWithPredn.Where(r => r.branch.EndsWith(_exprs)).Min(r => r.EtaInSecByEpo) - speechOffsetSec))}") : "";

                        var secondsTo2ndBus = _2ndBus == null ? -1 : (int)_2ndBus.EtaInSecByEpo - speechOffsetSec;
                        if (secondsTo2ndBus > 0) // if more than 1 bus scheduled:
                            await say(secondsTo1stBus < 15 ?
                              $"The first bus {(_1stBus.branch.EndsWith(_exprs) ? " - Express - " : "")} is arriving. Next {(_2ndBus.branch.EndsWith(_exprs) ? ", Express, " : "")} coming in {timeToMsg(secondsTo2ndBus)}. {totalVehs - 2} others are following. {expressesBeyond1stTwo}" :
                              _firstTime ?
                                $"{totalVehs - 2} vehicles are {(_isAllVsPrdctd ? "on the route" : "scheduled for the stop")}; first {(_1stBus.branch.EndsWith(_exprs) ? "- Express -" : "")} coming in {timeto1stMsg}; second {(_2ndBus.branch.EndsWith(_exprs) ? " - Express - " : "")} - in {timeToMsg(secondsTo2ndBus)}.  {expressesBeyond1stTwo}" :
                                $"First {(_1stBus.branch.EndsWith(_exprs) ? "- Express" : "")} - in {timeto1stMsg}; second {(_2ndBus.branch.EndsWith(_exprs) ? "- Express" : "")} - in {timeToMsg(secondsTo2ndBus)}. {totalVehs - 2} more following.  {expressesBeyond1stTwo}");
                        else
                            await say(secondsTo1stBus < 15 ?
                             $"The first bus is arriving. {totalVehs - 1} others are following.  {expressesBeyond1stTwo}" :
                             _firstTime ?
                               $"{totalVehs - 1} vehicles are {(_isAllVsPrdctd ? "on the route" : "scheduled for the stop")}; first coming in {timeto1stMsg}.  {expressesBeyond1stTwo}" :
                               $"{totalVehs - 1} vehicles; first - in {timeto1stMsg}.  {expressesBeyond1stTwo}");

                        _firstTime = false;
                    }
                }
            }
            catch (Exception ex) { ex.Pop(); await say(ex.Message); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
        }

        static string timeToMsg(int sec) => sec < 600 ? TimeSpan.FromSeconds(sec).ToString() : $"{(sec / 60.0):N0} minutes";

        string cpuLoadReport()
        {
            var rv = "...";
            var pdi = Windows.System.Diagnostics.ProcessDiagnosticInfo.GetForCurrentProcess();
            var cpu = pdi.CpuUsage.GetReport().UserTime;
            var now = DateTimeOffset.Now;
            var pst = now - pdi.ProcessStartTime;
            if (pst == TimeSpan.Zero) return "now == prcs start";

            if (_prevResChkTime > DateTimeOffset.MinValue)
            {
                var t = now - _prevResChkTime;
                var u = cpu - _prevu;
                if (t.TotalMilliseconds > 0)
                    rv = $"{(100d * u.TotalMilliseconds / t.TotalMilliseconds),6:N0} / {(100d * cpu.TotalMilliseconds / pst.TotalMilliseconds):N0} ";
            }

            _prevResChkTime = now;
            _prevu = cpu;

            return rv;
        }
        string batteryReport()
        {
            var br = Battery.AggregateBattery.GetReport();

            var curPercent = br.FullChargeCapacityInMilliwattHours == null || br.FullChargeCapacityInMilliwattHours.Value == 0 || br.RemainingCapacityInMilliwattHours == null ? 0 : 100d * br.RemainingCapacityInMilliwattHours.Value / br.FullChargeCapacityInMilliwattHours.Value;
            var HrLeft = (
              (br.ChargeRateInMilliwatts == null || br.ChargeRateInMilliwatts.Value == 0) ? 0 :
              br.Status == BatteryStatus.Charging ? ((br.FullChargeCapacityInMilliwattHours - br.RemainingCapacityInMilliwattHours) / (double)br.ChargeRateInMilliwatts) :
              br.Status == BatteryStatus.Discharging ? ((double)br.RemainingCapacityInMilliwattHours / br.ChargeRateInMilliwatts) :
              0);

            var rv = "...";

            var now = DateTimeOffset.Now;
            if (_prevResChkTime > DateTimeOffset.MinValue)
            {
                var t = now - _prevResChkTime;
                if (t.TotalMilliseconds > 0)
                {
                    rv = $"{curPercent:N1} %  \r\n{HrLeft:N2} hr";
                }
            }

            _prevResChkTime = now;

            return rv;
        }
    }
}