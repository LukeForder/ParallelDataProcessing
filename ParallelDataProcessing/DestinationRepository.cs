using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ParallelDataProcessing
{
    public class DestinationRepository
    {
        ConcurrentBag<int> data = new ConcurrentBag<int>();

        public void Add(int i)
        {
            data.Add(i);
        }

        public async Task SaveChangesAsync()
        {
            const string dbName = "db.xml";
            XDocument db = (File.Exists(dbName)) ? XDocument.Load(dbName) : new XDocument(new XElement("database"));

            db.Root.Add(data.Select(x => new XElement("data_item", x.ToString())));

            db.Save(dbName);
        }

    }
}
