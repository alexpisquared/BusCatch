using AAV.AsLink.Misc;
using AsLink;
using BusCatch.Model;
using MVVM.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using TTC.Model2015;
using TTC.Svc;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Storage.Streams;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace BusCatch.VwMdl
{
  public partial class TtcViewModel : ViewModelBase
  {
    bool _isGtfs;

    public async void AddFavVStop(bodyRouteStop brs)
    {
      var s = string.Format(_idfor, SelectAgency.tag, SelectRouteL.tag, brs.tag);
      if (FavVSOC.Any(r => string.Compare(r.Id, s) == 0)) // if already in the list
        return;

      var fvs = new FavVehStop(_selectRtStop.title, SelectAgency.tag, _selectRouteL.tag, brs.tag, brs.lat, brs.lon);
      FavVSOC.Add(fvs);
      _stngs.FavVSLst = FavVSOC;
      addMapIcon(fvs.StopName, _mapIconStreamReference_RtStop, new Geopoint(new BasicGeoposition() { Latitude = fvs.Lat, Longitude = fvs.Lon }));

      await startVehTrackng("Auto-started");
    }
    public void DeletFavVStop(bodyRouteStop brs)
    {
      //FavVSOC.Clear(); - clear here if schema changes.

      var s = string.Format(_idfor, SelectAgency.tag, SelectRouteL.tag, brs.tag);
      while (FavVSOC.Any(r => string.Compare(r.Id, s) == 0))
      {
        var re = FavVSOC.First(r => string.Compare(r.Id, s) == 0);
        FavVSOC.Remove(re);
        var mi = _map.MapElements.FirstOrDefault(x => x is MapIcon && ((MapIcon)x).Image == _mapIconStreamReference_RtStop && ((MapIcon)x).Title == re.StopName) as MapIcon;
        _map.MapElements.Remove(mi);
      }
      _stngs.FavVSLst = FavVSOC;
    }

    public async Task CheckStartNearestCurrentStopTracking()
    {
      var now = DateTime.Now;
      if (now > _nextTimeToCheckCVS)
      {
        var tt = (await GetGeolocation(1000)).Coordinate.Point.Position;
        await setNearestStopForTracking(now, tt);
      }
    }

    MapElement newBusProjectionVector(double latA, double lonA, Point pointB, Color clr)
    {
      //var pointB = GeoHelper.GetTargetLocation(latA, lonA, -radianBearing, radialDistance);

      var AtoB = new List<BasicGeoposition> { new BasicGeoposition { Latitude = latA, Longitude = lonA }, new BasicGeoposition { Latitude = pointB.X, Longitude = pointB.Y } };
      var path = new Geopath(AtoB);
      var line = new MapPolyline() { Path = path, StrokeColor = clr, StrokeThickness = 1, ZIndex = (int)ZIdx.Vector };

      //Binding vb = new Binding { Source = rmi, Path = new PropertyPath("Vsbl") };
      //line.SetBinding(Windows.UI.Xaml.Shapes.Line.VisibilityProperty, vb); //The following example shows how to create a binding object to set the source of the binding. The example uses SetBinding to bind the Text property of path, which is a TextBlock control, to MyDataProperty.

      return line;
    }
    MapElement breadcrumb(BasicGeoposition bgp, double thickness, Color clr, int zidx) => breadcrumb(bgp.Latitude, bgp.Longitude, thickness, clr, zidx);
    MapElement breadcrumb(double la, double lo, double thickness, Color clr, int zidx) // scaled to zoom
    {
      var AtoA = new List<BasicGeoposition> { new BasicGeoposition { Latitude = la, Longitude = lo }, new BasicGeoposition { Latitude = la + .00001, Longitude = lo + .00001 } };
      var line = new MapPolyline() { Path = new Geopath(AtoA), StrokeColor = clr, StrokeThickness = thickness, ZIndex = zidx };
      return line; // Map.MapElements.Add(line);
    }
    void addMapChildren_Triangle(double la, double lo, int thick, Brush clr, short heading, string tag) // scaled to screen
    {
      var pathFigureCollection = new PathFigureCollection();
      var pathSegmentCollection = new PathSegmentCollection
            {
                new LineSegment { Point = new Point(0, 1) },
                new LineSegment { Point = new Point(-4, -8) }
            };
      pathFigureCollection.Add(new PathFigure { StartPoint = new Point(4, -8), Segments = pathSegmentCollection });

      var path = new Windows.UI.Xaml.Shapes.Path
      {
        Stroke = clr,
        Fill = clr,
        StrokeThickness = thick,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        RenderTransformOrigin = new Point(0, 0),
        RenderTransform = new RotateTransform { Angle = heading - 180 },
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Data = new PathGeometry { Figures = pathFigureCollection },
        Tag = tag
      };

      MapControl.SetLocation(path, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));

      //Binding vb = new Binding { Source = rmi, Path = new PropertyPath("Vsbl") };   path.SetBinding(Windows.UI.Xaml.Shapes.Path.VisibilityProperty, vb); //The following example shows how to create a binding object to set the source of the binding. The example uses SetBinding to bind the Text property of path, which is a TextBlock control, to MyDataProperty.
      //Binding cb = new Binding { Source = rmi, Path = new PropertyPath("Brh") };    path.SetBinding(Windows.UI.Xaml.Shapes.Path.FillProperty, cb); 

      ////			BindingOperations.SetBinding(path, Windows.UI.Xaml.Shapes.Path.VisibilityProperty, vb);//Instead of calling SetBinding, you can use the SetBinding static method of the BindingOperations class. The following example, calls BindingOperations.SetBinding instead of FrameworkElement.SetBinding to bind path to myDataProperty.

      _map.Children.Add(path); //       var line = new MapPolygon() { Path = path, StrokeColor = clr, StrokeThickness = thick, ZIndex = (int)ZIdx.Vector };
    }
    void moveMapChildren_Triangle(double la, double lo, int thick, Brush clr, short heading, string tag) // scaled to screen
    {
      var trl = _map.Children.FirstOrDefault(r => r is Path && ((Path)r).Tag is string && ((string)((Path)r).Tag) == tag);
      if (trl == null)
      {
        addMapChildren_Triangle(la, lo, thick, clr, heading, tag);
      }
      else
      {
        var pathFigureCollection = new PathFigureCollection();
        var pathSegmentCollection = new PathSegmentCollection
                {
                    new LineSegment { Point = new Point(0, 1) },
                    new LineSegment { Point = new Point(-4, -8) }
                };
        pathFigureCollection.Add(new PathFigure { StartPoint = new Point(4, -8), Segments = pathSegmentCollection });

        var path = (Path)trl;

        path.Stroke = clr;
        path.Fill = clr;
        path.StrokeThickness = thick;
        path.RenderTransform = new RotateTransform { Angle = heading - 180 };

        MapControl.SetLocation(path, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));
      }
    }


    Path addBusTriangle(bodyVehicle rmi, double la, double lo, double heading, Brush color, Brush fill)
    {
      var pathFigureCollection = new PathFigureCollection();
      var pathSegmentCollection = new PathSegmentCollection
            {
                new LineSegment { Point = new Point(0, 12) },
                new LineSegment { Point = new Point(-7, -8) }
            };
      pathFigureCollection.Add(new PathFigure { StartPoint = new Point(7, -8), Segments = pathSegmentCollection });

      var path = new Windows.UI.Xaml.Shapes.Path
      {
        Stroke = color,
        Fill = fill,
        StrokeThickness = 2,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        RenderTransformOrigin = new Point(1.0, .0),
        RenderTransform = new RotateTransform { Angle = heading - 180 },
        StrokeStartLineCap = PenLineCap.Round,
        StrokeEndLineCap = PenLineCap.Round,
        StrokeLineJoin = PenLineJoin.Round,
        Data = new PathGeometry { Figures = pathFigureCollection },
        Tag = _curBus
      };

      //MapControl.SetNormalizedAnchorPoint(path, new Point(0.0, 0.0));
      MapControl.SetLocation(path, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));


      //Binding vb = new Binding { Source = rmi, Path = new PropertyPath("Vsbl") };
      //path.SetBinding(Windows.UI.Xaml.Shapes.Path.VisibilityProperty, vb); //The following example shows how to create a binding object to set the source of the binding. The example uses SetBinding to bind the Text property of path, which is a TextBlock control, to MyDataProperty.

      //Binding cb = new Binding { Source = rmi, Path = new PropertyPath("Brh") };
      //path.SetBinding(Windows.UI.Xaml.Shapes.Path.FillProperty, cb); //The following example shows how to create a binding object to set the source of the binding. The example uses SetBinding to bind the Text property of path, which is a TextBlock control, to MyDataProperty.

      ////			BindingOperations.SetBinding(path, Windows.UI.Xaml.Shapes.Path.VisibilityProperty, vb);//Instead of calling SetBinding, you can use the SetBinding static method of the BindingOperations class. The following example, calls BindingOperations.SetBinding instead of FrameworkElement.SetBinding to bind path to myDataProperty.

      return path;  // Map.Children.Add(path);
    }


    static void moveLine_NOGO(double ala, double alo, double bla, double blo, MapPolyline extg)
    {
      var a = extg.Path.Positions.First();
      if (a.Latitude != ala)
        a.Latitude = ala;
      if (a.Longitude != alo)
        a.Longitude = alo;
      var b = extg.Path.Positions.Last();
      if (b.Latitude != bla)
        b.Latitude = bla;
      if (b.Longitude != blo)
        b.Longitude = blo;
    }



    void addMapIcon(string agencyName, IRandomAccessStreamReference img, Geopoint gp)
    {
      var icn = new MapIcon
      {
        Location = gp,
        NormalizedAnchorPoint = new Point(0.5, 0.5),
        Title = agencyName,
        Image = img,
        ZIndex = 0,
        CollisionBehaviorDesired = MapElementCollisionBehavior.RemainVisible,
        //MapTabIndex = 2
      };

      _map.MapElements.Add(icn);
    }
    static void replaceMapChild_Ellipse(MapControl map, double la, double lo, string tag, Brush brush)
    {
      if (map.Children.Any(r => r is Ellipse && ((r as Ellipse).Tag as string) == tag))
      {
        foreach (var elps in map.Children.Where(r => r is Ellipse && ((r as Ellipse).Tag as string) == tag))
          MapControl.SetLocation(elps, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));
      }
      else
      {
        var bc = new Ellipse { Tag = tag, StrokeThickness = 1, Fill = _trns, Height = 16, Width = 16, Stroke = brush };

        map.Children.Add(bc);

        MapControl.SetLocation(bc, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));
        MapControl.SetNormalizedAnchorPoint(bc, new Point(0.5, 0.5));
      }
    }
    static void placeArrowTip(MapControl map, double la, double lo, string tag, Brush brush, double heading)
    {
      if (map.Children.Any(r => r is Ellipse && ((r as Ellipse).Tag as string) == tag))
      {
        foreach (var elps in map.Children.Where(r => r is Ellipse && ((r as Ellipse).Tag as string) == tag))
          MapControl.SetLocation(elps, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));
      }
      else
      {
        var pc = new PointCollection
                {
                    new Point(0, 0),
                    new Point(-20, -5),
                    new Point(-20, +5),
                    new Point(0, 0)
                };
        var bc = new Polygon { Points = pc, Tag = tag, StrokeThickness = 1, Fill = _trns, Height = 16, Width = 16, Stroke = brush };
        bc.RenderTransform = new RotateTransform { Angle = heading };

        map.Children.Add(bc);

        MapControl.SetLocation(bc, new Geopoint(new BasicGeoposition { Latitude = la, Longitude = lo }));
        MapControl.SetNormalizedAnchorPoint(bc, new Point(0, 0));
      }
    }
    void placeLine(MapControl map, BasicGeoposition a, BasicGeoposition b, ZIdx zidx, Color clr) { removeMapElementsByZIndex(map, zidx); map.MapElements.Add(new MapPolyline() { Path = new Geopath(new List<BasicGeoposition> { a, b }), StrokeColor = clr, StrokeThickness = 3, ZIndex = (int)zidx, StrokeDashed = true }); }
    void placeMinKmLine(MapControl map, BasicGeoposition a, BasicGeoposition b, BasicGeoposition c, ZIdx zidx, Color lineColor, TimeSpan dt, ref Border currentGrid)
    {
      removeMapElementsByZIndex(map, zidx);
      map.MapElements.Add(new MapPolyline() { Path = new Geopath(new List<BasicGeoposition> { a, b, c }), StrokeColor = lineColor, StrokeThickness = 2, ZIndex = (int)zidx, StrokeDashed = false });

      if (dt == TimeSpan.Zero) return;

      _distCurToStopM = GetDistanceTo(b, c);
      var mySpeedMeterPerMin = GetDistanceTo(a, b) / dt.TotalMinutes;

      var etaOffCurSpeed = mySpeedMeterPerMin > 0 ? _distCurToStopM / mySpeedMeterPerMin : _jogEtaInMin;
      var etaSmart = etaOffCurSpeed < _jogEtaInMin ? etaOffCurSpeed : _jogEtaInMin;

      var gp = new Geopoint(new BasicGeoposition { Latitude = a.Latitude + (c.Latitude - a.Latitude) / 2, Longitude = a.Longitude + (c.Longitude - a.Longitude) / 2 });

      map.Children.Remove(currentGrid);
      if (_distCurToStopM < 2500 && _map.ZoomLevel < 14 || _distCurToStopM < 100) return;

      var brh = new SolidColorBrush(lineColor);
      var content = new Border()
      {
        Background = new SolidColorBrush(Color.FromArgb(48, 240, 240, 240)),
        CornerRadius = new CornerRadius(12, 0, 12, 0),
        BorderBrush = brh,
        BorderThickness = new Thickness(1, 1, 1, 1)
      };
      var sp = new StackPanel { Orientation = Orientation.Horizontal };
      sp.Children.Add(new Image { Source = new BitmapImage(new Uri("ms-appx:///Assets/pedestrian-35x60.png")), Margin = new Thickness(5, 0, 0, 0), Height = 30 });
      sp.Children.Add(new TextBlock
      {
        Text = _distCurToStopM < 600 ?
          string.Format("{0:N0} m\r\n{1:N1} min", _distCurToStopM, etaSmart) :
          string.Format("{0:N1} km\r\n{1:N0} min", _distCurToStopM * .001, etaSmart),
        Foreground = brh,
        FontSize = 16,
        Margin = new Thickness(5, 3, 5, 3),
      });
      content.Child = sp;
      map.Children.Add(content);
      MapControl.SetLocation(content, gp);
      MapControl.SetNormalizedAnchorPoint(content, new Point(.5, .5));
      currentGrid = content;
    }

    static void addBusRouteToMap_NOGO_Disordered(MapControl map, Collection<bodyRouteStop> vStopOC, Color strokeColor)
    {
      if (map == null) return;

      try
      {
        var ab = new List<BasicGeoposition>();
        foreach (var stop in vStopOC.OrderBy(r => r.stopId))
          ab.Add(new BasicGeoposition() { Latitude = (double)stop.lat, Longitude = (double)stop.lon });

        var mapElement = new MapPolyline
        {
          Path = new Geopath(ab),
          ZIndex = (int)ZIdx.BusStop,
          StrokeColor = strokeColor,        //
          StrokeThickness = 1,
          StrokeDashed = true,
        };

        map.MapElements.Add(mapElement);
      }
      catch (Exception ex) { ex.Pop("static"); }
    }

    static void addBusStopFlag(MapControl map, double stopLat, double stopLon, Color strokeColor, double strokeThickness)
    {
      if (map == null) return;

      try
      {
        var ab = new List<BasicGeoposition>
                {
                    new BasicGeoposition() { /*Altitude =  NO EFFECT !!!!    */ Latitude = (double)stopLat,           /**/ Longitude = (double)stopLon },
                    new BasicGeoposition() { /*Altitude = stopLat + .00020,  */ Latitude = (double)stopLat + .00020,  /**/ Longitude = (double)stopLon },
                    new BasicGeoposition() { /*Altitude = stopLat + .00016,  */ Latitude = (double)stopLat + .00016,  /**/ Longitude = (double)stopLon + .00015 },
                    new BasicGeoposition() { /*Altitude = stopLat + .00012,  */ Latitude = (double)stopLat + .00012,  /**/ Longitude = (double)stopLon + .000001 },
                    new BasicGeoposition() { /*Altitude = stopLat,           */ Latitude = (double)stopLat,           /**/ Longitude = (double)stopLon }
                };

        var mapElement = new MapPolygon
        {
          Path = new Geopath(ab),
          ZIndex = (int)ZIdx.BusStop,
          StrokeColor = _clrMdGrn,                                                        //
          FillColor = _clrBrGrn,
          StrokeThickness = 1,//strokeThickness,
          StrokeDashed = false,
        };

        map.MapElements.Add(mapElement);
      }
      catch (Exception ex) { ex.Pop("static"); }
    }
    static void addBusStopToMapAndCenterThere(MapControl map, double stopLat, double stopLon, Color strokeColor)
    {
      Task.Factory.StartNew(async () => { await Task.Delay(50); Debug.WriteLine("** Done waiting before finished Dealy. 1"); }).ContinueWith(_ =>
      {
        addBusStopFlag(map, stopLat, stopLon, strokeColor, 5);
        //? map.AddMoveHereMark(stopLat, stopLon, "CurBusStop", new SolidColorBrush(Colors.Red));

        Task.Run(async () => await map.TrySetViewAsync(new Geopoint(new BasicGeoposition { Latitude = stopLat, Longitude = stopLon })));
      }, TaskScheduler.FromCurrentSynchronizationContext());
    }
    async static Task setRouteViewBounds(MapControl map, bodyRoute route)
    {
      if (route == null) return;
      await setMapBounds(map, route.latMax, route.lonMax, route.latMin, route.lonMin);
    }
    async static Task setMapBounds(MapControl map, double ala, double alo, double bla, double blo)
    {
      if (map == null) return;
      try
      {
        var northwestCorner = new BasicGeoposition { Latitude = Math.Max(ala, bla), Longitude = Math.Min(alo, blo) };
        var southeastCorner = new BasicGeoposition { Latitude = Math.Min(ala, bla), Longitude = Math.Max(alo, blo) };

        var box = new GeoboundingBox(northwestCorner, southeastCorner);

        await map.TrySetViewBoundsAsync(box, new Thickness(50), MapAnimationKind.Default);
      }
      catch (Exception ex) { ex.Pop("static"); }
    }

    static void removeMapElementsByZIndex(MapControl map, ZIdx zidx)
    {
      while (true)
      {
        var el2 = map.MapElements.FirstOrDefault(r => r.ZIndex == (int)zidx);
        if (el2 == null) break;
        map.MapElements.Remove(el2);
      }
      //map.MapElements.Where(r => r.ZIndex == 1).ToList().ForEach(r => map.MapElements.Remove(r));
    }
    static void removeMapElementsByTag(MapControl map, string tag)
    {
      while (true)
      {
        var el2 = map.Children.FirstOrDefault(r => r is Ellipse && ((r as Ellipse).Tag as string) == tag);
        if (el2 == null) break;
        map.Children.Remove(el2);
      }
    }


    public static double GetDistanceTo(BasicGeoposn that, BasicGeoposition other) { return GetDistanceTo(new BasicGeoposition { Latitude = that.Latitude, Longitude = that.Longitude }, other); }
    public static double GetDistanceTo(BasicGeoposition that, BasicGeoposition other)
    {
      if (double.IsNaN(that.Latitude) || double.IsNaN(that.Longitude) || double.IsNaN(other.Latitude) || double.IsNaN(other.Longitude))
      {
        throw new ArgumentException(("Argument_LatitudeOrLongitudeIsNotANumber"));
      }
      else
      {
        double latitude = that.Latitude * 0.0174532925199433;
        double longitude = that.Longitude * 0.0174532925199433;
        double num = other.Latitude * 0.0174532925199433;
        double longitude1 = other.Longitude * 0.0174532925199433;
        double num1 = longitude1 - longitude;
        double num2 = num - latitude;
        double num3 = Math.Pow(Math.Sin(num2 / 2), 2) + Math.Cos(latitude) * Math.Cos(num) * Math.Pow(Math.Sin(num1 / 2), 2);
        double num4 = 2 * Math.Atan2(Math.Sqrt(num3), Math.Sqrt(1 - num3));
        double num5 = 6376500 * num4;
        return num5;
      }
    }

    //tu: void translateFlagsToVwModeAndSetVwAsync(ViewModes vm) { Task.Run(async () => await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await setViewModeAddRmvTrk(vm))); }//tu: async on UI thread... from Prop to await.

    async Task setViewModeAddRmvTrk(ViewModes vm)
    {
      switch (_viewMode = vm)
      {
        case ViewModes.AgncySeln: await startAgencySeln(); break;
        case ViewModes.RouteList: await startRoutesSeln(); break;
        case ViewModes.VehTrackg: IsVehTracngVw = true;/*await startVehTrackng("OK!"); */break;
      }
      _ining = false;
    }

    async Task startAgencySeln()
    {
      BackButtonVisibility = FavVSOC.Count > 0 ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

      RouteLstVsblty = Visibility.Collapsed;
      IsPrivacyPlcVw = IsVehTracngVw = IsRouteListVw = false;
      IsAgncySelnVw = true;
      _viewMode = ViewModes.AgncySeln;

      await say("Please select the desired agency, route, bus stops by tapping them on the map or in the list");
      stopAndClearPredictions();
      removeMapElementsByZIndex(_map, ZIdx.MinKm); _map.Children.Remove(_currentGrid);
      FavColWidth = 0;          //.м.FavColWidth = VisState == "Maxi-ed" ? 240 : 0;
      _map.MapElements.Clear();
      //if (_txtSrchMode == TxtSrchModes.Ini)
      {
        AgencyVsblty = Visibility.Visible; //          
        await loadAgencies();
      }
    }
    async Task startRoutesSeln()
    {
      BackButtonVisibility = FavVSOC.Count > 0 ? AppViewBackButtonVisibility.Visible : AppViewBackButtonVisibility.Collapsed;

      RouteLstVsblty = Visibility.Visible;
      IsAgncySelnVw = IsVehTracngVw = false;
      IsRouteListVw = true;
      _viewMode = ViewModes.RouteList;

      await say("Please select the desired route by tapping on the list");
      stopAndClearPredictions();
      removeMapElementsByZIndex(_map, ZIdx.MinKm);
      _map.Children.Remove(_currentGrid);
      _map.MapElements.Clear();
      SelectRouteL = null;

      if (string.IsNullOrEmpty(_agncyTag)) // if never loaded
      {
        loadRoutesForSelectAgency(_agncyTag = SelectAgency.tag, _isGtfs);
      }
    }
    async Task startVehTrackng(string msg) //?set vs here
    {
      if (!FavVSOC.Any())
      {
        await say("No vehicles to track.");
        if (SelectAgency == null) await startAgencySeln();
        else await startRoutesSeln();
        return;
      }

      BackButtonVisibility = AppViewBackButtonVisibility.Collapsed;

      _busyInWebCall = true;
      FavColWidth = 0;
      RouteLstVsblty = Visibility.Collapsed;
      IsAgncySelnVw = IsRouteListVw = false;
      IsVehTracngVw = true;
      _viewMode = ViewModes.VehTrackg;

      await say(msg);

      try
      {
        //foreach (var fvs in FavVSOC) addMapIcon(fvs.StopName, _mapIconStreamReference_RtStop, new Geopoint(new BasicGeoposition() { Latitude = fvs.Lat, Longitude = fvs.Lon }));

        while (CurrentVS == null) await Task.Delay(50);

        _isGtfs = CurrentVS.IsGtfs;

        setupVehStopPresets(CurrentVS);

        _dt.Start();
      }
      catch (Exception ex) { ex.Pop(); } // System.Reflection.MethodInfo.GetMethodFromHandle(GetType().DeclaringType.TypeHandle)); } // System.Reflection.MethodInfo.GetCurrentMethod()); }
      finally { _busyInWebCall = false; }
    }

    async Task loadAgencies()
    {
      var al = await WebSvc.GetAgencyList();
      _agncis = new ObservableCollection<bodyAgency>(al.agency.Where(a => a.tag != "engr")); // && a.shortTitle != null)); <== nogo: short title is used in 17 out of 69 agencies. (Jul2016)
      var yrt = new BasicGeoposn
      {
        Latitude = 43.8971377,
        Longitude = -79.4598586
      };
      _agncis.Add(new bodyAgency { IsGtfs = true, regionTitle = "York Region", shortTitle = "YRT", Location = yrt, tag = "yrt", title = "York Region Transit" }); // http://yrt-transit.esolutionsgroup.ca/Map/GetVehicles : [{"TripId":"921147","Status":2,"Sequence":10,"VehicleId":"C0311","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.80295,"Longitude":-79.451,"Trip":{"TripId":"921147","Headsign":"Finch Terminal - EB","ShortName":"","DirectionId":"0","BlockId":"a_85606","Route":{"RouteId":"10","ShortName":"5","LongName":"Clark","Description":"","Color":"49706F","ColorText":"FFFFFF","RouteNameHtml":"5 - Clark","OrderBy":"0005"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921204","Status":2,"Sequence":43,"VehicleId":"C0313","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8007,"Longitude":-79.62845,"Trip":{"TripId":"921204","Headsign":"Al Pallidini Community Centre - NB","ShortName":"","DirectionId":"0","BlockId":"a_85571","Route":{"RouteId":"11","ShortName":"7","LongName":"Martin Grove","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"7 - Martin Grove","OrderBy":"0007"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921278","Status":1,"Sequence":2,"VehicleId":"C0315","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7758827,"Longitude":-79.5004,"Trip":{"TripId":"921278","Headsign":"Martin Grove - WB","ShortName":"","DirectionId":"1","BlockId":"a_85577","Route":{"RouteId":"12","ShortName":"10","LongName":"Woodbridge","Description":"","Color":"01A78F","ColorText":"FFFFFF","RouteNameHtml":"10 - Woodbridge","OrderBy":"0010"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921227","Status":2,"Sequence":47,"VehicleId":"C0316","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.729084,"Longitude":-79.6043854,"Trip":{"TripId":"921227","Headsign":"Woodbine Centre - SB","ShortName":"","DirectionId":"1","BlockId":"a_85570","Route":{"RouteId":"11","ShortName":"7","LongName":"Martin Grove","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"7 - Martin Grove","OrderBy":"0007"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921070","Status":1,"Sequence":49,"VehicleId":"C0321","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8367,"Longitude":-79.57623,"Trip":{"TripId":"921070","Headsign":"Pine Valley - WB","ShortName":"","DirectionId":"1","BlockId":"a_85561","Route":{"RouteId":"9","ShortName":"4A","LongName":"Major Mackenzie","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"4A - Major Mackenzie","OrderBy":"0004A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921300","Status":2,"Sequence":7,"VehicleId":"C0323","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8154,"Longitude":-79.53938,"Trip":{"TripId":"921300","Headsign":"Steeles AV. - SB","ShortName":"","DirectionId":"0","BlockId":"a_85578","Route":{"RouteId":"14","ShortName":"12","LongName":"Pine Valley","Description":"","Color":"6CC06A","ColorText":"FFFFFF","RouteNameHtml":"12 - Pine Valley","OrderBy":"0012"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"922187","Status":1,"Sequence":2,"VehicleId":"C0324","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.9546165,"Longitude":-79.51905,"Trip":{"TripId":"922187","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85718","Route":{"RouteId":"20","ShortName":"88","LongName":"Bathurst","Description":"","Color":"30B8A2","ColorText":"FFFFFF","RouteNameHtml":"88 - Bathurst","OrderBy":"0088"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921654","Status":2,"Sequence":5,"VehicleId":"C0325","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.76325,"Longitude":-79.61858,"Trip":{"TripId":"921654","Headsign":"Rutherford Rd. - NB","ShortName":"","DirectionId":"0","BlockId":"a_85619","Route":{"RouteId":"50","ShortName":"28","LongName":"Huntington","Description":"","Color":"DD62A5","ColorText":"FFFFFF","RouteNameHtml":"28 - Huntington","OrderBy":"0028"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921279","Status":2,"Sequence":42,"VehicleId":"C0328","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7837334,"Longitude":-79.5998154,"Trip":{"TripId":"921279","Headsign":"Martin Grove - WB","ShortName":"","DirectionId":"1","BlockId":"a_85576","Route":{"RouteId":"12","ShortName":"10","LongName":"Woodbridge","Description":"","Color":"01A78F","ColorText":"FFFFFF","RouteNameHtml":"10 - Woodbridge","OrderBy":"0010"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"922186","Status":2,"Sequence":33,"VehicleId":"C0329","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8374,"Longitude":-79.45647,"Trip":{"TripId":"922186","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85650","Route":{"RouteId":"20","ShortName":"88","LongName":"Bathurst","Description":"","Color":"30B8A2","ColorText":"FFFFFF","RouteNameHtml":"88 - Bathurst","OrderBy":"0088"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921381","Status":2,"Sequence":10,"VehicleId":"C0330","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.806633,"Longitude":-79.58687,"Trip":{"TripId":"921381","Headsign":"Steeles AV. - SB","ShortName":"","DirectionId":"1","BlockId":"a_85581","Route":{"RouteId":"15","ShortName":"13","LongName":"Islington","Description":"","Color":"A17D7D","ColorText":"FFFFFF","RouteNameHtml":"13 - Islington","OrderBy":"0013"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921877","Status":2,"Sequence":27,"VehicleId":"C0331","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8214,"Longitude":-79.5806,"Trip":{"TripId":"921877","Headsign":"Leslie Street - EB","ShortName":"","DirectionId":"0","BlockId":"a_85617","Route":{"RouteId":"385","ShortName":"85","LongName":"Rutherford","Description":"","Color":"A3248F","ColorText":"FFFFFF","RouteNameHtml":"85 - Rutherford","OrderBy":"0085"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"920941","Status":2,"Sequence":20,"VehicleId":"C0335","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.809185,"Longitude":-79.45415,"Trip":{"TripId":"920941","Headsign":"Don Mills - EB","ShortName":"","DirectionId":"1","BlockId":"a_85555","Route":{"RouteId":"6","ShortName":"3","LongName":"Thornhill","Description":"","Color":"A54785","ColorText":"FFFFFF","RouteNameHtml":"3 - Thornhill","OrderBy":"0003"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"920904","Status":1,"Sequence":52,"VehicleId":"C0412","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8092842,"Longitude":-79.4538651,"Trip":{"TripId":"920904","Headsign":"York U  - WB","ShortName":"","DirectionId":"0","BlockId":"a_85554","Route":{"RouteId":"6","ShortName":"3","LongName":"Thornhill","Description":"","Color":"A54785","ColorText":"FFFFFF","RouteNameHtml":"3 - Thornhill","OrderBy":"0003"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"920975","Status":2,"Sequence":15,"VehicleId":"C0501","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8549156,"Longitude":-79.51185,"Trip":{"TripId":"920975","Headsign":"Woodbine - EB","ShortName":"","DirectionId":"0","BlockId":"a_85564","Route":{"RouteId":"8","ShortName":"4","LongName":"Major Mackenzie","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"4 - Major Mackenzie","OrderBy":"0004"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921487","Status":2,"Sequence":31,"VehicleId":"C0513","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8269653,"Longitude":-79.5341,"Trip":{"TripId":"921487","Headsign":"Teston - NB","ShortName":"","DirectionId":"1","BlockId":"a_85590","Route":{"RouteId":"16","ShortName":"20","LongName":"Jane","Description":"","Color":"7F6951","ColorText":"FFFFFF","RouteNameHtml":"20 - Jane","OrderBy":"0020"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921050","Status":2,"Sequence":34,"VehicleId":"C0516","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8747826,"Longitude":-79.42108,"Trip":{"TripId":"921050","Headsign":"Woodbine - EB","ShortName":"","DirectionId":"0","BlockId":"a_85562","Route":{"RouteId":"9","ShortName":"4A","LongName":"Major Mackenzie","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"4A - Major Mackenzie","OrderBy":"0004A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921559","Status":2,"Sequence":2,"VehicleId":"C0579","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8511,"Longitude":-79.4595,"Trip":{"TripId":"921559","Headsign":"Promenade Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85607","Route":{"RouteId":"17","ShortName":"23","LongName":"Thornhill Woods","Description":"","Color":"FDB813","ColorText":"FFFFFF","RouteNameHtml":"23 - Thornhill Woods","OrderBy":"0023"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"922145","Status":2,"Sequence":34,"VehicleId":"C0705","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8570175,"Longitude":-79.46065,"Trip":{"TripId":"922145","Headsign":"Seneca King - NB","ShortName":"","DirectionId":"1","BlockId":"a_85719","Route":{"RouteId":"20","ShortName":"88","LongName":"Bathurst","Description":"","Color":"30B8A2","ColorText":"FFFFFF","RouteNameHtml":"88 - Bathurst","OrderBy":"0088"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"922146","Status":2,"Sequence":4,"VehicleId":"C0707","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.78765,"Longitude":-79.41732,"Trip":{"TripId":"922146","Headsign":"Seneca King - NB","ShortName":"","DirectionId":"1","BlockId":"a_85656","Route":{"RouteId":"20","ShortName":"88","LongName":"Bathurst","Description":"","Color":"30B8A2","ColorText":"FFFFFF","RouteNameHtml":"88 - Bathurst","OrderBy":"0088"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921069","Status":2,"Sequence":11,"VehicleId":"C0724","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8806839,"Longitude":-79.3912,"Trip":{"TripId":"921069","Headsign":"Pine Valley - WB","ShortName":"","DirectionId":"1","BlockId":"a_85566","Route":{"RouteId":"9","ShortName":"4A","LongName":"Major Mackenzie","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"4A - Major Mackenzie","OrderBy":"0004A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921195","Status":2,"Sequence":9,"VehicleId":"C0725","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8019829,"Longitude":-79.42094,"Trip":{"TripId":"921195","Headsign":"Glen Shields - WB","ShortName":"","DirectionId":"1","BlockId":"a_85613","Route":{"RouteId":"10","ShortName":"5","LongName":"Clark","Description":"","Color":"49706F","ColorText":"FFFFFF","RouteNameHtml":"5 - Clark","OrderBy":"0005"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921779","Status":2,"Sequence":24,"VehicleId":"C0803","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8078,"Longitude":-79.464035,"Trip":{"TripId":"921779","Headsign":"Finch Terminal via Centre Street - EB","ShortName":"","DirectionId":"1","BlockId":"a_85674","Route":{"RouteId":"19","ShortName":"77","LongName":"Highway 7","Description":"","Color":"00AEED","ColorText":"FFFFFF","RouteNameHtml":"77 - Highway 7","OrderBy":"0077"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921424","Status":1,"Sequence":2,"VehicleId":"C0804","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8665161,"Longitude":-79.5453644,"Trip":{"TripId":"921424","Headsign":"York University - SB","ShortName":"","DirectionId":"0","BlockId":"a_85586","Route":{"RouteId":"16","ShortName":"20","LongName":"Jane","Description":"","Color":"7F6951","ColorText":"FFFFFF","RouteNameHtml":"20 - Jane","OrderBy":"0020"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921908","Status":2,"Sequence":24,"VehicleId":"C0812","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8440666,"Longitude":-79.47652,"Trip":{"TripId":"921908","Headsign":"Napa Valley - WB","ShortName":"","DirectionId":"1","BlockId":"a_85633","Route":{"RouteId":"385","ShortName":"85","LongName":"Rutherford","Description":"","Color":"A3248F","ColorText":"FFFFFF","RouteNameHtml":"85 - Rutherford","OrderBy":"0085"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921906","Status":1,"Sequence":2,"VehicleId":"C0917","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8672829,"Longitude":-79.38558,"Trip":{"TripId":"921906","Headsign":"Napa Valley - WB","ShortName":"","DirectionId":"1","BlockId":"a_85632","Route":{"RouteId":"385","ShortName":"85","LongName":"Rutherford","Description":"","Color":"A3248F","ColorText":"FFFFFF","RouteNameHtml":"85 - Rutherford","OrderBy":"0085"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921486","Status":2,"Sequence":4,"VehicleId":"C1031","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.778717,"Longitude":-79.50695,"Trip":{"TripId":"921486","Headsign":"Teston - NB","ShortName":"","DirectionId":"1","BlockId":"a_85589","Route":{"RouteId":"16","ShortName":"20","LongName":"Jane","Description":"","Color":"7F6951","ColorText":"FFFFFF","RouteNameHtml":"20 - Jane","OrderBy":"0020"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921426","Status":2,"Sequence":39,"VehicleId":"C1037","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.77857,"Longitude":-79.50685,"Trip":{"TripId":"921426","Headsign":"York University - SB","ShortName":"","DirectionId":"0","BlockId":"a_85588","Route":{"RouteId":"16","ShortName":"20","LongName":"Jane","Description":"","Color":"7F6951","ColorText":"FFFFFF","RouteNameHtml":"20 - Jane","OrderBy":"0020"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921008","Status":1,"Sequence":35,"VehicleId":"C1419","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.85615,"Longitude":-79.50965,"Trip":{"TripId":"921008","Headsign":"Vaughan Mills Mall - WB","ShortName":"","DirectionId":"1","BlockId":"a_85563","Route":{"RouteId":"8","ShortName":"4","LongName":"Major Mackenzie","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"4 - Major Mackenzie","OrderBy":"0004"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"920903","Status":1,"Sequence":2,"VehicleId":"C1422","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.81275,"Longitude":-79.3587341,"Trip":{"TripId":"920903","Headsign":"York U  - WB","ShortName":"","DirectionId":"0","BlockId":"a_85553","Route":{"RouteId":"6","ShortName":"3","LongName":"Thornhill","Description":"","Color":"A54785","ColorText":"FFFFFF","RouteNameHtml":"3 - Thornhill","OrderBy":"0003"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921709","Status":2,"Sequence":2,"VehicleId":"C1424","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.782383,"Longitude":-79.41562,"Trip":{"TripId":"921709","Headsign":"Hwy 50 via Centre - WB","ShortName":"","DirectionId":"0","BlockId":"a_85673","Route":{"RouteId":"19","ShortName":"77","LongName":"Highway 7","Description":"","Color":"00AEED","ColorText":"FFFFFF","RouteNameHtml":"77 - Highway 7","OrderBy":"0077"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921007","Status":1,"Sequence":2,"VehicleId":"C1425","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8756,"Longitude":-79.37418,"Trip":{"TripId":"921007","Headsign":"Vaughan Mills Mall - WB","ShortName":"","DirectionId":"1","BlockId":"a_85565","Route":{"RouteId":"8","ShortName":"4","LongName":"Major Mackenzie","Description":"","Color":"0057A7","ColorText":"FFFFFF","RouteNameHtml":"4 - Major Mackenzie","OrderBy":"0004"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921878","Status":2,"Sequence":28,"VehicleId":"C1429","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8452,"Longitude":-79.47038,"Trip":{"TripId":"921878","Headsign":"Leslie Street - EB","ShortName":"","DirectionId":"0","BlockId":"a_85635","Route":{"RouteId":"385","ShortName":"85","LongName":"Rutherford","Description":"","Color":"A3248F","ColorText":"FFFFFF","RouteNameHtml":"85 - Rutherford","OrderBy":"0085"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921778","Status":1,"Sequence":2,"VehicleId":"C1432","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.773365,"Longitude":-79.62615,"Trip":{"TripId":"921778","Headsign":"Finch Terminal via Centre Street - EB","ShortName":"","DirectionId":"1","BlockId":"a_85662","Route":{"RouteId":"19","ShortName":"77","LongName":"Highway 7","Description":"","Color":"00AEED","ColorText":"FFFFFF","RouteNameHtml":"77 - Highway 7","OrderBy":"0077"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921710","Status":2,"Sequence":32,"VehicleId":"C1513","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7941322,"Longitude":-79.525116,"Trip":{"TripId":"921710","Headsign":"Hwy 50 via Centre - WB","ShortName":"","DirectionId":"0","BlockId":"a_85675","Route":{"RouteId":"19","ShortName":"77","LongName":"Highway 7","Description":"","Color":"00AEED","ColorText":"FFFFFF","RouteNameHtml":"77 - Highway 7","OrderBy":"0077"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921425","Status":2,"Sequence":16,"VehicleId":"C1516","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8259163,"Longitude":-79.53155,"Trip":{"TripId":"921425","Headsign":"York University - SB","ShortName":"","DirectionId":"0","BlockId":"a_85587","Route":{"RouteId":"16","ShortName":"20","LongName":"Jane","Description":"","Color":"7F6951","ColorText":"FFFFFF","RouteNameHtml":"20 - Jane","OrderBy":"0020"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"921910","Status":2,"Sequence":46,"VehicleId":"C1518","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8180656,"Longitude":-79.5960159,"Trip":{"TripId":"921910","Headsign":"Napa Valley - WB","ShortName":"","DirectionId":"1","BlockId":"a_85634","Route":{"RouteId":"385","ShortName":"85","LongName":"Rutherford","Description":"","Color":"A3248F","ColorText":"FFFFFF","RouteNameHtml":"85 - Rutherford","OrderBy":"0085"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"923775","Status":2,"Sequence":2,"VehicleId":"F0610","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7755,"Longitude":-79.50027,"Trip":{"TripId":"923775","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85800","Route":{"RouteId":"2624","ShortName":"22A","LongName":"King City","Description":"","Color":"486F6E","ColorText":"FFFFFF","RouteNameHtml":"22A - King City","OrderBy":"0022A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924348","Status":2,"Sequence":8,"VehicleId":"F0613","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.0536156,"Longitude":-79.48542,"Trip":{"TripId":"924348","Headsign":"404 Town Centre - EB","ShortName":"","DirectionId":"0","BlockId":"a_85847","Route":{"RouteId":"287","ShortName":"57A","LongName":"Mulock","Description":"","Color":"00ACF0","ColorText":"FFFFFF","RouteNameHtml":"57A - Mulock","OrderBy":"0057A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924453","Status":1,"Sequence":1,"VehicleId":"F0850","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.9014,"Longitude":-79.44413,"Trip":{"TripId":"924453","Headsign":"Bernard Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85865","Route":{"RouteId":"292","ShortName":"98","LongName":"Yonge","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"98 - Yonge","OrderBy":"0098"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"923776","Status":2,"Sequence":53,"VehicleId":"F0904","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.9426,"Longitude":-79.46378,"Trip":{"TripId":"923776","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85797","Route":{"RouteId":"2624","ShortName":"22A","LongName":"King City","Description":"","Color":"486F6E","ColorText":"FFFFFF","RouteNameHtml":"22A - King City","OrderBy":"0022A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924028","Status":2,"Sequence":42,"VehicleId":"F0907","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.20883,"Longitude":-79.46668,"Trip":{"TripId":"924028","Headsign":"Sutton via Keswick Marketplace - NB","ShortName":"","DirectionId":"0","BlockId":"a_85880","Route":{"RouteId":"4388","ShortName":"50","LongName":"Queensway","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"50 - Queensway","OrderBy":"0050"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924049","Status":2,"Sequence":65,"VehicleId":"F0909","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.2129326,"Longitude":-79.46208,"Trip":{"TripId":"924049","Headsign":"Newmarket Terminal - SB","ShortName":"","DirectionId":"1","BlockId":"a_85878","Route":{"RouteId":"4388","ShortName":"50","LongName":"Queensway","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"50 - Queensway","OrderBy":"0050"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924026","Status":1,"Sequence":2,"VehicleId":"F0911","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.0528336,"Longitude":-79.487114,"Trip":{"TripId":"924026","Headsign":"Pefferlaw via Keswick Marketplace - NB","ShortName":"","DirectionId":"0","BlockId":"a_85882","Route":{"RouteId":"4388","ShortName":"50","LongName":"Queensway","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"50 - Queensway","OrderBy":"0050"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"923752","Status":2,"Sequence":46,"VehicleId":"F0915","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.92915,"Longitude":-79.52648,"Trip":{"TripId":"923752","Headsign":"York U - SB","ShortName":"","DirectionId":"0","BlockId":"a_85848","Route":{"RouteId":"2624","ShortName":"22A","LongName":"King City","Description":"","Color":"486F6E","ColorText":"FFFFFF","RouteNameHtml":"22A - King City","OrderBy":"0022A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924053","Status":2,"Sequence":3,"VehicleId":"F0986","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.1960831,"Longitude":-79.48012,"Trip":{"TripId":"924053","Headsign":"Simcoe Landing - NB","ShortName":"","DirectionId":"0","BlockId":"a_85831","Route":{"RouteId":"279","ShortName":"51","LongName":"Keswick Local","Description":"","Color":"DD62A5","ColorText":"FFFFFF","RouteNameHtml":"51 - Keswick Local","OrderBy":"0051"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924376","Status":2,"Sequence":14,"VehicleId":"F0990","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.0413322,"Longitude":-79.45188,"Trip":{"TripId":"924376","Headsign":"Newmarket Terminal - WB","ShortName":"","DirectionId":"1","BlockId":"a_85845","Route":{"RouteId":"287","ShortName":"57A","LongName":"Mulock","Description":"","Color":"00ACF0","ColorText":"FFFFFF","RouteNameHtml":"57A - Mulock","OrderBy":"0057A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924212","Status":2,"Sequence":5,"VehicleId":"F1062","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.0554,"Longitude":-79.4771347,"Trip":{"TripId":"924212","Headsign":"404 Town Centre - EB","ShortName":"","DirectionId":"0","BlockId":"a_85841","Route":{"RouteId":"283","ShortName":"55","LongName":"Davis Drive","Description":"","Color":"0157AA","ColorText":"FFFFFF","RouteNameHtml":"55 - Davis Drive","OrderBy":"0055"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"924165","Status":2,"Sequence":18,"VehicleId":"F1063","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.038517,"Longitude":-79.451416,"Trip":{"TripId":"924165","Headsign":"Yonge and Wellington - SB","ShortName":"","DirectionId":"0","BlockId":"a_85836","Route":{"RouteId":"282","ShortName":"54","LongName":"Bayview","Description":"","Color":"806B50","ColorText":"FFFFFF","RouteNameHtml":"54 - Bayview","OrderBy":"0054"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915816","Status":2,"Sequence":36,"VehicleId":"M0562","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8827171,"Longitude":-79.29955,"Trip":{"TripId":"915816","Headsign":"Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85095","Route":{"RouteId":"5269","ShortName":"16","LongName":"16Th Avenue","Description":"","Color":"4A6CB3","ColorText":"FFFFFF","RouteNameHtml":"16 - 16\u003csup\u003eth\u003c/sup\u003e Avenue","OrderBy":"0016"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916521","Status":2,"Sequence":7,"VehicleId":"M0819","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8775177,"Longitude":-79.4159,"Trip":{"TripId":"916521","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85219","Route":{"RouteId":"22","ShortName":"91","LongName":"Bayview","Description":"","Color":"A37E54","ColorText":"FFFFFF","RouteNameHtml":"91 - Bayview","OrderBy":"0091"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915653","Status":2,"Sequence":7,"VehicleId":"M0927","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.88188,"Longitude":-79.3149,"Trip":{"TripId":"915653","Headsign":"Clayton/Harvest Moon - SB","ShortName":"","DirectionId":"1","BlockId":"a_85086","Route":{"RouteId":"374","ShortName":"8","LongName":"Kennedy","Description":"","Color":"2B2A87","ColorText":"FFFFFF","RouteNameHtml":"8 - Kennedy","OrderBy":"0008"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916688","Status":2,"Sequence":26,"VehicleId":"M0934","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.84955,"Longitude":-79.43203,"Trip":{"TripId":"916688","Headsign":"Green Lane - NB","ShortName":"","DirectionId":"1","BlockId":"a_85231","Route":{"RouteId":"24","ShortName":"98/99","LongName":"Yonge","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"98/99 - Yonge","OrderBy":"098/99"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916682","Status":2,"Sequence":21,"VehicleId":"M1001","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.01293,"Longitude":-79.47083,"Trip":{"TripId":"916682","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85234","Route":{"RouteId":"24","ShortName":"98/99","LongName":"Yonge","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"98/99 - Yonge","OrderBy":"098/99"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916031","Status":2,"Sequence":30,"VehicleId":"M1002","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.82515,"Longitude":-79.35149,"Trip":{"TripId":"916031","Headsign":"Honda Canada - NB","ShortName":"","DirectionId":"0","BlockId":"a_85286","Route":{"RouteId":"9481","ShortName":"24","LongName":"Woodbine Avenue","Description":"","Color":"FDB813","ColorText":"FFFFFF","RouteNameHtml":"24 - Woodbine Avenue","OrderBy":"0024"},"Calendar":{"CalendarId":"3495501","Monday":false,"Tuesday":false,"Wednesday":false,"Thursday":false,"Friday":false,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916061","Status":2,"Sequence":12,"VehicleId":"M1005","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8935165,"Longitude":-79.36594,"Trip":{"TripId":"916061","Headsign":"24 Don Mills Station - SB","ShortName":"","DirectionId":"1","BlockId":"a_85291","Route":{"RouteId":"9481","ShortName":"24","LongName":"Woodbine Avenue","Description":"","Color":"FDB813","ColorText":"FFFFFF","RouteNameHtml":"24 - Woodbine Avenue","OrderBy":"0024"},"Calendar":{"CalendarId":"3495501","Monday":false,"Tuesday":false,"Wednesday":false,"Thursday":false,"Friday":false,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916689","Status":2,"Sequence":84,"VehicleId":"M1006","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.0372658,"Longitude":-79.47612,"Trip":{"TripId":"916689","Headsign":"Green Lane - NB","ShortName":"","DirectionId":"1","BlockId":"a_85232","Route":{"RouteId":"24","ShortName":"98/99","LongName":"Yonge","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"98/99 - Yonge","OrderBy":"098/99"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916700","Status":2,"Sequence":34,"VehicleId":"M1010","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8248329,"Longitude":-79.42658,"Trip":{"TripId":"916700","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85214","Route":{"RouteId":"25","ShortName":"99","LongName":"Yonge","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"99 - Yonge","OrderBy":"0099"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915853","Status":2,"Sequence":33,"VehicleId":"M1015","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8738,"Longitude":-79.33936,"Trip":{"TripId":"915853","Headsign":"Bathurst St - WB","ShortName":"","DirectionId":"1","BlockId":"a_85067","Route":{"RouteId":"5269","ShortName":"16","LongName":"16Th Avenue","Description":"","Color":"4A6CB3","ColorText":"FFFFFF","RouteNameHtml":"16 - 16\u003csup\u003eth\u003c/sup\u003e Avenue","OrderBy":"0016"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915502","Status":2,"Sequence":51,"VehicleId":"M1025","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8344345,"Longitude":-79.3047,"Trip":{"TripId":"915502","Headsign":"Markham Road - EB","ShortName":"","DirectionId":"0","BlockId":"a_85046","Route":{"RouteId":"372","ShortName":"2","LongName":"Milliken","Description":"","Color":"C49F08","ColorText":"FFFFFF","RouteNameHtml":"2 - Milliken","OrderBy":"0002"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915852","Status":1,"Sequence":2,"VehicleId":"M1027","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8813171,"Longitude":-79.2261,"Trip":{"TripId":"915852","Headsign":"Bathurst St - WB","ShortName":"","DirectionId":"1","BlockId":"a_85099","Route":{"RouteId":"5269","ShortName":"16","LongName":"16Th Avenue","Description":"","Color":"4A6CB3","ColorText":"FFFFFF","RouteNameHtml":"16 - 16\u003csup\u003eth\u003c/sup\u003e Avenue","OrderBy":"0016"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916691","Status":1,"Sequence":2,"VehicleId":"M1105","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.78245,"Longitude":-79.4143,"Trip":{"TripId":"916691","Headsign":"Green Lane - NB","ShortName":"","DirectionId":"1","BlockId":"a_85215","Route":{"RouteId":"24","ShortName":"98/99","LongName":"Yonge","Description":"","Color":"674287","ColorText":"FFFFFF","RouteNameHtml":"98/99 - Yonge","OrderBy":"098/99"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916627","Status":2,"Sequence":32,"VehicleId":"M1112","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.85605,"Longitude":-79.41053,"Trip":{"TripId":"916627","Headsign":"Subrisco - NB","ShortName":"","DirectionId":"1","BlockId":"a_85218","Route":{"RouteId":"23","ShortName":"91A","LongName":"Bayview","Description":"","Color":"A37E54","ColorText":"FFFFFF","RouteNameHtml":"91A - Bayview","OrderBy":"0091A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916222","Status":2,"Sequence":38,"VehicleId":"M1115","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8853149,"Longitude":-79.45174,"Trip":{"TripId":"916222","Headsign":"Bernard Terminal - NB","ShortName":"","DirectionId":"0","BlockId":"a_85210","Route":{"RouteId":"383","ShortName":"83","LongName":"Trench","Description":"","Color":"70BF42","ColorText":"FFFFFF","RouteNameHtml":"83 - Trench","OrderBy":"0083"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915467","Status":2,"Sequence":25,"VehicleId":"M1116","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8614,"Longitude":-79.30988,"Trip":{"TripId":"915467","Headsign":"Richmond Hill Centre Terminal - WB","ShortName":"","DirectionId":"1","BlockId":"a_85034","Route":{"RouteId":"371","ShortName":"1","LongName":"Highway 7","Description":"","Color":"49706F","ColorText":"FFFFFF","RouteNameHtml":"1 - Highway 7","OrderBy":"0001"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915429","Status":2,"Sequence":9,"VehicleId":"M1118","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.83975,"Longitude":-79.4251,"Trip":{"TripId":"915429","Headsign":"Copper Creek via Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85036","Route":{"RouteId":"371","ShortName":"1","LongName":"Highway 7","Description":"","Color":"49706F","ColorText":"FFFFFF","RouteNameHtml":"1 - Highway 7","OrderBy":"0001"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916112","Status":1,"Sequence":2,"VehicleId":"M1401","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8928,"Longitude":-79.4687347,"Trip":{"TripId":"916112","Headsign":"Betty Roman - EB","ShortName":"","DirectionId":"0","BlockId":"a_85155","Route":{"RouteId":"5277","ShortName":"80","LongName":"Elgin Mills","Description":"","Color":"805D95","ColorText":"FFFFFF","RouteNameHtml":"80 - Elgin Mills","OrderBy":"0080"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916592","Status":2,"Sequence":41,"VehicleId":"M1403","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8022652,"Longitude":-79.40107,"Trip":{"TripId":"916592","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85220","Route":{"RouteId":"23","ShortName":"91A","LongName":"Bayview","Description":"","Color":"A37E54","ColorText":"FFFFFF","RouteNameHtml":"91A - Bayview","OrderBy":"0091A"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915430","Status":2,"Sequence":38,"VehicleId":"M1405","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8740158,"Longitude":-79.25902,"Trip":{"TripId":"915430","Headsign":"Copper Creek via Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85035","Route":{"RouteId":"371","ShortName":"1","LongName":"Highway 7","Description":"","Color":"49706F","ColorText":"FFFFFF","RouteNameHtml":"1 - Highway 7","OrderBy":"0001"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916278","Status":2,"Sequence":39,"VehicleId":"M1407","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8459,"Longitude":-79.4433,"Trip":{"TripId":"916278","Headsign":"Richmond Hill Centre Terminal - SB","ShortName":"","DirectionId":"1","BlockId":"a_85207","Route":{"RouteId":"383","ShortName":"83","LongName":"Trench","Description":"","Color":"70BF42","ColorText":"FFFFFF","RouteNameHtml":"83 - Trench","OrderBy":"0083"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916557","Status":2,"Sequence":2,"VehicleId":"M1411","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.78225,"Longitude":-79.41475,"Trip":{"TripId":"916557","Headsign":"Taylor Mills - NB","ShortName":"","DirectionId":"1","BlockId":"a_85178","Route":{"RouteId":"22","ShortName":"91","LongName":"Bayview","Description":"","Color":"A37E54","ColorText":"FFFFFF","RouteNameHtml":"91 - Bayview","OrderBy":"0091"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915854","Status":2,"Sequence":59,"VehicleId":"M1415","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.85425,"Longitude":-79.4644,"Trip":{"TripId":"915854","Headsign":"Bathurst St - WB","ShortName":"","DirectionId":"1","BlockId":"a_85097","Route":{"RouteId":"5269","ShortName":"16","LongName":"16Th Avenue","Description":"","Color":"4A6CB3","ColorText":"FFFFFF","RouteNameHtml":"16 - 16\u003csup\u003eth\u003c/sup\u003e Avenue","OrderBy":"0016"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916368","Status":2,"Sequence":3,"VehicleId":"M1506","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.77762,"Longitude":-79.34783,"Trip":{"TripId":"916368","Headsign":"Richmond Green Secondary School - NB","ShortName":"","DirectionId":"0","BlockId":"a_85129","Route":{"RouteId":"390","ShortName":"90","LongName":"Leslie","Description":"","Color":"FDAE35","ColorText":"FFFFFF","RouteNameHtml":"90 - Leslie","OrderBy":"0090"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"916416","Status":2,"Sequence":5,"VehicleId":"M1507","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8936348,"Longitude":-79.39467,"Trip":{"TripId":"916416","Headsign":"Don Mills Station - SB","ShortName":"","DirectionId":"1","BlockId":"a_85130","Route":{"RouteId":"390","ShortName":"90","LongName":"Leslie","Description":"","Color":"FDAE35","ColorText":"FFFFFF","RouteNameHtml":"90 - Leslie","OrderBy":"0090"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"915574","Status":2,"Sequence":62,"VehicleId":"M1508","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8182831,"Longitude":-79.4033,"Trip":{"TripId":"915574","Headsign":"Finch Terminal - WB","ShortName":"","DirectionId":"1","BlockId":"a_85042","Route":{"RouteId":"372","ShortName":"2","LongName":"Milliken","Description":"","Color":"C49F08","ColorText":"FFFFFF","RouteNameHtml":"2 - Milliken","OrderBy":"0002"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918652","Status":2,"Sequence":10,"VehicleId":"V1086","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8609657,"Longitude":-79.4347153,"Trip":{"TripId":"918652","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85385","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918654","Status":2,"Sequence":21,"VehicleId":"V1089","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.9909821,"Longitude":-79.4653854,"Trip":{"TripId":"918654","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85380","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918509","Status":2,"Sequence":23,"VehicleId":"V1094","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8221,"Longitude":-79.4258652,"Trip":{"TripId":"918509","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85399","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918507","Status":2,"Sequence":12,"VehicleId":"V1371","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.92895,"Longitude":-79.45157,"Trip":{"TripId":"918507","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85383","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918653","Status":2,"Sequence":17,"VehicleId":"V1373","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.9456,"Longitude":-79.45482,"Trip":{"TripId":"918653","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85379","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918886","Status":2,"Sequence":9,"VehicleId":"V1375","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.85227,"Longitude":-79.3171,"Trip":{"TripId":"918886","Headsign":"York U - WB","ShortName":"","DirectionId":"1","BlockId":"a_85395","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918506","Status":2,"Sequence":9,"VehicleId":"V1380","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.9806824,"Longitude":-79.46339,"Trip":{"TripId":"918506","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85382","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918508","Status":2,"Sequence":22,"VehicleId":"V1381","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.83985,"Longitude":-79.4251862,"Trip":{"TripId":"918508","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85396","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918651","Status":2,"Sequence":6,"VehicleId":"V1382","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.82707,"Longitude":-79.42688,"Trip":{"TripId":"918651","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85378","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918505","Status":1,"Sequence":2,"VehicleId":"V1383","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.0528831,"Longitude":-79.48645,"Trip":{"TripId":"918505","Headsign":"Finch Terminal - SB","ShortName":"","DirectionId":"0","BlockId":"a_85381","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918816","Status":1,"Sequence":2,"VehicleId":"V1389","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7757149,"Longitude":-79.5004654,"Trip":{"TripId":"918816","Headsign":"Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85418","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918650","Status":1,"Sequence":2,"VehicleId":"V1391","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7821655,"Longitude":-79.41552,"Trip":{"TripId":"918650","Headsign":"Newmarket Terminal - NB","ShortName":"","DirectionId":"1","BlockId":"a_85377","Route":{"RouteId":"1","ShortName":"Viva blue","LongName":"Viva Blue","Description":"","Color":"00A3E0","ColorText":"FFFFFF","RouteNameHtml":"Viva blue - Viva Blue","OrderBy":"0000Viva blue"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918815","Status":2,"Sequence":2,"VehicleId":"V1393","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7968674,"Longitude":-79.49837,"Trip":{"TripId":"918815","Headsign":"Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85400","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918884","Status":1,"Sequence":22,"VehicleId":"V1395","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8401337,"Longitude":-79.42582,"Trip":{"TripId":"918884","Headsign":"York U - WB","ShortName":"","DirectionId":"1","BlockId":"a_85397","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918812","Status":2,"Sequence":20,"VehicleId":"V1396","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8558846,"Longitude":-79.3053,"Trip":{"TripId":"918812","Headsign":"Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85398","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"919450","Status":1,"Sequence":6,"VehicleId":"V5101","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.05982,"Longitude":-79.4580154,"Trip":{"TripId":"919450","Headsign":"Newmarket Terminal - WB","ShortName":"","DirectionId":"1","BlockId":"a_85471","Route":{"RouteId":"8838","ShortName":"Viva yellow","LongName":"Viva Yellow","Description":"","Color":"E5E500","ColorText":"FFFFFF","RouteNameHtml":"Viva yellow - Viva Yellow","OrderBy":"0000Viva yellow"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"919295","Status":2,"Sequence":2,"VehicleId":"V5108","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7886,"Longitude":-79.52392,"Trip":{"TripId":"919295","Headsign":"Pine Valley - NB","ShortName":"","DirectionId":"1","BlockId":"a_85485","Route":{"RouteId":"4","ShortName":"Viva orange","LongName":"Viva Orange","Description":"","Color":"FFA100","ColorText":"FFFFFF","RouteNameHtml":"Viva orange - Viva Orange","OrderBy":"0000Viva orange"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"919371","Status":2,"Sequence":2,"VehicleId":"V5131","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.054718,"Longitude":-79.4807,"Trip":{"TripId":"919371","Headsign":"Highway 404 Park and Ride - EB","ShortName":"","DirectionId":"0","BlockId":"a_85472","Route":{"RouteId":"8838","ShortName":"Viva yellow","LongName":"Viva Yellow","Description":"","Color":"E5E500","ColorText":"FFFFFF","RouteNameHtml":"Viva yellow - Viva Yellow","OrderBy":"0000Viva yellow"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"919449","Status":1,"Sequence":2,"VehicleId":"V5133","Label":"","LicensePlate":"","Bearing":0,"Latitude":44.06753,"Longitude":-79.42105,"Trip":{"TripId":"919449","Headsign":"Newmarket Terminal - WB","ShortName":"","DirectionId":"1","BlockId":"a_85473","Route":{"RouteId":"8838","ShortName":"Viva yellow","LongName":"Viva Yellow","Description":"","Color":"E5E500","ColorText":"FFFFFF","RouteNameHtml":"Viva yellow - Viva Yellow","OrderBy":"0000Viva yellow"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"919287","Status":2,"Sequence":5,"VehicleId":"V5157","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.7894669,"Longitude":-79.5251846,"Trip":{"TripId":"919287","Headsign":"York University - SB","ShortName":"","DirectionId":"0","BlockId":"a_85440","Route":{"RouteId":"4","ShortName":"Viva orange","LongName":"Viva Orange","Description":"","Color":"FFA100","ColorText":"FFFFFF","RouteNameHtml":"Viva orange - Viva Orange","OrderBy":"0000Viva orange"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918814","Status":2,"Sequence":6,"VehicleId":"V5202","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.83145,"Longitude":-79.4412842,"Trip":{"TripId":"918814","Headsign":"Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85417","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918883","Status":2,"Sequence":24,"VehicleId":"V5217","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8089828,"Longitude":-79.45417,"Trip":{"TripId":"918883","Headsign":"York U - WB","ShortName":"","DirectionId":"1","BlockId":"a_85414","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918885","Status":2,"Sequence":14,"VehicleId":"V5222","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.84878,"Longitude":-79.36304,"Trip":{"TripId":"918885","Headsign":"York U - WB","ShortName":"","DirectionId":"1","BlockId":"a_85406","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918813","Status":2,"Sequence":13,"VehicleId":"V7204","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.8456841,"Longitude":-79.37695,"Trip":{"TripId":"918813","Headsign":"Markham Stouffville Hospital - EB","ShortName":"","DirectionId":"0","BlockId":"a_85415","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918888","Status":1,"Sequence":2,"VehicleId":"V8207","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.88485,"Longitude":-79.23282,"Trip":{"TripId":"918888","Headsign":"York U - WB","ShortName":"","DirectionId":"1","BlockId":"a_85416","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}},{"TripId":"918887","Status":2,"Sequence":2,"VehicleId":"V8208","Label":"","LicensePlate":"","Bearing":0,"Latitude":43.87882,"Longitude":-79.23402,"Trip":{"TripId":"918887","Headsign":"York U - WB","ShortName":"","DirectionId":"1","BlockId":"a_85393","Route":{"RouteId":"2","ShortName":"Viva purple","LongName":"Viva Purple","Description":"","Color":"A44DC4","ColorText":"FFFFFF","RouteNameHtml":"Viva purple - Viva Purple","OrderBy":"0000Viva purple"},"Calendar":{"CalendarId":"1_merged_3495509","Monday":true,"Tuesday":true,"Wednesday":true,"Thursday":true,"Friday":true,"Saturday":false,"Sunday":false,"StartDate":"\/Date(1460865600000)\/","EndDate":"\/Date(1466913599000)\/"}}}]

      AgncyOC = new ObservableCollection<bodyAgency>(_agncis);

      MsgBrdText = $"{AgncyOC.Count()} {AgencyVsblty}";

      await smartAgencyLoad();
    }

    void stopAndClearPredictions()
    {
      _dt.Stop();
      CurrentVS = null;
      CurVSs.Clear();
      _prevTimeMsSince1970 = 0;
      VLocnPredOC.ToList().ForEach(vp => VLocnPredOC.Remove(vp));
    }



    bool CanStartAgencySeln { get { return true; } }
    bool CanStartRoutesSeln { get { return SelectAgency != null; } }
    bool CanStartVehTrackng { get { return true; } } //  FavVSOC.Any(); } }





    async Task say(string s)
    {
      try
      {
        //Debug.WriteLine($"{DateTime.Now:HH:mm:ss.f}  say: '{s}'");
        if (!IsVrbl) return;
        var stream = await Synth.SynthesizeTextToStreamAsync(s);
        _me.SetSource(stream, stream.ContentType);
        _me.Play();
      }
      catch (Exception ex) { ex.Pop(s); }
    }














    void loadRoutesForSelectAgency(string agncyTag, bool isGtfs)
    {
      _prevAgncy = agncyTag;
      _isGtfs = isGtfs;
      try
      {
        RouteList oc = null;
        if (isGtfs)
          Task.Factory.StartNew(async () => await WebSvc.GetGtfsRoutes(agncyTag)).ContinueWith(_ => oc = YrtToTtc.ttc2yrtRoutes(_.Result.Result)).Wait();
        else
          Task.Factory.StartNew(async () => await WebSvc.GetRouteList(agncyTag)).ContinueWith(_ => oc = _.Result.Result).Wait();
        if (oc == null)
          return;

        _routes = new Collection<bodyRouteL>(oc.route);
        RouteOC.ClearAddRangeSynch<bodyRouteL>(_routes);

        Debug.WriteLine("* {0} agency loaded {1} routes.", agncyTag, oc.route.Length);
      }
      catch (Exception ex) { ex.Pop(); }
    }
    async void loadPlotRouteForCurAgncyRouteVoid(bool setViewBounds) => await loadPlotRouteForCurAgncyRoute(setViewBounds);
    async Task loadPlotRouteForCurAgncyRoute(bool setViewBounds)
    {
      try
      {
        var a = _isGtfs ?
          YrtToTtc.yrt2ttcStops(await WebSvc.GetGtfsStops(_prevRoute)) :
          await WebSvc.GetRouteConfig(_prevAgncy, _prevRoute);

        if (a.route == null)
          return;

        _vStops = new Collection<bodyRouteStop>(a.route.stop);
        foreach (var rstop in _vStops) rstop.IsFav = FavVSOC.Any(r => r.Id == string.Format(_idfor, SelectAgency == null ? "" : SelectAgency.tag, SelectRouteL == null ? "" : SelectRouteL.tag, rstop.tag));

        VStopOC.ClearAddRangeSynch<bodyRouteStop>(_vStops);


        Debug.WriteLine("* {0} route loaded {1} stops.", _prevRoute, VStopOC.Count);

        if (_map != null)
        {
          removeMapElementsByZIndex(_map, ZIdx.BusStop);

          VStopOC.ToList().ForEach(stop => addBusStopFlag(_map, stop.lat, stop.lon, _clrBrGrn, setViewBounds ? 2 : 2)); // addBusRouteToMap_NOGO_Disordered(_map, VStopOC, Colors.BlueViolet);

          if (setViewBounds)
            await setRouteViewBounds(_map, a.route);
        }
      }
      catch (Exception ex) { ex.Pop(); }
    }


    Collection<bodyPredictionsDirectionPrediction> _prevVPreds = null;
    double _distCurToStopM = 100;
    double _jogEtaInSec => _distCurToStopM / 3 - 60; // ~ 10 km/h + -60sec for margin of error.
    double _jogEtaInMin => _jogEtaInSec / 60;

    async Task webCallUpdate_VLocn_PredOC()
    {
      try
      {

        var vPreds = await GetPreds();
        updateLTCorner(vPreds ?? _prevVPreds);

        if ((vPreds == null || vPreds.Count == 0) && !_isAllVsPrdctd)
        {
          if (_prevVPreds != null)
            foreach (var pred in _prevVPreds)
              pred.seconds -= (DateTime.Now - _lastVehPredsGetAttempt).TotalSeconds;

          return;
        }

        if (vPreds.Count > 0)
          _prevVPreds = vPreds;

        var vLocns = _isGtfs ?
          YrtToTtc.yrt2ttcVehs(await WebSvc.GetGtfsVehLocs(_prevRoute)) :
          await WebSvc.GetVehLocations(_prevAgncy, _prevRoute, _prevTimeMsSince1970);

        if (vLocns?.vehicle == null)
          return;

        if (vLocns.lastTime != null)
          _prevTimeMsSince1970 = vLocns.lastTime.time;          //if (res.vehicle[0].secsSinceReport< 0)          {            var stream = await _synth.SynthesizeTextToStreamAsync("Negative!");            _me.SetSource(stream, stream.ContentType);            _me.Play();          }

        updateOC(vPreds, vLocns);

        updatePredForLocn(vPreds);
        removeOldPredictions();
        lineVStoBus0();
      }
      catch (Exception ex) { ex.Pop(); }

      await zoomTo1stBusses(_prevPosition);

      //good idea + no use for now: foreach (var locn in VLocnPredOC) locn.DistToPredStop = 1000.0 * Math.Sqrt((double)((locn.lat - CurrentVS.Lat) * (locn.lat - CurrentVS.Lat) + (locn.lon - CurrentVS.Lon) * (locn.lon - CurrentVS.Lon)));

      //Debug.WriteLine("* {0} route loaded {1} vehLocns.", _prevRoute, VLocnPredOC.Count);
      //Debug.WriteLine("* {0} rStop loaded {1} predns.", _prevVStop, vPreds.Count);

      //VLocnPredOC = new ObservableCollection<bodyVehicle>(VLocnPredOC.Where(r => IsAll ? true : r.SmartMinSecLbl != null).OrderBy(r => r.Predictn == null ? ushort.MaxValue : r?.Predictn?.seconds).ThenBy(r => r.DistToPredStop)
      //  //.Select(r => (bodyVehicle)r)
      //  );
    }

    void updateOC(Collection<bodyPredictionsDirectionPrediction> vPreds, VehLocations vLocns)
    {
      try
      {
        var i = 0;
        foreach (bodyVehicle vLocn in vLocns.vehicle.Where(vl => _isAllVsPrdctd ? true : vPreds.Any(vp => vp.vehicle == vl.id || string.IsNullOrEmpty(vp.vehicle)))) // for all or matching predictions
        {
          // Debug.WriteLine($"++i {(i < VLocnPredOC.Count ? "< " : ">=")} VLocnPredOC.Count: {i} · {VLocnPredOC.Count}");

          var vlp = vLocn.id == null ? (i < VLocnPredOC.Count ? VLocnPredOC[i] : null) : VLocnPredOC.FirstOrDefault(r => r.id == vLocn.id);
          if (vlp == null)
          {
            vLocn.Zoom = _map.ZoomLevel;
            VLocnPredOC.Add(vLocn);
          }
          else
          {
            vlp.lat = vLocn.lat;
            vlp.lon = vLocn.lon;
            vlp.heading = vLocn.heading;
            vlp.secsSinceReport = vLocn.secsSinceReport;
            vlp.Zoom = _map.ZoomLevel;
          }
          i++;
        }
      }
      catch (Exception ex) { ex.Pop(); }
    }

    void updatePredForLocn(Collection<bodyPredictionsDirectionPrediction> vPreds)
    {
      try
      {
        var i = 0;
        foreach (bodyPredictionsDirectionPrediction prdn in vPreds.OrderByDescending(r => r.seconds)) //in case multi schedule for the same bus - take the soonest prediction.
        {
          var locn = string.IsNullOrEmpty(prdn.vehicle) ?
            (i < VLocnPredOC.Count ? VLocnPredOC[i] : null) :
            VLocnPredOC.FirstOrDefault(r => r.id == prdn.vehicle);
          if (locn != null)
          {
            if (locn.Predictn == null)
              locn.Predictn = prdn;
            else
            {
              if (locn?.Predictn?.seconds == prdn.seconds) Debug.WriteLine($"+++++++====>> sec {prdn.seconds} are the same ");
              //if (locn.Predictn.epochTime == prdn.epochTime) Debug.WriteLine($"+++++++====>> epochTime {prdn.epochTime} are the same ");

              locn.Predictn.affectedByLayover = prdn.affectedByLayover;
              locn.Predictn.affectedByLayoverSpecified = prdn.affectedByLayoverSpecified;
              locn.Predictn.block = prdn.block;
              locn.Predictn.branch = prdn.branch;
              locn.Predictn.dirTag = prdn.dirTag;
              locn.Predictn.epochTime = prdn.epochTime;
              locn.Predictn.isDeparture = prdn.isDeparture;
              locn.Predictn.minutes = prdn.minutes;
              locn.Predictn.seconds = prdn.seconds;
              locn.Predictn.tripTag = prdn.tripTag;
              locn.Predictn.vehicle = prdn.vehicle;
              locn.SmartMinSecLbl = locn.CalcSmartMinSecLbl(prdn.seconds, prdn.epochTime, prdn.isDeparture);
            }
          }
          else
            Debug.WriteLine($"New Prediction - without location yet: {prdn.branch} - {prdn.seconds} sec ");
          i++;
        }
      }
      catch (Exception ex) { ex.Pop(); }
    }

    void removeOldPredictions()
    {
      try
      {
        VLocnPredOC.Where(v => v.Predictn != null && (v?.Predictn?.seconds > 3600 || v?.Predictn?.seconds < 0)).ToList().ForEach(locn => VLocnPredOC.Remove(locn));            //todo: ? remove vexctor parts too.
      }
      catch (Exception ex) { ex.Pop(); }
    }

    void updateLTCorner(Collection<bodyPredictionsDirectionPrediction> vPreds)
    {
      if (vPreds == null)
        CornerLT = $"null";
      else if (vPreds.Count == 0)
        CornerLT = $"...";
      else
      {
        var dt = DateTime.Now - _lastVehPredsGetSUCCESS;
        CornerLT = $"{string.Join("\n", vPreds./*Where(r => r.seconds > _jogEtaInSec).*/OrderBy((s => s.seconds)).Select(r => $"{((r.seconds - dt.TotalSeconds) / 60.0):N0} {(r.branch.EndsWith(_exprs) ? _exprs : "  ·")}").Take(_maxvp).ToArray())}"; //alt to string.Join():  string[] words = { "one", "two", "three" }; var res = words.Aggregate((current, next) => current + ", " + next);
      }
    }

    async Task<Collection<bodyPredictionsDirectionPrediction>> GetPreds()
    {
      var vPreds = new Collection<bodyPredictionsDirectionPrediction>();
      if (_prevRoute != null && _prevVStop != null)
      {
        try
        {
          var rex = _isGtfs ?
            YrtToTtc.Preds(await WebSvc.GetGtfsPredcts(_prevRoute, _prevVStop)) :
            await WebSvc.GetPredictions(_prevAgncy, _prevRoute, _prevVStop, false);

          if (rex?.predictions?.direction != null)
            foreach (var drn in rex.predictions.direction)
              foreach (var vp in drn.prediction)
                vPreds.Add(vp);

          _lastVehPredsGetAttempt = DateTime.Now;

          if (vPreds.Any())
            _lastVehPredsGetSUCCESS = DateTime.Now;
        }
        catch (Exception ex) { ex.Pop(); }
      }

      return vPreds.Count <= _maxvp ? vPreds : new Collection<bodyPredictionsDirectionPrediction>(vPreds.OrderBy(r => r.epochTime).Take(_maxvp).ToList()); //Nov 2017 //todo: make sure not cluttering with too many buses.
    }
    int _maxvp = 5;

    Collection<bodyPredictionsDirectionPrediction> getPredictions()
    {
      var vPreds = new Collection<bodyPredictionsDirectionPrediction>();
      try
      {
        Task.Factory.StartNew(async () => await WebSvc.GetPredictions(_prevAgncy, _prevRoute, _prevVStop, false)).ContinueWith(_ =>
        {
          if (_.Result.Result.predictions.direction != null)
            foreach (var drn in _.Result.Result.predictions.direction) // process all directions (like E,F,S,D etc.)
              foreach (var vp in drn.prediction)
                vPreds.Add(vp);
        }).Wait();
      }
      catch (Exception ex) { ex.Pop(); }

      return vPreds;
    }






    public bool CanGoBack { get { return _viewMode != ViewModes.VehTrackg && FavVSOC.Any(); } }
    public async Task GoBack() { await startVehTrackng("OK: Going back"); }

    bool canToRout(bodyAgency bodyAgency) { return true; }
    bool canToVStp(bodyRouteL bodyRouteL) { return true; }

    void onToRout(bodyAgency bodyAgency) { SelectAgency = bodyAgency; RouteLstVsblty = Visibility.Visible; ; }
    void onToVStp(bodyRouteL bodyRouteL) { SelectRouteL = bodyRouteL; MsgBrdText = string.Format("{0} {1}", VStopOC.Count, VStopsVsblty); }


    void onTglFav(bodyRouteStop vs)
    {
      SelectRtStop = vs;
      vs.IsFav = !vs.IsFav;

      if (vs.IsFav)
        AddFavVStop(vs);
      else
        DeletFavVStop(vs);
    }
    bool canTglFav(bodyRouteStop x)
    {
      return x != null;
    }



    async Task smartAgencyLoad()
    {
      try
      {
        ProgrsMax = _agncis.Count;

        await _map.TrySetViewAsync(new Geopoint(_stngs.LatestLocation), 2.1);

        foreach (var agncy in _agncis)
        {
          if (agncy.Location.Latitude == 0) // YRT is hardcoded to a location.
          {
            var oc = await WebSvc.GetRouteList(agncy.tag);
            if (oc == null)
              continue;

            if (oc.route == null)
              continue;

            var r1 = oc.route.FirstOrDefault();
            if (r1 == null)
              continue;

            var a = await WebSvc.GetRouteConfig(agncy.tag, r1.tag);
            if (a == null && a.route == null)
              continue;

            agncy.Location = new BasicGeoposn { Latitude = (a.route.latMax + a.route.latMin) / 2, Longitude = (a.route.lonMax + a.route.lonMin) / 2 };
          }
          else
            Debug.WriteLine(")");

          agncy.DistanceFromHere = GetDistanceTo(agncy.Location, _stngs.LatestLocation);  //Debug.WriteLine("@@> {0,3} / {1}:  {2:N3,8} : {3:N3,8}   {4}.", i, _agncis.Count, agncy.Location.Latitude, agncy.Location.Longitude, agncy.tag);

          addMapIcon(agncy.tag, _mapIconStreamReference_Agency, new Geopoint(new BasicGeoposition() { Latitude = agncy.Location.Latitude, Longitude = agncy.Location.Longitude })); // placeDblEllipse(_map, agncy.Location.Latitude, agncy.Location.Longitude, string.Format("Agncy'{0}'", agncy.tag), _dkRed);

          //if (i % 16 == 0)
          await _map.TrySetViewAsync(new Geopoint(_stngs.LatestLocation), 2.0 + 4.1 * ProgrsVal / _agncis.Count, null, null, MapAnimationKind.None);

          NotifyUser(string.Format("Locating agencies {0} / {1} ...", ++ProgrsVal, ProgrsMax));
        }

        AgncyOC = new System.Collections.ObjectModel.ObservableCollection<TTC.Model2015.bodyAgency>(_agncis.OrderBy(r => r.DistanceFromHere));

        var box = GeoboundingBox.TryCompute(AgncyOC.Select(r => new BasicGeoposition { Latitude = r.Location.Latitude, Longitude = r.Location.Longitude }).Take(2));
        await _map.TrySetViewBoundsAsync(box, new Thickness(22), MapAnimationKind.Default);

        ProgrsVal = 0;

        NotifyUser("Tap on trasportation agency (TA) to see its list of routes.");

        //if (_viewMode != ViewModes.AgncySeln)          MsgBrdVsblty = Visibility.Collapsed;
      }
      catch (Exception ex) { ex.Pop(); }
    }

    async void NotifyUser(string msg, int seconds = 3, Brush clr = null)
    {
      MsgBrdVsblty = Visibility.Visible;
      MsgBrdText = msg;

      if (clr != null)
        MsgBrdColor = clr;

      await Task.Delay(seconds * 1000);
      MsgBrdVsblty = Visibility.Collapsed;
    }
  }
}
