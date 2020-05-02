
using HttpServer;
using ArchBench.PlugIns;
using HttpServer.Sessions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ArchBench.PlugIns.Broker
{
    public class Broker : IArchBenchHttpPlugIn
    {
        public string Name => "PlugIn Broker";
        public string Description => "Broker";
        public string Author => "João Santos";
        public string Version => "1.0";

        public bool Enabled { get; set; } = false;
        public IArchBenchPlugInHost Host { get; set; }
        public IArchBenchSettings Settings { get; } = new ArchBenchSettings();

        public TcpListener Listener { get; private set; }

        //lista de servers registados  key - > <ip>       Value -> <port>
        public List<KeyValuePair<string, int>> Servers { get; } = new List<KeyValuePair<string, int>>();
        public List<KeyValuePair<string, int>> Sessions = new List<KeyValuePair<string, int>>();
        public Thread Thread { get; private set; }

        //Next Server
        private int NServer { get; set; }
        public void Dispose()
        {

        }

        public void Initialize()
        {
            Listener = new TcpListener(IPAddress.Any, 9000);
            Thread = new Thread(ReceiveThreadFunction) { IsBackground = true };
            
            Thread.Start();
        }


        private void ReceiveThreadFunction()
        {
            try
            {
                
                Listener.Start();

                // Usar buffer para ler dados
                byte[] bytes = new byte[256];

                while (true)
                {
                    //Pedido de coneção aceite do server
                    var client = Listener.AcceptTcpClient();

                    //Cria uam stream para ler os dados recebidos
                    var stream = client.GetStream();

                    //Le os dados e guarda na variavel count o numero de bytes lidos
                    int count = stream.Read(bytes, 0, bytes.Length);

                    //Se o que foi lido nao for vazio
                    if( count !=0)
                    {
                        //Passa os bytes para uma string
                        string data = Encoding.ASCII.GetString(bytes, 0, count);

                        //Separa os dados recebiods pelos :
                        var parts = data.Split(':');

                        // <ip> e a <port>
                        switch (parts[0])
                        {
                            case "+":
                               
                                RegistServer(parts[1], int.Parse(parts[2]));
                                break;
                            case "-":
                                
                                UnRegistServer(parts[1], int.Parse(parts[2]));
                                break;
                        }
                    }

                     client.Close();
                }

            }
            catch ( SocketException e)
            {
                Host.Logger.WriteLine($"SocketExecption: {e}");
            }
            finally
            {
                Listener.Stop();
            }
        }





        public bool Process(IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
        {

            if(aRequest.Uri.AbsolutePath.Equals("/favicon.ico", StringComparison.InvariantCultureIgnoreCase))
            {
                var write = new StreamWriter(aResponse.Body);
                var faviconSolution = "< link rel = \"icon\" href = \"data:;base64,=\" >";
                write.WriteLine(faviconSolution);
                write.Flush();
                write.Close();
                return true;

            }

            // Index do proximo servidor
            var index = GetServer(aSession.Id);
            if (index == -69)
            {
                Host.Logger.WriteLine("No Servers Available!");
                return false;
            }

            string sourceHost = $"{aRequest.Uri.Host}:{aRequest.Uri.Port}";
            string sourcePath = aRequest.UriPath;

            string serverUrl = $"http://{Servers[index].Key}:{Servers[index].Value}{aRequest.UriPath}";
            Host.Logger.WriteLine($"Sending request from server {sourceHost} to server {serverUrl}");
            Uri uri = new Uri(serverUrl);
            WebClient client = new WebClient();
            try
            {
                byte[] bytes = null;

                if(aRequest.Headers["Cookie"] != null)
                {
                    client.Headers.Add("Cookie", aRequest.Headers["Cookie"] );
                }

                if (aRequest.Method == Method.Get)
                {

                    bytes = client.DownloadData(uri);

                }
                else
                {
                    NameValueCollection form = new NameValueCollection();
                    foreach (HttpInputItem item in aRequest.Form)
                    {
                        form.Add(item.Name, item.Value);
                    }
                    bytes = client.UploadValues(uri, form);
                }


                //reenvioo de cookie
                if(client.ResponseHeaders["Set-Cookie"] != null)
                {
                    aResponse.AddHeader("Set-Cookie", client.ResponseHeaders["Set-Cookie"]);
                }

                aResponse.ContentType = client.ResponseHeaders[HttpResponseHeader.ContentType];


                if (aResponse.ContentType.StartsWith("text/html"))
                {
                    string data = client.Encoding.GetString(bytes);
                    data = data.Replace($"http://{Servers[NServer].Key}:{Servers[NServer].Value}/", "/");
                    StreamWriter writer = new StreamWriter(aResponse.Body, client.Encoding);
                    writer.Write(data); 
                    writer.Flush();

                    return true;
                }
                else
                {
                    aResponse.Body.Write(bytes, 0, bytes.Length);

                    return true;
                }   
            }
            catch (Exception e)
            {
                Host.Logger.WriteLine("Error: {0}", e);
            }

            return true;
        }

        private void RegistServer(string aAdress , int aPort )
        {
            // caso o address e a porta ja estejam registados a função ignora a chamada
            if (Servers.Any(server => server.Key == aAdress && server.Value == aPort)) return;
            // caso contrario é adicionado a lista de servers 
            Servers.Add(new KeyValuePair<string, int>(aAdress, aPort));

            Host.Logger.WriteLine($"Added server {aAdress}:{aPort}");
        }

        private void UnRegistServer(string aAdress, int aPort)
        {
            // Remove o o server da lista caso este eteja registado
            Host.Logger.WriteLine(
            Servers.Remove(new KeyValuePair<string, int>(aAdress, aPort)) ?
                $"Remove server {aAdress}:{aPort}" :
                $"The server {aAdress}:{aPort} is not registered");
        }


        public int GetServer(String aId)
        {
            if (Servers.Count == 0)
            {
                //Não tem nenhum servidor registado
                return -69;
            }
            else
            {
                var contains = -1;

                foreach(var keyValue in Sessions)
                {
                    if (keyValue.Key == aId) {
                        contains = keyValue.Value;
                    }

                }
                if (contains != -1 )
                {
                   
                    NServer = contains;
                    return NServer;
                }

                //Devolve um servidor random
                Random rand = new Random();
                NServer = rand.Next(Servers.Count);

                Sessions.Add(new KeyValuePair<string, int>(aId, NServer));
                return NServer;
            }
            
        }

    }
}
