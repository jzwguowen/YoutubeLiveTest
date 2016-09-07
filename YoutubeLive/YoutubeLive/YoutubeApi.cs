using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using System;
using System.IO;
using System.Threading;

namespace YoutubeLive
{
    public class YoutubeApi
    {
        private YouTubeService service;
        public YoutubeApi()
        {
            service = Auth();
            
            /* privacy = "private"
             *           "public"
            */
            var broadcast = ReturnBroadcast("prueba", "private");

            /* encode = "rtmp"
             *          "dash" 
             * format = "240p" 
             *          "1440p" 
            */
            var livestream = ReturnLivestream("live123", "rtmp", "240p", "", "");

            var bind = Bind(broadcast.Id, livestream.Id);
        }

        private LiveBroadcast ReturnBroadcast(string title, string privacy)
        {
            var snippet = new LiveBroadcastSnippet();
            snippet.Title = title;
            snippet.ScheduledStartTime = DateTime.Now;
            //snippet.ScheduledEndTime = DateTime.Now.AddDays(1);

            // Crear el template del evento del livestream
            var status = new LiveBroadcastStatus();
            status.PrivacyStatus = privacy;
            var broadcast = new LiveBroadcast();
            broadcast.Kind = "youtube#liveBroadcast";
            broadcast.Snippet = snippet;
            broadcast.Status = status;

            var insert = new LiveBroadcastsResource.InsertRequest(service, broadcast, "snippet, status");
            return insert.Execute();
        }

        private LiveStream ReturnLivestream(string title, string encode, string format, string url, string backup)
        {
            // Configuracion del livestream
            var snippet = new LiveStreamSnippet();
            snippet.Title = title;

            var ingestion = new IngestionInfo();
            //ingestion.StreamName = ""             // Nombre del stream
            //ingestion.IngestionAddress = url;       // Url del stream
            //ingestion.BackupIngestionAddress = backup; // Opcional

            var settings = new CdnSettings();
            settings.Format = format;
            settings.IngestionType = encode; // rtmp o dash
            settings.IngestionInfo = ingestion;

            var stream = new LiveStream();
            stream.Kind = "youtube#liveStream";
            stream.Snippet = snippet;
            stream.Cdn = settings;

            var insert = new LiveStreamsResource.InsertRequest(service, stream, "snippet, cdn");
            return insert.Execute();
        }

        private LiveBroadcast Bind(string broadcastId, string livestreamId)
        {
            var bind = new LiveBroadcastsResource.BindRequest(service, broadcastId, "id, contentDetails");
            bind.StreamId = livestreamId;
            return bind.Execute();
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