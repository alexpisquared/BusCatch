using MVVM.Common;
using System;
using System.Windows.Input;
using TTC.Model2015;
using Windows.Devices.Geolocation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
        ICommand _ToHereCmd;     /**/public ICommand ToHereCmd { get { return _ToHereCmd ?? (_ToHereCmd = new RelayCommand(async x => { await zoomTo1stBusses(_prevPosition); })); } }
        ICommand _ZomOutCmd;     /**/public ICommand ZomOutCmd { get { return _ZomOutCmd ?? (_ZomOutCmd = new RelayCommand(x => { _map.ZoomLevel--; })); } }
        ICommand _ToRoutCmd;     /**/public ICommand ToRoutCmd { get { return _ToRoutCmd ?? (_ToRoutCmd = new RelayCommand(x => onToRout(x as bodyAgency), x => canToRout(x as bodyAgency))); } }
        ICommand _ShowRoutesCmd; /**/public ICommand ShowRoutesCmd { get { return _ShowRoutesCmd ?? (_ShowRoutesCmd = new RelayCommand(x => RouteLstVsblty = Visibility.Visible)); } }
        ICommand _Add2CurVSsCmd; /**/public ICommand Add2CurVSsCmd { get { return _Add2CurVSsCmd ?? (_Add2CurVSsCmd = new RelayCommand(async x => await Add2CurVSs(_clickedRtStop))); } }
        ICommand _RemvFrmCurCmd; /**/public ICommand RemvFrmCurCmd { get { return _RemvFrmCurCmd ?? (_RemvFrmCurCmd = new RelayCommand(x => RemvFrmCur(_clickedRtStop))); } }
        ICommand _DeletFavVSCmd; /**/public ICommand DeletFavVSCmd { get { return _DeletFavVSCmd ?? (_DeletFavVSCmd = new RelayCommand(x => DeletFavVS(_clickedRtStop))); } }
        ICommand _ToVStpCmd;     /**/public ICommand ToVStpCmd { get { return _ToVStpCmd ?? (_ToVStpCmd = new RelayCommand(x => onToVStp(x as bodyRouteL), x => canToVStp(x as bodyRouteL))); } }
        ICommand _TglFavCmd;     /**/public ICommand TglFavCmd { get { return _TglFavCmd ?? (_TglFavCmd = new RelayCommand(x => onTglFav(x as bodyRouteStop), x => canTglFav(x as bodyRouteStop))); } }
        ICommand _HideVSMenuCmd; /**/public ICommand HideVSMenuCmd { get { return _HideVSMenuCmd ?? (_HideVSMenuCmd = new RelayCommand(x => VhStopMenuVis = Visibility.Collapsed)); } }

        ICommand _StartAgencySelnCmd; /**/public ICommand StartAgencySelnCmd { get { return _StartAgencySelnCmd ?? (_StartAgencySelnCmd = new RelayCommand(async x => await startAgencySeln(), x => CanStartAgencySeln)); } }
        ICommand _StartRoutesSelnCmd; /**/public ICommand StartRoutesSelnCmd { get { return _StartRoutesSelnCmd ?? (_StartRoutesSelnCmd = new RelayCommand(async x => await startRoutesSeln(), x => CanStartRoutesSeln)); } }
        ICommand _StartVehTrackngCmd; /**/public ICommand StartVehTrackngCmd { get { return _StartVehTrackngCmd ?? (_StartVehTrackngCmd = new RelayCommand(async x => await startVehTrackng("OK"), x => CanStartVehTrackng)); } }
        ICommand _StartPrivacyPlcCmd; /**/public ICommand StartPrivacyPlcCmd { get { return _StartPrivacyPlcCmd ?? (_StartPrivacyPlcCmd = new RelayCommand(x => PrivacyPlcVis = PrivacyPlcVis == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible, x => true)); } }


        //ICommand _TglAudCmd; public ICommand TglAudCmd { get { return _TglAudCmd ?? (_TglAudCmd = new RelayCommand(x => IsVrbl = !IsVrbl)); } }
    }
}
