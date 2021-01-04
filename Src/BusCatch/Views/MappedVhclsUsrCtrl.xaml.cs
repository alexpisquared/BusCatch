using MVVM.Common;
using BusCatch.VwMdl;
using Windows.UI.Xaml.Controls;

namespace BusCatch.Views
{
  public sealed partial class MappedVhclsUsrCtrl : UserControl
  {
    public MappedVhclsUsrCtrl()
    {
      this.InitializeComponent();
      var t = typeof(TTC.WP.Misc.StringFormatConverter);
      DataContextChanged += (s, e) => { VwMdl = ViewModelDispatcher.TtcViewModel; };         //1/2: http://www.bendewey.com/index.php/533/using-compiled-binding-xbind-with-a-viewmodel      //DataContext = ViewModelDispatcher.TtcViewModel;
    }
    public TtcViewModel VwMdl { get; set; }                                                  //2/2: http://www.bendewey.com/index.php/533/using-compiled-binding-xbind-with-a-viewmodel
  }
}
