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

namespace Laba_1
{
    public partial class MainWindow : Window
    {

        private TextBox _txtStartN;
        private TextBox _txtEndN;
        private TextBox _txtStep;
        private Button _btnStart;
        private ProgressBar _progressBar;

        private ComboBox _cbAlgorithms;

        // 2д график (с библиотекой LiveChartsCore) 
        private CartesianChart _chart2D;
        private readonly ObservableCollection<ObservablePoint> _chartValues = new();

        // 3д график (Canvas) — для матричных операций
        private Canvas _canvas3D;
        private StackPanel _legendPanel3D;
        private TextBlock _txtMinZ;
        private TextBlock _txtMaxZ;


        public MainWindow()
        {
            InitializeComponent();

            Title = "Анализ сложности алгоритмов";
            Width = 1000;
            Height = 720;
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

            _cbAlgorithms = new ComboBox
            {
                Width = 320,
                Height = 30,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 15, 0)
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
            _cbAlgorithms.Items.Add("13. Возведение в степень PowIterative O(n)");
            _cbAlgorithms.Items.Add("14. Возведение в степень PowRecursive O(n)");
            _cbAlgorithms.Items.Add("15. Возведение в степень PowBinary O(log n)");

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

            Grid displayGrid = new Grid();

            _chart2D = new CartesianChart
            {
                IsHitTestVisible = false,
                TooltipPosition = LiveChartsCore.Measure.TooltipPosition.Hidden,
                Series = new ISeries[]
                {
                    new LineSeries<ObservablePoint>
                    {
                        Values = _chartValues,
                        Name = "Время выполнения",
                        Fill = null,
                        GeometrySize = 6,
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

        private TextBox AddInputField(string label, string defaultValue, StackPanel container)
        {
            StackPanel fieldGroup = new StackPanel { Margin = new Thickness(0, 0, 15, 0) };
            fieldGroup.Children.Add(new TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });

            TextBox input = new TextBox { Text = defaultValue, Width = 70 };
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

        private bool IsMatrixAlgorithm(int index) => index == 14;




        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(_txtStartN.Text, out int startN) ||
                !int.TryParse(_txtEndN.Text, out int endN) ||
                !int.TryParse(_txtStep.Text, out int step) || step <= 0 || startN < 0 || endN < startN)
            {
                MessageBox.Show("Заполните корректно параметры диапазона.");
                return;
            }

            _btnStart.IsEnabled = false;
            _progressBar.Visibility = Visibility.Visible;
            _progressBar.IsIndeterminate = true;

            int selectedIndex = _cbAlgorithms.SelectedIndex;

            if (IsMatrixAlgorithm(selectedIndex))
            {
                await RunMatrix3DBenchmarkAsync(startN, endN, step);
            }
            else
            {
                await RunArray2DBenchmarkAsync(selectedIndex, startN, endN, step);
            }

            _progressBar.Visibility = Visibility.Collapsed;
            _btnStart.IsEnabled = true;
        }

        private async Task RunMatrix3DBenchmarkAsync(int startN, int endN, int step)
        {
            _canvas3D.Children.Clear();
            _legendPanel3D.Visibility = Visibility.Hidden;

            int count = ((endN - startN) / step) + 1;
            double[,] zData = new double[count, count];

            await Task.Run(() =>
            {
                Parallel.For(0, count, i =>
                {
                    int rowsA = startN + i * step;
                    Random rnd = new Random(Guid.NewGuid().GetHashCode());

                    for (int j = 0; j < count; j++)
                    {
                        int colsA = startN + j * step;
                        int colsB = rowsA;

                        double[,] A = Algorithms.GenerateMatrix(rowsA, colsA, rnd);
                        double[,] B = Algorithms.GenerateMatrix(colsA, colsB, rnd);

                        long startTicks = Stopwatch.GetTimestamp();
                        Algorithms.Multiply(A, B);
                        long endTicks = Stopwatch.GetTimestamp();

                        zData[i, j] = (endTicks - startTicks) * 1000.0 / Stopwatch.Frequency;
                    }
                });
            });

            DrawMatrix3DSurface(zData, count);
        }

        private async Task RunArray2DBenchmarkAsync(int algorithmIndex, int startN, int endN, int step)
        {
            _chartValues.Clear();

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
                    ExecuteAlgorithm(algorithmIndex, warmupInt, warmupDouble, warmupSize);
                }

                List<ObservablePoint> results = new List<ObservablePoint>();

                GC.Collect();
                GC.WaitForPendingFinalizers();

                for (int n = startN; n <= endN; n += step)
                {
                    const int runs = 5;
                    long totalTicks = 0;

                    for (int r = 0; r < runs; r++)
                    {
                        int[] currentIntData = new int[n];
                        Array.Copy(maxDataInt, currentIntData, n);

                        double[] currentDoubleData = new double[n];
                        for (int i = 0; i < n; i++) currentDoubleData[i] = currentIntData[i];

                        long startTicks = Stopwatch.GetTimestamp();
                        ExecuteAlgorithm(algorithmIndex, currentIntData, currentDoubleData, n);
                        long endTicks = Stopwatch.GetTimestamp();

                        totalTicks += (endTicks - startTicks);
                    }

                    double avgMs = ((double)totalTicks / runs * 1000.0) / Stopwatch.Frequency;
                    results.Add(new ObservablePoint(n, avgMs));
                }

                Thread.CurrentThread.Priority = ThreadPriority.Normal;

                Dispatcher.Invoke(() =>
                {
                    foreach (var pt in results) _chartValues.Add(pt);
                });
            });
        }

        private void ExecuteAlgorithm(int algorithmIndex, int[] intData, double[] doubleData, int n)
        {
            switch (algorithmIndex)
            {
                //Часть I.
                case 0: Algorithms.Constant(intData); break;
                case 1: Algorithms.Sum(intData); break;
                case 2: Algorithms.Product(intData); break;
                case 3: Algorithms.PolyNaive(doubleData, 1.5); break;
                case 4: Algorithms.PolyHorner(doubleData, 1.5); break;
                case 5: Algorithms.BubbleSort(doubleData); break;
                case 6: Algorithms.QuickSort(doubleData); break;
                case 7: Algorithms.TimSort(doubleData); break;
                //Часть III.
                case 8: Algorithms.HasDuplicates(intData); break;
                case 9: Algorithms.ReverseArray(intData); break;
                case 10: Algorithms.ShellSort(intData); break;
                //Часть IV.
                case 11: Algorithms.PowIterative(1.0001, n); break;
                case 12: Algorithms.PowRecursive(1.0001, n); break;
                case 13: Algorithms.PowBinary(1.0001, n); break;
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