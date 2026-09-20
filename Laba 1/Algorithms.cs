using System;
using System.Collections.Generic;
using System.Text;

namespace Laba_1
{
    internal class Algorithms
    {
        // Счётчик шагов для Части IV
        public static long StepCount { get; private set; } = 0;
        public static void ResetSteps() => StepCount = 0;

        // Часть I. Операции с векторами
        // 1 постоянная функция
        public static double Constant(double[] v) => 1.0;

        // 1 сумма элементов
        public static int Sum(int[] v)
        {
            int s = 0;
            for (int i = 0; i < v.Length; i++) s += v[i];
            return s;
        }

        // 2 произведение элементов
        public static int Product(int[] v)
        {
            int p = 1;
            for (int i = 0; i < v.Length; i++) p *= v[i];
            return p;
        }

        // 4 прямое (наивное вычисление) 
        public static double PolyNaive(double[] v, double x)
        {
            double result = 0;
            int n = v.Length;
            for (int k = 0; k < n; k++)
            {
                double term = v[k];
                for (int i = 0; i < k; i++) term *= x;
                result += term;
            }
            return result;
        }

        // 4 Полином по схеме Горнера
        public static double PolyHorner(double[] v, double x)
        {
            double result = 0;
            int n = v.Length;
            for (int i = n - 1; i >= 0; i--)
                result = result * x + v[i];
            return result;
        }

        // 5 алгоритм сортировки пузырьком (Bubble sort)
        public static void BubbleSort(double[] v)
        {
            int n = v.Length;
            for (int i = 0; i < n - 1; i++)
            {
                for (int j = 0; j < n - 1 - i; j++)
                {
                    if (v[j] > v[j + 1])
                    {
                        (v[j], v[j + 1]) = (v[j + 1], v[j]);
                    }
                }
            }
        }

        // 6 Быстрая сортировка
        public static void QuickSort(double[] v) => QuickSort(v, 0, v.Length - 1);

        private static void QuickSort(double[] v, int low, int high)
        {
            if (low >= high) return;
            int p = Partition(v, low, high);
            QuickSort(v, low, p - 1);
            QuickSort(v, p + 1, high);
        }

        private static int Partition(double[] v, int low, int high)
        {
            double pivot = v[high];
            int i = low - 1;
            for (int j = low; j < high; j++)
            {
                if (v[j] <= pivot)
                {
                    i++;
                    (v[i], v[j]) = (v[j], v[i]);
                }
            }
            (v[i + 1], v[high]) = (v[high], v[i + 1]);
            return i + 1;
        }
        // 7 Timsort
        public static void TimSort(double[] v)
        {
            Array.Sort(v);
        }




        // Часть II. Операции с матрицами
        // Генерация прямоугольной матрицы rows × cols
        public static double[,] GenerateMatrix(int rows, int cols, Random rnd)
        {
            var M = new double[rows, cols];
            for (int i = 0; i < rows; i++)
                for (int j = 0; j < cols; j++)
                    M[i, j] = rnd.NextDouble() * 10.0;
            return M;
        }

        // Умножение A(n×m) × B(m×k) → C(n×k), O(n·m·k)
        public static double[,] Multiply(double[,] A, double[,] B)
        {
            int n = A.GetLength(0);   // строки A
            int m = A.GetLength(1);   // столбцы A = строки B
            int k = B.GetLength(1);   // столбцы B

            if (B.GetLength(0) != m)
                throw new ArgumentException("Несовместимые размеры матриц");

            var C = new double[n, k];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < k; j++)
                {
                    double s = 0;
                    for (int t = 0; t < m; t++)
                        s += A[i, t] * B[t, j];
                    C[i, j] = s;
                }
            return C;
        }




        // Часть III. Индивидуальное задание
        // 9 проверка, есть ли в массиве повторяющиеся элементы
        public static bool HasDuplicates(double[] v)
        {
            var seen = new HashSet<double>();
            foreach (var x in v)
            {
                if (!seen.Add(x))
                    return true;
            }
            return false;
        }

        // 9 разворачивание массива, не создавая новый массив
        public static void ReverseArray(double[] v)
        {
            int left = 0, right = v.Length - 1;
            while (left < right)
            {
                (v[left], v[right]) = (v[right], v[left]);
                left++;
                right--;
            }
        }

        
        // 9 Сортировка Шелла (Shell Sort)
        // сортировка массива по возрастанию
        public static void ShellSort(double[] v)
        {
            int n = v.Length;
            for (int gap = n / 2; gap > 0; gap /= 2)
            {
                for (int i = gap; i < n; i++)
                {
                    double temp = v[i];
                    int j;
                    for (j = i; j >= gap && v[j - gap] > temp; j -= gap)
                        v[j] = v[j - gap];
                    v[j] = temp;
                }
            }
        }




        //Часть IV. Анализ алгоритмов возведения в степень (Подсчет шагов)
        // 10 Наивный итеративный — цикл из n умножений, O(n)
        public static double PowIterative(double x, int n)
        {
            double f = 1;
            for (int k = 0; k < n; k++)
            {
                f *= x;
                StepCount++;
            }
            return f;
        }

        // 11 Рекурсивный — по формуле x^n = x * x^(n-1), O(n)
        public static double PowRecursive(double x, int n)
        {
            if (n == 0) return 1;
            StepCount++;
            return x * PowRecursive(x, n - 1);
        }

        // 12 Быстрый (бинарный) — на основе двоичного представления, O(log n)
        public static double PowBinary(double x, int n)
        {
            double f = 1;
            double c = x;
            while (n > 0)
            {
                if (n % 2 == 1)
                {
                    f *= c;
                    StepCount++;
                }
                c *= c;
                StepCount++;
                n /= 2;
            }
            return f;
        }
    }
}
