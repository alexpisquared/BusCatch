using BusCatch.Model;
using MVVM.Common;
using System;
using TTC.W10.POC.VwMdl;
using Windows.Media.SpeechSynthesis;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

namespace BusCatch.VwMdl
{
    public partial class TtcViewModel : ViewModelBase
    {
#if DEBUG
        const int _webCallIntervalSec = 10;
#else
        const int _webCallIntervalSec = 20;
#endif
        const double _quickUiRefreshSec = .125;
        ulong _prevTimeMsSince1970 = 0;
        public const string _idfor = @"{0}\{1}\{2}";

        RandomAccessStreamReference _mapIconStreamReference_Agency, _mapIconStreamReference_RtStop, _mapIconStreamReference_Walkng;
        SpeechSynthesizer _synth = null;
        MediaElement _me = new MediaElement();
        DispatcherTimer _dt = new DispatcherTimer();
        DateTime _nextWebCall = DateTime.Now;
        Stngs _stngs = new Stngs();
        bool _busyInWebCall = false, _ining = true;
        bool _ignoreMapTap = false;
        string _prevAgncy = null, _prevRoute = null, _prevVStop = null, _agncyTag = null;
        ViewModes _viewMode = ViewModes.AgncySeln;
        static Color
          _clrDkGrn = Color.FromArgb(255, 0, 96, 0),
          _clrMdGrn = Color.FromArgb(255, 0, 128, 0),
          _clrBrGrn = Color.FromArgb(255, 0, 255, 0);
        static Brush
          _brRed = new SolidColorBrush(Color.FromArgb(255, 255, 0, 0)),
          _brGrn = new SolidColorBrush(_clrBrGrn),
          _brBlu = new SolidColorBrush(Color.FromArgb(255, 0, 0, 255)),
          _sbGrn = new SolidColorBrush(Color.FromArgb(255, 0, 192, 0)),
          _brYlw = new SolidColorBrush(Color.FromArgb(255, 255, 255, 0)),
          _dkRed = new SolidColorBrush(Colors.DarkRed),
          _smWht = new SolidColorBrush(Color.FromArgb(128, 255, 255, 255)),
          _smRed = new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)),
          _smGrn = new SolidColorBrush(Color.FromArgb(128, 0, 192, 0)),
          _smBlu = new SolidColorBrush(Color.FromArgb(128, 0, 0, 255)),
          _trns = new SolidColorBrush(Colors.Transparent),
          _wht = new SolidColorBrush(Colors.White),
          _gry = new SolidColorBrush(Color.FromArgb(64, 0, 0, 0)),
          _grn = new SolidColorBrush(Colors.Green),
          _brBlk = new SolidColorBrush(Colors.Black),
          _blu = new SolidColorBrush(Colors.Blue),
          _brushFvs = new SolidColorBrush(Colors.Orange);


        MapControl _map;
        //readonly double _lowestWorkngZoom = 11.0; // hide th busses if goes below that.
        readonly double _autoSaveableZoom = 13.5; // auto saves fav stop map view only for closer (larger) than this value.

        Border _currentGrid = null;
        MapIcon _clickedRtStop;
        DateTime
          _lastVehPredsGetAttempt = DateTime.Now,
          _lastVehPredsGetSUCCESS = DateTime.Now;
    }
}
