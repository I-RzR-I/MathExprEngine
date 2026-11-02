using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;

namespace MathExprEngineTests.Helpers.Concurrency
{
    internal static class WorkerThreads
    {
        public const int OneMegabyte = 1 << 20;

        public const int SixteenMegabytes = 16 << 20;

        public static void RunOnOneMegabyteStack(Action body)
        {
            RunWithStack(OneMegabyte, body);
        }

        public static void RunWithStack(int stackSize, Action body)
        {
            ExceptionDispatchInfo captured = null;
            var thread = new Thread(
                () =>
                {
                    try
                    {
                        body();
                    }
                    catch (Exception ex)
                    {
                        captured = ExceptionDispatchInfo.Capture(ex);
                    }
                },
                stackSize);
            thread.IsBackground = true;

            thread.Start();

            Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(9)), "The " + (stackSize >> 20) + " MB-stack worker did not finish within 9 s.");

            if (captured != null)
                captured.Throw();
        }

        public static void RunTogether(int count, Action<int> body, ConcurrentQueue<Exception> errors)
        {
            using (var barrier = new Barrier(count))
            {
                var threads = new Thread[count];

                for (var t = 0; t < count; t++)
                {
                    var index = t;
                    threads[t] = new Thread(() =>
                    {
                        try
                        {
                            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(20)))
                                throw new TimeoutException("Barrier was not released.");

                            body(index);
                        }
                        catch (Exception ex)
                        {
                            errors.Enqueue(ex);
                        }
                    });
                    threads[t].IsBackground = true;
                }

                foreach (var thread in threads)
                    thread.Start();

                foreach (var thread in threads)
                    Assert.IsTrue(thread.Join(TimeSpan.FromSeconds(50)), "A worker thread did not finish.");
            }
        }

        public static string Describe(ConcurrentQueue<Exception> errors)
        {
            return errors.Count + " error(s). Sample: " + string.Join(" | ", errors.Take(5).Select(e => e.GetType().Name + ": " + e.Message));
        }
    }
}
