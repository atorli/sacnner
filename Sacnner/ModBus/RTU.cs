using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Collections.Immutable;
using System.IO.Ports;

namespace Sacnner.ModBus
{
    public class RTU:IDisposable
    {
        private Socket _socket;

        //private SerialPort _serial_Port;

        private string _host = string.Empty;

        private int? _port;

        public RTU(string host,int port) 
        { 
            _host = host;
            _port = port;
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            //_serial_Port = new SerialPort();
            //_serial_Port.PortName = "COM6";
            //_serial_Port.BaudRate = 115200;
            //_serial_Port.Parity = Parity.None;
            //_serial_Port.DataBits = 8;
            //_serial_Port.StopBits = StopBits.One;
            //_serial_Port.Open();

        }    
        
        public void Connect()
        {
            if (string.IsNullOrEmpty(_host) || _port is null)
            {
                throw new ArgumentException("警报器ip地址或者端口号错误.");
            }
            _socket.Connect(_host, _port.Value);
        }

        public void Close()
        {
            _socket.Close();
        }


        public void Dispose()
        {
            Close();
        }

        public void Send(List<byte> data)
        {
            var crc = CRC16.Calculate(data.ToArray());

            if(crc is null)
            {
                throw new Exception("CRC计算校验错误");
            }

            data.AddRange(crc);

            //_serial_Port.Write(data.ToArray(), 0, data.Count);
            _socket.Send(data.ToArray());
        }
    }
}
