﻿using Ambermoon.Data.Enumerations;
using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    public class UIGraphics
    {
        readonly Dictionary<UIGraphic, Graphic> entries = new Dictionary<UIGraphic, Graphic>();

        public IReadOnlyDictionary<UIGraphic, Graphic> Entries => entries;

        internal UIGraphics(IDataReader dataReader)
        {
            var graphicReader = new GraphicReader();
            var graphicInfo = new GraphicInfo
            {
                Width = 16,
                Height = 6,
                Alpha = false,
                GraphicFormat = GraphicFormat.Palette3Bit,
                PaletteOffset = 24
            };

            Graphic ReadGraphic(IDataReader dataReader)
            {
                var graphic = new Graphic();

                graphicReader.ReadGraphic(graphic, dataReader, graphicInfo);

                return graphic;
            }

            dataReader.Position = 156;
            entries.Add(UIGraphic.DisabledOverlay16x6, ReadGraphic(dataReader));
            graphicInfo.Height = 16;
            // window frames
            entries.Add(UIGraphic.FrameUpperLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLowerLeft, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameTop, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameBottom, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameUpperRight, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameRight, ReadGraphic(dataReader));
            entries.Add(UIGraphic.FrameLowerRight, ReadGraphic(dataReader));

            graphicInfo.Width = 32;
            dataReader.Position = 0x1228; // start at eagle
            graphicInfo.Height = 47;
            entries.Add(UIGraphic.Eagle, ReadGraphic(dataReader));
            graphicInfo.Height = 44;
            entries.Add(UIGraphic.Explosion, ReadGraphic(dataReader));
            graphicInfo.Height = 24;
            entries.Add(UIGraphic.Ouch, ReadGraphic(dataReader));

            dataReader.Position = 0x1EAC; // start at wind chain
            graphicInfo.Height = 15;
            entries.Add(UIGraphic.Windchain, ReadGraphic(dataReader));
            graphicInfo.Height = 32;
            entries.Add(UIGraphic.MonsterEyeInactive, ReadGraphic(dataReader));
            entries.Add(UIGraphic.MonsterEyeActive, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Night, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Dusk, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Day, ReadGraphic(dataReader));
            entries.Add(UIGraphic.Dawn, ReadGraphic(dataReader));

            dataReader.Position = 0x285C; // start at base button shape

            graphicInfo.Height = 17;
            entries.Add(UIGraphic.ButtonFrame, ReadGraphic(dataReader));
            entries.Add(UIGraphic.ButtonFramePressed, ReadGraphic(dataReader));
            graphicInfo.Height = 4;
            entries.Add(UIGraphic.DisabledOverlay32x4, ReadGraphic(dataReader));
            graphicInfo.Height = 32;
            entries.Add(UIGraphic.Compass, ReadGraphic(dataReader));
            graphicInfo.Height = 9;
            entries.Add(UIGraphic.Unknown, ReadGraphic(dataReader));
            graphicInfo.Height = 34;
            entries.Add(UIGraphic.Skull, ReadGraphic(dataReader));
            entries.Add(UIGraphic.EmptyCharacterSlot, ReadGraphic(dataReader));
        }
    }
}
