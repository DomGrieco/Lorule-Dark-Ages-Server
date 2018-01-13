﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace Darkages.Types
{
    public class WarpTemplate : Template
    {
        [JsonProperty] public byte LevelRequired { get; set; }

        [JsonProperty] public int WarpRadius { get; set; }

        public List<Warp> Activations { get; set; }
        public Warp To { get; set; }
        public WarpType WarpType { get; set; }

        [JsonProperty] public int ActivationMapId { get; set; }
    }

    public enum WarpType
    {
        Map,
        World
    }
}