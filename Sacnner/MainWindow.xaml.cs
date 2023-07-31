using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using BarCode.Model;
using Newtonsoft.Json;
using NLog;
using Sacnner.ModBus;
using Sacnner.Printer;

namespace BarCode
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// 流程步骤信息
        /// </summary>
        private FlowModel Flow = new FlowModel();

        /// <summary>
        /// 配置对象
        /// </summary>
        private ConfigModel _config;

        /// <summary>
        /// 匹配结果
        /// </summary>
        private CompareResultModel compare_result_model = new CompareResultModel();

        /// <summary>
        /// 日志对象
        /// </summary>
        private Logger _logger;

        /// <summary>
        /// 打印机对象
        /// </summary>
        private GoDEX? _printer;

        /// <summary>
        /// RTU客户端
        /// </summary>
        private RTU? _buzzer;

        /// <summary>
        /// 批次处理计数器
        /// </summary>
        private int batch_counter = 0;

        /// <summary>
        /// 默认构造器，初始化一些配置
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //初始化日子
            InitLogger("Config/NLog.config");

            //加载配置
            InitConfiguer(@"Config\application.json");

            InitBuzzer(_config);

            InitPrinter(_config);

            //绑定下拉框
            mode_combobox.ItemsSource = _config?.Modes;

            //绑定提示信息
            tip.DataContext = Flow;
            Flow.Step = "请启动设备";

            //绑定结果展示上下文
            result_display_grid.DataContext = compare_result_model;        

        }

        /// <summary>
        /// 下拉框选中变化事件处理程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mode_combobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //设置motor_code_label的绑定
            motor_code_label.DataContext = mode_combobox.SelectedItem;

            //每次切换类型将批次处理数量清0
            ResetBatchCounter();        
        }

        /// <summary>
        /// 下拉框关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mode_combobox_DropDownClosed(object sender, EventArgs e)
        {
            pring_big_label.IsEnabled = true;
            code_textbox.IsEnabled = true;

            InputGetFoucs();

            Flow.Step = "请扫描电机标签";
        }

        /// <summary>
        /// 启动按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void start_Click(object sender, RoutedEventArgs e)
        {
            start.IsEnabled = false;
            stop.IsEnabled = true;
            mode_combobox.IsEnabled = true;

            Flow.Step = "请标签选型";
        }

        /// <summary>
        /// 停止按钮点击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void stop_Click(object sender, RoutedEventArgs e)
        {
            //停止时清空所有值
            mode_combobox.SelectedItem = null;
            code_textbox.Clear();
            compare_result_model.Color = string.Empty;
            compare_result_model.Result = string.Empty;

            code_textbox.IsEnabled = false;
            mode_combobox.IsEnabled = false;
            stop.IsEnabled = false;
            pring_big_label.IsEnabled= false;

            start.IsEnabled = true;

            Flow.Step = "请启动设备";
        }

        /// <summary>
        /// 输入框内容变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void code_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (code_textbox.Text.EndsWith(Environment.NewLine))
            {
                //匹配到换行就表示数据结束                
                string input = code_textbox.Text.Trim();

                var mode = mode_combobox.SelectedItem as ModeModel;

                if (mode != null)
                {
                    if (input != mode.MotorCode)
                    {
                        Flow.Step = "匹配失败";
                        compare_result_model.Color = "Red";
                        compare_result_model.Result = "失败";

                        _logger.Error($"扫描的电机零件号与选择的电机零件号不匹配,扫描的编码为:{input},选择的编码为{mode.MotorCode}");
                        MessageBox.Show("扫描电机零件号与选择的电机零件号不匹配！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        try
                        {
                            Flow.Step = "正在打印小标签";
                            compare_result_model.Color = "Green";
                            compare_result_model.Result = "成功";

                            _logger.Info($"开始打印电机零件号{mode.MotorCode}对应的小标签");

                            GoDEXConfig config = new GoDEXConfig(mode.PrintNums, mode.SmallLabelCMD);

                            _printer?.Print(ref config);

                            //打印小标签
                            ++batch_counter;
                            if (batch_counter == this._config?.BatchNums)
                            {
                                Flow.Step = "正在打印大标签";

                                //如果达到大标签打印次数，开始打印打标签，并且重置计数器，开始下一轮计数
                                _logger.Info($"达到批次处理上限，开始打印电机零件号{mode.MotorCode}对应的大标签");

                                GoDEXConfig _cfg = new GoDEXConfig(1,mode.LargeLabelCMD);
                                _printer?.Print(ref _cfg);
                                ResetBatchCounter();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Error(ex);
                            MessageBox.Show(ex.Message,"错误",MessageBoxButton.OK,MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    _logger.Error("模式对象不存在");
                    MessageBox.Show("模式对象不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //清空输入框的内容
                code_textbox.Clear();
                //输入框再次获取焦点
                InputGetFoucs();
                Flow.Step = "请扫描电机标签";
            }
        }

        /// <summary>
        /// 关闭窗体时清理资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            _printer?.Dispose();
            _buzzer?.Dispose();
        }

        /// <summary>
        /// 打印大标签事件处理程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pring_big_label_Click(object sender, RoutedEventArgs e)
        {
            Flow.Step = "正在打印大标签";
            try
            {
                var mode = mode_combobox.SelectedItem as ModeModel;

                if(mode != null)
                {
                    GoDEXConfig config = new GoDEXConfig(mode.PrintNums, mode.LargeLabelCMD);
                    _printer?.Print(ref config);
                }
                else
                {
                    _logger.Error("模式对象不存在.");
                    MessageBox.Show("模式对象不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }                 
                //_rtu_client.Send(new List<byte> { 0x01,0x0F,0x00,0x00,0x00,0x08,0x01,0x07});
            }
            catch(Exception ex)
            {
                _logger.Error(ex);
                MessageBox.Show(ex.Message,"错误",MessageBoxButton.OK,MessageBoxImage.Error);
            }

            InputGetFoucs();

            Flow.Step = "请扫描电机标签";
        }

        /// <summary>
        /// 输入框获得焦点
        /// </summary>
        private void InputGetFoucs()
        {
            code_textbox.Focus();
        }

        /// <summary>
        /// 重置批次计数器为0
        /// </summary>
        private void ResetBatchCounter()
        {
            batch_counter = 0;
        }

        /// <summary>
        /// 初始化配置器
        /// </summary>
        /// <returns></returns>
        private void InitConfiguer(string path)
        {
            try
            {
                var config = JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(path));
                if (config == null)
                {
                    MessageBox.Show($"加载配置文件失败,对象为空引用.","错误",MessageBoxButton.OK,MessageBoxImage.Error);
                }
                else
                {
                    _config = config;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载配置文件失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化日志
        /// </summary>
        /// <returns></returns>
        private void InitLogger(string path)
        {
            LogManager.Setup().LoadConfigurationFromFile(path);
            _logger =  LogManager.GetCurrentClassLogger();
        }

        /// <summary>
        /// 初始化打印机
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private void InitPrinter(ConfigModel config)
        {
            try
            {
                _printer = new GoDEX(config.PrinterIP, config.PrinterPort);
                _printer.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化打印机失败:{ex.Message}","错误",MessageBoxButton.OK,MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化蜂鸣器警报
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private void InitBuzzer(ConfigModel config)
        {
            try
            {
                _buzzer = new RTU(config.BuzzerIP, config.BuzzerPort);
                _buzzer.Connect();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"初始化警报失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
