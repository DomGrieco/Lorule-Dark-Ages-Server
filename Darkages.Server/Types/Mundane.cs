﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Darkages.Common;
using Darkages.Network.Game;
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Newtonsoft.Json;

namespace Darkages.Types
{
    public class Mundane : Sprite
    {
        public MundaneTemplate Template { get; set; }

        [JsonIgnore] public MundaneScript Script { get; set; }

        [JsonIgnore]
        public List<SkillScript> SkillScripts = new List<SkillScript>();

        [JsonIgnore]
        public List<SpellScript> SpellScripts = new List<SpellScript>();

        [JsonIgnore]
        public SkillScript DefaultSkill => SkillScripts.Find(i => i.IsScriptDefault) ?? null;

        [JsonIgnore]
        public SpellScript DefaultSpell => SpellScripts.Find(i => i.IsScriptDefault) ?? null;


        public void InitMundane()
        {
            if (Template.Spells != null)
                foreach (var spellscriptstr in Template.Spells)
                {
                    LoadSpellScript(spellscriptstr);
                }

            if (Template.Skills != null)
                foreach (var skillscriptstr in Template.Skills)
                {
                    LoadSkillScript(skillscriptstr);
                }

            LoadSkillScript("Assail", true);
        }

        public static void Create(MundaneTemplate template)
        {
            if (template == null)
                return;

            var existing = template.GetObject<Mundane>(p => p.Template != null && p.Template.Name == template.Name);

            //this npc was already created?
            if (existing != null)
                if (existing.CurrentHp == 0)
                    existing.OnDeath();
                else
                    return;


            var npc = new Mundane();
            npc.Template = template;

            if (npc.Template.TurnRate == 0)
                npc.Template.TurnRate = 5;

            if (npc.Template.CastRate == 0)
                npc.Template.CastRate = 2;

            if (npc.Template.WalkRate == 0)
                npc.Template.WalkRate = 2;


            npc.CurrentMapId = npc.Template.AreaID;
            lock (Generator.Random)
            {
                npc.Serial = Generator.GenerateNumber();
            }

            npc.X = template.X;
            npc.Y = template.Y;
            npc._MaximumHp = (int)(template.Level / 0.1 * 15);
            npc._MaximumMp = (int)(template.Level / 0.1 * 5);
            npc.Template.MaximumHp = npc.MaximumHp;
            npc.Template.MaximumMp = npc.MaximumMp;

            npc.CurrentHp = npc.Template.MaximumHp;
            npc.CurrentMp = npc.Template.MaximumMp;
            npc.Direction = npc.Template.Direction;
            npc.CurrentMapId = npc.Template.AreaID;

            //calculate what ac to give depending on level.
            npc.BonusAc = (sbyte)(70 - 101 / 70 * template.Level);

            if (npc.BonusAc > ServerContext.Config.BaseAC)
                npc.BonusAc = ServerContext.Config.BaseAC;

            npc.DefenseElement = Generator.RandomEnumValue<ElementManager.Element>();
            npc.OffenseElement = Generator.RandomEnumValue<ElementManager.Element>();

            npc.Script = ScriptManager.Load<MundaneScript>(template.ScriptKey, ServerContext.Game, npc);

            npc.Template.AttackTimer = new GameServerTimer(TimeSpan.FromMilliseconds(450));
            npc.Template.EnableTurning = false;
            npc.Template.WalkTimer  = new GameServerTimer(TimeSpan.FromSeconds(npc.Template.WalkRate));
            npc.Template.ChatTimer  = new GameServerTimer(TimeSpan.FromSeconds(Generator.Random.Next(20, 45)));
            npc.Template.TurnTimer  = new GameServerTimer(TimeSpan.FromSeconds(npc.Template.TurnRate));
            npc.Template.SpellTimer = new GameServerTimer(TimeSpan.FromSeconds(npc.Template.CastRate));
            npc.InitMundane();
            npc.AddObject(npc);
        }

        public void LoadSkillScript(string skillscriptstr, bool primary = false)
        {
            var script = ScriptManager.Load<SkillScript>(skillscriptstr,
                Skill.Create(1, ServerContext.GlobalSkillTemplateCache[skillscriptstr]));


            if (script != null)
            {
                script.IsScriptDefault = primary;
                SkillScripts.Add(script);
            }
        }

        private void LoadSpellScript(string spellscriptstr, bool primary = false)
        {
            var script = ScriptManager.Load<SpellScript>(spellscriptstr,
                Spell.Create(1, ServerContext.GlobalSpellTemplateCache[spellscriptstr]));


            if (script != null)
            {
                script.IsScriptDefault = primary;
                SpellScripts.Add(script);
            }
        }

        public void OnDeath()
        {
            Map.Update(X, Y, TileContent.None);

            RemoveActiveTargets();

            if (CurrentHp == 0)
                new TaskFactory().StartNew(() =>
                {
                    Thread.Sleep(1000);
                    Remove<Mundane>();
                });
        }

        private void RemoveActiveTargets()
        {
            var nearbyMonsters = GetObjects<Monster>(i => WithinRangeOf(this));
            foreach (var nearby in nearbyMonsters)
                if (nearby.Target != null && nearby.Target.Serial == Serial)
                {
                    nearby.Target = null;
                }
        }

        public void Update(TimeSpan update)
        {
            if (Template == null)
                return;

            if (Template.ChatTimer != null)
            {
                Template.ChatTimer.Update(update);

                if (Template.ChatTimer.Elapsed)
                {
                    var nearby = GetObjects<Aisling>(i => i.WithinRangeOf(this));
                    var idx = 0;
                    foreach (var obj in nearby)
                    {
                        lock (Generator.Random)
                        {
                            idx = Generator.Random.Next(Template.Speech.Count);
                        }

                        if (Template.Speech.Count > 0)
                            obj.Show(Scope.Self,
                                new ServerFormat0D
                                {
                                    Serial = Serial,
                                    Text = Template.Name + ": " + Template.Speech[idx],
                                    Type = 0x00
                                });
                    }


                    Template.ChatTimer.Reset();
                }
            }

            if (Template.EnableTurning)
            {
                if (Template.TurnTimer != null)
                {
                    Template.TurnTimer.Update(update);
                    if (Template.TurnTimer.Elapsed)
                    {
                        lock (Generator.Random)
                        {
                            Direction = (byte)Generator.Random.Next(0, 4);
                        }

                        Turn();

                        Template.TurnTimer.Reset();
                    }
                }
            }

            if (Template.EnableCasting)
            {
                Template.SpellTimer.Update(update);

                if (Template.SpellTimer.Elapsed)
                {

                    if (Target == null || Target.CurrentHp == 0 || !Target.WithinRangeOf(this))
                    {

                        var targets = GetObjects<Monster>(i => i.WithinRangeOf(this))
                               .OrderBy(i => i.Position.DistanceFrom(Position));

                        foreach (var t in targets) t.Target = this;

                        var target = Target == null ? targets.FirstOrDefault() : Target;

                        if (target?.CurrentHp == 0)
                            target = null;

                        if (IsFrozen || IsSleeping || IsBlind || IsConfused)
                            return;

                        Target = target;
                    }

                    if (Target != null && Target != null && SpellScripts.Count > 0)
                    {
                        var idx = 0;
                        lock (Generator.Random)
                        {
                            idx = Generator.Random.Next(SpellScripts.Count);
                        }

                        SpellScripts[idx].OnUse(this, Target);
                    }

                    Template.SpellTimer.Reset();
                }

            }

            if (Template.AttackTimer != null && Template.EnableWalking)
            {
                Template.AttackTimer.Update(update);
                if (Template.AttackTimer.Elapsed)
                {
                    var targets = GetObjects<Monster>(i => i.WithinRangeOf(this))
                        .OrderBy(i => i.Position.DistanceFrom(Position));

                    foreach (var t in targets) t.Target = this;

                    var target = Target == null ? targets.FirstOrDefault() : Target;

                    if (target?.CurrentHp == 0)
                        target = null;

                    if (target != null)
                    {
                        Script?.TargetAcquired(target);

                        if (!Position.IsNextTo(target.Position))
                        {
                            WalkTo(target.X, target.Y);
                        }
                        else
                        {
                            if (!Facing(target, out var direction))
                            {
                                Direction = (byte) direction;
                                Turn();
                            }
                            else
                            {
                                target.Target = this;
                                DefaultSkill?.OnUse(this);


                                if (SkillScripts.Count > 0 && target.Target != null)
                                {
                                    var idx = 0;
                                    lock (Generator.Random)
                                    {
                                        idx = Generator.Random.Next(SkillScripts.Count);
                                    }

                                    SkillScripts[idx]?.OnUse(this);
                                }
                            }
                        }
                    }
                    else
                    {
                        Wander();
                    }

                    Template.AttackTimer.Reset();
                }
            }
        }
    }
}