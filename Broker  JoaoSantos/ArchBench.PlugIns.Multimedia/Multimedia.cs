using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using HttpServer;
using HttpServer.Exceptions;
using HttpServer.Helpers;
using HttpServer.Sessions;

namespace ArchBench.PlugIns.Multimedia
{
    public class Multimedia : IArchBenchHttpPlugIn
    {
        public string Name => "PlugIn Multimedia";
        public string Description => "Imagens e video";
        public string Author => "João Santos";
        public string Version => "1.0";

        public bool Enabled { get; set; } = false;
        public IArchBenchPlugInHost Host { get; set; }
        public IArchBenchSettings Settings { get; } = new ArchBenchSettings();

        private readonly ResourceManager mManager = new ResourceManager();
        public void Dispose()
        {
        }

        public void Initialize()
        {
            mManager.LoadResources("/panda", Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly().GetName().Name);
        }

        public bool Process(
            IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
        {
            if (aRequest.Uri.AbsolutePath.StartsWith("/Image", StringComparison.InvariantCultureIgnoreCase))
            {
                var panda = Properties.Resources.Panda;
                aResponse.ContentType = "image/jpg";
                aResponse.Body.Write(panda, 0, panda.Length);
                aResponse.Send();

                return true;
            }

            if (aRequest.Uri.AbsolutePath.StartsWith("/Video", StringComparison.InvariantCultureIgnoreCase))
            {
                var video = Properties.Resources.mov;
                aResponse.ContentType = "video/mp4";
                aResponse.Body.Write(video, 0, video.Length);
                aResponse.Send();

                return true;
            }


            return false;
        }
    }
}
