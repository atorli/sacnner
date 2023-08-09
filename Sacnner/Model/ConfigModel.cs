using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace BarCode.Model
{
    public class ConfigModel
    {
        /// <summary>
        /// 模式集合
        /// </summary>
        [JsonProperty("modes")]
        public List<ModeModel> Modes { get; set; } = null;

        /// <summary>
        /// 打印机IP
        /// </summary>
        [JsonProperty("printer0_ip")]
        public string Printer0IP { get; set; } = string.Empty;

        /// <summary>
        /// 打印机端口
        /// </summary>
        [JsonProperty("printer0_port")]
        public int Printer0Port { get; set; } = -1;


        [JsonProperty("printer1_ip")]
        public string Printer1IP { get; set; } = string.Empty;

        [JsonProperty("printer1_port")]
        public int Printer1Port { get; set; } = -1;

        /// <summary>
        /// 蜂鸣器IP地址
        /// </summary>
        [JsonProperty("alertor_ip")]
        public string BuzzerIP { get; set; } = string.Empty;

        /// <summary>
        /// 蜂鸣器端口
        /// </summary>
        [JsonProperty("alertor_port")]
        public int BuzzerPort { get; set; } = -1;

        /// <summary>
        /// 批次数量
        /// </summary>
        [JsonProperty("batch_nums")]
        public int BatchNums { get; set; }

        /// <summary>
        /// 判断是键盘输入还是扫码枪输入的最大间隔时间
        /// </summary>
        [JsonProperty("max_interval_time")]
        public int MaxIntervalTime { get; set; } = 50;

        /// <summary>
        /// 比较字符串截取长度
        /// </summary>
        [JsonProperty("match_regex")]
        public string MatchRegex { get; set; }

    }
}
