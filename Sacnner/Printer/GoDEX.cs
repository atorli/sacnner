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
        private string m_host = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        private int m_port = -1;

        /// <summary>
        /// 打印机客户端socket
        /// </summary>
        private Socket m_socket = null;

        /// <summary>
        /// 打印失败重连的次数
        /// </summary>
        private uint m_MaxReConnectCnt;

        /// <summary>
        /// 通过host和prot初始化对象
        /// </summary>
        /// <param name="host">打印机ip地址</param>
        /// <param name="port">打印机端口号</param>
        public GoDEX(string host,int port,uint maxReConnectCnt = 3) 
        {
            if (string.IsNullOrEmpty(host) || port < 0 || port > 65535)
            {
                throw new ArgumentException("IP地址或者端口号无效");
            }
            m_host =host;
            m_port=port;
            m_MaxReConnectCnt = maxReConnectCnt;
        }

        /// <summary>
        /// 连接打印机
        /// </summary>
        /// <exception cref="ArgumentException"></exception>
        public void Open()
        {
            m_socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            m_socket.Connect(m_host, m_port);
        }

        /// <summary>
        /// 关闭socket连接
        /// </summary>
        public void Close()
        {
            m_socket?.Close();
        }

        /// <summary>
        /// 重新连接至客户端
        /// </summary>
        private void ReOpen()
        {
            Close();
            Open();
        }

        /// <summary>
        /// 清理所有本地资源
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
            StringBuilder sb = new StringBuilder();
            sb.Append($"^{config.TemplateName}");

            //插入日期
            DateTime now = DateTime.Now;

            sb.Append($"\r\n{now.ToString(config.DateFormat)}");

            //插入时间，某些标签的格式不需要时间
            if (!string.IsNullOrEmpty(config.TimeFormat))
            {
                sb.Append($"\r\n{now.ToString(config.TimeFormat)}");
            }

            sb.Append($"\r\nE\r\n~P{config.PrintNums}\r");

            string content = sb.ToString();

            byte[] buffer = Encoding.ASCII.GetBytes(content);

            int connectCnt = 0;
            bool sendSuccess = false;

            do
            {
                try
                {
                    m_socket.Send(buffer);
                    sendSuccess = true;
                }
                catch (SocketException)
                {
                    ReOpen();
                    ++connectCnt;
                    if(connectCnt > m_MaxReConnectCnt)
                    {
                        throw new Exception($"超过最大重连次数，无法连接至打印机，打印失败!");
                    }
                }
            }
            while (!sendSuccess);
        }
    }

    /// <summary>
    /// 打印配置
    /// </summary>
    public struct GoDEXConfig
    {
        /// <summary>
        /// 打印数量
        /// </summary>
        public int PrintNums { get; private set; }

        /// <summary>
        /// 模板名称
        /// </summary>
        public string TemplateName { get; private set; }

        /// <summary>
        /// 日期格式
        /// </summary>
        public string DateFormat { get; private set; }

        /// <summary>
        /// 时间格式
        /// </summary>
        public string TimeFormat { get; private set; }

        public GoDEXConfig(int print_nums,string template_name,string date_format,string time_format)
        {
            PrintNums = print_nums;
            TemplateName = template_name;
            DateFormat = date_format;
            TimeFormat = time_format;
        }
    }
}
