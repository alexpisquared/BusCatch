using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace BusCatch.Core.Roam
{

  public class RoamStore
  {
    public ObservableCollection<TargetStopGroup> TSGs { get; set; }
  }
  public class TargetStopGroup
  {
    public ObservableCollection<StopOfInterest> Stops { get; set; }
  }
  public class StopOfInterest
  {
    public string Agency { get; set; }
    public string Route { get; set; }
    public string Stop { get; set; }
  }
}
