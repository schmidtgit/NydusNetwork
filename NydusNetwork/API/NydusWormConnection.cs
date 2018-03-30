using Google.Protobuf;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NydusNetwork {
    public class NydusWormConnection {
        public int Port { get; private set; }
        private TcpListener _listener;
        private NetworkStream _stream;
        private Action<byte[]> _handler;
        public NydusWormConnection(Action<byte[]> handler) => _handler = handler;
         
        public void Initialize(int port = 0) {
            _listener = new TcpListener(IPAddress.Loopback,0);
            _listener.Start();
            Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        }

        public void Connect() {
            var client = _listener.AcceptTcpClient();
            _stream = client.GetStream();
            _listener.Stop();
            Task.Run(() => {
                Receive(client);
            });
        }

        private byte[] ReadBytes(int count) {
            byte[] bytes = new byte[count];
            _stream.Read(bytes,0,bytes.Length);
            return bytes;
        }

        private byte[] ReadMessage() {
            var size = new byte[4];
            _stream.Read(size, 0, 4);
            var buffsize = BitConverter.ToInt32(size, 0);
            byte[] messageBytes = ReadBytes(buffsize);
            return messageBytes;
        }

        private void Receive(TcpClient client) {
            while(client.Connected) {
                var msg = ReadMessage();
                if(msg.Length != 0)
                    _handler.Invoke(msg);
            }
            Close(client);
        }

        public void SendMessage(IMessage r) {
            var msg = r.ToByteArray();
            msg = BitConverter.GetBytes(msg.Length).Concat(msg).ToArray();
            _stream.WriteAsync(msg,0,msg.Length);
        }

        public void Close(TcpClient client) {
            _stream?.Close();
            client?.Close();
            _listener?.Stop();
        }
    }
}
