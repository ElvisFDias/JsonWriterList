using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace JsonWriterList
{
    public class JsonConverterObjectToJSonReader
    {
        private readonly JsonSerializer serializer;

        public JsonConverterObjectToJSonReader()
        {
            serializer = new JsonSerializer() { NullValueHandling = NullValueHandling.Ignore };
        }

        public async Task<JsonTextReader> ConvertObjectToJSonReaderAsync(object objTarget)
        {
            if (objTarget == null) return null;

            var memoryStream = new MemoryStream();

            var whiter = new JsonTextWriter(new StreamWriter(memoryStream));

            serializer.Serialize(whiter, objTarget);

            await whiter.FlushAsync();

            memoryStream.Position = 0;

            return new JsonTextReader(new StreamReader(memoryStream));

        }
    }
}
