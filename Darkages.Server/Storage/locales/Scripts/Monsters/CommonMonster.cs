﻿using System;
using System.Collections.Generic;
using System.Linq;
using Darkages.Network.Game;
using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Monsters
{
    [Script("Common Monster", "Dean")]
    public class CommonMonster : MonsterScript
    {
        private readonly Random _random = new Random();
        public List<SkillScript> SkillScripts = new List<SkillScript>();
        public List<SpellScript> SpellScripts = new List<SpellScript>();

        public SkillScript DefaultSkill => SkillScripts.Find(i => i.IsScriptDefault) ?? null;
        public SpellScript DefaultSpell => SpellScripts.Find(i => i.IsScriptDefault) ?? null;

        public CommonMonster(Monster monster, Area map)
            : base(monster, map)
        {

            LoadSkillScript("Assail", true);

            if (Monster.Template.SpellScripts != null)
                foreach (var spellscriptstr in Monster.Template.SpellScripts)
                {
                    LoadSpellScript(spellscriptstr);
                }

            if (Monster.Template.SkillScripts != null)
                foreach (var skillscriptstr in Monster.Template.SkillScripts)
                {
                    LoadSkillScript(skillscriptstr);
                }
        }

        private void LoadSkillScript(string skillscriptstr, bool primary = false)
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

        public Sprite Target => Monster.Target;

        public override void OnApproach(GameClient client)
        {
            if (client.Aisling.Dead)
                return;

            if (client.Aisling.Invisible)
                return;
        }

        public override void OnAttacked(GameClient client)
        {
            if (client == null)
                return;

            if (client.Aisling.Dead)
                return;

            Monster.Target = client.Aisling;
        }

        public override void OnCast(GameClient client)
        {
            if (client.Aisling.Dead)
                return;

            Monster.Target = client.Aisling;
        }

        public override void OnClick(GameClient client)
        {
            client.SendMessage(0x02,
                Monster.Template.Name +
                $"(Lv {Monster.Template.Level}, HP: {Monster.CurrentHp}/{Monster.MaximumHp}, AC: {Monster.Ac}, O: {Monster.OffenseElement}, D: {Monster.DefenseElement})");
        }

        public override void OnDeath(GameClient client)
        {
            if (Monster.Target != null)
                if (Monster.Target is Aisling)
                    Monster.GenerateRewards(Monster.Target as Aisling);


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

            UpdateTarget();

            Monster.BashTimer.Update(elapsedTime);
            Monster.CastTimer.Update(elapsedTime);
            Monster.WalkTimer.Update(elapsedTime);

            if (Monster.BashTimer.Elapsed)
            {
                Monster.BashTimer.Reset();

                if (Monster.BashEnabled)
                    Bash();
            }

            if (Monster.CastTimer.Elapsed)
            {
                Monster.CastTimer.Reset();

                if (Monster.CastEnabled)
                    CastSpell();
            }

            if (Monster.WalkTimer.Elapsed)
            {
                Monster.WalkTimer.Reset();

                if (Monster.WalkEnabled)
                    Walk();
            }
        }

        private void UpdateTarget()
        {
            if (Monster.Target != null)
            {
                if (Monster.Target is Aisling)
                    if (((Aisling) Monster.Target).Invisible)
                        ClearTarget();

                if (Monster.Target?.CurrentHp == 0)
                    ClearTarget();
            }
            else
            {
                if (Monster.Aggressive)
                {
                    if (Monster.Target != null && Monster.Target is Monster || Monster.Target == null)
                    {
                        Monster.Target = GetObjects(i => i.Serial != Monster.Serial
                        && i.WithinRangeOf(Monster) && i.CurrentHp > 0,
                        Monster.Template.MoodType == MoodQualifer.VeryAggressive ? Get.Aislings : Get.Monsters | Get.Aislings)
                                          .OrderBy(v => v.Position.DistanceFrom(Monster.Position)).FirstOrDefault();


                        if (Monster.Target != null && Monster.Target is Monster && Monster.AislingsNearby().Length > 0)
                        {
                            Monster.Target = GetObjects(i => i.Serial != Monster.Serial
                            && i.WithinRangeOf(Monster) && i.CurrentHp > 0, Get.Aislings)
                                              .OrderBy(v => v.Position.DistanceFrom(Monster.Position)).FirstOrDefault();

                        }
                    }

                    Monster.WalkEnabled = true;
                }
            }
        }

        private void CastSpell()
        {
            if (Monster.IsFrozen || Monster.IsSleeping || Monster.IsBlind || Monster.IsConfused)
                return;

            if (Monster != null && Monster.Target != null && SpellScripts.Count > 0)
            {
                if (_random.Next(1, 101) < ServerContext.Config.MonsterSpellSuccessRate)
                {
                    var spellidx = _random.Next(SpellScripts.Count);
                    SpellScripts[spellidx].OnUse(Monster, Target);
                }
            }

            if (Monster != null && Monster.Target != null && Monster.Target.CurrentHp > 0)
            {
                DefaultSpell?.OnUse(Monster, Monster.Target);
            }
        }

        private void Walk()
        {
            if (Monster.IsFrozen || Monster.IsSleeping || Monster.IsBlind || Monster.IsConfused)
                return;


            if (Target != null)
            {
                if (Monster.NextTo(Target.X, Target.Y))
                {
                    if (Monster.Facing(Target.X, Target.Y, out var direction))
                    {
                        Monster.BashEnabled = true;
                        Monster.CastEnabled = true;
                    }
                    else
                    {
                        Monster.BashEnabled = false;
                        Monster.CastEnabled = true;
                        Monster.Direction = (byte) direction;
                        Monster.Turn();
                    }
                }
                else
                {
                    Monster.BashEnabled = false;
                    Monster.CastEnabled = true;
                    Monster.WalkTo(Target.X, Target.Y);
                }
            }
            else
            {
                Monster.BashEnabled = false;
                Monster.CastEnabled = false;

                if (Monster.Template.PathQualifer.HasFlag(PathQualifer.Patrol))
                {
                    if (Monster.Template.Waypoints == null)
                        Monster.Wander();
                    else
                    {
                        if (Monster.Template.Waypoints?.Count > 0)
                            Monster.Patrol();
                        else
                            Monster.Wander();
                    }
                }
                else
                {
                    Monster.Wander();
                }
            }
        }

        private void Bash()
        {
            if (Monster.IsFrozen || Monster.IsSleeping || Monster.IsBlind || Monster.IsConfused)
                return;

            var obj = Monster.GetInfront(1);

            if (obj == null)
                return;

            if (Monster.Target != null)
                if (!Monster.Facing(Target.X, Target.Y, out var direction))
                {
                    Monster.Direction = (byte) direction;
                    Monster.Turn();
                }


            if (Target == null || Target.CurrentHp == 0)
            {
                ClearTarget();
                return;
            }

            if (Monster != null && Monster.Target != null && SkillScripts.Count > 0)
            {
                var idx = _random.Next(SkillScripts.Count);

                if (_random.Next(1, 101) < ServerContext.Config.MonsterSkillSuccessRate)
                    SkillScripts[idx].OnUse(Monster);
            }

            if (Monster != null && DefaultSkill != null)
            {
                DefaultSkill.OnUse(Monster);
            }

        }

        private void ClearTarget()
        {
            Monster.CastEnabled = false;
            Monster.BashEnabled = false;
            Monster.WalkEnabled = true;
            Monster.Target = null;
        }
    }
}