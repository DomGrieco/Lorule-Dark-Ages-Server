using Darkages.Network.Game;
using Darkages.Scripting;
using Darkages.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Darkages.Storage.locales.Scripts.Global
{
    [Script("Tutorial", "Dean")]
    public class Tutorial : GlobalScript
    {
        private readonly GameClient client;

        public Tutorial(GameClient client) : base(client)
        {
            this.client = client;
        }

        public override void OnDeath(GameClient client, TimeSpan elapsedTime)
        {

        }

        public override void Update(TimeSpan elapsedTime)
        {
            if (client != null && client.Aisling != null && client.Aisling.LoggedIn)
            {
                if (client.Aisling.CurrentMapId == ServerContext.Config.StartingMap)
                {
                    var quest = client.Aisling.Quests.FirstOrDefault(i => i.Name == "awakening");

                    if (quest == null)
                        quest = CreateQuest(quest);


                    quest.HandleQuest(client);

                    if (!quest.Completed && client.Aisling.Y >= 11)
                    {
                        client.Aisling.Y = 10;
                        client.SendMessage(0x02, "You hear walkers outside. you better find some equipment first.");
                        client.Refresh();
                    }
                    else
                    {
                        if (client.Aisling.Position.DistanceFrom(2, 2) <= 1)
                        {
                            if (!quest.Completed)
                            {
                                var item = Item.Create(client.Aisling, ServerContext.GlobalItemTemplateCache["Dirk"]);
                                item.GiveTo(client.Aisling, true);

                                client.SendMessage(0x02, "You pick up some gear from the chest.");
                            }
                        }
                    }
                }
            }
        }

        private Quest CreateQuest(Quest quest)
        {
            quest = new Quest { Name = "awakening" };
            quest.LegendRewards.Add(new Legend.LegendItem
            {
                Category = "Event",
                Color = (byte)LegendColor.White,
                Icon = (byte)LegendIcon.Community,
                Value = "A Spiritual Awakening"
            });
            quest.ExpReward  = 50;
            quest.GoldReward = 1000;
            quest.ItemRewards.Add(ServerContext.GlobalItemTemplateCache[client.Aisling.Gender == Gender.Male ? "Shirt" : "Blouse"]);

            client.Aisling.Quests.Add(quest);
            quest.QuestStages = new List<QuestStep<Template>>();

            var q2 = new QuestStep<Template> { Type = QuestType.ItemHandIn };

            q2.Prerequisites.Add(new QuestRequirement
            {
                Type = QuestType.HasItem,
                Amount = 1,
                TemplateContext = ServerContext.GlobalItemTemplateCache["Dirk"]
            });

            quest.QuestStages.Add(q2);

            return quest;
        }
    }
}
