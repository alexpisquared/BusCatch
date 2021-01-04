using MVVM.Common;
using BusCatch.VwMdl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace BusCatch.Views
{
  public sealed partial class StopRemoverUsrCtrl : UserControl
  {
    public StopRemoverUsrCtrl()
    {
      this.InitializeComponent();
      DataContextChanged += (s, e) => { VwMdl = ViewModelDispatcher.TtcViewModel; };         //1/2: http://www.bendewey.com/index.php/533/using-compiled-binding-xbind-with-a-viewmodel      //DataContext = ViewModelDispatcher.TtcViewModel;
    }
    public TtcViewModel VwMdl { get; set; }                                                  //2/2: http://www.bendewey.com/index.php/533/using-compiled-binding-xbind-with-a-viewmodel

    void scrollIntoView(object sender, SelectionChangedEventArgs e) { if (e.AddedItems.Count > 0) ((ListView)sender).ScrollIntoView(e.AddedItems[0], ScrollIntoViewAlignment.Leading); }
  }
}
