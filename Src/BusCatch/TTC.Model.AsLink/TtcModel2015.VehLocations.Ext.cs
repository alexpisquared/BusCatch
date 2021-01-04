using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Controls.Maps;

namespace TTC.Model2015
{
  [PropertyChanged.ImplementPropertyChanged]
  public partial class bodyVehicle
  {
    public bodyPredictionsDirectionPrediction Predictn { get; set; }
    public double DistToPredStop { get; set; }
    public double Zoom { get; set; }
    public double MaxFwd { get { return SecsSinceReportAbs * Math.Pow(2, Zoom)*.00012; } }
    public Geopoint Location { get { return new Geopoint(new BasicGeoposition() { Latitude = (double)lat, Longitude = (double)lon }); } }
    public Uri ImageSourceUri { get { return new Uri("ms-appx:///Assets/MapPin.png", UriKind.RelativeOrAbsolute); } }

    public string PredictnMinutes
    {
      get
      {
        if (Predictn == null) return null;
        else return
            Predictn.seconds < 5 ? "Now!" :
            Predictn.seconds < 99 ? string.Format(":{0:N0}", Predictn.seconds) :
            Predictn.seconds < 3600 ? string.Format("{0:N0}:", Predictn.seconds / 60.0) :
            "Now!";
      }
      set {; }   // MUST GO TOGETHER!!!
    }
    public string PredictnBranch { get { if (Predictn == null) return null; else return string.Compare(routeTag.ToString(), Predictn.branch, true) == 0 ? "" : Predictn.branch; } }
    public int HeadingPos { get { return -90 + heading; } }
    public int HeadingNeg { get { return +90 - heading; } }
    public double SecsSinceReportAbs { get { return secsSinceReport > 0 ? secsSinceReport : secsSinceReport < 18 ? 18 : 18 + secsSinceReport; } }

    ///any ref to MapPolyline renders veh list empty
    //public MapPolyline PredictionStick
    //{
    //  get
    //  {
    //    //var stops = new List<BasicGeoposition>();
    //    //stops.Add(new BasicGeoposition() { Latitude = (double)lat, Longitude = (double)lon });

    //    ////todo: forecastedPosition = curPos + speed * (secsSinceReport + secsSinceWebCall) * heading
    //    //stops.Add(new BasicGeoposition() { Latitude = (double)lat + .0001, Longitude = (double)lon });

    //    //mapElement.Path = new Geopath(stops);
    //    //mapElement.ZIndex = 1;
    //    ////mapElement.FillColor = Colors.Red;
    //    //mapElement.StrokeColor = Colors.Blue;
    //    //mapElement.StrokeThickness = 3;
    //    //mapElement.StrokeDashed = false;

    //    return null; //  mapElement;
    //  }
    //}
    //MapPolyline mapElement = new MapPolyline();

    [XmlIgnore]
    public MapElement VectorBase { get; set; }
    [XmlIgnore]
    public MapElement VectorLine { get; set; }
    [XmlIgnore]
    public MapElement VectorQTip { get; set; }
  }


  [PropertyChanged.ImplementPropertyChanged]
  public partial class bodyAgency
  {
    public BasicGeoposition Location { get; set; }
    public double DistanceFromHere { get; set; }
  }
}
