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

namespace ArchBench.PlugIns.HTML
{
    public class HTML : IArchBenchHttpPlugIn
    {
        public string Name => "PlugIn Html";
        public string Description => "Html";
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
            mManager.LoadResources("/test", Assembly.GetExecutingAssembly(), Assembly.GetExecutingAssembly().GetName().Name);
        }

        public bool Process(IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
        {
            if (aRequest.Uri.AbsolutePath.StartsWith("/Html", StringComparison.InvariantCultureIgnoreCase))
            {
                var html = Properties.Resources.test;
                var stream = new StreamWriter(aResponse.Body);
                stream.Write(html);
                stream.Flush();
               

                return true;
            }

            return false;
        }
    }
}
