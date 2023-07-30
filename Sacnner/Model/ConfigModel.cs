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
        public List<ModeModel>? Modes { get; set; } = null;

        /// <summary>
        /// 打印机IP
        /// </summary>
        [JsonProperty("printer_ip")]
        public string PrinterIP { get; set; } = string.Empty;

        /// <summary>
        /// 打印机端口
        /// </summary>
        [JsonProperty("printer_port")]
        public int PrinterPort { get; set; } = -1;

        /// <summary>
        /// 蜂鸣器IP地址
        /// </summary>
        [JsonProperty("buzzer_ip")]
        public string BuzzerIP { get; set; } = string.Empty;

        /// <summary>
        /// 蜂鸣器端口
        /// </summary>
        [JsonProperty("buzzer_port")]
        public int BuzzerPort { get; set; } = -1;

        /// <summary>
        /// 批次数量
        /// </summary>
        [JsonProperty("batch_nums")]
        public int BatchNums { get; set; }
    }
}
