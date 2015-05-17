using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ParallelDataProcessing
{
    public class DataSource
    {
        private readonly int[] _dataCollection;
        int _currentIdx;
        private readonly Random _rng;

        public DataSource(
            IEnumerable<int> dataCollection)
        {
            _dataCollection = dataCollection.ToArray();
            _currentIdx = 0;
            _rng = new Random();
        }

        public bool HasMoreData
        {
            get
            {
                return _currentIdx != (_dataCollection.Length - 1);
            }
        }

        public int Next()
        {
            Thread.Sleep(50);
            return _dataCollection[_currentIdx++];
        }   

    }
}
