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
    public class JsonWriterListBkp
    {
        private JsonWriter jsonWriter;
        private StreamWriter streamWriterJsonFile;
        private readonly ConcurrentQueue<object> objectQueue = new ConcurrentQueue<object>();
        private readonly JsonSerializer serializer = new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore };
        private readonly TimeSpan flushInterval;
        private string jsonDirectoryPath, jsonFilePath;
        private readonly CancellationToken token;
        private readonly Type typeList;
        private readonly string propertyName;
        private readonly SemaphoreSlim semaphoreControl = new SemaphoreSlim(1);
        private readonly HashSet<string> hashSetId;
        private readonly CancellationTokenSource internalTs;
        private bool started = false;
        private bool publishEnded = false;
        private bool finilized = false;
        private Task queueReaderTask;

        public JsonWriterListBkp(TimeSpan flushInterval, CancellationToken token, string jsonDirectoryPath, Type typeList, string propertyName)
        {
            this.flushInterval = flushInterval;
            this.jsonDirectoryPath = jsonDirectoryPath;
            this.token = token;
            this.typeList = typeList;
            this.propertyName = propertyName;
            this.hashSetId = new HashSet<string>();
            this.internalTs = new CancellationTokenSource();
        }

        public async Task StartAsync()
        {
            if (started) return;

            (jsonWriter, jsonFilePath) = JsonWriterFacory.CreateJsonWriter(jsonDirectoryPath, propertyName);

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

        public void Add(object item)
        {
            objectQueue.Enqueue(item);
        }

        public void Add(object item, string id)
        {
            if (hashSetId.Contains(id)) throw new ArgumentException($"Id already added: {id}");
            var added = false;

            lock (hashSetId)
            {
                added = hashSetId.Add(id);
            }

            if(added)
                this.Add(item);
        }

        public bool Exists(string id)
        {
            return hashSetId.Contains(id);
        }

        public bool IsEmpty()
        {
            return hashSetId.Count == 0;
        }

        public IEnumerable<string> GetIds()
        {
            foreach (var id in hashSetId)
            {
                yield return id;
            }
        }

        public string GetJSonFilePath()
        {
            if (!finilized) return string.Empty;

            return jsonFilePath;
        }

        private async Task EndJSonWriteObject()
        {
            await jsonWriter.WriteEndArrayAsync();

            await jsonWriter.WriteEndObjectAsync();
        }

        private JsonWriter CreateJSONWriter()
        {
            if (!Directory.Exists(jsonDirectoryPath))
                throw new DirectoryNotFoundException($"Path: {jsonDirectoryPath}");

            this.jsonFilePath = Path.Combine(jsonDirectoryPath, $"temporary-{typeList}-{DateTime.Now.Ticks}.json");

            this.streamWriterJsonFile = File.CreateText(jsonFilePath);

            return new JsonTextWriter(streamWriterJsonFile)
            {
                Formatting = Formatting.Indented
            };
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
                catch (TaskCanceledException){}
                

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

        private void JSonWriteObject(object item)
        {
            serializer.Serialize(jsonWriter, item);
        }
    }
}
