using Laba_1;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace AlgorithmBenchmark
{
    public partial class MainWindow : Window
    {
        private TextBox _txtStartN;
        private TextBox _txtEndN;
        private TextBox _txtStep;
        private Button _btnStart;
        private ProgressBar _progressBar;

        private ComboBox _cbAlgorithms; // Поле для выпадающего списка

        private CartesianChart _chart;
        private readonly ObservableCollection<ObservablePoint> _chartValues = new();

        public MainWindow()
        {
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

            // Создаем выпадающий список алгоритмов
            _cbAlgorithms = new ComboBox
            {
                Width = 200,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
            };

            // Заполняем список названиями алгоритмов
            _cbAlgorithms.Items.Add("1. Постоянная функция");
            _cbAlgorithms.Items.Add("2. Сумма элементов");
            _cbAlgorithms.Items.Add("3. Произведение элементов");
            _cbAlgorithms.Items.Add("4. Прямое (наивное вычисление)");
            _cbAlgorithms.Items.Add("5. Полином по схеме Горнера");
            _cbAlgorithms.Items.Add("6. (Bubble sort) Алгоритм сортировки пузырьком");
            _cbAlgorithms.Items.Add("7. (Quick sort) Алгоритм быстрой сортировки");
            _cbAlgorithms.Items.Add("8. (Timsort) Гибридный алгоритм сортировки элементов");

            // Выбираем первый элемент по умолчанию
            _cbAlgorithms.SelectedIndex = 0;

            controlsPanel.Children.Add(_cbAlgorithms);

            _txtStartN = AddInputField("Старт (N):", "0", controlsPanel);
            _txtEndN = AddInputField("Конец (N):", "1000", controlsPanel);
            _txtStep = AddInputField("Шаг (Step):", "1", controlsPanel);

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
                Visibility = Visibility.Collapsed
            };
            Grid.SetRow(_progressBar, 1);
            mainGrid.Children.Add(_progressBar);

            _chart = new CartesianChart
            {
                IsHitTestVisible = false,
                TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden,
                Series = new ISeries[]
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = _chartValues,
                        Name = "Сумма элементов",
                        Fill = null,
                        GeometrySize = 6,
                        EnableNullSplitting = false
                    }
                },
                XAxes = new Axis[] { new Axis { Name = "Размер массива" } },
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

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_txtStartN.Text, out int startN) ||
                !int.TryParse(_txtEndN.Text, out int endN) ||
                !int.TryParse(_txtStep.Text, out int step) || step <= 0 || startN < 0 || endN < startN)
            {
                MessageBox.Show("Заполните корректно все поля числовыми значениями.");
                return;
            }

            _btnStart.IsEnabled = false;
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = true;

            _chartValues.Clear();

            int selectedAlgorithm = _cbAlgorithms.SelectedIndex; // Выбранный алгоритм пользователем

            await Task.Run(() =>
            {
                // Создаем исходные данные типа double (согласно классу Algorithms)
                int[] maxData = new int[endN];
                Random rng = new Random();
                for (int i = 0; i < endN; i++)
                {
                    maxData[i] = rng.Next(0,100);
                }

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                // Прогрев JIT-компилятора для выбранного алгоритма
                int warmupSize = Math.Min(1000, endN);

                // Готовим срезы двух типов заранее
                int[] warmupInt = maxData.AsSpan(0, warmupSize).ToArray();
                double[] warmupDouble = warmupInt.Select(x => (double)x).ToArray();

                for (int i = 0; i < 50; i++)
                {
                    ExecuteAlgorithm(selectedAlgorithm, warmupInt, warmupDouble, warmupSize);
                }

                List<ObservablePoint> results = new List<ObservablePoint>((endN - startN) / step + 1);

                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                for (int n = startN; n <= endN; n += step)
                {
                    const int runs = 5;
                    long totalTicks = 0;

                    for (int r = 0; r < runs; r++)
                    {
                        //Готовка целочисленного среза
                        int[] currentIntData = new int[n];
                        Array.Copy(maxData, currentIntData, n);

                        // Готовка вещественного среза
                        double[] currentDoubleData = new double[n];
                        for (int i = 0; i < n; i++)
                        {
                            currentDoubleData[i] = currentIntData[i];
                        }

                        // замер времени
                        long startTicks = Stopwatch.GetTimestamp();

                        ExecuteAlgorithm(selectedAlgorithm, currentIntData, currentDoubleData, n);

                        long endTicks = Stopwatch.GetTimestamp();

                        totalTicks += (endTicks - startTicks);
                    }

                    double avgTicks = (double)totalTicks / runs;
                    double avgMs = (avgTicks * 1000.0) / Stopwatch.Frequency;

                    _chartValues.Add(new ObservablePoint(n, avgMs));
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                Dispatcher.Invoke(() =>
                {
                    foreach (var pt in results)
                    {
                        _chartValues.Add(pt);
                    }
                });
            });

            _progressBar.Visibility = Visibility.Collapsed;
            _btnStart.IsEnabled = true;
        }

        // Вспомогательный метод с конструкцией switch для вызова метода из Algorithms
        private void ExecuteAlgorithm(int algorithmIndex, int[] intData, double[] doubleData, int n)
        {
            switch (algorithmIndex)
            {
                case 0:
                    Algorithms.Constant(intData);
                    break;
                case 1:
                    Algorithms.Sum(intData);
                    break;
                case 2:
                    Algorithms.Product(intData);
                    break;
                case 3:
                    Algorithms.PolyNaive(doubleData, 1.5);
                    break;
                case 4:
                    Algorithms.PolyHorner(doubleData, 1.5);
                    break;
                case 5:
                    Algorithms.BubbleSort(doubleData);
                    break;
                case 6:
                    Algorithms.QuickSort(doubleData);
                    break;
                case 7:
                    //  Algorithms.FindPivot();
                    break;
            }
        }
    }
}