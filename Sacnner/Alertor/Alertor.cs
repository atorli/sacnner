using Sacnner.ModBus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacnner.Alertor
{
    public class Alertor : IAlertor
    {
        /// <summary>
        /// rtu客户端
        /// </summary>
        private ModBusRtu rtu_client;

        /// <summary>
        /// 发送的数据
        /// </summary>
       // private List<Byte> data = new List<byte> { 0x01, 0x0F, 0x00, 0x00, 0x00, 0x08, 0x01, 0xFF};

        /// <summary>
        /// 当前的状态
        /// </summary>
        private byte current_state = 0x00;

        /// <summary>
        /// 锁对象
        /// </summary>
        private object lock_object = new object();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public Alertor(string host,int port)
        {
            rtu_client = new ModBusRtu(host,port);
        }

        public void Open()
        {
            rtu_client.Open();
        }

        public void Close()
        {
           rtu_client.Close();
        }

        public void Dispose()
        {   
            rtu_client.Dispose();
        }

        public void GreenLightOff()
        {
            lock (lock_object)
            {
                current_state &= 0xFE;//将 bit 0 置 0
                List<Byte> data = new List<byte> { 0x01, 0x0F, 0x00, 0x00, 0x00, 0x08, 0x01, current_state };
                rtu_client.Send(data);
            }
        }

        public void GreenLightOn()
        {
            lock(lock_object)
            {
                current_state |= 0x01;//将 bit 0 置 1
                List<Byte> data = new List<byte> { 0x01, 0x0F, 0x00, 0x00, 0x00, 0x08, 0x01, current_state };
                rtu_client.Send(data);
            }
        }

        public void RedLightOff()
        {
            lock (lock_object)
            {
                current_state &= 0xFD;
                List<Byte> data = new List<byte> { 0x01, 0x0F, 0x00, 0x00, 0x00, 0x08, 0x01, current_state };
                rtu_client.Send(data);
            }
        }

        public void RedLightOn()
        {
            lock (lock_object)
            {
                current_state |= 0x02;
                List<Byte> data = new List<byte> { 0x01, 0x0F, 0x00, 0x00, 0x00, 0x08, 0x01, current_state };
                rtu_client.Send(data);
            }
        }
    }
}
