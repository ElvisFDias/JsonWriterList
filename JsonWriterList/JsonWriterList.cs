using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace JsonWriterList
{
    public class JsonWriterList<T> : IList<T>
    {
        private JsonWriter jsonWriter;
        private StreamWriter streamWriterJsonFile;
        private readonly ConcurrentQueue<T> objectQueue;
        private int itemsCount;
        private readonly JsonSerializer serializer = new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore };
        private readonly TimeSpan flushInterval;
        private string jsonFilePath;
        private readonly CancellationToken token;
        private readonly string propertyName;
        private readonly SemaphoreSlim semaphoreControl = new SemaphoreSlim(1);
        private readonly HashSet<int> hashSetId;
        private readonly CancellationTokenSource internalTs;
        private bool started = false;
        private bool publishEnded = false;
        private bool finilized = false;
        private Task queueReaderTask;

        public JsonWriterList(TimeSpan flushInterval, CancellationToken token, string jsonFilePath,  string propertyName)
        {
            this.flushInterval = flushInterval;
            this.token = token;
            this.jsonFilePath = jsonFilePath;
            this.propertyName = propertyName;

            this.hashSetId = new HashSet<int>();
            this.internalTs = new CancellationTokenSource();
            this.objectQueue = new ConcurrentQueue<T>();
            this.itemsCount = 0;
        }

        public async Task StartAsync()
        {
            if (started) return;

            jsonWriter = JsonWriterFacory.CreateJsonWriter(jsonFilePath);

            await semaphoreControl.WaitAsync();

            try
            {
                if (started) return;

                await jsonWriter.WriteStartObjectAsync();

                await jsonWriter.WritePropertyNameAsync(propertyName);

                await jsonWriter.WriteStartArrayAsync();

            }
            finally
            {
                started = true;

                this.queueReaderTask = Task.Run(() => QueueReader());

                semaphoreControl.Release();
            }
        }

        private async Task QueueReader()
        {
            var crono = Stopwatch.StartNew();

            while (!token.IsCancellationRequested)
            {

                while (objectQueue.TryDequeue(out var item))
                {
                    JSonWriteObject(item);

                    await AutoFlushTask(crono);
                }

                try
                {
                    await Task.Delay((int)flushInterval.TotalMilliseconds, internalTs.Token);
                }
                catch (TaskCanceledException) { }


                await AutoFlushTask(crono);

                if (publishEnded && objectQueue.IsEmpty) break;
            }
        }

        private async Task AutoFlushTask(Stopwatch crono)
        {
            if (crono.ElapsedMilliseconds > flushInterval.TotalMilliseconds)
            {
                await jsonWriter.FlushAsync();

                crono.Restart();
            }
        }

        public async Task WaitFlushFinish()
        {
            if (finilized) return;

            await semaphoreControl.WaitAsync();

            try
            {
                this.publishEnded = true;

                internalTs.Cancel();

                queueReaderTask.Wait();

                await EndJSonWriteObject();

                await jsonWriter.CloseAsync();

                hashSetId.Clear();
            }
            finally
            {
                finilized = true;

                semaphoreControl.Release();
            }
        }

        private async Task EndJSonWriteObject()
        {
            await jsonWriter.WriteEndArrayAsync();

            await jsonWriter.WriteEndObjectAsync();
        }

        private void JSonWriteObject(T item)
        {
            serializer.Serialize(jsonWriter, item);
        }

        #region implementaion IList<T>
        public T this[int index] { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public int Count => itemsCount;

        public bool IsReadOnly => true;

        public void Add(T item)
        {
            objectQueue.Enqueue(item);

            hashSetId.Add(item.GetHashCode());

            Interlocked.Add(ref itemsCount, 1);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(T item)
        {
            return hashSetId.Contains(item.GetHashCode());
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<T> GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public int IndexOf(T item)
        {
            throw new NotImplementedException();
        }

        public void Insert(int index, T item)
        {
            throw new NotImplementedException();
        }

        public bool Remove(T item)
        {
            throw new NotImplementedException();
        }

        public void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
