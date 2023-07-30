using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Diagnostics;

namespace Sacnner.Printer
{
    public class GoDEX:IDisposable
    {
        /// <summary>
        /// 打印机ip地址
        /// </summary>
        private string _host = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        private int? _port;

        /// <summary>
        /// 打印指令模板
        /// </summary>
        private string template = "^{0}\r\nE\r\n~P{1}\r";

        /// <summary>
        /// 打印机客户端socket
        /// </summary>
        private Socket _socket;

        /// <summary>
        /// 通过host和prot初始化对象
        /// </summary>
        /// <param name="host">打印机ip地址</param>
        /// <param name="port">打印机端口号</param>
        public GoDEX(string host,int port) 
        {
            _host=host;
            _port=port;
            _socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// 连接打印机
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Connect()
        {
            if (string.IsNullOrEmpty(_host) || _port == null) 
            {
                throw new ArgumentException("打印机ip地址或者端口号无效");
            }

            _socket.Connect(_host, _port.Value);
        }

        /// <summary>
        /// 关闭socket连接
        /// </summary>
        public void Close()
        {
            _socket.Close();
        }


        /// <summary>
        /// 释放socket对象
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public void Dispose()
        {
            Close();
        }

        /// <summary>
        /// 发送打印指令
        /// </summary>
        /// <param name="config">包含打印配置的对象</param>
        public void Print(ref GoDEXConfig config)
        {
            string content = string.Format(template, config.TemplateName, config.PrintNums);

            byte[] buffer = Encoding.ASCII.GetBytes(content);

            _socket.Send(buffer);
        }


    }

    public struct GoDEXConfig
    {
        public int PrintNums { get;private set; }

        public string TemplateName { get; private set; }

        public GoDEXConfig(int print_nums,string template_name)
        {
            PrintNums = print_nums;
            TemplateName = template_name;
        }
    }
}
