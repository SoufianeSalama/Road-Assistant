using Microsoft.WindowsAzure.MobileServices;
using Road_Assistant.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Devices.Geolocation.Geofencing;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.Networking.Connectivity;
using Windows.Services.Maps;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Globalization;
using Windows.Storage;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkID=390556

namespace Road_Assistant
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Startpagina : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private MobileServiceCollection<Places, Places> items;
        private IMobileServiceTable<Places> placesTable = App.MobileService.GetTable<Places>();

        private Geolocator locator = new Geolocator();
        private Geoposition geoposition;
        
        private BasicGeoposition basicGeoposition;

        private MessageDialog dialog;

        private ResourceLoader loader = new ResourceLoader();

        private NumberFormatInfo nfi = new NumberFormatInfo();
        

        public Startpagina()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            nfi.NumberDecimalSeparator = ".";
            nfi.CurrencyDecimalDigits = 4;

            
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Gets the view model for this <see cref="Page"/>.
        /// This can be changed to a strongly typed view model.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation.  Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session.  The state will be null the first time a page is visited.</param>
        private async  void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            //  Kijken of de Locatie instelling aan staat (en internet)
            StartControleAsync();

            // de data uit Azure halen en een lijst van objecten van de klasse Pushpin maken van de gekregen data
            await GetLocatiesAsync();

            // de positionChanged event parameters instellen 
            LocatieConfiguratie();

            // de geofence parameters isntellen
            GeofenceConfiguratie();

            await RegisterTask();
        }



        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/></param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
        }

        #region NavigationHelper registration

        /// <summary>
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion


        private async Task RegisterTask()
        {
            //string taskName = "RoadAssistantGeofence";
            //bool isTaskRegisterd = BackgroundTaskRegistration.AllTasks.Any(x => x.Value.Name == taskName);
            //if (!isTaskRegisterd)
            //{
            //    BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            //    builder.Name = taskName;
            //    builder.TaskEntryPoint = "BTask.BackgroundTask";
            //    builder.SetTrigger(new LocationTrigger(LocationTriggerType.Geofence));

            //    builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
            //    BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
            //    if (status != BackgroundAccessStatus.Denied)
            //    {
            //        BackgroundTaskRegistration task = builder.Register();
            //    }
            //}

            string taskName = "Test task";
            bool isTaskRegisterd = BackgroundTaskRegistration.AllTasks.Any(x => x.Value.Name == taskName);
            if (!isTaskRegisterd)
            {
                BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                builder.Name = taskName;
                builder.TaskEntryPoint = "BTask.BackgroundTask";
                builder.SetTrigger(new LocationTrigger(LocationTriggerType.Geofence));
                BackgroundAccessStatus status = await BackgroundExecutionManager.RequestAccessAsync();
                if (status != BackgroundAccessStatus.Denied)
                {
                    BackgroundTaskRegistration task = builder.Register();
                }
            }

        }
        private async void StartControleAsync()
        {
            //http://aboutwindowsphoneandwindowsstore.blogspot.be/2014/10/how-to-show-message-dialog-box-in.html

            ConnectionProfile profile = NetworkInformation.GetInternetConnectionProfile();
            NetworkConnectivityLevel connectivityLevel = profile.GetNetworkConnectivityLevel();

            string TitleErrorLocation = loader.GetString("TitleErrorLocation/Text");
            string ContentErrorLocation = loader.GetString("ContentErrorLocation/Text");

            string buttonSettingsText = loader.GetString("btnSettings/Text");
            string buttonCancelText = loader.GetString("btnCancel/Text");



            //
            if ((locator.LocationStatus == PositionStatus.Disabled) || (connectivityLevel != NetworkConnectivityLevel.InternetAccess))      // Controleren of locatie en internet aanstaat op het toestel
            {
                dialog = new MessageDialog(ContentErrorLocation);
                dialog.Title = TitleErrorLocation;
                dialog.Commands.Add(new UICommand(buttonSettingsText));
                dialog.Commands.Add(new UICommand(buttonCancelText));

                var resultaat = await dialog.ShowAsync();

                if (resultaat.Label == buttonCancelText)
                {
                    Application.Current.Exit();
                }
                if (resultaat.Label == buttonSettingsText)
                {
                    //await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));    //Ga naar de locatie instellingen van de Phone
                    //await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:network-cellular"));    //Ga naar de Mobiele internet instellingen van de Phone

                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:"));

                    Application.Current.Exit();
                }

        }
            else
            {
                GeofenceMonitor.Current.GeofenceStateChanged += Current_GeofenceStateChanged;
            }


}

        private void LocatieConfiguratie()
        {

            locator.MovementThreshold = 20;
            locator.ReportInterval = 1000;
            locator.DesiredAccuracy = PositionAccuracy.High;
            locator.PositionChanged += locator_PositionChanged;
        }

        private void GeofenceConfiguratie()
        {
            

            GeofenceMonitor.Current.Geofences.Clear();
            basicGeoposition = new BasicGeoposition();
            
            double radius = 200;
            
            for (int i = 0; i< items.Count; i++)
            {
                basicGeoposition.Latitude = Convert.ToDouble(items[i].Latitude, nfi);
                basicGeoposition.Longitude = Convert.ToDouble(items[i].Longitude, nfi);
                
                Geocircle geocircle = new Geocircle(basicGeoposition, radius);

                MonitoredGeofenceStates mask = 0;
                mask |= MonitoredGeofenceStates.Entered;

                TimeSpan span = TimeSpan.FromSeconds(2);
                Geofence geofence = new Geofence(i.ToString(), geocircle, mask, false, span);
                GeofenceMonitor.Current.Geofences.Add(geofence);
            }

           
        }


        
        private async void Current_GeofenceStateChanged(GeofenceMonitor sender, object args)
        {
            var reports = sender.ReadReports();
            foreach (GeofenceStateChangeReport report in reports)
            {
                GeofenceState state = report.NewState;
                switch (state)
                {
                    case GeofenceState.Entered:
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                        {
                            string TitleGeofence = loader.GetString("TitleGeofence/Text");
                            string ContentGeofence = loader.GetString("ContentGeofence/Text");

                            dialog = new MessageDialog(ContentGeofence);
                            dialog.Title = TitleGeofence;
                            Melding.Volume = 1;     // een schaal tussen 0 en 1 (en 0.5 is default)
                            //Melding.AutoPlay = true;
                            Melding.Play();

                            await dialog.ShowAsync();
                        });
                        break;
        
                }
            }
        }
    

        private async void locator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {


                Geoposition currentPosition = args.Position;

                Toonlocatie(currentPosition);


            });
        }
            
        private async void WaarBenIkAppBarButton_Click(object sender, RoutedEventArgs e)
        {

            string title = loader.GetString("TitleWhereAmI/Text");
            string content = loader.GetString("ContentWhereAmI/Text");

            geoposition = await locator.GetGeopositionAsync();
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(geoposition.Coordinate.Point);

            if (result.Status == MapLocationFinderStatus.Success)
            {
                MapAddress address = result.Locations.FirstOrDefault().Address;
                string fullAddress = string.Format(address.Street + ", " + address.Town);

                dialog = new MessageDialog(content + fullAddress);
                dialog.Title = title;

                await dialog.ShowAsync();
            }


        }

        private async void Toonlocatie(Geoposition geoposition) 
        {
            MyMap.MapElements.Clear();

            await MyMap.TrySetViewAsync(geoposition.Coordinate.Point, 17);      // -> gaat inzoomen op uw huidige locatie

            string TitlePosition = loader.GetString("TitlePosition/Text");
            
            MapIcon icon = new MapIcon();
            icon.Location = geoposition.Coordinate.Point;

            icon.Title = TitlePosition; // "Uw Positie";

            //icon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("msappx:///Assets/logo.jpg"));

            MyMap.MapElements.Add(icon);        //-> een nieuw icoon plaatsen op huidige locatie


        }

        private void NavigeerLocaties_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(Locatielijst), items))
            {
                throw new Exception("Failed to create initial page");
            }
        }


        private void ZetLocatiesOpMap()
        {
            BasicGeoposition position = new BasicGeoposition();
            List<PushPin> pushpins = new List<PushPin>();

            for (int i = 0; i < items.Count; i++)
            {
                position.Latitude = Convert.ToDouble(items[i].Latitude, nfi);
                position.Longitude = Convert.ToDouble(items[i].Longitude, nfi);

                Geopoint geopoint = new Geopoint(position);

                PushPin pushpin = new PushPin();
                pushpin.Name = items[i].Soort;
                pushpin.Location = geopoint;


                pushpins.Add(pushpin);



            }
            Pushpins.ItemsSource = pushpins;
        }

        private async Task GetLocatiesAsync()
        {
            MobileServiceInvalidOperationException exception = null;
            try
            {
                // This code refreshes the entries in the list view by querying the TodoItems table.
                // The query excludes completed TodoItems.
                items = await placesTable
                    
                    .ToCollectionAsync();

                

            }
            catch (MobileServiceInvalidOperationException e)
            {
                exception = e;
            }

            if (exception != null)
            {
                string ContentErrorData = loader.GetString("ContentErrorData/Text");
                
                await new MessageDialog(exception.Message, ContentErrorData).ShowAsync();

            }
            else
            {

               ZetLocatiesOpMap();
            }
        }

        private async void OnPushpinClicked(object sender, TappedRoutedEventArgs e)
        {
            

            Image image = sender as Image;
            PushPin selectedPushpin = image.DataContext as PushPin;

            string ContentPushPin = loader.GetString("TitlePushPinSoort/Text");
            string titlePushpin = loader.GetString("TitleClickOnPushPin/Text");

            dialog = new MessageDialog(ContentPushPin + selectedPushpin.Name);
            dialog.Title = titlePushpin;
            await dialog.ShowAsync();


        }

        private void AboutAppBarMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(Over)))
            {
                throw new Exception("Failed to create initial page");
            }
        }
    }
}

