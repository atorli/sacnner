using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sacnner.Alertor
{
    /// <summary>
    /// 警报器接口
    /// </summary>
    public interface IAlertor:IDisposable
    {
        /// <summary>
        /// 打开设备
        /// </summary>
        public void Open();

        /// <summary>
        /// 关闭设备
        /// </summary>
        public void Close();

        /// <summary>
        /// 红灯开
        /// </summary>
        public void RedLightOn();

        /// <summary>
        /// 红灯关
        /// </summary>
        public void RedLightOff();

        /// <summary>
        /// 绿灯开
        /// </summary>
        public void GreenLightOn();

        /// <summary>
        /// 绿灯关
        /// </summary>
        public void GreenLightOff();
    }
}
