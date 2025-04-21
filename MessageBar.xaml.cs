using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Text;
using System.Windows.Threading;

namespace VpetChatWithOllama
{
    /// <summary>
    /// MessageBar.xaml 的交互逻辑
    /// </summary>
    public partial class OllamaMessageBar : UserControl
    {
        ChatWithOllama mainPlug;
        public OllamaMessageBar(ChatWithOllama chatWithOllama)
        {
            mainPlug = chatWithOllama;
            InitializeComponent();
        }

        private DispatcherTimer closeTimer; // 计时器用于检测超时
        private StringBuilder sb = new();   // 记录完整的文本内容

        public void Show(string name)
        {
            if (!(mainPlug.MW.Main.UIGrid.Children.Contains(this))){
                mainPlug.MW.Main.UIGrid.Children.Insert(0, this);
            }
            else
            {
                Panel.SetZIndex(this, 0);
            }
            MessageBoxContent.Children.Clear();
            sb.Clear();
            TText.Text = ""; // 初始清空
            LName.Content = name;

            //closeTimer.Start();

            this.Visibility = Visibility.Visible;
            this.Opacity = 0.8;

            // 监听鼠标进入事件
            this.MouseEnter += ResetTimerOnMouseEnter;
            this.MouseLeave += UserControl_MouseLeave;
        }

        /// <summary>
        /// 强制关闭
        /// </summary>
        public void ForceClose()
        {
            closeTimer?.Stop(); 
            this.Visibility = Visibility.Collapsed;
            MessageBoxContent.Children.Clear();
            
        }

        public void UpdateText(string newText)
        {
            Dispatcher.Invoke(() =>
            {
                sb.Append(newText);  // 追加文本
                TText.Text = sb.ToString(); // 更新 UI
            });
        }

        public void FinishText()
        {
            if (closeTimer != null)
            {
                closeTimer.Stop();
            }

            closeTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(Math.Max(TText.Text.Length * 0.05, 5))
            };

            closeTimer.Tick += CloseTimer_Tick;
            closeTimer.Start();
        }

        private void CloseTimer_Tick(object sender, EventArgs e)
        {
            CloseWin();
        }

        private void ResetCloseTimer()
        {
            if (closeTimer != null)
            {
                closeTimer.Stop();
                closeTimer.Start(); // 重新启动计时器
            }
        }

        private void ResetTimerOnMouseEnter(object sender, MouseEventArgs e)
        {
            if(closeTimer != null)
                closeTimer.Stop();
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            ResetCloseTimer();
        }

        public void CloseWin()
        {
            this.Visibility = Visibility.Collapsed;
            MessageBoxContent.Children.Clear();
            closeTimer?.Stop();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            ForceClose();
        }

        private void TText_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            sv.ScrollToEnd();
        }

        private void ContextMenu_Opened(object sender, RoutedEventArgs e) => ResetTimerOnMouseEnter(null, null);

        private void ContextMenu_Closed(object sender, RoutedEventArgs e) => UserControl_MouseLeave(null, null);
    }
}
