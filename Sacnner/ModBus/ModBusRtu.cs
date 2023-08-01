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
    /// <summary>
    /// ModBus RTU客户端
    /// </summary>
    public class ModBusRtu:IDisposable
    {
        private Socket m_socket;

        private string m_host = string.Empty;

        private int? m_port;

        public ModBusRtu(string host,int port) 
        { 
            m_host = host;
            m_port = port;
            m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }    
        
        public void Open()
        {
            if (string.IsNullOrEmpty(m_host) || m_port is null)
            {
                throw new ArgumentException("警报器ip地址或者端口号错误.");
            }
            m_socket.Connect(m_host, m_port.Value);
        }

        public void Close()
        {
            m_socket.Close();
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
            m_socket.Send(data.ToArray());
        }
    }
}
