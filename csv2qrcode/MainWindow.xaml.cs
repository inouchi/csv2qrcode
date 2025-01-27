﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Win32;
using QRCoder;


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
        }
    }

    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private string _csvFilePath;
        private bool _isGenerating;
        private bool _canGenerateQrCode;

        public string CsvFilePath
        {
            get => _csvFilePath;
            set
            {
                _csvFilePath = value;
                OnPropertyChanged(nameof(CsvFilePath));
                CanGenerateQrCode = !string.IsNullOrWhiteSpace(value);
            }
        }

        public bool IsGenerating
        {
            get => _isGenerating;
            set
            {
                _isGenerating = value;
                OnPropertyChanged(nameof(IsGenerating));
            }
        }

        public bool CanGenerateQrCode
        {
            get => _canGenerateQrCode;
            set
            {
                _canGenerateQrCode = value;
                OnPropertyChanged(nameof(CanGenerateQrCode));
                ((RelayCommand)GenerateQrCodeCommand).RaiseCanExecuteChanged(); // ボタンの有効/無効を再確認
            }
        }


        public ICommand SelectFileCommand { get; }
        public ICommand GenerateQrCodeCommand { get; }

        public MainWindowViewModel()
        {
            SelectFileCommand = new RelayCommand(SelectFile);
            GenerateQrCodeCommand = new RelayCommand(GenerateQrCode, () => CanGenerateQrCode);
        }

        private void SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                Filter = "CSVファイル (*.csv)|*.csv",
                Multiselect = false
            };

            if (dialog.ShowDialog() == true)
            {
                CsvFilePath = dialog.FileName;
            }
        }

        private async void GenerateQrCode()
        {
            if (!File.Exists(CsvFilePath))
            {
                MessageBox.Show("CSVファイルが存在しません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            IsGenerating = true;

            try
            {
                var outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    DateTime.Now.ToString("yyyyMMddHHmmss"));
                Directory.CreateDirectory(outputDirectory);

                await Task.Run(() =>
                {
                    using var reader = new StreamReader(CsvFilePath, Encoding.GetEncoding("shift-jis"));
                    using var csv = new CsvReader(reader, new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
                    {
                        HasHeaderRecord = false
                    });

                    var records = csv.GetRecords<dynamic>().Skip(1);  // 最初のレコード（ヘッダー相当）をスキップ
                    int index = 1;

                    foreach (var record in records)
                    {
                        var qrData = record.Field1;
                        if (string.IsNullOrWhiteSpace(qrData)) continue;

                        using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
                        using (QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrData, QRCodeGenerator.ECCLevel.Q))
                        using (PngByteQRCode qrCode = new PngByteQRCode(qrCodeData))
                        {
                            byte[] qrCodeImage = qrCode.GetGraphic(20);

                            var fileName = Path.Combine(outputDirectory, $"{index++:D5}.png");
                            File.WriteAllBytes(fileName, qrCodeImage);
                        }
                    }
                });

                MessageBox.Show("QRコードの生成が完了しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                System.Diagnostics.Process.Start("explorer.exe", outputDirectory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsGenerating = false;
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
