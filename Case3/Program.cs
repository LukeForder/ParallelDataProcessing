using ParallelDataProcessing;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using System.Collections.Generic;
using System.Reactive;
using System.Diagnostics;

namespace Case3
{
    class Program
    {
    private static Func<IObserver<int>, IDisposable> DataSourceEnumerator(Func<DataSource> sourceFactory, CancellationToken ct)
    {
        return (observer) =>
        {
            var ds = sourceFactory();
            try
            {
                while (ds.HasMoreData)
                {
                    ct.ThrowIfCancellationRequested();

                    var data = ds.Next();

                    observer.OnNext(data);

                    //Console.WriteLine("Thread({0})::ObservableDataSource[{1}]::EnumerateAsync[data::{2}])", Thread.CurrentThread.ManagedThreadId, this.GetHashCode(), data);

                }
                observer.OnCompleted();
            }
            catch (TaskCanceledException) { }
            catch (Exception e)
            {
                observer.OnError(e);
            }


            return  Disposable.Empty;
        };
    }


    public class RepositoryObserver : IObserver<int>
    {
        ConcurrentBag<int> _data;
        private readonly DestinationRepository _repo;

        public RepositoryObserver(DestinationRepository repo)
        {
            _repo = repo;
            _data = new ConcurrentBag<int>();
        }

        public async void OnCompleted()
        {
            // Console.WriteLine("Thread({0})::RepositoryObserver[{1}]::OnCompleted", Thread.CurrentThread.ManagedThreadId, this.GetHashCode());
            await _repo.SaveChangesAsync();
        }

        public void OnError(Exception error)
        {
            // TODO: log 
        }

        public void OnNext(int value)
        {
            // Console.WriteLine("Thread({0})::RepositoryObserver[{1}]::OnNext({2})", Thread.CurrentThread.ManagedThreadId, this.GetHashCode(), value);
            _repo.Add(value);
        }
    }

    public class LoggingObserver : IObserver<string>
    {
        private readonly ErrorLog _errorLog;
        private readonly string _logFilePath;

        public LoggingObserver(string logFilePath)
        {
            _logFilePath = logFilePath;

            _errorLog = new ErrorLog();
        }

        public async void OnCompleted()
        {
            using (var errorLogFile = File.AppendText(_logFilePath))
            {
                await _errorLog.Flush(errorLogFile);
            }

            // Console.WriteLine("Thread({0})::LoggingObserver[{1}]::OnCompleted()", Thread.CurrentThread.ManagedThreadId, this.GetHashCode());
        }

        public void OnError(Exception error)
        {
            // TODO: log etc
        }

        public void OnNext(string value)
        {
            _errorLog.Add(value);

            //  Console.WriteLine("Thread({0})::LoggingObserver[{1}]::OnNext({2})", Thread.CurrentThread.ManagedThreadId, this.GetHashCode(), value);
        }
    }

        static void Main(string[] args)
        {
    CancellationTokenSource cts = new CancellationTokenSource();

    var validator = new Validator();

    var dataSource = Observable.Create<int>(DataSourceEnumerator(() => new DataSource(Enumerable.Range(1, 100)), cts.Token));

    var repository = new RepositoryObserver(new DestinationRepository());
    var errorLog = new LoggingObserver("errors.txt");

    var multicastConnectable =  
        Observable.Publish( 
            dataSource
            // run the validation on a separate thread from the data read
            .ObserveOn(NewThreadScheduler.Default)
            .Select(i => validator.Validate(i)));
            
    multicastConnectable
        .Where(x => x.IsValid)
        .Select(x => x.ValidatedData)
        .Subscribe(repository);
            
    multicastConnectable
        .Where(x => !x.IsValid)
        .SelectMany(x => x.Errors)
        .Subscribe(errorLog);

    Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);

    Console.WriteLine("Starting::{0}", DateTime.Now);
    // run the observable
    multicastConnectable.Connect();
    Console.WriteLine("Finished::{0}", DateTime.Now);

    Console.ReadKey();
        }
    }
}
