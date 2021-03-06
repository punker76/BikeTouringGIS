﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Layers;
using Esri.ArcGISRuntime.Symbology;
using GPX;
using Squirrel;
using BicycleTripsPreparationApp;
using theRightDirection.Library.Logging;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using BikeTouringGISLibrary;
using BikeTouringGISLibrary.Model;

namespace BikeTouringGIS
{
    /// <summary>
    /// Interaction logic for MainScreen.xaml
    /// </summary>
    public partial class MainScreen : UserControl
    {
        private List<List<wptType>> _routeParts;
        private List<wptType> _routePoints;
        private string _lastUsedFolder;
        private GraphicsLayer _routelayer, _poiLayer;
        private GraphicsLayer _splitLayer;
        private List<Route> _routes;
        private DistanceAnalyzer _distanceAnalyzer;
        private string _originalFile;
        public MainScreen()
        {
            InitializeComponent();
            _routes = new List<Route>();
            var versionApp = typeof(App).Assembly.GetName().Version;
            version.Text = string.Format("version {0}.{1}.{2}", versionApp.Major, versionApp.Minor,versionApp.Build);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Filter = "GPX files (*.gpx)|*.gpx";
            _routelayer = Map.Layers["route"] as GraphicsLayer;
            _splitLayer = Map.Layers["split"] as GraphicsLayer;
            _poiLayer = Map.Layers["POIs"] as GraphicsLayer;
            openFileDialog.Multiselect = false;
            openFileDialog.InitialDirectory = DropBoxHelper.GetDropBoxFolder();
            if (openFileDialog.ShowDialog() == true)
            {
                _routelayer.Graphics.Clear();
                _splitLayer.Graphics.Clear();
                _poiLayer.Graphics.Clear();
                _originalFile = openFileDialog.FileName;
                _lastUsedFolder = Path.GetDirectoryName(_originalFile);
                var gpxFileInformation = new GpxFileReader().LoadFile(openFileDialog.FileName);
                foreach (var track in gpxFileInformation.Tracks)
                {
                    StringBuilder textBuilder = new StringBuilder();
                    textBuilder.AppendLine($"Track {track.Name} is defined as track and not as route");
                    textBuilder.AppendLine();
                    textBuilder.AppendLine("routes are used by navigation-devices");
                    textBuilder.AppendLine("tracks are to register where you have been");
                    textBuilder.AppendLine();
                    textBuilder.AppendLine("Do you want to convert it to a route?");
                    var convertTrack = await ConvertTrackToRoute(textBuilder.ToString());
                    if(convertTrack)
                    {
                        track.ConvertTrackToRoute();
                    }
                }
                gpxFileInformation.CreateGeometries();
                ShowData(gpxFileInformation.AllRoutes,gpxFileInformation.GetExtent());
            }
        }

        private void ShowData(List<IRoute> routes, Envelope extent)
        {
            MyMapView.SetView(extent.Expand(1.2));
            var startSymbol = grid.Resources["StartSymbol"] as SimpleMarkerSymbol;
            var endSymbol = grid.Resources["EndSymbol"] as SimpleMarkerSymbol;
            var routeSymbol = grid.Resources["TotalRoute"] as SimpleLineSymbol;
            foreach (var route in routes)
            {
                _routelayer.Graphics.Add(route.RouteGeometry.AddSymbol(routeSymbol));
                _routelayer.Graphics.Add(route.StartLocation.AddSymbol(startSymbol));
                _routelayer.Graphics.Add(route.EndLocation.AddSymbol(endSymbol));
            }
        }

        private async Task<bool> ConvertTrackToRoute(string text)
        {
            var window = Application.Current.MainWindow as MetroWindow;
            var result = await window.ShowMessageAsync("Convert track to route", text, MessageDialogStyle.AffirmativeAndNegative);
            if (result == MessageDialogResult.Affirmative)
            {
                return true;
            }
            return false;
        }








        private void ShowWayPoints(List<wptType> waypoints)
        {
            var poiSymbol = this.Resources["POISymbol"] as SimpleMarkerSymbol;
            foreach (var point in waypoints)
            {
                var mapPoint = new Graphic(new MapPoint((double)point.lon, (double)point.lat, new SpatialReference(4326)), poiSymbol);
                _poiLayer.Graphics.Add(mapPoint);
            }
        }

        /*
        private void SetRoute(string routeName)
        {
            // bepaal afstand gehele route
            _distanceAnalyzer = new DistanceAnalyzer();
            length.Text = _distanceAnalyzer.CalculateDistance(_wayPoints[routeName]).ToString();
        }
        */

        private void DisplayPartOfTrack(List<wptType> wayPoints, Color color)
        {
            /*
            var lineSymbol = new SimpleLineSymbol();
            lineSymbol.Width = 4;
            lineSymbol.Color = color;
            var geometry = CreateGeometryFromWayPoints(wayPoints);
            var track = new Graphic(geometry, lineSymbol);
            _splitLayer.Graphics.Add(track);
            */
        }

        private void SaveGPSTracks_Click(object sender, RoutedEventArgs e)
        {
            var pois = GetPOIs();
            // TODO pois bufferen om route
                for (int i = 0; i < _routeParts.Count; i++)
                {
                    var filename = string.Format(@"{0}\{1}_{2}.gpx", _lastUsedFolder, prefix.Text, i + 1);
                    var gpxFile = new GPXFile();
                    var gpx = new gpxType();
                    var rte = new rteType();
                    rte.name = $"{i + 1}_{prefix.Text}";
                    rte.rtept = _routeParts[i].ToArray();
                    gpx.rte = new List<rteType>() { rte }.ToArray();
                    gpx.wpt = pois;
                    gpxFile.Save(filename, gpx);
                }
            SaveAllSplitPointsAndLinesInOneFile();
        }

        private wptType[] GetPOIs()
        {
            var pois = _poiLayer.Graphics;
            var result = new List<wptType>();
            foreach(var poi in pois)
            {
                var wpt = new wptType();
                var point = poi.Geometry as MapPoint;
                wpt.lon = (decimal)point.X;
                wpt.lat = (decimal)point.Y;
                wpt.name = poi.Attributes["title"] as string;
                wpt.desc = poi.Attributes["title"] as string;
                result.Add(wpt);
            }
            return result.ToArray();
        }

        private void SaveAllSplitPointsAndLinesInOneFile()
        {
            var filename = string.Format(@"{0}\{2}_splitlines_and_points_{1}_km.gpx", _lastUsedFolder, distanceTxt.Text, prefix.Text);
            var gpxFile = new GPXFile();
            var gpx = new gpxType();
            gpx.wpt = _routePoints.ToArray();
            var routes = new List<rteType>();
            for (int i = 0; i < _routeParts.Count; i++)
            {
                var rte = new rteType();
                rte.name = string.Format("{0}_{1}", prefix.Text, i + 1);
                rte.rtept = _routeParts[i].ToArray();
                routes.Add(rte);
            }
            gpx.rte = routes.ToArray();
            gpxFile.Save(filename, gpx);
        }

        private void CreateSplitPoints_Click(object sender, RoutedEventArgs e)
        {
            if (_routelayer.Graphics.Count == 3)
            {
                _routelayer.Graphics.RemoveAt(0);
            }
            _splitLayer.Graphics.Clear();
            _routePoints = new List<wptType>();
            //// splits de route op naar stukken van x km
            //var routeSplitter = new RouteSplitter(_wayPoints);
            //routeSplitter.SplitRoute(int.Parse(distanceTxt.Text));
            //// collectie van routes en verschillende kleuren
            //var x = routeSplitter.SplittedRoutes;
            //_routeParts = x;
            //var items = ColorUtils.RgbLinearInterpolate(Colors.LightBlue, Colors.DarkBlue, x.Count, Colors.LightBlue, Colors.DarkBlue);
            //// doorloop alle routes, teken iedere route op de kaart zet voor ieder beginpunt een stuk op de kaart plus afstand tot dit punt
            //int distance = 0;
            //for (int j = 0; j < x.Count; j++)
            //{
            //    DisplayPartOfTrack(x[j], items[j]);
            //    if (j > 0)
            //    {
            //        var point = x[j][0];
            //        var g = new Graphic(new MapPoint((double)point.lon, (double)point.lat, new SpatialReference(4326)), grid.Resources["RouteSymbol"] as SimpleMarkerSymbol);
            //        g.Attributes["index"] = distance;
            //        point.name = distance.ToString();
            //        _routePoints.Add(point);
            //        _splitLayer.Graphics.Add(g);
            //    }
            //    distance += _distanceAnalyzer.CalculateDistance(x[j]);
            //}

        }

        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (Map.Layers[0].IsVisible)
            {
                Map.Layers[0].IsVisible = false;
                Map.Layers[1].IsVisible = true;
                return;
            }
            if (Map.Layers[1].IsVisible)
            {
                Map.Layers[0].IsVisible = true;
                Map.Layers[1].IsVisible = false;
                return;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            Map.Layers[2].IsVisible = !Map.Layers[2].IsVisible;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
//            _wayPoints.Reverse();
 //           SetRoute();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var pois = GetPOIs();
            var filename = _originalFile;
            var gpxFile = new GPXFile();
            var gpx = new gpxType();
            var rte = new rteType();
//            rte.rtept = _wayPoints.ToArray();
            gpx.rte = new List<rteType>() { rte }.ToArray();
            gpx.wpt = pois;
            gpxFile.Save(filename, gpx);
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/MannusEtten/BikeTouringGIS"))
                {
                        var updateInfo = await mgr.CheckForUpdate();
                        var currentVersion = updateInfo.CurrentlyInstalledVersion.Version;
                        var futureVersion = updateInfo.FutureReleaseEntry.Version;
                        if (currentVersion != futureVersion)
                        {
                            var window = Application.Current.MainWindow as MetroWindow;
                            var controller = await window.ShowProgressAsync("Please wait...", "Updating application");
                            await mgr.UpdateApp();
                            await controller.CloseAsync();
                        }
                }
            }
            catch(Exception ex)
            {
                ILogger logger = Logger.GetLogger();
                logger.LogException(ex);
            }
        }
    }
}
