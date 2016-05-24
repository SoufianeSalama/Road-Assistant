using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation.Geofencing;
using Windows.UI.Notifications;

namespace BTask
{
    public sealed class BackgroundTask : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();
            
            var reports = GeofenceMonitor.Current.ReadReports();
            foreach (GeofenceStateChangeReport report in reports)
            {
                GeofenceState state = report.NewState;
                //string status = string.Empty;

                switch (state)
                {
                    case GeofenceState.Entered:
                        //status = "The user entered the area";
                        
                        break;
                    default:
                        break;

                }

                XmlDocument template = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                XmlNodeList texts = template.GetElementsByTagName("text");
                texts[0].AppendChild(template.CreateTextNode("Road-Assistant"));
                texts[1].AppendChild(template.CreateTextNode("Warning"));

                ToastNotification notification = new ToastNotification(template);
                ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
                notifier.Show(notification);



            }
            deferral.Complete();
        }
    }
}
