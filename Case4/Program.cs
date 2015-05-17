using ParallelDataProcessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace Case4
{
    class Program
    {

        static void Main(string[] args)
        {
    CancellationTokenSource cts = new CancellationTokenSource();

    var validator = new Validator();
            
    // setup the execution blocks
    var validationBlock = new TransformBlock<int, ValidationResult>(x => validator.Validate(x));            
    var mulitplexBlock = new BroadcastBlock<ValidationResult>(
        x => new ValidationResult(x.IsValid, x.ValidatedData, x.Errors.ToList()));
    var transformToValidatedDataBlock = new TransformBlock<ValidationResult, int>(x => x.ValidatedData);
    var bufferDataUntilCompletionBlock = new BatchBlock<int>(int.MaxValue);
    var saveDataToDatabaseBlock = new ActionBlock<int[]>(SaveDataToDatabase);
    var transformToErrorsBlock = new TransformBlock<ValidationResult, string[]>(x => x.Errors.ToArray());
    var bufferErrorsUntilCompletionBlock = new BatchBlock<string[]>(int.MaxValue);
    var saveErrorsToLogBlock = new ActionBlock<string[][]>(SaveToErrorLog);

    // link the blocks together
    validationBlock.LinkTo(mulitplexBlock);

    mulitplexBlock.LinkTo(transformToValidatedDataBlock, x => x.IsValid);
    transformToValidatedDataBlock.LinkTo(bufferDataUntilCompletionBlock);
    bufferDataUntilCompletionBlock.LinkTo(saveDataToDatabaseBlock);
            
    mulitplexBlock.LinkTo(transformToErrorsBlock, x => !x.IsValid);
    transformToErrorsBlock.LinkTo(bufferErrorsUntilCompletionBlock);
    bufferErrorsUntilCompletionBlock.LinkTo(saveErrorsToLogBlock);
            
    // Meh: there must be a better way of propagating completion
    validationBlock.Completion.ContinueWith(x => mulitplexBlock.Complete());
    mulitplexBlock.Completion.ContinueWith(x => transformToValidatedDataBlock.Complete());
    mulitplexBlock.Completion.ContinueWith(x => transformToErrorsBlock.Complete());
    transformToErrorsBlock.Completion.ContinueWith(x => bufferErrorsUntilCompletionBlock.Complete());
    bufferErrorsUntilCompletionBlock.Completion.ContinueWith(x => saveErrorsToLogBlock.Complete());
    transformToValidatedDataBlock.Completion.ContinueWith(x => bufferDataUntilCompletionBlock.Complete());
    bufferDataUntilCompletionBlock.Completion.ContinueWith(x => saveDataToDatabaseBlock.Complete());
            
    Console.WriteLine("Starting on threadId:{0}", Thread.CurrentThread.ManagedThreadId);

    Console.WriteLine("Starting::{0}", DateTime.Now);
            
    var dataSource = new DataSource(Enumerable.Range(1, 100));

    while (dataSource.HasMoreData)
    {
        var item = dataSource.Next();
        validationBlock.Post(item);
    }

    validationBlock.Complete();

    Console.WriteLine("Finished::{0}", DateTime.Now);

    Console.ReadKey();
        }

        private static async Task SaveToErrorLog(string[][] obj)
        {
            var errorLog = new ErrorLog();

            obj.SelectMany(x => x)
                .ToList()
                .ForEach(errorLog.Add);

            using (var file = File.AppendText("errors.txt"))
            {
                await errorLog.Flush(file);
            }
        }

        private static async Task SaveDataToDatabase(int[] obj)
        {
            var db = new DestinationRepository();

            foreach (var item in obj)
            {
                db.Add(item);
            }

            await db.SaveChangesAsync();
        }
    }
}
