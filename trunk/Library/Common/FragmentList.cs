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
            Fragment foundFragment = null;

            foreach (Fragment fragment in _fragments)
            {
                if (fragment.IsLogical && (fragment.NextVirtualClusterNumber > virtualClusterNumber))
                {
                    foundFragment = fragment;

                    break;
                }
            }

            return foundFragment;
        }


        public UInt64 LogicalClusterNumber
        {
            get
            {
                Fragment fragment = _fragments.FirstOrDefault(x => x.IsLogical);
                if (fragment == null)
                    return 0;
                return fragment.LogicalClusterNumber;
            }
        }

        public int FragmentCount
        {
            get
            {
                int count = 0;
                UInt64 nextLcn = 0;

                foreach (Fragment fragment in _fragments)
                {
                    if (fragment.IsLogical)
                    {
                        if ((nextLcn != 0) && (fragment.LogicalClusterNumber != nextLcn))
                            count++;
                        nextLcn = fragment.NextLogicalClusterNumber;
                    }
                }

                if (nextLcn != 0)
                    count++;

                return count;
            }
        }

        public UInt64 TotalLength
        {
            get 
            {
                UInt64 sum = 0;
                foreach (Fragment fragment in _fragments)
                {
                    if (fragment.IsLogical)
                        sum += fragment.Length;
                }
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
