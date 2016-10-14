using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace YoutubeLive
{
    public class YoutubeApi
    {
        private YouTubeService service;

        public YoutubeApi()
        {
            service = Auth();

            var broadcastSnippet = new LiveBroadcastSnippet();
            broadcastSnippet.Title = "Biometrico";
            broadcastSnippet.ScheduledStartTime = DateTime.Now;

            var broadcastStatus = new LiveBroadcastStatus();
            broadcastStatus.PrivacyStatus = "unlisted";

            var broadcastMonitorStream = new MonitorStreamInfo();
            broadcastMonitorStream.EnableMonitorStream = false;

            var broadcastContentDetails = new LiveBroadcastContentDetails();
            broadcastContentDetails.MonitorStream = broadcastMonitorStream;

            var broadcast = new LiveBroadcast();
            broadcast.Kind = "youtube#liveBroadcast";
            broadcast.Snippet = broadcastSnippet;
            broadcast.Status = broadcastStatus;
            broadcast.ContentDetails = broadcastContentDetails;

            var liveBroadcastInsert = service.LiveBroadcasts.Insert(broadcast, "snippet,status,contentDetails");
            var returnedBroadcast = liveBroadcastInsert.Execute();

            Console.WriteLine("\n================== Returned Broadcast ==================\n");
            Console.WriteLine("  - Id: " + returnedBroadcast.Id);
            Console.WriteLine("  - Title: " + returnedBroadcast.Snippet.Title);
            Console.WriteLine("  - Description: " + returnedBroadcast.Snippet.Description);
            Console.WriteLine("  - Published At: " + returnedBroadcast.Snippet.PublishedAt);
            Console.WriteLine(
                    "  - Scheduled Start Time: " + returnedBroadcast.Snippet.ScheduledStartTime);
            Console.WriteLine(
                    "  - Scheduled End Time: " + returnedBroadcast.Snippet.ScheduledEndTime);

            var streamSnippet = new LiveStreamSnippet();
            streamSnippet.Title = "Camara de Establecimiento";

            var cdnSettings = new CdnSettings();
            cdnSettings.Format = "720p";
            cdnSettings.IngestionType = "rtmp";

            var stream = new LiveStream();
            stream.Kind = "youtube#liveStream";
            stream.Snippet = streamSnippet;
            stream.Cdn = cdnSettings;

            var liveStreamInsert = service.LiveStreams.Insert(stream, "snippet,cdn");
            var returnedStream = liveStreamInsert.Execute();

            Console.WriteLine("\n================== Returned Stream ==================\n");
            Console.WriteLine("  - Id: " + returnedStream.Id);
            Console.WriteLine("  - Title: " + returnedStream.Snippet.Title);
            Console.WriteLine("  - Description: " + returnedStream.Snippet.Description);
            Console.WriteLine("  - Published At: " + returnedStream.Snippet.PublishedAt);
            Console.WriteLine("  - URL: " + returnedStream.Cdn.IngestionInfo.IngestionAddress);
            Console.WriteLine("  - Name: " + returnedStream.Cdn.IngestionInfo.StreamName);

            var liveBroadcastBind = service.LiveBroadcasts.Bind(returnedBroadcast.Id, "id, contentDetails");
            liveBroadcastBind.StreamId = returnedStream.Id;
            returnedBroadcast = liveBroadcastBind.Execute();

            Console.WriteLine("\n================== Returned Bound Broadcast ==================\n");
            Console.WriteLine("  - Broadcast Id: " + returnedBroadcast.Id);
            Console.WriteLine("  - Bound Stream Id: " + returnedBroadcast.ContentDetails.BoundStreamId);

            var liveStreamRequest = service.LiveStreams.List("id,status");
            liveStreamRequest.Id = returnedStream.Id;

            string streamLoop = "0";
            while (!streamLoop.Contains("A"))
            {
                var returnedStreamListResponse = liveStreamRequest.Execute();
                var foundStream = returnedStreamListResponse.Items.Single();
                Console.WriteLine(foundStream.Status.StreamStatus);
                streamLoop = Console.ReadKey().Key.ToString();
            }

            service.LiveBroadcasts.Transition(LiveBroadcastsResource.TransitionRequest.BroadcastStatusEnum.Testing, returnedBroadcast.Id, "");
            var liveBroadcastRequest = service.LiveBroadcasts.List("id,status");
            liveBroadcastRequest.Id = returnedBroadcast.Id;

            char broadcastLoop = '0';
            while (broadcastLoop != 'A')
            {
                
                var returnedBroadcastListResponse = liveBroadcastRequest.Execute();
                var foundBroadcast = returnedBroadcastListResponse.Items.Single();
                Console.WriteLine(foundBroadcast.Status.LifeCycleStatus);
                broadcastLoop = Console.ReadKey().KeyChar;
            }

            service.LiveBroadcasts.Transition(LiveBroadcastsResource.TransitionRequest.BroadcastStatusEnum.Live, returnedBroadcast.Id, "");

            broadcastLoop = '0';
            while (broadcastLoop != ('A'))
            {

                var returnedBroadcastListResponse = liveBroadcastRequest.Execute();
                var foundBroadcast = returnedBroadcastListResponse.Items.Single();
                Console.WriteLine(foundBroadcast.Status.LifeCycleStatus);
                broadcastLoop = Console.ReadKey().KeyChar;
            }


            //rtsp://192.168.1.99/axis-media/media.amp
        }

        private YouTubeService Auth()
        {
            UserCredential creds;
            using (var stream = new FileStream(AppDomain.CurrentDomain.BaseDirectory + "client_secret.json", FileMode.Open, FileAccess.Read))
            {
                creds = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    new[] { YouTubeService.Scope.Youtube },
                    "user",
                    CancellationToken.None,
                    new FileDataStore("YoutubeTest")
                    ).Result;
            }

            var service = new YouTubeService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = creds,
                ApplicationName = "YoutubeTest"
            });

            return service;
        }
    }
}