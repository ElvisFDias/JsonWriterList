using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonWriterList
{
    public static class JsonIOHelper
    {
        public static void ValidateFiles(List<string> filesList)
        {
            var filesNotFound = new List<string>();

            foreach (var filePath in filesList)
            {
                if (!File.Exists(filePath))
                    filesNotFound.Add(filePath);
            }

            if (filesNotFound.Any())
            {
                throw new FileNotFoundException($"Files: {string.Join(",", filesNotFound)}");
            }
        }
    }
}
