using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BarCode.Model
{
    public class CompareResultModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public string _color = string.Empty;

        public string Color 
        { 
            get
            {
                return _color;
            }
            set
            {
                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Color"));
            }
        }

        private string _result = string.Empty;

        public string Result
        {
            get
            {
                return _result;
            }
            set
            {
                _result = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Result"));
            }
        }

    }
}
