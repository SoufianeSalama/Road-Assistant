using Road_Assistant.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
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
       


        private Geolocator locator = new Geolocator();
        private Geoposition geoposition;

        private MessageDialog dialog;
        public Startpagina()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
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
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {

            StartConfiguratie();

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


        private async void StartConfiguratie()
        {
            //http://aboutwindowsphoneandwindowsstore.blogspot.be/2014/10/how-to-show-message-dialog-box-in.html



            if (locator.LocationStatus == PositionStatus.Disabled)
            {
                dialog = new MessageDialog("De locatie service is uitgeschakeld!");
                dialog.Commands.Add(new UICommand("Instellingen"));
                dialog.Commands.Add(new UICommand("Annuleer"));

                var resultaat = await dialog.ShowAsync();

                if (resultaat.Label == "Annuleer")
                {
                    Application.Current.Exit();
                }
                if (resultaat.Label == "Instellingen")
                {
                    await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));     //Ga naar de locatie instellingen van de Phone
                    Application.Current.Exit();
                }

            }
            else
            {
                geoposition = await locator.GetGeopositionAsync();

                Toonlocatie(geoposition);

                locator.MovementThreshold = 20;
                locator.ReportInterval = 1000;
                locator.DesiredAccuracy = PositionAccuracy.High;
                locator.PositionChanged += locator_PositionChanged;
            }


        }

        async void locator_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                Geoposition currentPosition = args.Position;

                Toonlocatie(currentPosition);


            });
        }


        private async void WaarBenIkAppBarButton_Click(object sender, RoutedEventArgs e)
        {
            geoposition = await locator.GetGeopositionAsync();
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(geoposition.Coordinate.Point);

            if (result.Status == MapLocationFinderStatus.Success)
            {
                MapAddress address = result.Locations.FirstOrDefault().Address;
                string fullAddress = string.Format("U bent in de buurt van: {0}, {1}.", address.Street, address.Town);

                dialog = new MessageDialog(fullAddress);
                await dialog.ShowAsync();
            }
        }

        private async void Toonlocatie(Geoposition geoposition)  //async
        {


            await MyMap.TrySetViewAsync(geoposition.Coordinate.Point, 15);      // -> gaat inzoomen op uw huidige locatie


            MapIcon icon = new MapIcon();
            icon.Location = geoposition.Coordinate.Point;
            icon.Title = "Uw Positie";

            //icon.Image = RandomAccessStreamReference.CreateFromUri(new Uri("msappx:///Assets/logo.jpg"));

            MyMap.MapElements.Add(icon);        //-> een nieuw icoon plaatsen op huidige locatie


        }

    }
}

