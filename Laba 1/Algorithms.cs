using System;
using System.Collections.Generic;
using System.Text;

namespace Laba_1
{
    internal class Algorithms
    {
        // 0 постоянная функция
        public static int Constant(int[] v) => 1;

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

        // 3 Прямое (наивное вычисление)
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

        // 4 Полином по схеме Горнера P(x)
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

        // 6. алгоритм быстрой сортировки (Quick sort)

        public static void QuickSort(double[] v) => QuickSort(v, 0, v.Length - 1);
        private static void QuickSort(double[] v, int minIndex, int maxIndex)
        {
            if (minIndex >= maxIndex) return;
            int pivot = FindPivot(v, minIndex, maxIndex);
            QuickSort(v, minIndex, pivot - 1);
            QuickSort(v, pivot + 1, maxIndex);
        }

        // 7. (Timsort) Гибридный алгоритм сортировки элементов
        private static int FindPivot(double[] v, int minIndex, int maxIndex)
        {
            int pivot = minIndex - 1, n = v.Length; ;
            for (int i = 0; i < n - 1; i++)
            {
                if (v[i] < v[n - 1])
                {
                    pivot++;
                    (v[pivot], v[i]) = (v[i], v[pivot]);
                }
            }
            pivot++;
            (v[pivot], v[maxIndex]) = (v[maxIndex], v[pivot]);
            return pivot;
        }


        /// Временно убрано (так делать нельзя)



        /*
                // 8 Возведение в степень (4 варианта)
                public static double Pow(double x, int n)
                {
                    double f = 1;
                    for (int k = 0; k < n; k++) f *= x;
                    return f;
                }

                //рекурсивно
                public static double RecPow(double x, int n)  
                {
                    if (n == 0) return 1;

                    double f = RecPow(x, n / 2);
                    f = f * f;

                    if (n % 2 == 1)
                    {
                        f = f * x;
                    }

                    return f;
                }

                // быстрый алгоритм
                public static double QuickPow(double x, int n)
                {
                    double f = 1;
                    double c = x;

                    while (n > 0)
                    {
                        if (n % 2 == 1)
                        {
                            f = f * c;
                        }
                        c = c * c;
                        n = n / 2;
                    }

                    return f;
                }
                // классический быстрый алгоритм
                public static double QuickPow1(double x, int n)
                {
                    double f = 1;

                    while (n > 0)
                    {
                        if (n % 2 == 1)
                        {
                            f = f * x;
                        }
                        x = x * x;
                        n = n / 2;
                    }

                    return f;
                }*/

    }
}
