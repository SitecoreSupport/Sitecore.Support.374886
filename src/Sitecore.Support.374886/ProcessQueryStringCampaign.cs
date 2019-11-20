// Sitecore.Analytics.Pipelines.StartTracking.ProcessQueryStringCampaign
using Sitecore.Analytics;
using Sitecore.Analytics.Configuration;
using Sitecore.Analytics.Data.Items;
using Sitecore.Analytics.Pipelines.StartTracking;
using Sitecore.Data;
using Sitecore.Diagnostics;
using System;
using System.Web;

namespace Sitecore.Support.Analytics.Pipelines.StartTracking
{


    public class ProcessQueryStringCampaign : StartTrackingProcessor
    {
        public override void Process(StartTrackingArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Assert.IsNotNull(Tracker.Current, "Tracker.Current is not initialized");
            Assert.IsNotNull(Tracker.Current.Session, "Tracker.Current.Session is not initialized");
            Assert.IsNotNull(Tracker.Current.Session.Interaction, "Tracker.Current.Session.Interaction is not initialized");
            Assert.IsNotNull(Tracker.Current.Session.Interaction.CurrentPage, "Tracker.Current.Session.Interaction.CurrentPage is not initialized");
            Assert.IsNotNull(args.HttpContext, "The HttpContext is not set.");
            Assert.IsNotNull(args.HttpContext.Request, "The HttpRequest is not set.");
            TriggerCampaign(args.HttpContext.Request);
        }

        private void TriggerCampaign(HttpRequestBase request)
        {
            string campaignQueryStringKey = AnalyticsSettings.CampaignQueryStringKey;
            string queryStringValue = StartTrackingProcessor.GetQueryStringValue(request, campaignQueryStringKey);
            /* SITECORE SUPPORT FIX 
               Previously, just checked if queryStringValue != null, but if the value was an empty string, it would run and cause an error. 
             */
            if (!String.IsNullOrEmpty(queryStringValue))
            {
                queryStringValue = queryStringValue.Trim();
                TriggerCampaign(queryStringValue);
            }
            else
            {
                Log.Warn("Null or Empty Campaign Query String",this);
            }
        }

        private void TriggerCampaign(string campaign)
        {
            CampaignItem campaignItem;
            if (ShortID.IsShortID(campaign))
            {
                ID id = ShortID.DecodeID(campaign);
                campaignItem = Tracker.DefinitionItems.Campaigns[id];
            }
            else if (ID.IsID(campaign))
            {
                ID id2 = new ID(campaign);
                campaignItem = Tracker.DefinitionItems.Campaigns[id2];
            }
            else
            {
                campaignItem = Tracker.DefinitionItems.Campaigns[campaign];
            }
            if (campaignItem == null)
            {
                Log.Error("Campaign not found: " + campaign, typeof(ProcessQueryStringCampaign));
                return;
            }
            Guid? campaignId = Tracker.Current.Session.Interaction.CampaignId;
            int trafficType = Tracker.Current.Session.Interaction.TrafficType;
            Tracker.Current.CurrentPage.TriggerCampaign(campaignItem);
            Guid? currentCampaignId = Tracker.Current.Session.Interaction.CampaignId;
            int currentTraficType = Tracker.Current.Session.Interaction.TrafficType;
            if (campaignId != Tracker.Current.Session.Interaction.CampaignId)
            {
                TrackerEvents.OnCurrentPageCancelled += delegate
                {
                    Tracker.Current.Session.Interaction.CampaignId = currentCampaignId;
                };
            }
            if (trafficType != currentTraficType)
            {
                TrackerEvents.OnCurrentPageCancelled += delegate
                {
                    Tracker.Current.Session.Interaction.TrafficType = currentTraficType;
                };
            }
        }
    }

}