using System;
using System.Diagnostics;
using System.Collections.Generic;

namespace TDefragLib.FileSystem.Ntfs
{
    [DebuggerDisplay("Streams: {Count}")]
    public class StreamCollection : IEnumerable<Stream>
    {
        public StreamCollection()
        {
            StreamList = new List<Stream>();
        }

        public void Add(Stream newStream)
        {
            StreamList.Insert(0, newStream);
        }

        public int Count { get { return StreamList.Count; } }

        private IList<Stream> StreamList { get; set; }

        #region IEnumerable<Stream> Members

        public IEnumerator<Stream> GetEnumerator()
        {
            return StreamList.GetEnumerator();
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
