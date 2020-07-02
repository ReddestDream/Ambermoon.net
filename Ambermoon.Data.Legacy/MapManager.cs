﻿using System.Collections.Generic;

namespace Ambermoon.Data.Legacy
{
    public class MapManager : IMapManager
    {
        readonly Dictionary<uint, Map> maps = new Dictionary<uint, Map>();

        public MapManager(IGameData gameData, IMapReader mapReader)
        {
            // Map 1-256 -> File 1
            // Map 300-369 -> File 2
            // Map 257-299, 400-455, 513-528 -> File 3
            for (int i = 1; i <= 3; ++i)
            {
                var file = gameData.Files[$"{i}Map_data.amb"];

                foreach (var mapFile in file.Files)
                {
                    uint index = (uint)mapFile.Key;
                    maps.Add(index, Map.Load(index, mapReader, mapFile.Value));
                }
            }
        }

        public Map GetMap(uint index) => maps[index];
    }
}
