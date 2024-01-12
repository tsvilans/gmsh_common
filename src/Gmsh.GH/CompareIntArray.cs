using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmshCommon
{
    /// <summary>
    /// An equality comparer for int triplets (int[3]). Adapted from PrePoMax.
    /// </summary>
    public class CompareIntArray : IEqualityComparer<int[]>
    {
        public bool Equals(int[] x, int[] y)
        {
            if (x.Length != y.Length) return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (x[i] != y[i]) return false;
            }
            return true;
        }

        public int GetHashCode(int[] x)
        {
            unchecked
            {
                int hc = -1817952719;
                hc = (-1521134295) * hc + x[0];
                hc = (-1521134295) * hc + x[1];
                hc = (-1521134295) * hc + x[2];
                return hc;
            }

            //int hash = 23;
            //for (int i = 0; i < x.Length; i++)
            //{
            //    hash = hash * 31 + x[i];
            //}
            //return hash;
        }
    }
}
