using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GmshCommon
{
    public class HashBucket<T>
    {
        private Dictionary<T, int> m_record;

        public HashBucket()
        {
            m_record = new Dictionary<T, int>();
        }

        public HashBucket(IEqualityComparer<T> comparer)
        {
            m_record = new Dictionary<T, int>(comparer);

        }

        public void Add(T v)
        {
            if (m_record.ContainsKey(v))
            {
                m_record[v]++;
            }
            else
            {
                m_record[v] = 0;
            }
        }

        public List<T> GetUnique()
        {
            var unique = new List<T>();
            foreach (var kvp in m_record)
            {
                if (kvp.Value == 0)
                {
                    unique.Add(kvp.Key);
                }
            }

            return unique;
        }
    }
}
