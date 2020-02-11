using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace JsonWriterList
{
    public class JsonGroupFiles
    {
        private readonly TimeSpan flushTIme;
        private readonly JsonSerializer serializer;
        private readonly JsonConverterObjectToJSonReader jsonConverterObjectToReader;
        private readonly SemaphoreSlim writerSemapho = new SemaphoreSlim(1);
        private JsonWriter jsonWriter;

        public JsonGroupFiles(TimeSpan flushTIme)
        {
            this.flushTIme = flushTIme;
            this.serializer = new JsonSerializer();
            this.jsonConverterObjectToReader = new JsonConverterObjectToJSonReader();
        }

        public async Task<string> GroupFiles(List<string> filesList, string destinationDirectoryPath, string fileName)
        {
            return await GroupFiles(filesList, destinationDirectoryPath, fileName, false, null, null);
        }

        public async Task<string> GroupFiles(List<string> filesList, string destinationDirectoryPath, string rootFileName, bool deleteFiles, object objectBase, string rootPropertyName)
        {
            if(
                (objectBase != null && string.IsNullOrEmpty(rootPropertyName)) ||
                (objectBase == null && !string.IsNullOrEmpty(rootPropertyName))
                )
            {
                throw new ArgumentException("ObjectBase and RootPropertyName can not be null ");
            }

            JsonIOHelper.ValidateFiles(filesList);

            var isThereObjectBase = objectBase != null && !string.IsNullOrWhiteSpace(rootPropertyName);
            var ts = new CancellationTokenSource();
            var createWriterResult = JsonWriterFacory.CreateJsonWriter(destinationDirectoryPath, rootFileName, Formatting.None);
            var objectBaseReader = await jsonConverterObjectToReader.ConvertObjectToJSonReaderAsync(objectBase);

            using (this.jsonWriter = createWriterResult.writer)
            {
                var flushTask = Task.Run(() => FlushWriter(ts.Token));
                var lastTokenType = JsonToken.None;
                var lastPropertyname = string.Empty;

                while (objectBaseReader?.Read() ?? false)
                {

                    await WriteTokenAsync(objectBaseReader, false);

                    if (lastTokenType == JsonToken.PropertyName && lastPropertyname == rootPropertyName)
                    {
                        break;
                    }

                    lastTokenType = objectBaseReader.TokenType;
                    lastPropertyname = objectBaseReader.Value?.ToString();
                }


                if (!isThereObjectBase)
                {
                    await WriteStartObjectAsync();
                }


                foreach (var jsonFilePath in filesList)
                {

                    using (var jsonReader = new JsonTextReader(File.OpenText(jsonFilePath)))
                    {
                        var countStartEndObject = 0;

                        while (jsonReader.Read())
                        {
                            if (jsonReader.TokenType == JsonToken.StartObject) countStartEndObject++;

                            if (jsonReader.TokenType == JsonToken.EndObject) countStartEndObject--;

                            if (jsonReader.TokenType == JsonToken.StartObject && countStartEndObject == 1) continue;

                            if (jsonReader.TokenType == JsonToken.EndObject && countStartEndObject == 0) continue;

                            WriteToken(jsonReader, jsonReader.TokenType == JsonToken.StartArray);
                        }
                    }

                    if (deleteFiles)
                    {
                        File.Delete(jsonFilePath);
                    }
                }

                if (!isThereObjectBase)
                {
                    await WriteEndObjectAsync();
                }

                while (objectBaseReader?.Read() ?? false)
                {

                    await WriteTokenAsync(objectBaseReader, false);

                }

                ts.Cancel();

                flushTask.Wait();

                await jsonWriter?.FlushAsync();

                return createWriterResult.filePath;
            }
        }

        private async Task WriteTokenAsync(JsonReader objectBaseReader, bool writeChildren)
        {
            try
            {
                await writerSemapho.WaitAsync();

                await jsonWriter.WriteTokenAsync(objectBaseReader, writeChildren);
                
            }
            finally
            {
                writerSemapho.Release();
            }

        }

        private void WriteToken(JsonReader objectBaseReader, bool writeChildren)
        {
            try
            {
                writerSemapho.Wait();
                
                jsonWriter.WriteToken(objectBaseReader, writeChildren);
            }
            finally
            {
                writerSemapho.Release();
            }
            
        }

        private async Task WriteStartObjectAsync()
        {
            try
            {
                await writerSemapho.WaitAsync();

                await jsonWriter.WriteStartObjectAsync();

            }
            finally
            {
                writerSemapho.Release();
            }
            
        }

        private async Task WriteEndObjectAsync()
        {
            try
            {
                await writerSemapho.WaitAsync();

                await jsonWriter.WriteEndObjectAsync();

            }
            finally
            {
                writerSemapho.Release();
            }

        }

        private async Task FlushWriter(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(flushTIme, token);

                    await writerSemapho.WaitAsync();

                    await jsonWriter?.FlushAsync();

                }
                catch (TaskCanceledException){}
                finally
                {
                    writerSemapho.Release();
                }
            }
        }

   
    }
}

