using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Brain_uwp.Data
{
    /// <summary>
    /// Basic Template for storing two types together
    /// </summary>
    /// <typeparam name="T1">Type 1</typeparam>
    /// <typeparam name="T2">Type 2</typeparam>
	public class Pair<T1, T2>
	{
        /// <summary>
        /// Construct a pair with two types
        /// </summary>
        /// <param name="t1">Type 1</param>
        /// <param name="t2">Type 2</param>
		public Pair(T1 t1, T2 t2){
			First = t1;
			Second = t2;
		}

        /// <summary>
        /// Gets the first type
        /// </summary>
		public T1 First { get; set; }

        /// <summary>
        /// Gets the second type
        /// </summary>
		public T2 Second { get; set; }
	}
}
