using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Geolocation;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Media.Capture;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ARTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SimpleAR : Page
    {
        private SimpleOrientationSensor orientationSensor;
        private Compass compass;
        private Geolocator gps;

        private uint movementThreshold = 100;
        private double defaultSearchRadius = 1;

        private string sessionKey;
        private double earthRadiusKM = 6378.135;
        private Geoposition currentLocation = null;
        private double currentHeading = 0;
        private List<Geopoint> poiLocations;
        //private MapLayer pinLayer;
        public SimpleAR()
        {
            this.InitializeComponent();
            poiLocations = new List<Geopoint>();
            //var accessStatus = await Geolocator.RequestAccessAsync();
            poiLocations.Add(new Windows.Devices.Geolocation.Geopoint(
                new Windows.Devices.Geolocation.BasicGeoposition()
                {
                    Latitude = 47.6166726675232592,
                    Longitude = -122.34717558119712
                }));
            poiLocations.Add(new Windows.Devices.Geolocation.Geopoint(
                new Windows.Devices.Geolocation.BasicGeoposition()
                {
                    Latitude = 47.616743,
                    Longitude = -122.347397
                }));
            poiLocations.Add(new Windows.Devices.Geolocation.Geopoint(
                new Windows.Devices.Geolocation.BasicGeoposition()
                {
                    Latitude = 47.616529,
                    Longitude = -122.347014
                }));
            poiLocations.Add(new Windows.Devices.Geolocation.Geopoint(
                new Windows.Devices.Geolocation.BasicGeoposition()
                {
                    Latitude = 47.616647,
                    Longitude = -122.347207
                }));

            compass = Compass.GetDefault();
                     
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var accessStatus = await Geolocator.RequestAccessAsync();
            if(accessStatus == GeolocationAccessStatus.Allowed)
            {
                Geolocator geolocator = new Geolocator { };
                currentLocation = await geolocator.GetGeopositionAsync();
            }
            MapView.Visibility = Visibility.Collapsed;
            MyMap.Visibility = Visibility.Collapsed;
            await StartCamera();
            UpdateARView();
        }
        private async Task StartCamera()
        {
            var source = new Windows.Media.Capture.MediaCapture();
            try
            {
                var cameraId = await FindRearFacingCamera();
                if (!String.IsNullOrEmpty(cameraId))
                {
                    var settings = new MediaCaptureInitializationSettings();
                    settings.VideoDeviceId = cameraId;
                    await source.InitializeAsync(settings);
                    PreviewScreen.Source = source;

                    // start video preview
                    await source.StartPreviewAsync();
                }
            }
            catch (Exception ex)
            {

            }
        }
        private async Task<string> FindRearFacingCamera()
        {
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            var device = (from d in devices
                          where d.EnclosureLocation != null && d.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front
                          select d).FirstOrDefault();

            if (device == null)
            {
                device = (from d in devices
                          where d.Name.ToLower().Contains("back") || d.Id.ToLower().Contains("back")
                          || d.Name.ToLower().Contains("rear") ||
                          d.Id.ToLower().Contains("rear")
                          select d).FirstOrDefault();
            }

            if (device != null)
            {
                return device.Id;
            }
            return null;
        }
        
        private async void StopCamera()
        {
            try
            {
                if (PreviewScreen.Source != null)
                {
                    await PreviewScreen.Source.StopPreviewAsync();
                    PreviewScreen.Source = null;
                }
            }
            catch { }
        }
        
        //private double getHeading()
        private void UpdateARView()
        {
            if (currentLocation != null)
            {
               // ItemCanvas.Children.Clear();
                if(poiLocations != null && poiLocations.Count > 0)
                {
                    foreach(var poi in poiLocations)
                    {
                        var dlat1 = (Math.PI / 180) * (currentLocation.Coordinate.Latitude);
                        var dlat2 = (Math.PI / 180) * (poi.Position.Latitude);
                        var dLon = (Math.PI / 180) * (poi.Position.Longitude - currentLocation.Coordinate.Latitude);
                        var y = (Math.Sin(dLon)) * Math.Cos(dlat2);
                        var x = Math.Cos(dlat1) * Math.Sin(dlat2) - Math.Sin(dlat1) * Math.Cos(dlat2) * Math.Cos(dLon);
                        var poiHeading = ((180 / Math.PI) * Math.Atan2(y, x) + 360) % 360;
                        var diff = currentHeading - poiHeading;
                        if (diff > 180)
                        {
                            diff = currentHeading - (poiHeading + 360);
                        }
                        else if (diff < -180)
                        {
                            diff = currentHeading + 360 - poiHeading;
                        }
                        if (Math.Abs(diff) <= 22.5)
                        {
                            var dlatdistance = (Math.PI / 180) * (poi.Position.Latitude - currentLocation.Coordinate.Latitude);
                            var dlondistance = (Math.PI / 180) * (poi.Position.Longitude - currentLocation.Coordinate.Longitude);
                            var a = Math.Sin(dlatdistance / 2) * Math.Sin(dlatdistance / 2) + Math.Cos((Math.PI / 180) * (currentLocation.Coordinate.Latitude)) * Math.Cos((Math.PI / 180) * (currentLocation.Coordinate.Latitude)) * Math.Sin(dlondistance / 2) * Math.Sin(dlondistance / 2);
                            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
                            var haversinedistance = earthRadiusKM * c;
                            double left = 0;
                            if (diff > 0)
                            {
                                left = ItemCanvas.ActualHeight /( 2 * ((22.5 - diff)));

                            }
                            else
                            {
                                left = ItemCanvas.ActualWidth / (2 * (1 + -diff / 22.5));
                            }
                            double top = ItemCanvas.ActualHeight * (1 - haversinedistance / defaultSearchRadius);
                            var tb = new TextBlock()
                            {
                                Text = "Here",
                                FontSize = 24,
                                
                                TextAlignment = Windows.UI.Xaml.TextAlignment.Center,
                                HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center
                            };
                            Canvas.SetLeft(tb, 20);
                            Canvas.SetTop(tb, 20);
                            ItemCanvas.Children.Add(tb);
                        }
                    }
                   // var center = new Coordinate();
                   //calculating heading 
                   //var dlat1 = (Math.PI/180)*
                }
            }
        } 
        
               
    }
}
