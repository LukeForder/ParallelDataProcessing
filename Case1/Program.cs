using ParallelDataProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Case1
{
    class Program
    {
        static void Main(string[] args)
        {
    var source = new DataSource(Enumerable.Range(1, 100));
    var validator = new Validator();
    var repository = new DestinationRepository();
    var errorLog = new ErrorLog();

            
    Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Starting::{0}", DateTime.Now);

    while (source.HasMoreData)
    {
        var next = source.Next();

        var validationResult = validator.Validate(next);

        if (validationResult.IsValid)
        {
            repository.Add(validationResult.ValidatedData);
        }
        else
        {
            errorLog.Add(
                string.Format("Error with  value {0}", validationResult.ValidatedData));
        }
    }

    using (var errorLogFile = File.AppendText("errors.txt"))
    {
        Task.WaitAll(
        repository.SaveChangesAsync(),
        errorLog.Flush(errorLogFile));
    }


            Console.WriteLine("Finished::{0}", DateTime.Now);

            Console.ReadKey();
        }
        
    }
}
