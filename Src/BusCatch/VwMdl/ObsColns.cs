using MVVM.Common;
using System.Collections.ObjectModel;
using TTC.Model2015;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {

        public ObservableCollection<FavVehStop> FavVSOC { get; private set; }
        public ObservableCollection<FavVehStop> CurVSs { get; private set; } = new ObservableCollection<FavVehStop>();

        Collection<bodyAgency>       /**/ _agncis; public ObservableCollection<bodyAgency> AgncyOC { get; private set; }
        Collection<bodyRouteL>       /**/ _routes; public ObservableCollection<bodyRouteL> RouteOC { get; private set; }
        Collection<bodyRouteStop>    /**/ _vStops; public ObservableCollection<bodyRouteStop> VStopOC { get; private set; }

        ObservableCollection<bodyVehicle> _vlopre; public ObservableCollection<bodyVehicle> VLocnPredOC { get { return _vlopre; } private set { if (_vlopre != value) { _vlopre = value; OnPropertyChanged(); } } }
    }
}
