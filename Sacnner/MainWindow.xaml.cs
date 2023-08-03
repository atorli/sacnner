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
        private FlowModel m_flow = new FlowModel();

        /// <summary>
        /// 配置对象
        /// </summary>
        private ConfigModel m_config;

        /// <summary>
        /// 匹配结果
        /// </summary>
        private CompareResultModel compare_result_model = new CompareResultModel();

        /// <summary>
        /// 日志对象
        /// </summary>
        private ILogger m_logger;

        /// <summary>
        /// 打印机
        /// </summary>
        private GoDEX m_small_printer;

        /// <summary>
        /// 
        /// </summary>
        private GoDEX m_large_printer;

        /// <summary>
        /// 警报器
        /// </summary>
        private IAlertor m_alertor;

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

            //初始化日志模块
            InitLogger(@"Config/NLog.config");

            //加载配置
            InitConfiguer(@"Config\application.json");

            //绑定下拉框
            mode_combobox.ItemsSource = m_config?.Modes;

            //绑定提示信息
            tip.DataContext = m_flow;
            m_flow.Step = "请启动设备";

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
            large_label_btn.IsEnabled = true;
            code_textbox.IsEnabled = true;

            InputGetFoucs();

            m_flow.Step = "请扫描电机标签";
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

            m_flow.Step = "请标签选型";
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
            large_label_btn.IsEnabled= false;

            start.IsEnabled = true;
            m_flow.Step = "请启动设备";

            try
            {
                m_alertor.CloseAllLight();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"关闭报警灯失败:{ex.Message}","错误",MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                try 
                {
                    string input = code_textbox.Text.Trim();
                    var mode = mode_combobox.SelectedItem as ModeModel;

                    if (mode != null)
                    {
                        if (input != mode.MotorCode)
                        {
                            m_flow.Step = "匹配失败";
                            compare_result_model.Color = "Red";
                            compare_result_model.Result = "失败";

                            m_logger.Error($"扫描的电机零件号与选择的电机零件号不匹配,扫描的编码为:{input},选择的编码为{mode.MotorCode}");
                            m_alertor.RedLightOn();//打开红色灯
                            //m_alertor.GreenLightOff();//关闭绿色灯
                            MessageBox.Show("扫描电机零件号与选择的电机零件号不匹配！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning); 
                        }
                        else
                        {
                            m_alertor.GreenLightOn();//打开绿色灯
                            //m_alertor.RedLightOff();//关闭红色灯
                            m_flow.Step = "正在打印小标签";
                            compare_result_model.Color = "Green";
                            compare_result_model.Result = "成功";

                            GoDEXConfig config = new GoDEXConfig(mode.PrintNums, mode.SmallLabelCMD);
                            m_small_printer?.Print(ref config);

                            if (batch_counter++ == m_config.BatchNums)
                            {
                                m_flow.Step = "正在打印大标签";
                                GoDEXConfig _cfg = new GoDEXConfig(1, mode.LargeLabelCMD);
                                m_large_printer.Print(ref _cfg);
                                ResetBatchCounter();
                            }
                        }
                    }
                    else
                    {
                        m_logger.Error("模式对象不存在");
                        MessageBox.Show("模式对象不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    m_logger.Error(ex);
                    MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //清空输入框的内容
                code_textbox.Clear();
                //输入框再次获取焦点
                InputGetFoucs();
                m_flow.Step = "请扫描电机标签";
            }
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
                    m_config = config;
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
                m_logger = LogManager.GetCurrentClassLogger();
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
            try
            {
                m_small_printer = new GoDEX(config.Printer0IP, config.Printer0Port);
                m_small_printer.Open();

                m_large_printer = new GoDEX(config.Printer1IP, config.Printer1Port);
                m_large_printer.Open();
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
        private void InitAlertor(ConfigModel config)
        {
            try
            {
                m_alertor = new Alertor(config.BuzzerIP, config.BuzzerPort);
                m_alertor.Open();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"初始化警报失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 打印大标签事件处理程序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void large_label_btn_Click(object sender, RoutedEventArgs e)
        {
            m_flow.Step = "正在打印大标签";
            try
            {
                var mode = mode_combobox.SelectedItem as ModeModel;

                if (mode != null)
                {
                    GoDEXConfig config = new GoDEXConfig(1, mode.LargeLabelCMD);
                    m_large_printer.Print(ref config);
                }
                else
                {
                    m_logger.Error("模式对象不存在.");
                    MessageBox.Show("模式对象不存在", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                m_logger.Error(ex);
                MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            InputGetFoucs();

            m_flow.Step = "请扫描电机标签";
        }

        /// <summary>
        /// 关闭窗体时清理资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Closed(object sender, EventArgs e)
        {
            m_small_printer.Dispose();
            m_alertor.Dispose();
        }

        /// <summary>
        /// 窗体加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                //初始化报警器
                InitAlertor(m_config);

                //初初始化打印机
                InitPrinter(m_config);

                //起始状态关闭两个灯
                m_alertor.CloseAllLight();
                //m_alertor.RedLightOff();
                //m_alertor.GreenLightOff();
            }
            catch(Exception ex)
            {
                MessageBox.Show($"关闭报警灯失败:{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
