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
        [JsonProperty("modes")]
        public List<ModeModel>? Modes { get; set; } = null;

        /// <summary>
        /// 主机名称
        /// </summary>
        [JsonProperty("host_name")]
        public string HostName { get; set; }   = string.Empty;

        /// <summary>
        /// 打印机名称0
        /// </summary>
        [JsonProperty("printer_name_0")]
        public string PrinterName0 { get; set; } = string.Empty;

        /// <summary>
        /// 打印机名称1
        /// </summary>
        [JsonProperty("printer_name_1")]
        public string PrinterName1 { get; set; } = string.Empty;

        /// <summary>
        /// 批次数量
        /// </summary>
        [JsonProperty("batch_nums")]
        public int BatchNums { get; set; }
    }
}
