﻿using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data
{
    // Note: 3D graphics are loaded through labdata
    public enum GraphicType
    {
        Tileset1,
        Tileset2,
        Tileset3,
        Tileset4,
        Tileset5,
        Tileset6,
        Tileset7,
        Tileset8,
        Player,
        Portrait,
        Item,
        Layout,
        LabBackground,
        Cursor,
        Pics80x80,
        UIElements,
        EventPictures,
        TravelGfx,
        Transports,
        NPC,
        CombatBackground,
        /// <summary>
        /// Farthest battle row (1st)
        /// </summary>
        MonsterTiny,
        /// <summary>
        /// Far battle row (2nd)
        /// </summary>
        MonsterSmall,
        /// <summary>
        /// Middle battle row (3rd)
        /// </summary>
        MonsterNormal,
        /// <summary>
        /// Closest battle row (4th)
        /// </summary>
        MonsterBig
        // TODO ...
    }

    public enum MonsterRow
    {
        Farthest,
        Far,
        Middle,
        Near
    }

    public interface IGraphicProvider
    {
        Dictionary<int, Graphic> Palettes { get; }
        List<Graphic> GetGraphics(GraphicType type);
        CombatBackgroundInfo Get2DCombatBackground(uint index);
        CombatBackgroundInfo Get3DCombatBackground(uint index);
        CombatGraphicInfo GetCombatGraphicInfo(CombatGraphicIndex index);
        MonsterGraphicInfo GetMonsterGraphicInfo(MonsterGraphicIndex index);
        float GetMonsterRowImageScaleFactor(MonsterRow row);
    }
}
