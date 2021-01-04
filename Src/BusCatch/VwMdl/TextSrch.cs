using BusCatch.Model;
using MVVM.Common;
using TTC.Model2015;
using Windows.UI.Xaml;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        bodyRouteL _selectRouteL;    /**/ public bodyRouteL SelectRouteL
        {
            get { return _selectRouteL; }
            set
            {
                IsAgncySelnVw = false;
                IsRouteListVw = true; // making sure the menu state is On and Bck Button is ON.

                if (_selectRouteL != value)
                {
                    _selectRouteL = value;
                    _viewMode = ViewModes.RouteList;

                    if (_selectRouteL != null && !string.IsNullOrEmpty(_selectRouteL.tag))
                    {
                        _prevRoute = _selectRouteL.tag;

                        //Task.Run(async () =>            {              await 
                        loadPlotRouteForCurAgncyRouteVoid(true);
                        //});
                    }
                }

                OnPropertyChanged();
            }
        }
        bodyRouteStop _selectRtStop; /**/ public bodyRouteStop SelectRtStop
        {
            get { return _selectRtStop; }
            set
            {
                if (_selectRtStop == value) return;

                _selectRtStop = value;

                if (_selectRtStop == null)
                {
                    RouteLstVsblty = Visibility.Visible;
                }
                else
                {
                    _stngs.LastVStop = new FavVehStop(_selectRtStop.title, SelectAgency.tag, _selectRouteL.tag, _selectRtStop.tag, _selectRtStop.lat, _selectRtStop.lon);
                }

                OnPropertyChanged();
            }
        }
    }
}
