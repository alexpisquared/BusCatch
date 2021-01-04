using AsLink;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using TTC.Model2015;
using Windows.Devices.Geolocation;
using Windows.Storage;

namespace TTC.W10.POC.VwMdl
{
    public partial class Stngs
    {
        ApplicationDataContainer _lclStngs;
        public Stngs()
        {
            _lclStngs = Windows.Storage.ApplicationData.Current.LocalSettings;  // Windows.Storage.StorageFolder localFolder = Windows.Storage.ApplicationData.Current.LocalFolder;      // Store and retrieve settings and other app data        https://msdn.microsoft.com/en-us/library/windows/apps/mt299098.aspx
        }

        public double MapZoom { get { return _lclStngs.Values["MapZoom"] as double? ?? 12.0; } set { if ((_lclStngs.Values["MapZoom"] as double?) != value) _lclStngs.Values["MapZoom"] = value; } }

        public double MapCenterLat { get { return _lclStngs.Values.ContainsKey("MapCenterLat") ? (double)_lclStngs.Values["MapCenterLat"] : +36.6; } set { if ((_lclStngs.Values["MapCenterLat"] as double?) != value) _lclStngs.Values["MapCenterLat"] = value; } }
        public double MapCenterLon { get { return _lclStngs.Values.ContainsKey("MapCenterLon") ? (double)_lclStngs.Values["MapCenterLon"] : -99.6; } set { if ((_lclStngs.Values["MapCenterLon"] as double?) != value) _lclStngs.Values["MapCenterLon"] = value; } }
        public Geopoint MapCenterLocation { get { return new Geopoint(new BasicGeoposition() { Latitude = MapCenterLat, Longitude = MapCenterLon }); } }

        public double LatestLocnLat { get { return _lclStngs.Values["CurLocnLat"] as double? ?? +36.6; } set { if ((_lclStngs.Values["CurLocnLat"] as double?) != value) _lclStngs.Values["CurLocnLat"] = value; } }
        public double LatestLocnLon { get { return _lclStngs.Values["CurLocnLon"] as double? ?? -99.6; } set { if ((_lclStngs.Values["CurLocnLon"] as double?) != value) _lclStngs.Values["CurLocnLon"] = value; } }
        public BasicGeoposition LatestLocation { get { return new BasicGeoposition() { Latitude = LatestLocnLat, Longitude = LatestLocnLon }; } }

        public bodyAgency SelectAgency
        {
            get
            {
                if (_lclStngs.Values.ContainsKey("SelectAgency"))
                {
                    var s = _lclStngs.Values["SelectAgency"] as string;
                    var rv = // Serializer.LoadFromString<bodyAgency>(s) as bodyAgency;          
                      (bodyAgency)new XmlSerializer(typeof(bodyAgency)).Deserialize(new StringReader(s));
                    return rv;
                }
                else
                    return new bodyAgency();
            }
            set
            {
                var s = SaveToString(value);
                if ((_lclStngs.Values["SelectAgency"] as string) != s)
                    _lclStngs.Values["SelectAgency"] = s;
            }
        }
        public FavVehStop LastVStop
        {
            get
            {
                if (_lclStngs.Values.ContainsKey("LastVStop"))
                {
                    var s = _lclStngs.Values["LastVStop"] as string;
                    var rv = // Serializer.LoadFromString<FavVehStop>(s) as FavVehStop;          
                      (FavVehStop)new XmlSerializer(typeof(FavVehStop)).Deserialize(new StringReader(s));
                    return rv;
                }
                else
                    return new FavVehStop();
            }
            set
            {
                var s = SaveToString(value);
                if ((_lclStngs.Values["LastVStop"] as string) != s)
                    _lclStngs.Values["LastVStop"] = s;
            }
        }

        public Collection<FavVehStop> LastVStops
        {
            get
            {
                if (_lclStngs.Values.ContainsKey("LastVStops"))
                {
                    var s = _lclStngs.Values["LastVStops"] as string;
                    var rv = (Collection<FavVehStop>)new XmlSerializer(typeof(Collection<FavVehStop>)).Deserialize(new StringReader(s)); // Serializer.LoadFromString<Collection<FavVehStop>>(s) as Collection<FavVehStop>;
                    return rv;
                }
                else
                    return new Collection<FavVehStop>();
            }
            set
            {
                var s = SaveToString(value);
                if ((_lclStngs.Values["LastVStops"] as string) != s)
                    _lclStngs.Values["LastVStops"] = s;
            }
        }
        public Collection<FavVehStop> FavVSLst
        {
            get
            {
                if (_lclStngs.Values.ContainsKey(cFavVSLst))
                {
                    var s = _lclStngs.Values[cFavVSLst] as string;
#if SerializationCodeIsMissingForType_RESOLVED
                    try { return JsonStringSerializer.Load<Collection<FavVehStop>>(s); } catch (Exception ex) { ex.Pop("Ignore: Hasnot switched to JSON yet."); }
#endif
                    try { return (Collection<FavVehStop>)new XmlSerializer(typeof(Collection<FavVehStop>)).Deserialize(new StringReader(s)); } catch (Exception ex) { ex.Pop(); }                    //todo: remove this legacy one-time support after a publish or two to the store (Apr 2018)
                }

                return new Collection<FavVehStop>();
            }
            set
            {
                string s = "{Uh-oh...Failed}";
                try
                {
#if SerializationCodeIsMissingForType_RESOLVED
                    s = JsonStringSerializer.Save<Collection<FavVehStop>>(value); // causes SerializationCodeIsMissingForType exception in release mode only! 
#else
                    s = SaveToString(value);
#endif
                    if ((_lclStngs.Values[cFavVSLst] as string) != s)
                        _lclStngs.Values[cFavVSLst] = s;
                }
                catch (Exception ex) { ex.Pop($"{value.Count} stops -> {s.Length} bytes => DELETE SOME!!!"); throw; } // FavVSLst(10 stops -> 4154 / 2745  bytes => DELETE SOME!!!):  ==> max 4k!
            }
        }

        public string Srch { get; internal set; }
        public bool IsVrbl { get { return _lclStngs.Values["IsVrbl"] as bool? ?? true; } set { if ((_lclStngs.Values["IsVrbl"] as bool?) != value) _lclStngs.Values["IsVrbl"] = value; } }

        public bool MvNone { get { return _lclStngs.Values["MvNone"] as bool? ?? false; } set { if ((_lclStngs.Values["MvNone"] as bool?) != value) _lclStngs.Values["MvNone"] = value; } }
        public bool MvRoad { get { return _lclStngs.Values["MvRoad"] as bool? ?? true; } set { if ((_lclStngs.Values["MvRoad"] as bool?) != value) _lclStngs.Values["MvRoad"] = value; } }
        public bool MvAeri { get { return _lclStngs.Values["MvAeri"] as bool? ?? false; } set { if ((_lclStngs.Values["MvAeri"] as bool?) != value) _lclStngs.Values["MvAeri"] = value; } }
        public bool MvAer3 { get { return _lclStngs.Values["MvAer3"] as bool? ?? false; } set { if ((_lclStngs.Values["MvAer3"] as bool?) != value) _lclStngs.Values["MvAer3"] = value; } }
        public bool MvAeRd { get { return _lclStngs.Values["MvAeRd"] as bool? ?? false; } set { if ((_lclStngs.Values["MvAeRd"] as bool?) != value) _lclStngs.Values["MvAeRd"] = value; } }
        public bool MvAeR3 { get { return _lclStngs.Values["MvAeR3"] as bool? ?? false; } set { if ((_lclStngs.Values["MvAeR3"] as bool?) != value) _lclStngs.Values["MvAeR3"] = value; } }
        public bool MvTern { get { return _lclStngs.Values["MvTern"] as bool? ?? false; } set { if ((_lclStngs.Values["MvTern"] as bool?) != value) _lclStngs.Values["MvTern"] = value; } }
        public bool MvTrfc { get { return _lclStngs.Values["MvTrfc"] as bool? ?? true; } set { if ((_lclStngs.Values["MvTrfc"] as bool?) != value) _lclStngs.Values["MvTrfc"] = value; } }
        public bool Is1stTm { get { return _lclStngs.Values["Is1stTm"] as bool? ?? true; } set { if ((_lclStngs.Values["Is1stTm"] as bool?) != value) _lclStngs.Values["Is1stTm"] = value; } }

        public bool DevOps { get; internal set; }
        public bool IsZ2P { get; internal set; }
        public bool IsOn02 { get; internal set; }

        public static string SaveToString(object o)
        {
            var sb = new StringBuilder();
            using (var sw = new StringWriter(sb))
            {
                new XmlSerializer(o.GetType()).Serialize(sw, o);
            }
            return sb.ToString();
        }

        const string cFavVSLst = "FavVSLst";
    }
}
