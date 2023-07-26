using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarCode.Model
{
    internal class FlowModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// 流程步骤字段
        /// </summary>
        private string _step = string.Empty;

        /// <summary>
        /// 流程步骤属性
        /// </summary>
        public string Step
        {
            get
            {
                return _step;
            }
            set
            {
                _step = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Step"));
            }
        }
    }
}
