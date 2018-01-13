﻿using System.IO;
using System.Linq;
using Darkages.IO;
using Newtonsoft.Json;

namespace Darkages.Storage
{
    public class AreaStorage : IStorage<Area>
    {
        public static string StoragePath;

        static AreaStorage()
        {
            if (ServerContext.StoragePath == null)
                ServerContext.LoadConstants();

            StoragePath = $@"{ServerContext.StoragePath}\areas";

            if (!Directory.Exists(StoragePath))
                Directory.CreateDirectory(StoragePath);
        }

        public Area Load(string Name)
        {
            var path = Path.Combine(StoragePath, string.Format("{0}.json", Name.ToLower()));

            if (!File.Exists(path))
                return null;

            using (var s = File.OpenRead(path))
            using (var f = new StreamReader(s))
            {
                return JsonConvert.DeserializeObject<Area>(f.ReadToEnd(), StorageManager.Settings);
            }
        }

        public void Save(Area obj)
        {
            var path = Path.Combine(StoragePath, string.Format("{0}.json", obj.Name.ToLower()));
            var objString = JsonConvert.SerializeObject(obj, StorageManager.Settings);
            File.WriteAllText(path, objString);
        }

        public void CacheFromStorage()
        {
            var area_dir = StoragePath;
            if (!Directory.Exists(area_dir))
                return;
            var area_names = Directory.GetFiles(area_dir, "*.json", SearchOption.TopDirectoryOnly);

            foreach (var area in area_names)
            {
                var mapObj = StorageManager.AreaBucket.Load(Path.GetFileNameWithoutExtension(area));
                var mapFile = Directory.GetFiles($@"{ServerContext.StoragePath}\maps", $"lod{mapObj.Number}.map",
                    SearchOption.TopDirectoryOnly).FirstOrDefault();

                if (mapFile != null && File.Exists(mapFile))
                {
                    mapObj.Data = File.ReadAllBytes(mapFile);
                    mapObj.Hash = Crc16Provider.ComputeChecksum(mapObj.Data);
                    StorageManager.AreaBucket.Save(mapObj);

                    //mapObj.Script = ScriptManager.Load<MapScript>(mapObj.ScriptKey, mapObj);
                    mapObj.OnLoaded();


                    ServerContext.GlobalMapCache[mapObj.Number] = mapObj;
                }
            }
        }
    }
}