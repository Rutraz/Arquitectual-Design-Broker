using ArchBench.PlugIns;
using HttpServer;
using HttpServer.Sessions;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ArchBench.DispacherServer
{
    public class DispacherServerPlugin : IArchBenchHttpPlugIn
    {
        public string Name => "Dispacher Server Plug-In";

        public string Description => "Implement the registration behaviors";

        public string Author => "Leonel Nobrega";

        public string Version => "1.0";

        bool OnService { get; set; }

        public bool Enabled { get => OnService; set => Registration(value); }
        public IArchBenchPlugInHost Host { get; set; }

        public IArchBenchSettings Settings { get; } = new ArchBenchSettings();

        public void Dispose()
        {
        }

        public void Initialize()
        {
            Settings["DispacherAddress"] = "127.0.0.1:9000";
            Settings["ServerPort"] = "8082";
        }

        public bool Process(IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
        {
            return false; 
        }

        public void Registration(bool aOnService)
        {
            if (OnService == aOnService) return;

            OnService = aOnService;

            try 
            {
                if (string.IsNullOrEmpty(Settings["DispacherAddress"]))
                {
                    Host.Logger.WriteLine("The Dispacher adress is not define.");
                    return;
                }

                // Divide o endereço do dispacher em duas partes 
                // parts[0] -> <ip>
                // parts[1] -> <port>
                var parts = Settings["DispacherAddress"].Split(':');

                // Verifica se o endereço do dispacher esta formatado de forma correta
                // <IP>:<Port>
                if(parts.Length != 2)
                {
                    Host.Logger.Write("The Dispatcher Address format is not well defined must be <ip>:<port>)");
                    Host.Logger.WriteLine($"{Settings["DispacherAddress"]}");
                }

                //Verifica se o valor atribuido à porta é um inteiro
                //caso seja possivel converter para int então cria a variavel port com o valor da porta do dispacher address
                if (!int.TryParse(parts[1], out int port))
                {
                    Host.Logger.Write("The Dispatcher Address format is not well defined must be <ip>:<port>): ");
                    Host.Logger.WriteLine($"A number is expected on <port> : { parts[1] }");
                }

                //Criação variavel cliente que é do tipo TcpClient que recebe como parametros <ip> e <port>
                var client = new TcpClient(parts[0], port);

                // Dependendo da situação definimos as operações:
                // + -> registar
                // - -> anular registo
                var operation = OnService ? "+": "-";

                // Define os dados a enviar  
                var data = Encoding.ASCII.GetBytes($"{operation}:{GetIP()}:{Settings["ServerPort"]}");
                // data = +:<ip>:<port>
                // data = -:<ip>:<port>

                // Criação de um canal de comunicação
                var stream = client.GetStream();
                // stream.write(buffer, offset, size)
                //buffer -> Array de bytes que contém os dados para escrever no canal de comunicação
                //offset -> Local onde começa a ver os dados no buffer
                //size -> numero de bytes escritos no buffer
                stream.Write(data, 0, data.Length);
                stream.Close();
            }
            catch (SocketException e)
            {
                Host.Logger.WriteLine("SocketException: {0}", e);
            }
        }

        //Função que devolve o IP  
        private static string GetIP() 
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach(var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) return ip.ToString();
            }

            return "0.0.0.0";
        }
    }
}
