using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private ConfigModel? config = null;

        /// <summary>
        /// 匹配结果
        /// </summary>
        private CompareResultModel compare_result_model = new CompareResultModel();

        /// <summary>
        /// 日志对象
        /// </summary>
        private Logger? logger = null;

        /// <summary>
        /// 打印机0路径
        /// </summary>
        private string printer0_path = string.Empty;

        /// <summary>
        /// 打印机1路径
        /// </summary>
        private string printer1_path = string.Empty;

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

            //加载配置
            config = SetConfig(@"Config\application.json");

            //设置日志
            logger = SetLogger("Config/NLog.config");

            //绑定下拉框
            mode_combobox.ItemsSource = config?.Modes;

            //绑定提示信息
            tip.DataContext = Flow;
            Flow.Step = "请启动设备";

            //绑定结果展示上下文
            result_display_grid.DataContext = compare_result_model;

            //小标签打印机路径
            printer0_path = $@"\\{config?.HostName}\{config?.PrinterName0}";

            //大标签打印机路径
            printer1_path = $@"\\{config?.HostName}\{config?.PrinterName1}";
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

            logger?.Info($"切换至标签类型:{(motor_code_label.DataContext as ModeModel)?.MotorCode}");            
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
                string selected_motor_code = motor_code_label.Content.ToString() ?? string.Empty;

                if (input != selected_motor_code)
                {
                    Flow.Step = "匹配失败";
                    compare_result_model.Color = "Red";
                    compare_result_model.Result = "失败";

                    logger?.Error($"扫描的电机零件号与选择的电机零件号不匹配,扫描的编码为:{input},选择的编码为{selected_motor_code}");
                    MessageBox.Show("扫描电机零件号与选择的电机零件号不匹配！", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    try
                    {
                        Flow.Step = "正在打印小标签";
                        compare_result_model.Color = "Green";
                        compare_result_model.Result = "成功";

                        logger?.Info($"开始打印电机零件号{selected_motor_code}对应的小标签");

                        string small_template_path = $@"Template/{(mode_combobox.SelectedItem as ModeModel)?.Mode}.prn";
                        File.Copy(small_template_path, printer0_path);
                        //打印小标签
                        ++batch_counter;
                        if (batch_counter == config?.BatchNums)
                        {
                            Flow.Step = "正在打印大标签";
                            //如果达到大标签打印次数，开始打印打标签，并且重置计数器，开始下一轮技术

                            logger?.Info($"达到批次处理上限，开始打印电机零件号{selected_motor_code}对应的大标签");

                            string big_template_path = $@"Template/{(mode_combobox.SelectedItem as ModeModel)?.Mode}-B.prn";
                            File.Copy(big_template_path, printer1_path);

                            ResetBatchCounter();
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.Error(ex);
                    }
                }
                //清空输入框的内容
                code_textbox.Clear();
                //输入框再次获取焦点
                InputGetFoucs();
                Flow.Step = "请扫描电机标签";
            }
        }

        /// <summary>
        /// 设置配置
        /// </summary>
        /// <returns></returns>
        private ConfigModel? SetConfig(string path)
        {
            return JsonConvert.DeserializeObject<ConfigModel>(File.ReadAllText(path));
        }

        /// <summary>
        /// 设置日志
        /// </summary>
        /// <returns></returns>
        private Logger SetLogger(string path)
        {
            LogManager.Setup().LoadConfigurationFromFile(path);
            return LogManager.GetCurrentClassLogger();
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
                string big_template_path = $@"Template/{(mode_combobox.SelectedItem as ModeModel)?.Mode}-B.prn";
                File.Copy(big_template_path, printer1_path);
            }
            catch(Exception ex)
            {
                logger?.Error(ex);
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
    }
}
