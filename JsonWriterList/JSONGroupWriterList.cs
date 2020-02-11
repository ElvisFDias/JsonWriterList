using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JsonWriterList
{
    public class JsonGroupWriterList
    {
        private readonly TimeSpan flushInterval;
        private readonly string jsonDirectoryPath;
        private readonly CancellationToken token;
        private readonly Dictionary<Type, JsonWriterListBkp> contentsDic = new Dictionary<Type, JsonWriterListBkp>();
        private readonly object objectLockSync = new object();

        public JsonGroupWriterList(TimeSpan flushInterval, CancellationToken token, string jsonDirectoryPath)
        {
            this.flushInterval = flushInterval;
            this.jsonDirectoryPath = jsonDirectoryPath;
            this.token = token;
            this.contentsDic = new Dictionary<Type, JsonWriterListBkp>();
        }


        public async Task RegisterContentList(Type typeList, string propertyName)
        {
            lock (objectLockSync)
            {
                if (!contentsDic.ContainsKey(typeList))
                {

                    contentsDic.Add(typeList, new JsonWriterListBkp(flushInterval, token, jsonDirectoryPath, typeList, propertyName));

                }
            }

            await contentsDic[typeList].StartAsync();
        }

        public void Add(Type typeList, object item, string id)
        {
            contentsDic[typeList].Add(item, id);
        }

        public void Add(Type typeList, object item)
        {
            contentsDic[typeList].Add(item);
        }

        public bool Exists(string id)
        {

            foreach (var dicType in contentsDic)
            {
                if (dicType.Value.Exists(id))
                    return true;
            }

            return false;
        }

        public bool Exists(Type typeList, string id)
        {
            return contentsDic[typeList].Exists(id);
        }

        public IEnumerable<string> GetIds(Type typeList, string replaceId)
        {
            if(string.IsNullOrEmpty(replaceId))
                return contentsDic[typeList].GetIds();

            return contentsDic[typeList].GetIds().Select(x => x.Replace(replaceId, ""));
        }

        public bool IsEmpty()
        {
            foreach (var dicType in contentsDic)
            {
                if (!dicType.Value.IsEmpty())
                    return false;
            }

            return true;
        }

        public async Task<List<string>> WaitFlushFinish()
        {
            var fileList = new List<string>();

            foreach (var listItem in contentsDic)
            {
                await listItem.Value.WaitFlushFinish();

                fileList.Add(listItem.Value.GetJSonFilePath());
            }

            return fileList;
        }
        

    }
}
