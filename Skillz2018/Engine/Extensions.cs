using Pirates;
using System.Collections.Generic;
using System.Linq;
using MyBot.Engine;

namespace MyBot.Engine
{
    public static class Extensions
    {
        /// <summary>
        /// Extension function for power
        /// </summary>
        /// <param name="i"></param>
        /// <param name="p"></param>
        /// <returns>i^p</returns>
        public static double Power(this int i, double p)
        {
            return System.Math.Pow(i, p);
        }
        /// <summary>
        /// Checks if a number is in range
        /// </summary>
        /// <param name="num">Number in question</param>
        /// <param name="bound1">First range bound</param>
        /// <param name="bound2">Second range bound</param>
        /// <returns>Boolean indicating whether the number is in the given range</returns>
        public static bool IsBetween(this int num, int bound1, int bound2)
        {
            if (bound1 > bound2)
                return num >= bound2 && num <= bound1;
            else
                return num >= bound1 && num <= bound2;
        }
        /// <summary>
        /// Checks if a number is in range
        /// </summary>
        /// <param name="num">Number in question</param>
        /// <param name="bound1">First range bound</param>
        /// <param name="bound2">Second range bound</param>
        /// <returns>Boolean indicating whether the number is in the given range</returns>
        public static bool IsBetween(this double num, double bound1, double bound2)
        {
            if (bound1 > bound2)
                return num >= bound2 && num <= bound1;
            else
                return num >= bound1 && num <= bound2;
        }
        /// <summary>
        /// checks whether a collection is empty
        /// </summary>
        /// <typeparam name="T">Collection type</typeparam>
        /// <param name="collection">Collection in question</param>
        /// <returns>Boolean indicating whether the collection is empty</returns>
        public static bool IsEmpty<T>(this IEnumerable<T> collection)
        {
            return !collection.Any();
        }

        /// <summary>
        /// Returns the first object by a specified selector function
        /// </summary>
        /// <typeparam name="T">Collection type</typeparam>
        /// <typeparam name="U">Ordering function output</typeparam>
        /// <param name="collection">Collection in question</param>
        /// <param name="f">Ordering function (A selector to a type which is easily comparable)</param>
        /// <returns>A single object which is the first object in relation to the sorting function (or else null)</returns>
        public static T FirstBy<T,U>(this IEnumerable<T> collection, System.Func<T,U> f) where U : System.IComparable
        {
            return collection.OrderBy(f).FirstOrDefault();
        }

        /// <summary>
        /// Returns the nearest object to a specified location
        /// </summary>
        /// <typeparam name="T">Collection type, must inherit from MapObject</typeparam>
        /// <param name="collection">Collection in question</param>
        /// <param name="loc">Anchor location</param>
        /// <returns>A single object which is the closest object to the location (or else null)</returns>
        public static T Nearest<T> (this IEnumerable<T> collection, MapObject loc) where T : MapObject
        {
            return FirstBy<T, int>(collection, x => x.Distance(loc));
        }

        /// <summary>
        /// Applies a transformation on an object if a condition is satisfied
        /// </summary>
        /// <typeparam name="T">Collection type</typeparam>
        /// <param name="obj">The object to apply the transformation on</param>
        /// <param name="condition">The condition which to satisfy</param>
        /// <param name="f">Transformation to apply on object</param>
        /// <returns>If the condition was satisfied returns transformed object; Else returns original object</returns>
        public static T Transform<T>(this T obj, bool condition, System.Func<T, T> f)
        {
            if (condition)
                return f(obj);
            else return obj;
        }
        /// <summary>
        /// Invokes a function on an object (While returning the object)
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="obj">The object to invoke the function on</param>
        /// <param name="f">Function to invoke on the object</param>
        /// <returns>Original object</returns>
        public static T WithSideEffect<T>(this T obj, System.Func<T, object> f)
        {
            f(obj);
            return obj;
        }
        /// <summary>
        /// Returns first element in the list
        /// </summary>
        /// <typeparam name="T">List Type</typeparam>
        /// <param name="collection">The collection</param>
        /// <param name="def">Default value (if collection is empty)</param>
        /// <returns></returns>
        public static T FirstOr<T>(this IEnumerable<T> collection, T def) where T : System.IComparable
        {
            if (collection.IsEmpty()) return def;
            else return collection.First();
        }
        /// <summary>
        /// Sorts an array by two parts, main sorting done by first selector and inner sorting by second selector
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <typeparam name="W"></typeparam>
        /// <param name="collection">Collection to sort</param>
        /// <param name="FirstSelector">Outer sorting</param>
        /// <param name="SecondSelector">Inner sorting</param>
        /// <returns>A double sorted collection</returns>
        public static IEnumerable<T> DoubleSort<T, U, W>(this IEnumerable<T> collection,
            System.Func<T, U> FirstSelector, System.Func<T, W> SecondSelector) where U : System.IComparable where W : System.IComparable
        {
            IComparer<T> Comparator = new DoubleSortComparator<T, U, W>(FirstSelector, SecondSelector);
            return collection.OrderBy(x => x, Comparator);
        }
        private class DoubleSortComparator<T, U, W> : IComparer<T> where W : System.IComparable where U : System.IComparable
        {
            private System.Func<T, U> FirstSelector;
            private System.Func<T, W> SecondSelector;
            public DoubleSortComparator(System.Func<T, U> FirstSelector, System.Func<T, W> SecondSelector)
            {
                this.FirstSelector = FirstSelector;
                this.SecondSelector = SecondSelector;
            }

            public int Compare(T x, T y)
            {
                if (FirstSelector(x).CompareTo(FirstSelector(y)) == 0)
                    return SecondSelector(x).CompareTo(y);
                return FirstSelector(x).CompareTo(FirstSelector(y));
            }
        }

        /// <summary>
        /// Joins an array of strings
        /// </summary>
        /// <param name="arr">The string array which to join</param>
        /// <param name="delimiter">A delimiter between each to entries</param>
        /// <returns>A string composed of array values seperated with the delimiter</returns>
        public static string Join(this string[] arr, string delimiter)
        {
            string str = "";
            for (int i = 0; i < arr.Length; i++)
            {
                str += arr[i];
                if (i < arr.Length - 1)
                    str += delimiter;
            }
            return str;
        }
        /// <summary>
        /// Repeats a string
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="multiplier">Amount of times to repeat</param>
        /// <returns>The repeated string</returns>
        public static string Multiply(this string source, int multiplier)
        {
            string r = "";
            for (int i = 0; i < multiplier; i++)
            {
                r += source;
            }
            return r;
        }

        /// <summary>
        /// Finds the middle of a collection of MapObjects
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection">Locations</param>
        /// <returns>Middle of all locations</returns>
        public static Location Middle<T>(this IEnumerable<T> collection) where T : MapObject
        {
            return (collection.Count() > 0) ?
            new Location(collection.Select(x => x.GetLocation().Row).Sum() / collection.Count(), collection.Select(x => x.GetLocation().Col).Sum() / collection.Count())
                : new Location(0,0);
        }
    }
}
