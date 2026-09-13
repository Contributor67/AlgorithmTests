using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;

namespace AlgorithmBenchmark
{
    /// <summary>
    /// Окно приложения
    /// </summary>
    public partial class MainWindow : Window
    {
        // Элементы управления интерфейса  
        private TextBox _txtStartN;        // Поле ввода начального размера массива (N)
        private TextBox _txtEndN;          // Поле ввода конечного размера массива (N)
        private TextBox _txtStep;          // Поле ввода шага приращения размера
        private Button _btnStart;          // Кнопка запуска процесса тестирования
        private ProgressBar _progressBar;  // Индикатор прогресса выполнения расчетов

        // Элементы библиотеки LiveCharts2        
        private CartesianChart _chart;
        private readonly List<ObservablePoint> _chartValues = new();

        /// <summary>
        /// Конструктор главного окна. Инициализирует базовые параметры окна и запускает сборку UI.
        /// </summary>
        public MainWindow()
        {
            // Настройка свойств
            Title = "Анализ времени выполнения алгоритма";
            Width = 900;
            Height = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Content = BuildInterface();
        }

        private UIElement BuildInterface()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(15) };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            GroupBox groupBox = new GroupBox
            {
                Header = " Параметры измерения ",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };

            StackPanel controlsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _txtStartN = AddInputField("Старт (N):", "1000000", controlsPanel);
            _txtEndN = AddInputField("Конец (N):", "10000000", controlsPanel);
            _txtStep = AddInputField("Шаг (Step):", "1000000", controlsPanel);

            _btnStart = new Button
            {
                Content = " Начать замер ",
                Height = 30,
                Padding = new Thickness(15, 0, 15, 0),
                VerticalAlignment = VerticalAlignment.Bottom
            };

            _btnStart.Click += BtnStart_Click;
            controlsPanel.Children.Add(_btnStart);

            groupBox.Content = controlsPanel;
            Grid.SetRow(groupBox, 0);
            mainGrid.Children.Add(groupBox);

            _progressBar = new ProgressBar
            {
                Height = 6,
                Margin = new Thickness(0, 0, 0, 10),
                Visibility = Visibility.Collapsed // По умолчанию скрыт
            };
            Grid.SetRow(_progressBar, 1);
            mainGrid.Children.Add(_progressBar);

            // График LiveCharts2
            _chart = new CartesianChart
            {
                // Настройка серии данных (линейный график)
                Series = new ISeries[]
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = _chartValues,
                        Name = "Сумма элементов O(N)",
                        Fill = null,
                        GeometrySize = 6       // Размер точек
                    }
                },
                // Подписи и конфигурация осей координат
                XAxes = new Axis[] { new Axis { Name = "Размер массива (N)" } },
                YAxes = new Axis[] { new Axis { Name = "Время (мс)" } }
            };
            Grid.SetRow(_chart, 2);
            mainGrid.Children.Add(_chart);

            return mainGrid;
        }

        private TextBox AddInputField(string label, string defaultValue, StackPanel container)
        {
            StackPanel fieldGroup = new StackPanel { Margin = new Thickness(0, 0, 15, 0) };
            fieldGroup.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });

            TextBox input = new TextBox { Text = defaultValue, Width = 110 };
            fieldGroup.Children.Add(input);

            container.Children.Add(fieldGroup);
            return input;
        }

        private static long CalculateSum(int[] array)
        {
            long sum = 0;
            for (int i = 0; i < array.Length; i++)
            {
                sum += array[i];
            }
            return sum;
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            // Пользовательскии ввод
            if (!int.TryParse(_txtStartN.Text, out int startN) ||
                !int.TryParse(_txtEndN.Text, out int endN) ||
                !int.TryParse(_txtStep.Text, out int step) || step <= 0)
            {
                MessageBox.Show("Заполните все поля числовыми значениями.");
                return;
            }

            // Блокировка интерфейса перед расчетами
            _btnStart.IsEnabled = false;
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = true; // Анимация полосы загрузки

            // Очистка предыдущих результатов графика
            _chartValues.Clear();

            await Task.Run(() =>
            {
                Random rng = new Random();

                CalculateSum(new int[100]);

                for (int n = startN; n <= endN; n += step)
                {
                    int[] data = new int[n];
                    for (int i = 0; i < n; i++)
                    {
                        data[i] = rng.Next(1, 100);
                    }

                    // измерение времени выполнения метода
                    Stopwatch sw = Stopwatch.StartNew();
                    CalculateSum(data);
                    sw.Stop();

                    double elapsedMs = sw.Elapsed.TotalMilliseconds;

                    Dispatcher.Invoke(() =>
                    {
                        _chartValues.Add(new ObservablePoint(n, elapsedMs));
                    });
                }
            });

            _progressBar.Visibility = Visibility.Collapsed;
            _btnStart.IsEnabled = true;
        }
    }
}