﻿using Darkages.Types;
using System;
using System.Threading.Tasks;

namespace Darkages.Network.Game.Components
{
    public class MundaneComponent : GameServerComponent
    {
        public GameServerTimer Timer { get; set; }

        public MundaneComponent(GameServer server) : base(server)
        {
            Timer = new GameServerTimer(TimeSpan.FromSeconds(ServerContext.Config.MundaneRespawnInterval));
        }

        public override void Update(TimeSpan elapsedTime)
        {
            Timer.Update(elapsedTime);

            if (Timer.Elapsed)
            {
                SpawnMundanes();
                Timer.Reset();
            }
        }

        public void SpawnMundanes()
        {
            foreach (var mundane in ServerContext.GlobalMundaneTemplateCache)
            {
                if (mundane.Value == null || mundane.Value.AreaID == 0)
                    continue;

                if (!ServerContext.GlobalMapCache.ContainsKey(mundane.Value.AreaID))
                    continue;

                var map = ServerContext.GlobalMapCache[mundane.Value.AreaID];

                if (map == null || !map.Ready)
                    continue;

                var npc = GetObject<Mundane>(i => i.CurrentMapId == map.ID && i.Template != null
                    && i.Template.Name == mundane.Value.Name);

                if (npc != null && npc.CurrentHp > 0)
                    continue;

                new TaskFactory().StartNew(() => 
                {
                    Mundane.Create(mundane.Value);
                });
            }
        }
    }
}