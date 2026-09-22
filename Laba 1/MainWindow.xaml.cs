using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WPF;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace Laba_1
{
    public partial class MainWindow : Window
    {
        private TextBox _txtStartN;
        private TextBox _txtEndN;
        private TextBox _txtStep;
        private Button _btnStart;
        private Button _btnStop;

        // Новые элементы управления для истории БД
        private ComboBox _cbHistory;
        private Button _btnLoadHistory;

        private CancellationTokenSource _cts;
        private ProgressBar _progressBar;

        private ComboBox _cbAlgorithms;

        // 2D график (с библиотекой LiveChartsCore) 
        private CartesianChart _chart2D;
        private readonly ObservableCollection<ObservablePoint> _chartValues = new();
        private readonly ObservableCollection<ObservablePoint> _theoreticalValues = new(); // Коллекция для теоретических замеров

        // 3D график (Canvas) — для матричных операций
        private Canvas _canvas3D;
        private StackPanel _legendPanel3D;
        private TextBlock _txtMinZ;
        private TextBlock _txtMaxZ;

        public MainWindow()
        {
            InitializeComponent();

            Title = "Анализ сложности алгоритмов (с БД PostgreSQL)";
            Width = 1250;
            Height = 750;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            Content = BuildInterface();

            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await AppDbContext.InitDatabaseAsync();
                await RefreshHistoryComboBoxAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к PostgreSQL: {ex.Message}\nПроверьте строку подключения в AppDbContext.cs",
                                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Вспомогательный метод для обновления списка замеров в ComboBox
        private async Task RefreshHistoryComboBoxAsync()
        {
            try
            {
                var sessions = await AppDbContext.GetExperimentSessionsAsync();
                _cbHistory.ItemsSource = sessions;
                if (sessions.Count > 0)
                {
                    _cbHistory.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка загрузки истории: {ex.Message}");
            }
        }

        private UIElement BuildInterface()
        {
            Grid mainGrid = new Grid { Margin = new Thickness(15) };

            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            GroupBox groupBox = new GroupBox
            {
                Header = " Панель управления измерениями и база данных ",
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(10)
            };

            StackPanel controlsPanel = new StackPanel { Orientation = Orientation.Horizontal };

            _cbAlgorithms = new ComboBox
            {
                Width = 260,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0)
            };
            // Часть I. Операции с векторами
            _cbAlgorithms.Items.Add("1. Постоянная функция");
            _cbAlgorithms.Items.Add("2. Сумма элементов");
            _cbAlgorithms.Items.Add("3. Произведение элементов");
            _cbAlgorithms.Items.Add("4. Прямое вычисление полинома");
            _cbAlgorithms.Items.Add("5. Полином по схеме Горнера");
            _cbAlgorithms.Items.Add("6. (Bubble Sort) Алгоритм сортировки пузырьком");
            _cbAlgorithms.Items.Add("7. (Quick Sort) Алгоритм быстрой сортировки");
            _cbAlgorithms.Items.Add("8. (TimSort) Гибридный алгоритм сортировки элементов");
            // Часть II. Матричные операции (3D)
            _cbAlgorithms.Items.Add("9. Умножение матриц A*B (3D Поверхность)");
            // Часть III. Индивидуальное задание
            _cbAlgorithms.Items.Add("10. (HasDuplicates) Проверка дубликатов ");
            _cbAlgorithms.Items.Add("11. (ReverseArray) Разворот массива");
            _cbAlgorithms.Items.Add("12. (ShellSort) Сортировка Шелла");
            // Часть IV. Алгоритмы возведения в степень
            _cbAlgorithms.Items.Add("13. Простой итеративный алгоритм");
            _cbAlgorithms.Items.Add("14. Рекурсивный алгоритм");
            _cbAlgorithms.Items.Add("15. Быстрый (бинарный) алгоритм возведения в степень");

            _cbAlgorithms.SelectedIndex = 0;
            _cbAlgorithms.SelectionChanged += CbAlgorithms_SelectionChanged;

            controlsPanel.Children.Add(_cbAlgorithms);

            _txtStartN = AddInputField("Старт (N):", "1", controlsPanel);
            _txtEndN = AddInputField("Конец (N):", "500", controlsPanel);
            _txtStep = AddInputField("Шаг (Step):", "10", controlsPanel);

            _btnStart = new Button
            {
                Content = " Начать замер ",
                Height = 30,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _btnStart.Click += BtnStart_Click;
            controlsPanel.Children.Add(_btnStart);

            _btnStop = new Button
            {
                Content = " Стоп ",
                Height = 30,
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Bottom,
                IsEnabled = false
            };
            _btnStop.Click += BtnStop_Click;
            controlsPanel.Children.Add(_btnStop);

            // Вертикальный разделитель
            Border separator = new Border
            {
                Width = 1,
                Background = Brushes.LightGray,
                Margin = new Thickness(10, 2, 10, 2)
            };
            controlsPanel.Children.Add(separator);

            // Выпадающий список сохраненных замеров в БД
            StackPanel historyPanel = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            historyPanel.Children.Add(new TextBlock { Text = "История замеров (из БД):", Margin = new Thickness(0, 0, 0, 4) });

            _cbHistory = new ComboBox
            {
                Width = 320,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            historyPanel.Children.Add(_cbHistory);
            controlsPanel.Children.Add(historyPanel);

            // Кнопка загрузки сохраненного графика
            _btnLoadHistory = new Button
            {
                Content = " Загрузить график ",
                Height = 30,
                Padding = new Thickness(10, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Bottom
            };
            _btnLoadHistory.Click += BtnLoadHistory_Click;
            controlsPanel.Children.Add(_btnLoadHistory);

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

            Grid displayGrid = new Grid();

            _chart2D = new CartesianChart
            {
                IsHitTestVisible = false,
                TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden,
                Series = new ISeries[]
                {
                    // Практический замер
                    new LineSeries<ObservablePoint>
                    {
                        Values = _chartValues,
                        Name = "Фактический график",
                        Fill = null,
                        GeometrySize = 6,
                        EnableNullSplitting = false
                    },

                    // Теоретический график
                    new LineSeries<ObservablePoint>
                    {
                        Values = _theoreticalValues,
                        Name = "Идеальный график",
                        Fill = null,
                        GeometrySize = 0, // Без кружков, только гладкая линия
                        Stroke = new SolidColorPaint(SKColors.Crimson) { StrokeThickness = 2 },
                        EnableNullSplitting = false
                    }
                },
                XAxes = new Axis[] { new Axis { Name = "Размер N" } },
                YAxes = new Axis[] { new Axis { Name = "Время (мс)" } },
                Visibility = Visibility.Visible
            };
            displayGrid.Children.Add(_chart2D);

            Grid container3D = new Grid { Visibility = Visibility.Collapsed };

            _canvas3D = new Canvas
            {
                Background = Brushes.White,
                Width = 750,
                Height = 500,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            container3D.Children.Add(_canvas3D);

            _legendPanel3D = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(15),
                Visibility = Visibility.Hidden
            };
            _txtMaxZ = new TextBlock { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkRed };
            _txtMinZ = new TextBlock { FontWeight = FontWeights.Bold, Foreground = Brushes.DarkBlue };
            _legendPanel3D.Children.Add(new TextBlock { Text = "Макс. время (мс):" });
            _legendPanel3D.Children.Add(_txtMaxZ);
            _legendPanel3D.Children.Add(new TextBlock { Text = "Мин. время (мс):", Margin = new Thickness(0, 5, 0, 0) });
            _legendPanel3D.Children.Add(_txtMinZ);
            container3D.Children.Add(_legendPanel3D);

            Grid.SetRow(displayGrid, 2);
            displayGrid.Children.Add(container3D);

            mainGrid.Children.Add(displayGrid);

            return mainGrid;
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _cts?.Cancel();
            }
            catch (ObjectDisposedException) { }

            _btnStop.IsEnabled = false;
        }

        // Загрузка графика выбранного замера из БД
        private async void BtnLoadHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_cbHistory.SelectedItem is not ExperimentSession selectedSession)
            {
                MessageBox.Show("Выберите замер из списка истории.", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                var records = await AppDbContext.GetResultsForSessionAsync(selectedSession.ExperimentDate, selectedSession.AlgorithmName);
                if (records == null || records.Count == 0) return;

                _chartValues.Clear();
                _theoreticalValues.Clear(); // Очищаем теоретическую коллекцию

                // Переключение отображения (2D/3D)
                bool is3D = selectedSession.AlgorithmName.Contains("3D") || selectedSession.AlgorithmName.Contains("матриц");
                _chart2D.Visibility = is3D ? Visibility.Collapsed : Visibility.Visible;
                if (_canvas3D.Parent is Grid container3D)
                {
                    container3D.Visibility = is3D ? Visibility.Visible : Visibility.Collapsed;
                }

                if (!is3D)
                {
                    // Группируем замеры по N и берем среднее время для точек графика
                    var aggregatedPoints = records
                        .GroupBy(r => r.N)
                        .OrderBy(g => g.Key)
                        .Select(g => new ObservablePoint(g.Key, g.Average(r => r.ExecutionTimeMs)))
                        .ToList();

                    foreach (var pt in aggregatedPoints)
                    {
                        _chartValues.Add(pt);
                    }

                    // Расчет идеального теоретического графика из БД
                    CalculateTheoreticalCurve(selectedSession.AlgorithmName, aggregatedPoints);
                }

                MessageBox.Show($"График успешно восстановлен из БД!\nФункция: {selectedSession.AlgorithmName}\nЗаписей: {records.Count}",
                                "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных из БД: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private TextBox AddInputField(string label, string defaultValue, StackPanel container)
        {
            StackPanel fieldGroup = new StackPanel { Margin = new Thickness(0, 0, 8, 0) };
            fieldGroup.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });

            TextBox input = new TextBox { Text = defaultValue, Width = 60 };
            fieldGroup.Children.Add(input);

            container.Children.Add(fieldGroup);
            return input;
        }

        private void CbAlgorithms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool isMatrix3D = IsMatrixAlgorithm(_cbAlgorithms.SelectedIndex);

            _chart2D.Visibility = isMatrix3D ? Visibility.Collapsed : Visibility.Visible;
            if (_canvas3D.Parent is Grid container3D)
            {
                container3D.Visibility = isMatrix3D ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private bool IsMatrixAlgorithm(int index) => index == 8;

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_txtStartN.Text, out int startN) ||
                !int.TryParse(_txtEndN.Text, out int endN) ||
                !int.TryParse(_txtStep.Text, out int step) || step <= 0 || startN < 0 || endN < startN)
            {
                MessageBox.Show("Заполните корректно параметры диапазона.");
                return;
            }

            _cts = new CancellationTokenSource();
            _btnStart.IsEnabled = false;
            _btnStop.IsEnabled = true;
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = true;

            int selectedIndex = _cbAlgorithms.SelectedIndex;

            try
            {
                List<BenchmarkResult> dbResults;

                if (IsMatrixAlgorithm(selectedIndex))
                {
                    dbResults = await RunMatrix3DBenchmarkAsync(startN, endN, step, _cts.Token);
                }
                else
                {
                    dbResults = await RunArray2DBenchmarkAsync(selectedIndex, startN, endN, step, _cts.Token);
                }

                if (_cts != null && !_cts.IsCancellationRequested && dbResults != null && dbResults.Count > 0)
                {
                    // Автосохранение
                    await AppDbContext.SaveResultsAsync(dbResults);

                    // Автоматическое обновление выпадающего списка истории замеров
                    await RefreshHistoryComboBoxAsync();
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Вычисление остановлено пользователем.", "Отмена", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при выполнении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _progressBar.Visibility = Visibility.Collapsed;
                _btnStart.IsEnabled = true;
                _btnStop.IsEnabled = false;
                _cts?.Dispose();
                _cts = null;
            }
        }

        private async Task<List<BenchmarkResult>> RunMatrix3DBenchmarkAsync(int startN, int endN, int step, CancellationToken token)
        {
            _canvas3D.Children.Clear();
            _legendPanel3D.Visibility = Visibility.Hidden;

            int count = ((endN - startN) / step) + 1;
            double[,] zData = new double[count, count];
            List<BenchmarkResult> dbResults = new List<BenchmarkResult>();
            string algName = _cbAlgorithms.SelectedItem.ToString();
            DateTime experimentTimestamp = DateTime.UtcNow;

            await Task.Run(() =>
            {
                Parallel.For(0, count, (i, loopState) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        loopState.Stop();
                        return;
                    }

                    int rowsA = startN + i * step;
                    Random rnd = new Random(Guid.NewGuid().GetHashCode());

                    for (int j = 0; j < count; j++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            loopState.Stop();
                            return;
                        }

                        int colsA = startN + j * step;
                        int colsB = rowsA;

                        double[,] A = Algorithms.GenerateMatrix(rowsA, colsA, rnd);
                        double[,] B = Algorithms.GenerateMatrix(colsA, colsB, rnd);

                        long startTicks = Stopwatch.GetTimestamp();
                        Algorithms.Multiply(A, B);
                        long endTicks = Stopwatch.GetTimestamp();

                        double elapsedMs = (endTicks - startTicks) * 1000.0 / Stopwatch.Frequency;
                        zData[i, j] = elapsedMs;

                        lock (dbResults)
                        {
                            dbResults.Add(new BenchmarkResult
                            {
                                AlgorithmName = algName,
                                N = rowsA,
                                RunNumber = 1,
                                ExecutionTimeMs = elapsedMs,
                                StepCount = (long)rowsA * colsA * colsB,
                                ExperimentDate = experimentTimestamp
                            });
                        }
                    }
                });
            }, token);

            if (token.IsCancellationRequested) return null;

            DrawMatrix3DSurface(zData, count);
            return dbResults;
        }

        private async Task<List<BenchmarkResult>> RunArray2DBenchmarkAsync(int algorithmIndex, int startN, int endN, int step, CancellationToken token)
        {
            _chartValues.Clear();
            _theoreticalValues.Clear(); // Очищаем теоретическую коллекцию перед новым замером
            string algName = _cbAlgorithms.SelectedItem.ToString();
            List<BenchmarkResult> dbResults = new List<BenchmarkResult>();
            DateTime experimentTimestamp = DateTime.UtcNow;

            await Task.Run(() =>
            {
                int[] maxDataInt = new int[endN];
                Random rng = new Random();
                for (int i = 0; i < endN; i++) maxDataInt[i] = rng.Next(0, 100);

                Thread.CurrentThread.Priority = ThreadPriority.Highest;

                int warmupSize = Math.Min(1000, endN);
                int[] warmupInt = maxDataInt.AsSpan(0, warmupSize).ToArray();
                double[] warmupDouble = warmupInt.Select(x => (double)x).ToArray();

                for (int i = 0; i < 10; i++)
                {
                    if (token.IsCancellationRequested) break;
                    ExecuteAlgorithm(algorithmIndex, warmupInt, warmupDouble, warmupSize);
                }

                List<ObservablePoint> results = new List<ObservablePoint>();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                int[] intData = new int[endN];
                double[] doubleData = new double[endN];

                for (int n = startN; n <= endN; n += step)
                {
                    if (token.IsCancellationRequested) return;

                    const int runs = 5;
                    long totalTicks = 0;

                    for (int r = 0; r < runs; r++)
                    {
                        Array.Copy(maxDataInt, intData, n);

                        for (int i = 0; i < n; i++) doubleData[i] = intData[i];

                        long startTicks = Stopwatch.GetTimestamp();
                        long steps = ExecuteAlgorithm(algorithmIndex, intData, doubleData, n);
                        long endTicks = Stopwatch.GetTimestamp();

                        long elapsedTicks = endTicks - startTicks;
                        totalTicks += elapsedTicks;

                        double singleRunMs = (elapsedTicks * 1000.0) / Stopwatch.Frequency;

                        dbResults.Add(new BenchmarkResult
                        {
                            AlgorithmName = algName,
                            N = n,
                            RunNumber = r + 1,
                            ExecutionTimeMs = singleRunMs,
                            StepCount = steps > 0 ? steps : null,
                            ExperimentDate = experimentTimestamp
                        });
                    }

                    double avgMs = ((double)totalTicks / runs * 1000.0) / Stopwatch.Frequency;
                    results.Add(new ObservablePoint(n, avgMs));
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                Dispatcher.Invoke(() =>
                {
                    foreach (var pt in results) _chartValues.Add(pt);

                    // Расчет и построение идеальной кривой
                    CalculateTheoreticalCurve(algName, results);
                });
            }, token);

            if (token.IsCancellationRequested) return null;

            return dbResults;
        }

        private long ExecuteAlgorithm(int algorithmIndex, int[] intData, double[] doubleData, int n)
        {
            switch (algorithmIndex)
            {
                // Часть I.
                case 0: Algorithms.Constant(intData); return n;
                case 1: Algorithms.Sum(intData); return n;
                case 2: Algorithms.Product(intData); return n;
                case 3: Algorithms.PolyNaive(doubleData, 1.5); return n;
                case 4: Algorithms.PolyHorner(doubleData, 1.5); return n;
                case 5: Algorithms.BubbleSort(doubleData); return (long)n * n;
                case 6: Algorithms.QuickSort(doubleData); return (long)(n * Math.Log2(n));
                case 7: Algorithms.TimSort(doubleData); return (long)(n * Math.Log2(n));
                // Часть III.
                case 9: Algorithms.HasDuplicates(intData); return (long)n * n;
                case 10: Algorithms.ReverseArray(intData); return n / 2;
                case 11: Algorithms.ShellSort(intData); return (long)(n * Math.Log2(n));
                // Часть IV.
                case 12: Algorithms.PowIterative(1.0001, n); return n;
                case 13: Algorithms.PowRecursive(1.0001, n); return n;
                case 14: Algorithms.PowBinary(1.0001, n); return (long)Math.Log2(n);
                default: return -1;
            }
        }


        // Метод определяет математическую функцию сложности по названию выбранного алгоритма
        private (Func<double, double> Func, string Label) GetComplexityInfo(string algorithmName)
        {
            if (algorithmName.Contains("Постоянная"))
                return (n => 1.0, "O(1)");

            if (algorithmName.Contains("Bubble") || algorithmName.Contains("HasDuplicates") || algorithmName.Contains("Прямое вычисление"))
                return (n => n * n, "O(N²)");

            if (algorithmName.Contains("Quick") || algorithmName.Contains("TimSort") || algorithmName.Contains("Shell"))
                return (n => n * Math.Log2(Math.Max(n, 1)), "O(N log N)");

            if (algorithmName.Contains("быстрый") || algorithmName.Contains("бинарный") || algorithmName.Contains("PowBinary"))
                return (n => Math.Log2(Math.Max(n, 1)), "O(log N)");

            // По умолчанию O(N): Сумма, Произведение, Горнер, Разворот, Простой/Рекурсивный Pow и др.
            return (n => n, "O(N)");
        }

        // Построение теоретической линии поверх практических результатов
        private void CalculateTheoreticalCurve(string algorithmName, IEnumerable<ObservablePoint> actualPoints)
        {
            _theoreticalValues.Clear();
            var pointsList = actualPoints?.Where(p => p.X.HasValue && p.Y.HasValue).ToList();
            if (pointsList == null || pointsList.Count == 0) return;

            var (complexityFunc, label) = GetComplexityInfo(algorithmName);

            // Берём последнюю точку (максимальный N) для приведения к реальному масштабу времени (мс)
            var maxPoint = pointsList.OrderBy(p => p.X.Value).LastOrDefault();
            if (maxPoint == null) return;

            double maxN = maxPoint.X.Value;
            double maxY = maxPoint.Y.Value;
            double theoreticalMax = complexityFunc(maxN);

            if (theoreticalMax <= 0) theoreticalMax = 1;

            // Коэффициент масштабирования k = Y_реальное / Y_теоретическое
            double k = maxY / theoreticalMax;

            foreach (var point in pointsList.OrderBy(p => p.X.Value))
            {
                double n = point.X.Value;
                double idealTime = k * complexityFunc(n);
                _theoreticalValues.Add(new ObservablePoint(n, idealTime));
            }

            // Обновляем название теоретической серии
            if (_chart2D.Series.ElementAtOrDefault(1) is LineSeries<ObservablePoint> idealSeries)
            {
                idealSeries.Name = $"Идеальный {label}";
            }
        }


        // дальше рисовка 3д графика для матриц


        private void DrawMatrix3DSurface(double[,] zData, int count)
        {
            double minZ = double.MaxValue;
            double maxZ = double.MinValue;

            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < count; j++)
                {
                    if (zData[i, j] < minZ) minZ = zData[i, j];
                    if (zData[i, j] > maxZ) maxZ = zData[i, j];
                }
            }

            if (maxZ == minZ) maxZ = minZ + 1;

            _txtMinZ.Text = $"{minZ:F2}";
            _txtMaxZ.Text = $"{maxZ:F2}";
            _legendPanel3D.Visibility = Visibility.Visible;

            DrawAxes(count, minZ, maxZ);

            for (int i = count - 2; i >= 0; i--)
            {
                for (int j = 0; j < count - 1; j++)
                {
                    Point p1 = GetIsometricProjection(i, j, zData[i, j], count, minZ, maxZ);
                    Point p2 = GetIsometricProjection(i + 1, j, zData[i + 1, j], count, minZ, maxZ);
                    Point p3 = GetIsometricProjection(i + 1, j + 1, zData[i + 1, j + 1], count, minZ, maxZ);
                    Point p4 = GetIsometricProjection(i, j + 1, zData[i, j + 1], count, minZ, maxZ);

                    double avgZ = (zData[i, j] + zData[i + 1, j] + zData[i + 1, j + 1] + zData[i, j + 1]) / 4.0;
                    double normalizedZ = (avgZ - minZ) / (maxZ - minZ);

                    Polygon polygon = new Polygon
                    {
                        Points = new PointCollection { p1, p2, p3, p4 },
                        Fill = new SolidColorBrush(GetHeatmapColor(normalizedZ)),
                        Stroke = new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)),
                        StrokeThickness = 0.5,
                        StrokeLineJoin = PenLineJoin.Round
                    };

                    _canvas3D.Children.Add(polygon);
                }
            }
        }

        private void DrawAxes(int count, double minZ, double maxZ)
        {
            Point p00 = GetIsometricProjection(0, 0, minZ, count, minZ, maxZ);
            Point pN0 = GetIsometricProjection(count - 1, 0, minZ, count, minZ, maxZ);
            Point pNN = GetIsometricProjection(count - 1, count - 1, minZ, count, minZ, maxZ);
            Point p0N = GetIsometricProjection(0, count - 1, minZ, count, minZ, maxZ);

            Polygon floor = new Polygon
            {
                Points = new PointCollection { p00, pN0, pNN, p0N },
                Fill = new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)),
                Stroke = Brushes.LightGray,
                StrokeThickness = 1
            };
            _canvas3D.Children.Add(floor);

            double extCount = (count - 1) * 1.15;
            Point origin = p00;
            Point xAxis = GetIsometricProjection(extCount, 0, minZ, count, minZ, maxZ);
            Point yAxis = GetIsometricProjection(0, extCount, minZ, count, minZ, maxZ);

            double zExt = maxZ + (maxZ - minZ) * 0.2;
            Point zAxis = GetIsometricProjection(0, 0, zExt, count, minZ, maxZ);

            Line CreateLine(Point p1, Point p2, Brush color) =>
                new Line { X1 = p1.X, Y1 = p1.Y, X2 = p2.X, Y2 = p2.Y, Stroke = color, StrokeThickness = 2 };

            _canvas3D.Children.Add(CreateLine(origin, xAxis, Brushes.Crimson));
            _canvas3D.Children.Add(CreateLine(origin, yAxis, Brushes.SeaGreen));
            _canvas3D.Children.Add(CreateLine(origin, zAxis, Brushes.RoyalBlue));

            AddLabelToCanvas("Строки A (X)", xAxis, Brushes.Crimson, -45, 10);
            AddLabelToCanvas("Столбцы A (Y)", yAxis, Brushes.SeaGreen, 10, 10);
            AddLabelToCanvas("Время (Z)", zAxis, Brushes.RoyalBlue, -40, -25);
        }

        private void AddLabelToCanvas(string text, Point p, Brush color, double offsetX, double offsetY)
        {
            TextBlock tb = new TextBlock
            {
                Text = text,
                Foreground = color,
                FontWeight = FontWeights.Bold,
                FontSize = 13
            };
            Canvas.SetLeft(tb, p.X + offsetX);
            Canvas.SetTop(tb, p.Y + offsetY);
            _canvas3D.Children.Add(tb);
        }

        private Point GetIsometricProjection(double x, double y, double z, int count, double minZ, double maxZ)
        {
            double normX = x / (count - 1);
            double normY = y / (count - 1);
            double normZ = (z - minZ) / (maxZ - minZ);

            double angleX = Math.PI / 6;

            double isoX = normY + (normX * Math.Cos(angleX));
            double isoY = -(normX * Math.Sin(angleX)) - (normZ * 0.4);

            double canvasWidth = _canvas3D.Width;
            double canvasHeight = _canvas3D.Height;

            double finalX = (canvasWidth * 0.25) + (isoX * canvasWidth * 0.35);
            double finalY = (canvasHeight * 0.7) + (isoY * canvasHeight * 0.45);

            return new Point(finalX, finalY);
        }

        private Color GetHeatmapColor(double value)
        {
            value = Math.Max(0, Math.Min(1, value));
            byte r = 0, g = 0, b = 0;

            if (value <= 0.25)
            {
                double t = value / 0.25;
                r = 0; g = (byte)(255 * t); b = 255;
            }
            else if (value <= 0.5)
            {
                double t = (value - 0.25) / 0.25;
                r = 0; g = 255; b = (byte)(255 * (1 - t));
            }
            else if (value <= 0.75)
            {
                double t = (value - 0.5) / 0.25;
                r = (byte)(255 * t); g = 255; b = 0;
            }
            else
            {
                double t = (value - 0.75) / 0.25;
                r = 255; g = (byte)(255 * (1 - t)); b = 0;
            }

            return Color.FromRgb(r, g, b);
        }
    }
}