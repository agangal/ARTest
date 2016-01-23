using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Capture;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Storage;
using Windows.ApplicationModel;
using Windows.Graphics.Display;
using Windows.System.Display;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Sensors;
// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace ARTest
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class VideoView : Page
    {
        private readonly DisplayInformation _displayInformation = DisplayInformation.GetForCurrentView();
        private readonly DisplayRequest _displayRequest = new DisplayRequest();
        private readonly SimpleOrientationSensor _orientationSensor = SimpleOrientationSensor.GetDefault();
        private SimpleOrientation _deviceOrientation = SimpleOrientation.NotRotated;
        private DisplayOrientations _displayOrientation = DisplayOrientations.Portrait;
        // MediaCapture and it's state variables
        private MediaCapture _mediaCapture;
        private bool _isInitialized;
        private bool _isPreviewing;
        private bool _isRecording;

        // Information about the camera device
        private bool _mirroringPreview;
        private bool _externalCamera;
        public VideoView()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Disabled;
            Application.Current.Suspending += Application_Suspending;
            Application.Current.Resuming += Application_Resuming;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            /*CameraCaptureUI captureUI = new CameraCaptureUI();
            captureUI.VideoSettings.Format = CameraCaptureUIVideoFormat.Mp4;
            */
            myMap.Center = new Windows.Devices.Geolocation.Geopoint(
                new Windows.Devices.Geolocation.BasicGeoposition()
                {
                    Latitude = 47.59322,
                    Longitude = -122.33
                });
            myMap.ZoomLevel = 12;
            await InitializeCameraAsync();
        }
        private async void Application_Suspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            // cleanupCamera

            deferral.Complete();
        }

        private async void Application_Resuming(object sender, object o)
        {
            if(Frame.CurrentSourcePageType == typeof(VideoView))
            {
                await InitializeCameraAsync();
            }
        }

        private async Task InitializeCameraAsync()
        {
            Debug.WriteLine("InitializeCameraAsync");
            if(_mediaCapture == null)
            {
                var cameraDevice = await FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel.Back);
                if (cameraDevice == null)
                {
                    Debug.WriteLine("No camera device found!");
                    return;
                }

                _mediaCapture = new MediaCapture();
                var settings = new MediaCaptureInitializationSettings { VideoDeviceId = cameraDevice.Id };

                try
                {
                    await _mediaCapture.InitializeAsync(settings);
                    _isInitialized = true;
                }
                catch (UnauthorizedAccessException)
                {
                    Debug.WriteLine("The app was denied access to the camera");
                }

                if (_isInitialized)
                {
                    if(cameraDevice.EnclosureLocation == null || cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Unknown)
                    {
                        _externalCamera = true;
                    }
                    else
                    {
                        _externalCamera = false;
                        _mirroringPreview = (cameraDevice.EnclosureLocation.Panel == Windows.Devices.Enumeration.Panel.Front);
                    }
                    await StartPreviewAsync();
                }
            }
        }

        private static async Task<DeviceInformation> FindCameraDeviceByPanelAsync(Windows.Devices.Enumeration.Panel desiredPanel)
        {
            var allVideoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);
            DeviceInformation desiredDevice = allVideoDevices.FirstOrDefault(x => x.EnclosureLocation != null && x.EnclosureLocation.Panel == desiredPanel);
            return desiredDevice ?? allVideoDevices.FirstOrDefault();
        }

        private async Task StartPreviewAsync()
        {
            _displayRequest.RequestActive();
            PreviewControl.Source = _mediaCapture;
            PreviewControl.FlowDirection = _mirroringPreview ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
            await _mediaCapture.StartPreviewAsync();
            _isPreviewing = true;
        }
    }
}
