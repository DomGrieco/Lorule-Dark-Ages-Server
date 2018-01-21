﻿using Darkages.Network.Game;
using Darkages.Scripting;
using Darkages.Types;
using System;
using System.Collections.Generic;

namespace Darkages.Storage.locales.Scripts.Monsters
{
    [Script("Tower Defense", "Dean")]
    public class TowerDefense : MonsterScript
    {

        public TowerDefense(Monster monster, Area map)
            : base(monster, map)
        {
            Monster.Template.SpawnSize = 100;
            Monster.Template.SpawnMax  = 100;
            Monster.Template.SpawnRate = 1;
        }

        public override void OnApproach(GameClient client)
        {
            Monster.Target = client.Aisling;
        }

        public override void OnAttacked(GameClient client)
        {

        }

        public override void OnCast(GameClient client)
        {

        }

        public override void OnClick(GameClient client)
        {

        }

        public override void OnDeath(GameClient client)
        {
            var remaining = GetObjects<Monster>(i => i.CurrentMapId == client.Aisling.AreaID
                && i.Template.Name == Monster.Template.Name).Length;

            if (remaining <= 1)
            {

                var temp = Monster.Template;
                temp.Image += 2;
                temp.MovementSpeed = 300;
                temp.MaximumHP *= 2;
                temp.SpawnMax++;
                temp.SpawnRate--;

                temp.SpawnSize++;

                ServerContext.GlobalMonsterTemplateCache.Remove(temp.Name);
                ServerContext.GlobalMonsterTemplateCache[temp.Name] = temp;

                client.SendMessage(0x02, string.Format("[Difficulty: {0}] Creeps get stronger ...", temp.Level));
            }

            if (GetObject<Monster>(i => i.Serial == Monster.Serial) != null)
                DelObject(Monster);
        }

        public override void OnLeave(GameClient client)
        {
            Monster.Target = null;
        }

        public override void Update(TimeSpan elapsedTime)
        {
            if (!Monster.IsAlive) return;

            Monster.WalkTimer.Update(elapsedTime);

            if (Monster.WalkTimer.Elapsed)
            {
                Monster.WalkTimer.Reset();

                if (Monster.WalkEnabled)
                    Walk();
            }
        }

        private void Walk()
        {
            if (Monster.IsFrozen || Monster.IsSleeping || Monster.IsBlind || Monster.IsConfused)
                return;

            if (Monster.Template.Waypoints.Count > 0)
                Monster.Patrol();

            if (Monster.CurrentMapId == 510 && Monster.Position.DistanceFrom(new Position(0, 12)) <= 1)
            {
                Monster.CurrentHp = 0;

                if (Monster.Target != null)
                {
                    Monster.Target.ApplyDamage(Monster, Monster.Target.MaximumHp / 10, true, 88);

                }
            }
        }
    }
}