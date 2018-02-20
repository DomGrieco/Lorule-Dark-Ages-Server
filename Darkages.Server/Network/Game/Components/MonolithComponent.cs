
using Darkages.Types;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Darkages.Network.Game.Components
{
    public class MonolithComponent : GameServerComponent
    {
        private readonly GameServerTimer _timer;

        public readonly Queue<Spawn> SpawnQueue = new Queue<Spawn>();

        public MonolithComponent(GameServer server)
            : base(server)
        {
            _timer = new GameServerTimer(TimeSpan.FromMilliseconds(ServerContext.Config.GlobalSpawnTimer));
            var spawnThread = new Thread(SpawnEmitter) { IsBackground = true };
            spawnThread.Start();
        }

        private void SpawnEmitter()
        {
            while (true)
            {
                if (SpawnQueue.Count > 0)
                    ConsumeSpawns();

                Thread.Sleep(500);
            }
        }

        private void ConsumeSpawns()
        {
            Spawn spawn;

            lock (SpawnQueue)
            {
                spawn = SpawnQueue.Dequeue();
            }

            if (spawn != null)
            {
                SpawnOn(spawn.Template, spawn.Map);
            }
        }

        public override void Update(TimeSpan elapsedTime)
        {
            _timer.Update(elapsedTime);

            if (_timer.Elapsed)
            {
                _timer.Reset();

                var templates = ServerContext.GlobalMonsterTemplateCache.Values;
                if (templates.Count == 0)
                    return;

                foreach (var map in ServerContext.GlobalMapCache.Values)
                {
                    if (map == null || map.Rows == 0 || map.Cols == 0)
                        return;

                    var temps = templates.Where(i => i.AreaID == map.ID);
                    foreach (var template in temps)
                    {
                        if (template.SpawnOnlyOnActiveMaps && !map.Has<Aisling>())
                            continue;

                        if (template.ReadyToSpawn())
                        {
                            var spawn = new Spawn
                            {
                                Template = template,
                                Map = map
                            };

                            lock (SpawnQueue)
                            {
                                SpawnQueue.Enqueue(spawn);
                            }
                        }
                    }
                }
            }
        }

        public async void SpawnOn(MonsterTemplate template, Area map)
        {
            var count = GetObjects<Monster>(i => i.Template.Name == template.Name).Length;

            if (count < Math.Abs(template.SpawnMax))
            {
                if ((template.SpawnType & SpawnQualifer.Random) == SpawnQualifer.Random)
                    await CreateFromTemplate(template, map, template.SpawnSize);
                else if ((template.SpawnType & SpawnQualifer.Defined) == SpawnQualifer.Defined)
                    await CreateFromTemplate(template, map, template.SpawnSize);
            }

        }


        public async Task<bool> CreateFromTemplate(MonsterTemplate template, Area map, int count)
        {
            await Task.Run(() =>
            {
                var newObj = Monster.Create(template as MonsterTemplate, map);
                AddObject(newObj);

                return true;
            });

            return false;
        }

        public class Spawn
        {
            public DateTime LastSpawned { get; set; }
            public MonsterTemplate Template { get; set; }
            public Area Map { get; set; }
        }
    }
}

