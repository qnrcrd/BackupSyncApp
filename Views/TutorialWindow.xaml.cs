using BackupSyncApp.Models;
using BackupSyncApp.Services;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BackupSyncApp.Views
{
    /// <summary>
    /// Interaction logic for TutorialWindow.xaml
    /// </summary>
    public partial class TutorialWindow : Window
    {
        private int _currentSlide = 0;
        private readonly ILocalizationService _localizationService;

        private readonly string[] _titles;
        private readonly string[] _contents;

        public TutorialWindow(Window owner=null)
        {
            InitializeComponent();

            if (owner != null)
            {
                Owner = owner;
                WindowStartupLocation = WindowStartupLocation.CenterOwner;
            }
            else WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Инициализируем сервис локализации
            var settings = AppSettings.Load();
            _localizationService = new LocalizationService(settings);

            // Загружаем тексты через локализацию
            _titles = new[]
            {
                L("Tutorial_Welcome_Title"),
                L("Tutorial_How_Title"),
                L("Tutorial_Done_Title")
            };

            _contents = new[]
            {
                L("Tutorial_Welcome_Text"),
                L("Tutorial_How_Text"),
                L("Tutorial_Done_Text")
            };

            ShowSlide(_currentSlide);
        }

        private string L(string key)
        {
            return _localizationService[key];
        }

        private void ShowSlide(int slideIndex)
        {
            SlideTitle.Text = _titles[slideIndex];
            SlideContent.Text = _contents[slideIndex];

            // Обновляем индикаторы
            Dot1.Fill = slideIndex >= 0 ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Gray;
            Dot2.Fill = slideIndex >= 1 ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Gray;
            Dot3.Fill = slideIndex >= 2 ? System.Windows.Media.Brushes.Blue : System.Windows.Media.Brushes.Gray;

            // Обновляем кнопку
            BtnNext.Content = slideIndex >= 2 ? L("Tutorial_Button_Done") : L("Tutorial_Button_Next");

            // Кнопка Назад
            BtnPrev.IsEnabled = slideIndex > 0;
        }

        private void BtnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlide < 2)
            {
                _currentSlide++;
                ShowSlide(_currentSlide);
            }
            else
            {
                // Завершаем туториал и сбрасываем флаг
                var settings = AppSettings.Load();
                settings.IsFirstRun = false;
                settings.Save();

                Close();
            }
        }

        private void BtnPrev_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSlide > 0)
            {
                _currentSlide--;
                ShowSlide(_currentSlide);
            }
        }
    }

}
