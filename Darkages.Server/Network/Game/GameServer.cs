using Darkages.Network.Game.Components;
using Darkages.Network.Object;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Darkages.Network.Game
{
    [Serializable]
    public partial class GameServer
    {
        int Frames;
        bool isRunning;

        DateTime lastServerUpdate = DateTime.UtcNow;
        DateTime lastClientUpdate = DateTime.UtcNow;
        TimeSpan ServerUpdateSpan, ClientUpdateSpan;


        public ObjectService ObjectFactory;
        public Dictionary<Type, GameServerComponent> Components;
        public ObjectComponent ObjectPulseController 
            => Components[typeof(ObjectComponent)] as ObjectComponent;

        public GameServer(int capacity)
            : base(capacity)
        {
            Frames = ServerContext.Config.FRAMES;

            ServerUpdateSpan = TimeSpan.FromSeconds(1.0 / Frames);
            ClientUpdateSpan = TimeSpan.FromSeconds(1.0 / (Frames / 2));

            InitializeGameServer();
        }

        private void AutoSave(GameClient client)
        {
            if ((DateTime.UtcNow - client.LastSave)
                .TotalSeconds > ServerContext.Config.SaveRate)
                client.Save();
        }

        private void DoClientWork()
        {
            isRunning = true;
            lastClientUpdate = DateTime.UtcNow;

            while (isRunning)
            {
                var delta = DateTime.UtcNow - lastClientUpdate;
                {
                    ExecuteClientWork(delta);
                    lastClientUpdate = DateTime.UtcNow;
                }
                Thread.Sleep(ClientUpdateSpan);
            }
        }
    

        private void DoServerWork()
        {
            isRunning = true;
            lastServerUpdate = DateTime.UtcNow;

            while (isRunning)
            {
                var delta = DateTime.UtcNow - lastServerUpdate;
                {
                    ExecuteServerWork(delta);
                    lastServerUpdate = DateTime.UtcNow;
                }
                Thread.Sleep(ServerUpdateSpan);
            }
        }

        public void InitializeGameServer()
        {
            ObjectFactory = new ObjectService();

            Components = new Dictionary<Type, GameServerComponent>
            {
                [typeof(MonolithComponent)] = new MonolithComponent(this),
                [typeof(DaytimeComponent)] = new DaytimeComponent(this),
                [typeof(MundaneComponent)] = new MundaneComponent(this),
                [typeof(MessageComponent)] = new MessageComponent(this),
                [typeof(ObjectComponent)] = new ObjectComponent(this),
                [typeof(PingComponent)] = new PingComponent(this),
                [typeof(ServerCacheComponent)] = new ServerCacheComponent(this)
            };


            ThreadPool.QueueUserWorkItem(UpdateConnectedClients);

            Console.WriteLine(Components.Count + " Server Components loaded.");
        }

        public void ExecuteClientWork(TimeSpan elapsedTime)
        {
            UpdateClients(elapsedTime);            
        }

        public void ExecuteServerWork(TimeSpan elapsedTime)
        {
            UpdateAreas(elapsedTime);
            UpdateComponents(elapsedTime);
        }

        private void UpdateComponents(TimeSpan elapsedTime)
        {
            foreach (var component in Components.Values)
            {
                component.Update(elapsedTime);
            }
        }

        private static void UpdateAreas(TimeSpan elapsedTime)
        {
            foreach (var area in ServerContext.GlobalMapCache.Values)
            {
                area.Update(elapsedTime);
            }
        }

        private void UpdateClients(TimeSpan elapsedTime)
        {
            foreach (var client in Clients)
                if (client != null && client.Aisling != null)
                {
                    client.Update(elapsedTime);
                }
        }

        private void UpdateConnectedClients(object state)
        {
            while (true)
            {
                foreach (var client in Clients)
                    if (client != null && client.Aisling != null)
                    {
                        if (client.Aisling.LoggedIn)
                        {
                            ServerContext.Game
                                .ObjectPulseController?
                                .OnObjectUpdate(client.Aisling);

                            client.RefreshObjects();
                        }
                    }

                Thread.Sleep(100);
            }
        }

        public override void ClientConnected(GameClient client)
        {
        }

        public override void ClientDisconnected(GameClient client)
        {
            if (client == null || client.Aisling == null)
                return;

            client.Aisling.LoggedIn = false;

            AutoSave(client);
            client.Aisling.Remove(true);

            base.ClientDisconnected(client);
        }

        public override void Abort()
        {
            base.Abort();

            isRunning = false;
        }

        public override void Start(int port)
        {
            base.Start(port);

            if (isRunning)
                return;


            new TaskFactory().StartNew(DoClientWork);
            new TaskFactory().StartNew(DoServerWork);

            isRunning = true;
        }
    }
}