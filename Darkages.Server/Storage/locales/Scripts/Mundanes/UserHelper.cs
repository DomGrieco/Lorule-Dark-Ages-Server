using System.Collections.Generic;
using System.Linq;
using Darkages.Common;
using Darkages.Network.Game;
using Darkages.Network.ServerFormats;
using Darkages.Scripting;
using Darkages.Types;

namespace Darkages.Storage.locales.Scripts.Mundanes
{
    [Script("User Helper Menu")]
    public class UserHelper : MundaneScript
    {
        public UserHelper(GameServer server, Mundane mundane) : base(server, mundane)
        {
        }

        public override void OnClick(GameServer server, GameClient client)
        {
            client.SendOptionsDialog(Mundane, "What do you need?",
                new OptionsDataItem(0x0001, "Warp"));
        }

        public override void OnResponse(GameServer server, GameClient client, ushort responseID, string args)
        {
            switch (responseID)
            {
                case 0x0001:
                {
                    var opts = new List<OptionsDataItem>();

                    var map = ServerContext.GlobalWarpTemplateCache;
                    var warp_names = map.Select(i => i)
                        .Where(i => i != null && i.WarpType == WarpType.Map && i.To != null);

                    foreach (var warp in warp_names)
                        opts.Add(new OptionsDataItem(
                            (short) warp.To.AreaID, warp.Name));

                    client.SendOptionsDialog(Mundane, "You may travel to these location.",
                        opts.ToArray()
                    );
                }
                    break;
                default:

                    if (responseID > 0)
                    {
                        var key = responseID;

                        if (ServerContext.GlobalMapCache.ContainsKey(key))
                        {
                            var warp = ServerContext.GlobalWarpTemplateCache
                                .Where(i => i.Activations.Count(p => p.AreaID == key) > 0).FirstOrDefault();

                            if (warp != null)
                            {
                                client.TransitionToMap(warp.ActivationMapId, 
                                    warp.Activations.RandomElement()?.Location ??
                                    warp.Activations.FirstOrDefault().Location);
                            }
                            else
                            {
                                client.SendMessage(0x02, "You can't travel there at this time.");
                            }
                        }
                    }

                    break;
            }
        }

        public override void OnGossip(GameServer server, GameClient client, string message)
        {
        }

        public override void TargetAcquired(Sprite Target)
        {
        }
    }
}