using ParallelDataProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Concurrent;
using System.Threading;

namespace Case2
{
    class Program
    {
        static void Main(string[] args)
        {
    var validator = new Validator();
    var repository = new DestinationRepository();
    var errorLog = new ErrorLog();

    var partialData = new BlockingCollection<int>();
    var dataSource = new DataSource(Enumerable.Range(1, 100));

    CancellationTokenSource cts = new CancellationTokenSource();

    Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);
    Console.WriteLine("Starting::{0}", DateTime.Now);
    Task.WhenAll(
        Task.Run(
            () =>
            {
                while (dataSource.HasMoreData)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    var data = dataSource.Next();
                    //Console.WriteLine("Thread({0})::GetDataTask", Thread.CurrentThread.ManagedThreadId, data);

                    partialData.Add(data);
                }

                partialData.CompleteAdding();

            },
            cts.Token),
        Task.Run(
            () =>
            {
                while (!partialData.IsCompleted)
                {
                    cts.Token.ThrowIfCancellationRequested();

                    int next;
                    if (!partialData.TryTake(out next))
                        continue;

                    //Console.WriteLine("Thread({0})::ProcessDataTask({1})", Thread.CurrentThread.ManagedThreadId, next);
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

            },
        cts.Token)
    )
    .ContinueWith(t =>
    {
        Console.WriteLine("Thread({0})::FinishingTask()", Thread.CurrentThread.ManagedThreadId);
        using (var errorLogFile = File.AppendText("errors.txt"))
        {
            Task.WaitAll(
            repository.SaveChangesAsync(),
            errorLog.Flush(errorLogFile));
        }
    })
    .Wait();

    Console.WriteLine("Ending::{0}", DateTime.Now);
    Console.ReadKey();
        }
    }
}
