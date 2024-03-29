using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using ThreadPriority = System.Threading.ThreadPriority;

namespace Omni.Core
{
	public class DatabaseBehaviour : MonoBehaviour
	{
		private readonly CancellationTokenSource cancellationTokenSource = new();
		private readonly ConcurrentQueue<Task> tasks = new();

		/// <summary>
		/// Enable sequential execution of queries.<br/>
		/// </summary>
		protected virtual bool EnableSequentialExecution { get; } = false;
		/// <summary>
		/// Prevents CPU from being overloaded.<br/>
		/// Wait x milliseconds before executing the next block of queries.<br/>
		/// </summary>
		protected virtual int WhileDelay { get; } = 30;
		/// <summary>
		/// Executes the given query on a thread pool thread.<br/>
		/// Prevents the main thread from being blocked while the query is being executed.<br/>
		/// Fast execution, because a pool of connections is used.<br/>
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
		/// Fast execution, because a pool of connections is used.<br/>
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
		/// Slowest execution, because it waits for the previous query to finish before executing the next one.<br/>
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

		/// <summary>
		/// Call base.Start() in your Start() method.
		/// </summary>
		protected virtual void Start()
		{
			if (EnableSequentialExecution)
			{
				new Thread(() =>
				{
					while (!cancellationTokenSource.IsCancellationRequested)
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
					Name = "DatabaseBehaviour",
					Priority = ThreadPriority.Normal
				}.Start();
			}
		}

		protected virtual void OnApplicationQuit()
		{
			cancellationTokenSource.Cancel();
			cancellationTokenSource.Dispose();
		}
	}
}