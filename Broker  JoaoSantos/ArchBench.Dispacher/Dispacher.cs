
using ArchBench.PlugIns;
using HttpServer;
using HttpServer.Sessions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ArchBench.Dispacher
{
    public class Dispacher : IArchBenchHttpPlugIn
    {
        public string Name => "Dispacher Pattern";

        public string Description => "Implementation fo Architectural Pattern: Dispacher";

        public string Author => "João Santos";

        public string Version => "1.0";

        public bool Enabled { get; set; } = false;
        public IArchBenchPlugInHost Host { get; set; }

        public IArchBenchSettings Settings { get; } = new ArchBenchSettings();


        // TCP Listener
        public TcpListener Listener { get; private set; }

        // Thread para receber pedidos de registos de outros plugins
        public Thread Thread { get; private set; }

        // Lista de servers key<ip> value<port>
        public List<KeyValuePair<string, int>> Servers { get; } = new List<KeyValuePair<string, int>>();

        // Variavel que guarda o indexe do proximo server a receber o pedido
        private int NextServer {get;set;}

        public void Dispose()
        {
        }

        public void Initialize()
        {
            //Criar TCP listener na porta 9000
            Listener = new TcpListener(IPAddress.Any, 9000);
            //Criação de uma thread que corre a função ReciveThreadFunction
            Thread = new Thread(ReceiveThreadFunction) { IsBackground = true };
            //Inicia a thread
            Thread.Start();
        }

        public bool Process(IHttpRequest aRequest, IHttpResponse aResponse, IHttpSession aSession)
        {

            Host.Logger.WriteLine("Processing...");

            // Obter index do proximo server que vai responder ao pedido
            var index = GetNextServer();
            if (index == -1) 
            {
                Host.Logger.WriteLine("No Servers Available!");
                return false;
            }
            

            Host.Logger.WriteLine($"Dispaching to server on port {Servers[index].Value}");

            // Criar link de redirecionamento
            var redirection = new StringBuilder();
            // cria uma string com "http://<ip>:<port>"
            redirection.Append($"http://{Servers[index].Key}:{Servers[index].Value}");
            // adiciona a route que pretendemos
            redirection.Append(aRequest.Uri.AbsolutePath);
            // http://<ip>:<port>/hello
            // Verifica se estamos a passar parametros pelo url
            var count = aRequest.QueryString.Count();

            if(count >0)
            {
                // Caso esteja a passar parametros temos de adicionar um '?'
                redirection.Append('?');
                // Para cada parametro fazemos append ao http://<ip>:<port>/route?name=value
                // No caso de ser mais de um adiciona o '&' http://<ip>:<port>/route?name1=value1&name2=value2
                foreach (HttpInputItem item in aRequest.QueryString)
                {
                    redirection.Append($"{item.Name}={item.Value}");
                    if (--count > 0) redirection.Append('&');
                }
            }

            // Rederecionar para o redirection que é do genero
            aResponse.Redirect(redirection.ToString());
            return true;
        }

        //Usa Round Robin para definir o proximo servidor a responder ao pedido
        public int GetNextServer()
        {
            if (Servers.Count == 0) return -1;
            NextServer = (NextServer + 1) % Servers.Count;
            return NextServer;
        }

        private void ReceiveThreadFunction()
        {
            try
            {
                // Iniciar o Listener
                Listener.Start();
                
                // Buffer para ler dados
                byte[] bytes = new byte[256];

                //Loop para listenning
                while (true)
                {
                    // Listener aceita pedido connecção de o cliente
                    var client = Listener.AcceptTcpClient();

                    //Criação de uma stream para receber dados 
                    var stream = client.GetStream();

                    // Lê os dados da stream e guarda o numero de bytes lidos no buffer
                    int count = stream.Read(bytes, 0, bytes.Length);

                    // se o tamanho do buffer for maior que 0 bytes
                    if (count != 0)
                    {
                        // Transforma a sequencia de bytes que estão no buffer em uma string
                        string data = Encoding.ASCII.GetString(bytes, 0, count);

                        // Divide a string em três partes usando os : como separação
                        var parts = data.Split(':');

                        // parts[0] guarda a informação + (registar) ou - (anular registo)
                        switch (parts[0])
                        {
                            case "+":
                                //Registar enviando o <ip> e a <port>
                                Regist(parts[1], int.Parse(parts[2]));
                                break;
                            case "-":
                                //Anular Registo enviando o <ip> e a <port>
                                Unregist(parts[1], int.Parse(parts[2]));
                                break;
                        }
                        // Após efectuar registo/anular registo fechamos a ligação TCP
                        client.Close();
                    }
                }
            }
            catch( SocketException e)
            {
                Host.Logger.WriteLine($"SocketExecption: {e}");
            }
            finally
            {
                // Qualquer outra exepção que ocorra é interrompido o Listener
                Listener.Stop();
            }
        }

        private void Regist(string aAdress, int aPort)
        {
            // caso o address e a porta ja estejam registados a função ignora a chamada
            if (Servers.Any(server => server.Key == aAdress && server.Value == aPort)) return;
            // caso contrario é adicionado a lista de servers 
            Servers.Add(new KeyValuePair<string, int>(aAdress, aPort));

            Host.Logger.WriteLine($"Added server {aAdress}:{aPort}");
        }

        private void Unregist(string aAdress, int aPort)
        {
            // Remove o o server da lista caso este eteja registado
            Host.Logger.WriteLine(
            Servers.Remove(new KeyValuePair<string, int>(aAdress, aPort)) ?
                $"Remove server {aAdress}:{aPort}" :
                $"The server {aAdress}:{aPort} is not registered");
        }
    }
}
