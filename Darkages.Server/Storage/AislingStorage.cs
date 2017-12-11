﻿using System.IO;
using Newtonsoft.Json;

namespace Darkages.Storage
{
    public class AislingStorage : IStorage<Aisling>
    {
        public static string StoragePath = $@"{ServerContext.STORAGE_PATH}\aislings";

        static AislingStorage()
        {
            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        public Aisling Load(string Name)
        {
            var path = Path.Combine(StoragePath, string.Format("{0}.json", Name.ToLower()));

            if (!File.Exists(path))
                return null;

            using (var s = File.OpenRead(path))
            using (var f = new StreamReader(s))
                return JsonConvert.DeserializeObject<Aisling>(f.ReadToEnd(), StorageManager.Settings);
        }

        public void Save(Aisling obj)
        {
            var path = Path.Combine(StoragePath, string.Format("{0}.json", obj.Username.ToLower()));
            var objString = JsonConvert.SerializeObject(obj, StorageManager.Settings);
            File.WriteAllText(path, objString);
        }
    }
}