using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Omni.Core
{
    public class DBMSBehaviour : MonoBehaviour
    {
        private readonly ConcurrentQueue<Task> tasks = new();
        /// <summary>
        /// Prevents CPU from being overloaded.<br/>
        /// Wait x milliseconds before executing the next block of queries.<br/>
        /// </summary>
        protected virtual int WhileDelay { get; } = 30;
        /// <summary>
        /// Executes the given query on a thread pool thread.<br/>
        /// Prevents the main thread from being blocked while the query is being executed.<br/>
        /// </summary>
        /// <param name="query">The query to execute.</param>
        protected Task RunAsync(Action query)
        {
            return Task.Run(() =>
            {
                try
                {
                    query?.Invoke();
                }
                catch (Exception e)
                {
                    OmniLogger.PrintError(e);
                }
            });
        }

        /// <summary>
        /// Executes the given query on a thread pool thread.<br/>
        /// Prevents the main thread from being blocked while the query is being executed.<br/>
        /// </summary>
        /// <param name="query">The query to execute.</param>
        protected void Run(Action query)
        {
            Task.Run(() =>
            {
                try
                {
                    query?.Invoke();
                }
                catch (Exception e)
                {
                    OmniLogger.PrintError(e);
                }
            });
        }

        /// <summary>
        /// Executes the given query on a thread pool thread.<br/>
        /// Prevents the main thread from being blocked while the query is being executed.<br/>
        /// Wait for the previous query to finish before executing the next one.<br/>
        /// </summary>
        /// <param name="query"></param>
        protected void RunSequentially(Action query)
        {
            var task = new Task(() =>
            {
                try
                {
                    query?.Invoke();
                }
                catch (Exception e)
                {
                    OmniLogger.PrintError(e);
                }
            });

            tasks.Enqueue(task);
        }

        protected virtual void Start()
        {
            new Thread(() =>
            {
                while (true)
                {
                    if (tasks.Count > 0)
                    {
                        if (tasks.TryDequeue(out Task task))
                        {
                            task.Start();
                            task.Wait();
                        }
                    }
                    else
                    {
                        // Prevents CPU from being overloaded
                        Thread.Sleep(WhileDelay);
                    }
                }
            })
            {
                Name = "DBMSBehaviour",
                Priority = ThreadPriority.Normal
            }.Start();
        }
    }
}