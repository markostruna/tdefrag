using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TDefragWpf.Properties;

namespace TDefragLib
{
    public class FragmentCollection : IEnumerable<Fragment>
    {
        public FragmentCollection()
        {
            _fragments = new List<Fragment>();
        }


        public void Add(Int64 logicalClusterNumber, UInt64 virtualClusterNumber, UInt64 length, Boolean isVirtual)
        {
            if (logicalClusterNumber < 0)
                throw new InvalidDataException("LCN numbers cannot be negative");

            _fragments.Add(new Fragment((UInt64)logicalClusterNumber, virtualClusterNumber, length, isVirtual));
        }

        public Fragment FindContaining(UInt64 virtualClusterNumber)
        {
            Fragment foundFragment = _fragments.First(x => (x.IsLogical && x.NextVirtualClusterNumber > virtualClusterNumber));

            return foundFragment;
        }


        public UInt64 LogicalClusterNumber
        {
            get
            {
                UInt64 retValue = _fragments.FirstOrDefault(x => x.IsLogical).LogicalClusterNumber;

                return retValue;
            }
        }

        public int FragmentCount
        {
            get
            {
                int count =
                    (from fr in _fragments
                     where fr.IsLogical == true && fr.NextLogicalClusterNumber != 0
                     select fr).Count();

                return count;
            }
        }

        public UInt64 TotalLength
        {
            get 
            {
                UInt64 sum = (UInt64)
                    (from f in _fragments
                    where f.IsLogical == true
                    select f.Length).Sum(x => Convert.ToDecimal(x));

                return sum;
            }
        }

        private IList<Fragment> _fragments;

        #region IEnumerable<Fragment> Members

        public IEnumerator<Fragment> GetEnumerator()
        {
            return _fragments.GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
