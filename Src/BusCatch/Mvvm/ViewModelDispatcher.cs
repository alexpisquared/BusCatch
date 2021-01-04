using BusCatch.VwMdl;
using System;

namespace MVVM.Common
{
  /// <summary>
  /// A simple class to lazy load and dispatch the view model of our modeules.
  /// The Lazy<T> ensures the classes are not created until referenced by name
  /// They are static to ensure they can be accessed across many classes, just not their views
  /// E.g This is used in the main view to ensure the UI gets updated upon different events 
  /// fired only in the main view (Resume, pivot change)
  /// </summary>
  static class ViewModelDispatcher
  {
    static Lazy<TtcViewModel> _ttcViewModel;
    public static TtcViewModel TtcViewModel // Ensures there is one Ttc view model and its only created when referenced.
    {
      get
      {
        if (_ttcViewModel == null)
          _ttcViewModel = new Lazy<TtcViewModel>();

        return _ttcViewModel.Value;
      }
    }
  }
}
