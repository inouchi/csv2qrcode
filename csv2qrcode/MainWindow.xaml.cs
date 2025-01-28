using csv2qrcode;
using System.Windows;


namespace Csv2QrCode
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            // ViewModelのインスタンスを作成
            var viewModel = new MainWindowViewModel();

            // DataContextにViewModelを設定
            this.DataContext = viewModel;

            // ウィンドウのサイズ変更を無効にする
            this.ResizeMode = ResizeMode.CanMinimize;

            // ウィンドウを画面中央に表示
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }
}
