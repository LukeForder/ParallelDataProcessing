using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelDataProcessing
{
    public class ErrorLog
    {
        ConcurrentBag<string> messages = new ConcurrentBag<string>();

        public void Add(string message)
        {
            messages.Add(message);
        }

        public async Task Flush(StreamWriter streamWriter)
        {
            foreach (var message in messages)
            {
                await streamWriter.WriteLineAsync(message);
            }
        }
    }
}
