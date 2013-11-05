using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Statistics
{

    /// <summary>
    /// Computes some simple statistics for a data set.
    /// </summary>
    public class Statistics
    {

        /// <summary>
        /// Returned when an error occurrs. You could also use
        /// Double.Nan.
        /// </summary>
        private const double NO_RESULT = 0.0;

        /// <summary>
        /// The data set this object acts on
        /// </summary>
        private double[] list;

        /// <summary>
        /// Construct a new Statistics object from the given data set
        /// </summary>
        /// <param name="list"></param>
        public Statistics(params double[] list)
        {
            this.list = list;
        }

        /// <summary>
        /// Change the data set
        /// </summary>
        /// <param name="list"></param>
        public void update(params double[] list)
        {
            this.list = list;
        }

        /// <summary>
        /// Compute the mode of the data
        /// </summary>
        /// <returns></returns>
        public double mode()
        {
            try
            {
                double[] i = new double[list.Length];
                list.CopyTo(i, 0);
                sort(i);
                double val_mode = i[0], help_val_mode = i[0];
                int old_counter = 0, new_counter = 0;
                int j = 0;
                for (; j <= i.Length - 1; j++)
                    if (i[j] == help_val_mode) new_counter++;
                    else if (new_counter > old_counter)
                    {
                        old_counter = new_counter;
                        new_counter = 1;
                        help_val_mode = i[j];
                        val_mode = i[j - 1];
                    }
                    else if (new_counter == old_counter)
                    {
                        val_mode = NO_RESULT;
                        help_val_mode = i[j];
                        new_counter = 1;
                    }
                    else
                    {
                        help_val_mode = i[j];
                        new_counter = 1;
                    }
                if (new_counter > old_counter) val_mode = i[j - 1];
                else if (new_counter == old_counter) val_mode = NO_RESULT;
                return val_mode;
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        /// <summary>
        /// Get the number of data points in this set
        /// </summary>
        /// <returns></returns>
        public int length()
        {
            return list.Length;
        }

        /// <summary>
        /// Get the minimum value
        /// </summary>
        /// <returns></returns>
        public double min()
        {
            double minimum = double.PositiveInfinity;
            for (int i = 0; i <= list.Length - 1; i++)
                if (list[i] < minimum) minimum = list[i];
            return minimum;
        }

        /// <summary>
        /// Get the maximum value
        /// </summary>
        /// <returns></returns>
        public double max()
        {
            double maximum = double.NegativeInfinity;
            for (int i = 0; i <= list.Length - 1; i++)
                if (list[i] > maximum) maximum = list[i];
            return maximum;
        }

        /// <summary>
        /// First quartile
        /// </summary>
        /// <returns></returns>
        public double Q1()
        {
            return Qi(0.25);
        }

        /// <summary>
        /// Second quartile
        /// </summary>
        /// <returns></returns>
        public double Q2()
        {
            return Qi(0.5);
        }

        /// <summary>
        /// Third quartile
        /// </summary>
        /// <returns></returns>
        public double Q3()
        {
            return Qi(0.75);
        }

        /// <summary>
        /// Compute the mean
        /// </summary>
        /// <returns></returns>
        public double mean()
        {
            try
            {
                double sum = 0;
                for (int i = 0; i <= list.Length - 1; i++)
                    sum += list[i];
                return sum / list.Length;
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        /// <summary>
        /// Compute the range (max - min)
        /// </summary>
        /// <returns></returns>
        public double range()
        {
            double minimum = min();
            double maximum = max();
            return (maximum - minimum);
        }

        public double IQ()
        {
            return Q3() - Q1();
        }

        /// <summary>
        /// Compute the midpoint between the minimum and maximum
        /// </summary>
        /// <returns></returns>
        public double middle_of_range()
        {
            double minimum = min();
            double maximum = max();
            return (minimum + maximum) / 2;
        }

        /// <summary>
        /// Compute the variance
        /// </summary>
        /// <returns></returns>
        public double var()
        {
            try
            {
                double s = 0;
                for (int i = 0; i <= list.Length - 1; i++)
                    s += Math.Pow(list[i], 2);
                return (s - list.Length * Math.Pow(mean(), 2)) / (list.Length); //-1
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        /// <summary>
        /// Compute the standard deviation
        /// </summary>
        /// <returns></returns>
        public double s()
        {
            return Math.Sqrt(Math.Abs(var()));
        }

        public double YULE()
        {
            try
            {
                if (Q3() - Q1() > 0.001)
                    return ((Q3() - Q2()) - (Q2() - Q1())) / (Q3() - Q1());
                else
                    return ((Q3() - Q2()) - (Q2() - Q1())) / (Q3() - Q1() + 1.0);
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public double Z(double member)
        {
            try
            {
                if (exist(member)) return (member - mean()) / s();
                else return NO_RESULT;
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public double cov(Statistics s)
        {
            try
            {
                if (this.length() != s.length()) return NO_RESULT;
                int len = this.length();
                double sum_mul = 0;
                for (int i = 0; i <= len - 1; i++)
                    sum_mul += (this.list[i] * s.list[i]);
                return (sum_mul - len * this.mean() * s.mean()) / (len - 1);
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public static double cov(Statistics s1, Statistics s2)
        {
            try
            {
                if (s1.length() != s2.length()) return NO_RESULT;
                int len = s1.length();
                double sum_mul = 0;
                for (int i = 0; i <= len - 1; i++)
                    sum_mul += (s1.list[i] * s2.list[i]);
                return (sum_mul - len * s1.mean() * s2.mean()) / (len - 1);
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public double r(Statistics design)
        {
            try
            {
                return this.cov(design) / (this.s() * design.s());
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public static double r(Statistics design1, Statistics design2)
        {
            try
            {
                return cov(design1, design2) / (design1.s() * design2.s());
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public double a(Statistics design)
        {
            try
            {
                return this.cov(design) / (Math.Pow(design.s(), 2));
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public static double a(Statistics design1, Statistics design2)
        {
            try
            {
                return cov(design1, design2) / (Math.Pow(design2.s(), 2));
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        public double b(Statistics design)
        {
            return this.mean() - this.a(design) * design.mean();
        }

        public static double b(Statistics design1, Statistics design2)
        {
            return design1.mean() - a(design1, design2) * design2.mean();
        }

        private double Qi(double i)
        {
            try
            {
                double[] j = new double[list.Length];
                list.CopyTo(j, 0);
                sort(j);
                if (Math.Ceiling(list.Length * i) == list.Length * i) return (j[(int)(list.Length * i - 1)] + j[(int)(list.Length * i)]) / 2;
                else return j[((int)(Math.Ceiling(list.Length * i))) - 1];
            }
            catch (Exception)
            {
                return NO_RESULT;
            }
        }

        private void sort(double[] i)
        {
            double[] temp = new double[i.Length];
            merge_sort(i, temp, 0, i.Length - 1);
        }

        private void merge_sort(double[] source, double[] temp, int left, int right)
        {
            int mid;
            if (left < right)
            {
                mid = (left + right) / 2;
                merge_sort(source, temp, left, mid);
                merge_sort(source, temp, mid + 1, right);
                merge(source, temp, left, mid + 1, right);
            }
        }

        private void merge(double[] source, double[] temp, int left, int mid, int right)
        {
            int i, left_end, num_elements, tmp_pos;
            left_end = mid - 1;
            tmp_pos = left;
            num_elements = right - left + 1;
            while ((left <= left_end) && (mid <= right))
            {
                if (source[left] <= source[mid])
                {
                    temp[tmp_pos] = source[left];
                    tmp_pos++;
                    left++;
                }
                else
                {
                    temp[tmp_pos] = source[mid];
                    tmp_pos++;
                    mid++;
                }
            }
            while (left <= left_end)
            {
                temp[tmp_pos] = source[left];
                left++;
                tmp_pos++;
            }
            while (mid <= right)
            {
                temp[tmp_pos] = source[mid];
                mid++;
                tmp_pos++;
            }
            for (i = 1; i <= num_elements; i++)
            {
                source[right] = temp[right];
                right--;
            }
        }

        private bool exist(double member)
        {
            bool is_exist = false;
            int i = 0;
            while (i <= list.Length - 1 && !is_exist)
            {
                is_exist = (list[i] == member);
                i++;
            }
            return is_exist;
        }

        public override string ToString()
        {
            string str = "mean:  " + this.mean() + "\n";
            str += "stdev: " + this.s() + "\n";
            str += "IQ:    " + this.IQ() + "\n";
            str += "min:   " + this.min() + "\n";
            str += "max:   " + this.max() + "\n";
            return str;
        }

    }
}
