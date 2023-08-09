using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
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
using Sacnner.Alertor;
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
        private FlowModel flow = new FlowModel();

        /// <summary>
        /// 配置对象
        /// </summary>
        private ConfigModel config;

        /// <summary>
        /// 匹配结果
        /// </summary>
        private CompareResultModel cmpResult = new CompareResultModel();

        /// <summary>
        /// 日志对象
        /// </summary>
        private ILogger logger;

        /// <summary>
        /// 打印机
        /// todo: 将打印机抽象出来，通过接口来实现解耦
        /// </summary>
        private GoDEX smallPrinter;

        /// <summary>
        /// 
        /// </summary>
        private GoDEX largePrinter;

        /// <summary>
        /// 警报器
        /// </summary>
        private IAlertor alector;

        /// <summary>
        /// 批次处理计数器
        /// </summary>
        private int batchCounter = 0;

        /// <summary>
        /// 上一次字符输入的时间
        /// </summary>
        private DateTime? prevPressTime = null;

        /// <summary>
        /// 默认构造器，初始化一些配置
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            //初始化日志模块
            InitLogger(@"Config/NLog.config");

            //加载配置
            InitConfiguer(@"Config\application.json");

            //绑定下拉框
            mode_combobox.ItemsSource = config?.Modes;

            //绑定提示信息
            tip.DataContext = flow;
            flow.Step = "请启动设备";

            //绑定结果展示上下文
            result_display_grid.DataContext = cmpResult;
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
            large_label_btn.IsEnabled = true;
            code_textbox.IsEnabled = true;
            InputGetFoucs();
            flow.Step = "请扫描电机标签";
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

            flow.Step = "请标签选型";
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
            cmpResult.Color = string.Empty;
            cmpResult.Result = string.Empty;

            code_textbox.IsEnabled = false;
            mode_combobox.IsEnabled = false;
            stop.IsEnabled = false;
            large_label_btn.IsEnabled = false;

            start.IsEnabled = true;
            flow.Step = "请启动设备";

            try
            {
                alector.CloseAllLight();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"关闭报警灯失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 输入框内容变化事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void code_textbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DateTime now = DateTime.Now;

            if (prevPressTime == null)
            {
                //如果last_press_time为null,说明是第一次输入，那么更新为当前时间即可
                prevPressTime = now;
                InputGetFoucs();
                return;
            }

            //判断时间间隔
            TimeSpan ts = now.Subtract(prevPressTime.Value);
            if (ts.Milliseconds > config.MaxIntervalTime)
            {
                code_textbox.Clear();
                prevPressTime = now;
                InputGetFoucs();
                return;
            }

            //判断是否换行符
            if (!code_textbox.Text.EndsWith(Environment.NewLine))
            {
                //如果没有遇到换行符，那么说明还存在数据
                //更新时间即可
                prevPressTime = now;
                InputGetFoucs();
                return;
            }

            try
            {
                string input = code_textbox.Text.Trim();

                if (string.IsNullOrEmpty(config.MatchRegex))
                {
                    throw new Exception("匹配正则表达式为空");
                }

                Match match = Regex.Match(input, config.MatchRegex);
                
                //如果匹配成功了，就进行替换
                if(match.Success)
                {
                    input = match.Value;
                }

                ModeModel mode = mode_combobox.SelectedItem as ModeModel;

                if (mode == null)
                {
                    throw new Exception("模式对象不存在");
                }

                if (input != mode.MotorCode)
                {
                    flow.Step = "匹配失败";
                    cmpResult.Color = "Red";
                    cmpResult.Result = "失败";

                    logger.Error($"扫描的电机零件号与选择的电机零件号不匹配,扫描的编码为:{input},选择的编码为{mode.MotorCode}");
                    alector.RedLightOn();
                    MessageBox.Show("扫描电机零件号与选择的电机零件号不匹配！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    alector.GreenLightOn();
                    flow.Step = "正在打印小标签";
                    cmpResult.Color = "Green";
                    cmpResult.Result = "成功";

                    GoDEXConfig config = new GoDEXConfig(mode.PrintNums, mode.SmallLabelCMD, mode.SmallLabelDateFormat, mode.SmallLabelTimeFormat);
                    smallPrinter.Print(ref config);

                    if (batchCounter++ == this.config.BatchNums)
                    {
                        flow.Step = "正在打印大标签";
                        GoDEXConfig _cfg = new GoDEXConfig(1, mode.LargeLabelCMD, mode.LargeLabelDateFormat, mode.LargeLaelTimeFormat);
                        largePrinter.Print(ref _cfg);
                        ResetBatchCounter();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            code_textbox.Clear();
            flow.Step = "请扫描电机标签";
            prevPressTime = null;
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
            batchCounter = 0;
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
                    MessageBox.Show($"加载配置文件失败,对象为空引用.", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    this.config = config;
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
            try
            {
                LogManager.Setup().LoadConfigurationFromFile(path);
                logger = LogManager.GetCurrentClassLogger();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载日志模块失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 初始化打印机
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private void InitPrinter(ConfigModel config)
        {
            smallPrinter = new GoDEX(config.Printer0IP, config.Printer0Port);
            smallPrinter.Open();

            largePrinter = new GoDEX(config.Printer1IP, config.Printer1Port);
            largePrinter.Open();
        }

        /// <summary>
        /// 初始化蜂鸣器警报
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        private void InitAlertor(ConfigModel config)
        {
            alector = new Alertor(config.BuzzerIP, config.BuzzerPort);
            alector.Open();
        }

        /// <summary>
        /// 打印大标签事件处理程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void large_label_btn_Click(object sender, RoutedEventArgs e)
        {
            flow.Step = "正在打印大标签";
            try
            {
                var mode = mode_combobox.SelectedItem as ModeModel;

                if (mode != null)
                {
                    GoDEXConfig config = new GoDEXConfig(1, mode.LargeLabelCMD, mode.LargeLabelDateFormat, mode.LargeLaelTimeFormat);
                    largePrinter.Print(ref config);
                }
                else
                {
                    logger.Error("模式对象不存在.");
                    MessageBox.Show("模式对象不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            InputGetFoucs();

            flow.Step = "请扫描电机标签";
        }

        /// <summary>
        /// 关闭窗体时清理资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            smallPrinter?.Dispose();
            largePrinter?.Dispose();
            alector?.Dispose();
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //由于目前的加载项不是很多，并且速度很快，
            //所以暂时不需要通过后台线程来进行加载
            //Task.Run(() =>
            //{
            try
            {
                //初始化报警器
                InitAlertor(config);
                //初初始化打印机
                InitPrinter(config);

                //起始状态关闭两个灯
                alector.CloseAllLight();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"初始化失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            //});
        }
    }
}
