using Newtonsoft.Json;
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
        [JsonProperty("mode")]
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
        [JsonProperty("motor_code")]
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
        /// 打印数量
        /// </summary>
        [JsonProperty("print_nums")]
        public int PrintNums { get; set; } = 20;

        /// <summary>
        /// 小标签命令
        /// </summary>
        [JsonProperty("small_label_cmd")]
        public string SmallLabelCMD { get; set; } = string.Empty;

        /// <summary>
        /// 大标签指令命令
        /// </summary>
        [JsonProperty("large_label_cmd")]
        public string LargeLabelCMD { get; set; } = string.Empty;

        /// <summary>
        /// 小标签日期格式字符串
        /// </summary>
        [JsonProperty("small_label_date_format")]
        public string SmallLabelDateFormat { get; set; }

        /// <summary>
        /// 小标签时间格式字符串
        /// </summary>
        [JsonProperty("small_label_time_format")]
        public string SmallLabelTimeFormat { get; set; }

        /// <summary>
        /// 大标签日期格式字符串
        /// </summary>
        [JsonProperty("large_label_date_foramt")]
        public string LargeLabelDateFormat { get; set; }

        /// <summary>
        /// 大标签时间格式字符串
        /// </summary>
        [JsonProperty("large_label_time_format")]
        public string LargeLaelTimeFormat { get; set; }
    }
}
