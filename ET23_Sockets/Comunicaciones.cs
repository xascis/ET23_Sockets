using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Controls;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.Media.SpeechSynthesis;

namespace ET23_Sockets
{
    class Comunicaciones
    {
        Socket _socket = null;
        static ManualResetEvent _clientDone = new ManualResetEvent(false);
        const int TIMEOUT_MS = 5000;
        const int MAX_BUFF_SIZE = 2048;
        public const String serverIpAddress = "kona2.alc.upv.es";
        public const int IPPORT = 8081;
        bool conectat = false;
        public double dirvent = 0.0;

        public string Connect(string hostName, int portNumber)
        {
            string result = string.Empty;
            if (conectat == true) return result;
            DnsEndPoint hostEntry = new DnsEndPoint(hostName, portNumber);
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
            socketEventArg.RemoteEndPoint = hostEntry;

            socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate (object s, SocketAsyncEventArgs e)
            {
                result = e.SocketError.ToString();
                _clientDone.Set();
            });

            _clientDone.Reset();
            _socket.ConnectAsync(socketEventArg);
            _clientDone.WaitOne(TIMEOUT_MS);

            if (result == "Success") conectat = true;
            else conectat = false;
            return result;
        }

        public string Send(byte[] data)
        {
            string response = "Timeout de conexión";
            if (_socket != null)
            {
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;
                socketEventArg.UserToken = null;

                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate (object s, SocketAsyncEventArgs e)
                {
                    response = e.SocketError.ToString();
                    _clientDone.Set();
                });

                socketEventArg.SetBuffer(data, 0, data.Length);
                _clientDone.Reset();
                _socket.SendAsync(socketEventArg);
                _clientDone.WaitOne(TIMEOUT_MS);
            }
            else
            {
                response = "Conexión no establecida";
            }

            return response;
        }

        public string Receive()
        {
            string response = "Timeout de conexión";

            // We are receiving over an established socket connection
            if (_socket != null)
            {
                // Create SocketAsyncEventArgs context object
                SocketAsyncEventArgs socketEventArg = new SocketAsyncEventArgs();
                socketEventArg.RemoteEndPoint = _socket.RemoteEndPoint;

                // Setup the buffer to receive the data
                socketEventArg.SetBuffer(new Byte[MAX_BUFF_SIZE], 0, MAX_BUFF_SIZE);

                // Inline event handler for the Completed event.
                // Note: This even handler was implemented inline in order to make 
                // this method self-contained.
                socketEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(delegate (object s, SocketAsyncEventArgs e)
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        // Retrieve the data from the buffer
                        response = Encoding.UTF8.GetString(e.Buffer, e.Offset + 8, e.BytesTransferred - 8);
                        response = response.Trim('\0');
                    }
                    else
                    {
                        response = e.SocketError.ToString();
                    }

                    _clientDone.Set();
                });

                // Sets the state of the event to nonsignaled, causing threads to block
                _clientDone.Reset();

                // Make an asynchronous Receive request over the socket
                _socket.ReceiveAsync(socketEventArg);

                // Block the UI thread for a maximum of TIMEOUT_MILLISECONDS milliseconds.
                // If no response comes back within this time then proceed
                _clientDone.WaitOne(TIMEOUT_MS);
            }
            else
            {
                response = "Conexión no establecida";
            }

            return response;
        }

        // añadido
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct intercom
        {
            public UInt32 tam_datagrama;
            public byte tipo;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        struct intercom_d_control
        {
            public UInt32 clave;
            public UInt32 orden;
            public UInt32 datos1;
            public UInt32 datos2;
            public UInt32 clave_fin;
        }

        public static byte[] ToByteArray(object objeto)
        {
            int rawsize = Marshal.SizeOf(objeto);
            byte[] rawdata = new byte[rawsize];
            GCHandle handle = GCHandle.Alloc(rawdata, GCHandleType.Pinned);
            Marshal.StructureToPtr(objeto, handle.AddrOfPinnedObject(), false);
            handle.Free();

            return rawdata;
        }

        public enum TipoDatagrama
        {
            DATAGRAMA_INFOMETEO = 10,
            DATAGRAMA_SUBIRPERSIANAS,
            DATAGRAMA_BAJARPERSIANAS,
            DATAGRAMA_ENCENDERLUZ
        }
        // fin

        public string Enviar(byte orden, byte datos1, byte datos2, out string informacion)
        {
            string valret = "";
            informacion = "";

            // añadido
            intercom cabecera;
            cabecera.tam_datagrama = 0;
            cabecera.tipo = 2;
            int tam_cab = Marshal.SizeOf(cabecera);

            intercom_d_control paquete;
            paquete.clave = 0x9F;
            paquete.clave_fin = 0xA3;
            paquete.orden = (UInt32) TipoDatagrama.DATAGRAMA_INFOMETEO;
            paquete.datos1 = paquete.datos2 = 0;
            int tam_paq = Marshal.SizeOf(paquete);

            cabecera.tam_datagrama = (UInt32) (tam_cab + tam_paq);
            // fin

            if (conectat == false) return "Sin conexión";
            try
            {
                //byte[] buffer = new byte[]{28, 0, 0, 0,     //tam_datagrama
                //                        2, 0, 0, 0,     //tipo_datagrama
                //                        0x9F, 0, 0, 0,  //Clave inicio
                //                        10, 0, 0, 0,    //Orden
                //                        0, 0, 0, 0,     //Datos 1
                //                        0, 0, 0, 0,     //Datos 2
                //                        0xA3, 0, 0, 0,  //Clave final
                //                       };

                //buffer[6] = orden;

                byte[] paqueteCabecera = ToByteArray(cabecera);
                byte[] paqueteEntero = paqueteCabecera.Concat(ToByteArray(paquete)).ToArray();

                string resposta;
                //resposta = Send(buffer);
                resposta = Send(paqueteEntero);
                valret = "Mensaje enviado\nResposta: " + resposta;

                if (true)
                {
                    String texte;
                    texte = Receive();
                    informacion = AnalizarDatos(texte);
                }
                return valret;
            }
            catch (IOException e)
            {
                valret = "Error de envio\n" + e.Message.ToString();
            }
            return "OK";
        }

        public string AnalizarDatos(string texte)
        {
            string result = string.Empty;
            string[] vents = new string[] { "Tramuntana", "Gregal", "Llevant", "Xaloc", "Migjorn", "Llebeig", "Ponent", "Mestral" };
            string[] elements = texte.Split(' ');
            double vent, rafaga;
            vent = Convert.ToDouble(elements[1]) * 1.8;
            rafaga = Convert.ToDouble(elements[2]) * 1.8;
            result += "Viento: " + vent.ToString("0.0") + " km/h, Ráfaga: " + rafaga.ToString("0.0") + "km/h, ";
            dirvent = Convert.ToDouble(elements[3]);
            int angle = ((((int)(dirvent + 22.5)) % 360) / 45);
            result += vents[angle] + "\n";
            result += "T. Exterior: " + elements[4] + "ºC, T. Interior: " + elements[12] +
                        "ºC\nHumedad: " + elements[5] + "%\nLluvia hoy: " + elements[7] + " mm (Intensitat: " +
                        elements[11] + " mm/h)";

            return result;
        }

        private async void SpeakText(string texto)
        {
            MediaElement media = new MediaElement();
            SpeechSynthesizer syn = new SpeechSynthesizer();
            IRandomAccessStream stream = await syn.SynthesizeTextToStreamAsync(texto);
            //await MediaElement.PlayStreamAsync(stream, true); // hay un error
        }
    }

    static class MediaElementExtensions
    {
        public static async Task PlayStreamAsync(
            this MediaElement mediaElement,
            IRandomAccessStream stream,
            bool disposeStream = true
            )
        {
            TaskCompletionSource<bool> taskCompleted = new TaskCompletionSource<bool>();
            RoutedEventHandler endOfPlayHandler = (s, e) =>
            {
                if (disposeStream)
                {
                    stream.Dispose();
                }
                taskCompleted.SetResult(true);
            };
            mediaElement.MediaEnded += endOfPlayHandler;
            mediaElement.SetSource(stream, string.Empty);
            mediaElement.Play();

            await taskCompleted.Task;
            mediaElement.MediaEnded -= endOfPlayHandler;
        }
    }
}
