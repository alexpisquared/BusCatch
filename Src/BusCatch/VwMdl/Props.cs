using MVVM.Common;
using System;
using System.Collections.ObjectModel;
using TTC.Model2015;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

namespace BusCatch.VwMdl
{
  public partial class TtcViewModel : ViewModelBase
  {
    //public ObservableCollection<FavVehStop>         /**/ CurrentVSs { get; private set; } = new ObservableCollection<FavVehStop>();
    FavVehStop _CurrentVS = null;                   /**/ public FavVehStop CurrentVS { get { return _CurrentVS; } set { if (_CurrentVS != value) { _CurrentVS = value; OnPropertyChanged(); } } }
    string _CpuUse;                                 /**/ public string CpuUse { get => _CpuUse; set => Set(ref _CpuUse, value); }
    Thickness _FavColThick = new Thickness(0);      /**/ public Thickness FavColThick { get { return _FavColThick; } set { if (_FavColThick != value) { _FavColThick = value; OnPropertyChanged(); } } }
    Thickness _VhStopMenuPsn = new Thickness(0);    /**/ public Thickness VhStopMenuPsn { get { return _VhStopMenuPsn; } set { if (_VhStopMenuPsn != value) { _VhStopMenuPsn = value; OnPropertyChanged(); } } }
    string _VhStopMenuTtl = "";                     /**/ public string VhStopMenuTtl { get { return _VhStopMenuTtl; } set { if (_VhStopMenuTtl != value) { _VhStopMenuTtl = value; OnPropertyChanged(); } } }
    double _FavColWidth = 0;                        /**/ public double FavColWidth { get { return _FavColWidth; } set { if (Math.Abs(_FavColWidth - value) > TOLERANCE) { _FavColWidth = value; OnPropertyChanged(); FavColThick = new Thickness(/*_vSte != "Maxi-ed" ? 0 : */value, 0, 0, 0); } } }
    double _ProgrsMax = 1;                          /**/ public double ProgrsMax { get { return _ProgrsMax; } set { if (Math.Abs(_ProgrsMax - value) > TOLERANCE) { _ProgrsMax = value; OnPropertyChanged(); } } }
    string _CornerLT = "";                          /**/ public string CornerLT { get { return _CornerLT; } set { if (_CornerLT != value) { _CornerLT = value; OnPropertyChanged(); } } }
    double _ProgrsVal = 0;                          /**/ public double ProgrsVal { get { return _ProgrsVal; } set { if (Math.Abs(_ProgrsVal - value) > TOLERANCE) { _ProgrsVal = value; OnPropertyChanged(); } } }
    double _Timer_360 = 0;                          /**/ public double Timer_360 { get { return _Timer_360; } set { if (Math.Abs(_Timer_360 - value) > TOLERANCE) { _Timer_360 = value; OnPropertyChanged(); } } }
    string _Seln = "";                              /**/ public string UsrSeln { get { return _Seln; } set { if (_Seln != value) { _Seln = value; OnPropertyChanged(); } } }
    string _Tags = "";                              /**/ public string TagSeln { get { return _Tags; } set { if (_Tags != value) { _Tags = value; OnPropertyChanged(); } } }
    string _vSte = "";                              /**/ public string VisState { get { return _vSte; } set { if (_vSte != value) { _vSte = value; OnPropertyChanged(); } } }
    string _Zoom = "";                              /**/ public string ZoomLvl { get { return _Zoom; } set { if (_Zoom != value) { _Zoom = value; OnPropertyChanged(); } } }
    string _sMsg = "";                              /**/ public string MsgBrdText { get { return _sMsg; } set { if (_sMsg != value) { _sMsg = value; OnPropertyChanged(); } } }
    bool _isZoom2Prdctd = false;                    /**/ public bool IsZ2P { get { return _isZoom2Prdctd; } set { if (_isZoom2Prdctd != value) { _isZoom2Prdctd = value; OnPropertyChanged(); } } }
    bool _isAllVsPrdctd = false;                    /**/ public bool IsAll { get { return _isAllVsPrdctd; } set { if (_isAllVsPrdctd != value) { _isAllVsPrdctd = value; _firstTime = true; _prevTimeMsSince1970 = 0; if (!value) VLocnPredOC = new ObservableCollection<bodyVehicle>(); OnPropertyChanged(); } } }
    bool _IsOrientLockd = false;                    /**/ public bool IsOrientLockd { get { return _IsOrientLockd; } set { if (_IsOrientLockd != value) { _IsOrientLockd = value; OnPropertyChanged(); } } }
    bool _IsAgncySelnVw = false;                    /**/ public bool IsAgncySelnVw { get { return _IsAgncySelnVw; } set { if (_IsAgncySelnVw != value) { _IsAgncySelnVw = value; OnPropertyChanged(); } } }
    bool _IsRouteListVw = false;                    /**/ public bool IsRouteListVw { get { return _IsRouteListVw; } set { if (_IsRouteListVw != value) { _IsRouteListVw = value; OnPropertyChanged(); } } }
    bool _IsVehTracngVw = false;                    /**/ public bool IsVehTracngVw { get { return _IsVehTracngVw; } set { if (_IsVehTracngVw != value) { _IsVehTracngVw = value; OnPropertyChanged(); } } }
    bool _IsPrivacyPlcVw = false;                   /**/ public bool IsPrivacyPlcVw { get { return _IsPrivacyPlcVw; } set { if (_IsPrivacyPlcVw != value) { _IsPrivacyPlcVw = value; OnPropertyChanged(); } } }
    bool _IsTrackingLocation = true;                /**/ public bool IsTrackingLocation { get { return _IsTrackingLocation; } set { if (_IsTrackingLocation != value) { _IsTrackingLocation = value; OnPropertyChanged(); toggleGeoTracking(value); } } }

    public bodyAgency SelectAgency { get { return _stngs.SelectAgency; } set { if (_stngs.SelectAgency != value) { _stngs.SelectAgency = value; OnPropertyChanged(); } } }

    public AppViewBackButtonVisibility BackButtonVisibility
    {
      get { return SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility; }
      set
      {
        if (SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility == value) return;
        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = value;
        OnPropertyChanged();
      }
    }

    public bool MvNone { get { return _stngs.MvNone; } set { if (_stngs.MvNone != value) { _stngs.MvNone = value; if (value) { _map.Style = MapStyle.None; BusTextClr = _brRed;                 /**/  } OnPropertyChanged(); } } }
    public bool MvRoad { get { return _stngs.MvRoad; } set { if (_stngs.MvRoad != value) { _stngs.MvRoad = value; if (value) { _map.Style = MapStyle.Road; BusTextClr = _blu;                   /**/  } OnPropertyChanged(); } } }
    public bool MvAeri { get { return _stngs.MvAeri; } set { if (_stngs.MvAeri != value) { _stngs.MvAeri = value; if (value) { _map.Style = MapStyle.Aerial; BusTextClr = _brYlw;               /**/  } OnPropertyChanged(); } } }
    public bool MvAer3 { get { return _stngs.MvAer3; } set { if (_stngs.MvAer3 != value) { _stngs.MvAer3 = value; if (value) { _map.Style = MapStyle.Aerial3D; BusTextClr = _brYlw;             /**/  } OnPropertyChanged(); } } }
    public bool MvAeRd { get { return _stngs.MvAeRd; } set { if (_stngs.MvAeRd != value) { _stngs.MvAeRd = value; if (value) { _map.Style = MapStyle.AerialWithRoads; BusTextClr = _brYlw;      /**/  } OnPropertyChanged(); } } }
    public bool MvAeR3 { get { return _stngs.MvAeR3; } set { if (_stngs.MvAeR3 != value) { _stngs.MvAeR3 = value; if (value) { _map.Style = MapStyle.Aerial3DWithRoads; BusTextClr = _brYlw;    /**/  } OnPropertyChanged(); } } }
    public bool MvTern { get { return _stngs.MvTern; } set { if (_stngs.MvTern != value) { _stngs.MvTern = value; if (value) { _map.Style = MapStyle.Terrain; BusTextClr = _blu;                /**/  } OnPropertyChanged(); } } }
    public bool MvTrfc { get { return _stngs.MvTrfc; } set { if (_stngs.MvTrfc != value) { _stngs.MvTrfc = value; _map.TrafficFlowVisible = value;                                  /**/ OnPropertyChanged(); } } }
    public bool IsVrbl { get { return _stngs.IsVrbl; } set { if (_stngs.IsVrbl != value) { doesNotWorkInlineInPropSetter(value); _stngs.IsVrbl = value; OnPropertyChanged(); } } }

    public bool DevOps { get { return _stngs.DevOps; } set { if (_stngs.DevOps != value) { _stngs.DevOps = value; OnPropertyChanged(); DevOpsVsblty = value ? Visibility.Visible : Visibility.Collapsed; } } }
    public bool IsOn02 { get { return _stngs.IsOn02; } set { if (_stngs.IsOn02 != value) { _stngs.IsOn02 = value; OnPropertyChanged(); } } }

    Visibility _RouteLstVsblty = Visibility.Collapsed;    /**/ public Visibility RouteLstVsblty { get { return _RouteLstVsblty; } set { if (_RouteLstVsblty != value) { _RouteLstVsblty = value; OnPropertyChanged(); } } }
    //Visibility _RmvFvsVsblty = Visibility.Collapsed;    /**/ public Visibility RmvFvsVsblty { get { return _RmvFvsVsblty; } set { if (_RmvFvsVsblty != value) { _RmvFvsVsblty = value; OnPropertyChanged(); } } }
    Visibility _ShowRoutsBtnVis = Visibility.Collapsed;    /**/ public Visibility ShowRoutsBtnVis { get { return _ShowRoutsBtnVis; } set { if (_ShowRoutsBtnVis != value) { _ShowRoutsBtnVis = value; OnPropertyChanged(); } } }
    Visibility _BusImgVsblty = Visibility.Visible;      /**/ public Visibility BusImgVsblty { get { return _BusImgVsblty; } set { if (_BusImgVsblty != value) { _BusImgVsblty = value; OnPropertyChanged(); } } }
    Visibility _LetLcnVsblty = Visibility.Collapsed;    /**/ public Visibility LetLcnVsblty { get { return _LetLcnVsblty; } set { if (_LetLcnVsblty != value) { _LetLcnVsblty = value; OnPropertyChanged(); } } }
    Visibility _MsgBrdVsblty = Visibility.Collapsed;    /**/ public Visibility MsgBrdVsblty { get { return _MsgBrdVsblty; } set { if (_MsgBrdVsblty != value) { _MsgBrdVsblty = value; OnPropertyChanged(); } } }
    Visibility _AgencyVsblty = Visibility.Collapsed;    /**/ public Visibility AgencyVsblty { get { return _AgencyVsblty; } set { if (_AgencyVsblty != value) { _AgencyVsblty = value; OnPropertyChanged(); } } }
    Visibility _VStopsVsblty = Visibility.Collapsed;    /**/ public Visibility VStopsVsblty { get { return _VStopsVsblty; } set { if (_VStopsVsblty != value) { _VStopsVsblty = value; OnPropertyChanged(); } } }
    Visibility _DevOpsVsblty = Visibility.Collapsed;    /**/ public Visibility DevOpsVsblty { get { return _DevOpsVsblty; } set { if (_DevOpsVsblty != value) { _DevOpsVsblty = value; OnPropertyChanged(); } } }
    Visibility _VhStopMenuVis = Visibility.Collapsed;    /**/ public Visibility VhStopMenuVis { get { return _VhStopMenuVis; } set { if (_VhStopMenuVis != value) { _VhStopMenuVis = value; OnPropertyChanged(); } } }
    Visibility _PrivacyPlcVis = Visibility.Collapsed;    /**/ public Visibility PrivacyPlcVis { get { return _PrivacyPlcVis; } set { if (_PrivacyPlcVis != value) { _PrivacyPlcVis = value; OnPropertyChanged(); } } }
    Visibility _NotInWebCall = Visibility.Collapsed;     /**/ public Visibility NotInWebCall { get { return _NotInWebCall; } set { if (_NotInWebCall != value) { _NotInWebCall = value; OnPropertyChanged(); } } }

    Brush _MsgBrdColor = _gry;                          /**/ public Brush MsgBrdColor { get { return _MsgBrdColor; } set { if (_MsgBrdColor != value) { _MsgBrdColor = value; OnPropertyChanged(); } } }
    double TOLERANCE = .001;

    Brush _BusTextClr = _brYlw;                         /**/    public Brush BusTextClr { get { return _BusTextClr; } set { if (_BusTextClr != value) { _BusTextClr = value; OnPropertyChanged(); } } }


    public SpeechSynthesizer Synth { get { if (_synth == null) _synth = new SpeechSynthesizer(); return _synth; } set { _synth = value; } }
    public MapControl Map
    {
      set
      {
        _map = value;

        _map.Center = _stngs.MapCenterLocation;
        _map.ZoomLevel = _stngs.MapZoom;
        _map.TrafficFlowVisible = _stngs.MvTrfc;
        _map.Style = MvNone ? MapStyle.None : MvRoad ? MapStyle.Road : MvAeri ? MapStyle.Aerial : MvAer3 ? MapStyle.Aerial3D : MvAeRd ? MapStyle.AerialWithRoads : MvAeR3 ? MapStyle.Aerial3DWithRoads : MapStyle.Terrain;

        _map.ZoomLevelChanged += onMapZoomLvlChangd;
        _map.MapElementClick += onMapElntClk;
        _map.CenterChanged += onMapCenterChanged;
        _map.MapHolding += onMapHolding;
        _map.MapTapped += onMapTappped;
      }
    }


    async void doesNotWorkInlineInPropSetter(bool value)
    {
      if (value)
        _firstTime = true;
      else
        await say("OK.");
    }

  }
}
