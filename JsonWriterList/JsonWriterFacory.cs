using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JsonWriterList
{
    public static class JsonWriterFacory
    {
        public static (JsonWriter writer, string filePath) CreateJsonWriter(string destinationDirectoryPath, string rootFileName, Formatting formatting = Formatting.None)
        {
            if (!Directory.Exists(destinationDirectoryPath))
                throw new DirectoryNotFoundException($"Path: {destinationDirectoryPath}");

            var jsonFilePath = Path.Combine(destinationDirectoryPath, $"{Path.GetFileNameWithoutExtension(rootFileName)}-{DateTime.Now.Ticks}.json");

            return 
                (
                    new JsonTextWriter(File.CreateText(jsonFilePath)) { Formatting = formatting, CloseOutput = true}, 
                    jsonFilePath
                 );
        }

        public static JsonWriter CreateJsonWriter(string jsonFilePath, Formatting formatting = Formatting.None)
        {
            if (!Directory.Exists(jsonFilePath))
                throw new DirectoryNotFoundException($"Path: {jsonFilePath}");

            return new JsonTextWriter(File.CreateText(jsonFilePath)) { Formatting = formatting, CloseOutput = true };
        }
    }


}
