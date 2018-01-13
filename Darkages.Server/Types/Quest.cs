﻿using System;
using System.Collections.Generic;
using System.Linq;
using Darkages.Network.Game;
using Newtonsoft.Json;

namespace Darkages.Types
{
    public enum QuestType
    {
        ItemHandIn = 0,
        KillCount = 1,
        Gossip = 2,
        Boss = 3,
        Legend = 4,
        Accept = 255
    }

    public class Quest
    {
        public List<ItemTemplate> ItemRewards = new List<ItemTemplate>();
        public List<Legend.LegendItem> LegendRewards = new List<Legend.LegendItem>();

        [JsonIgnore] public List<QuestStep<Template>> QuestStages = new List<QuestStep<Template>>();

        public List<SkillTemplate> SkillRewards = new List<SkillTemplate>();
        public List<SpellTemplate> SpellRewards = new List<SpellTemplate>();
        public string Name { get; set; }
        public bool Started { get; set; }
        public bool Completed { get; set; }

        public DateTime TimeStarted { get; set; }
        public DateTime TimeCompleted { get; set; }

        public uint ExpReward { get; set; }
        public uint GoldReward { get; set; }
        public int StageIndex { get; set; }

        public bool Rewarded { get; set; }
        public QuestStep<Template> Current => QuestStages.Count > 0 ? QuestStages[StageIndex] : null;


        public void OnCompleted(Aisling user)
        {
            var completeStages = QuestStages.Where(i => i.StepComplete).SelectMany(i => i.Prerequisites);

            foreach (var step in completeStages)
                if (step.Type == QuestType.ItemHandIn)
                {
                    var obj = user.Inventory.Get(o => o.Template.Name == step.TemplateContext.Name)
                        .FirstOrDefault();

                    if (obj != null && obj.Template.Flags.HasFlag(ItemFlags.QuestRelated))
                        if (step.IsMet(user, b => b(obj.Template)))
                            user.Inventory.RemoveRange(user.Client, obj, step.Amount);
                }


            Completed = true;
            TimeCompleted = DateTime.Now;

            foreach (var items in SkillRewards)
                if (!Skill.GiveTo(user.Client, items.Name))
                {
                    Completed = false;
                    return;
                }

            foreach (var legends in LegendRewards)
                user.LegendBook.AddLegend(new Legend.LegendItem
                {
                    Category = "Quest Reward",
                    Color = legends.Color,
                    Icon = legends.Icon,
                    Value = legends.Value
                });

            Rewarded = true;
        }

        public void UpdateQuest(Aisling user)
        {
            if (StageIndex + 1 < QuestStages.Count)
                StageIndex++;
            else
                OnCompleted(user);
        }


        public void HandleQuest(GameClient client, Dialog menu)
        {
            var valid = false;

            foreach (var stage in QuestStages)
            foreach (var reqs in stage.Prerequisites)
            {
                valid = reqs.IsMet(client.Aisling, i => i(reqs.TemplateContext));
                stage.StepComplete = valid;
            }

            if (valid)
                if (menu.CanMoveNext)
                {
                    menu.MoveNext(client);
                    menu.Invoke(client);
                }
        }
    }


    public class QuestRequirement
    {
        public int Amount { get; set; }
        public Template TemplateContext { get; set; }
        public QuestType Type { get; set; }
        public string Value { get; set; }

        public bool IsMet(Aisling user, Func<Predicate<Template>, bool> predicate)
        {
            if (Type == QuestType.ItemHandIn) return predicate(i => user.Inventory.Has(TemplateContext) >= Amount);

            return false;
        }
    }

    public class QuestStep<T>
    {
        [JsonIgnore] public List<QuestRequirement> Prerequisites
            = new List<QuestRequirement>();

        public QuestType Type { get; set; }

        public bool StepComplete { get; set; }

        public string AcceptedMessage { get; set; }

        public string RejectedMessage { get; set; }
    }
}