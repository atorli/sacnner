using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace BarCode.Model
{
    public class ModeModel : INotifyPropertyChanged
    {

        private string _mode = string.Empty;

        private string _motor_code = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 模式对应的编码
        /// </summary>
        public string Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                _mode = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Mode"));
            }
        }

        /// <summary>
        /// 电机编码
        /// </summary>
        public string MotorCode
        {
            get
            {
                return _motor_code;
            }
            set
            {
                _motor_code = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("MotorCode"));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        /// <param name="motor_code"></param>
        public ModeModel(string mode,string motor_code) 
        {
            Mode = mode;
            MotorCode = motor_code;
        }

    }
}
