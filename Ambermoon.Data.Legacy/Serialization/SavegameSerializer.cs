﻿using Ambermoon.Data.Enumerations;
using System;

namespace Ambermoon.Data.Legacy
{
    public class SavegameSerializer : ISavegameSerializer
    {
        void ReadSaveData(Savegame savegame, IDataReader dataReader)
        {
            savegame.Year = dataReader.ReadWord();
            savegame.Month = dataReader.ReadWord();
            savegame.DayOfMonth = dataReader.ReadWord();
            savegame.Hour = dataReader.ReadWord();
            savegame.Minute = dataReader.ReadWord();
            savegame.CurrentMapIndex = dataReader.ReadWord();
            savegame.CurrentMapX = dataReader.ReadWord();
            savegame.CurrentMapY = dataReader.ReadWord();
            savegame.CharacterDirection = (CharacterDirection)dataReader.ReadWord();

            // Active spells (2 words each)
            // First word: duration in 5 minute chunks. So 120 means 120 * 5 minutes = 600 minutes = 10h
            // Second word: level (e.g. for light: 1 = magic torch, 2 = magic lantern, 3 = magic sun)
            // An active light spell replaces an existing one.
            // The active spells are in fixed spots:
            // - 0: Light (candle)
            // - 1: Magic barrier (shield)
            // - 2: Magic attack (sword)
            // - 3: Anti-magic barrier (star)
            // - 4: Clairvoyance (eye)
            // - 5: Magic map (map)
            foreach (var activeSpellType in Enum.GetValues<ActiveSpellType>())
            {
                var duration = dataReader.ReadWord();

                if (duration == 0)
                {
                    savegame.ActiveSpells[(int)activeSpellType] = null;
                    dataReader.Position += 2;
                }
                else
                    savegame.ActivateSpell(activeSpellType, duration, dataReader.ReadWord());
            }

            dataReader.ReadWord(); // Number of party members. We don't really need it.
            savegame.ActivePartyMemberSlot = dataReader.ReadWord() - 1; // it is stored 1-based

            for (int i = 0; i < 6; ++i)
                savegame.CurrentPartyMemberIndices[i] = dataReader.ReadWord();

            // TODO: Unknown word
            dataReader.ReadWord();
            savegame.TravelType = (TravelType)dataReader.ReadWord();
            savegame.SpecialItemsActive = dataReader.ReadWord();
            savegame.GameOptions = dataReader.ReadWord();
            savegame.HoursWithoutSleep = dataReader.ReadWord();

            // up to 32 transport positions
            for (int i = 0; i < 32; ++i)
            {
                var type = dataReader.ReadByte();

                if (type == 0)
                {
                    savegame.TransportLocations[i] = null;
                    dataReader.Position += 5;
                }
                else
                {
                    var x = dataReader.ReadByte();
                    var y = dataReader.ReadByte();
                    dataReader.Position += 1; // unknown byte
                    var mapIndex = dataReader.ReadWord();

                    savegame.TransportLocations[i] = new TransportLocation
                    {
                        TravelType = (TravelType)type,
                        MapIndex = mapIndex,
                        Position = new Position(x, y)
                    };
                }
            }

            dataReader.Position = 0x0112; // 16 bits for wind gates (1 = active, 0 = broken)
            savegame.WindGatesActive = dataReader.ReadWord();

            dataReader.Position = 0x04FC; // 8 bytes (64 bits) for every map starting at theoretical index 0
            // each bit stands for a map variable. order is 76543210 FECDBA98 ...
            for (int i = 0; i < 529; ++i)
            {
                savegame.MapEventBits[i] = dataReader.ReadQword();
            }

            dataReader.Position = 0x35a4;
            savegame.ChestUnlockStates = dataReader.ReadBytes(64); // 64 * 8 bits = 512 bits (1 for each chest, possible chest indices 0 to 511)

            dataReader.Position = 0x35e4;
            Buffer.BlockCopy(dataReader.ReadBytes(6), 0, savegame.BattlePositions, 0, 6);

            savegame.TileChangeEvents.Clear();

            while (dataReader.Position < dataReader.Size)
            {
                var mapIndex = dataReader.ReadWord();

                if (mapIndex == 0) // end
                    break;

                var x = dataReader.ReadByte();
                var y = dataReader.ReadByte();
                var tileIndex = dataReader.ReadWord();

                if (mapIndex < 0x0300 && tileIndex < (1 << 11)) // should be a real map and a valid front tile index (11 bit value)
                {
                    savegame.TileChangeEvents.SafeAdd(mapIndex, new ChangeTileEvent
                    {
                        Type = MapEventType.ChangeTile,
                        Index = uint.MaxValue,
                        MapIndex = mapIndex,
                        X = x,
                        Y = y,
                        FrontTileIndex = tileIndex,
                        BackTileIndex = 0
                    });
                }
            }

            // TODO: load other data from Party_data.sav
        }

        public void Read(Savegame savegame, SavegameFiles files)
        {
            var partyMemberReader = new Characters.PartyMemberReader();
            var chestReader = new ChestReader();
            var merchantReader = new MerchantReader();
            // TODO automap reader

            savegame.PartyMembers.Clear();
            savegame.Chests.Clear();
            savegame.Merchants.Clear();
            // TODO automaps

            foreach (var partyMemberDataReader in files.PartyMemberDataReaders.Files)
            {
                partyMemberDataReader.Value.Position = 0;
                savegame.PartyMembers.Add(PartyMember.Load(partyMemberReader, partyMemberDataReader.Value));
            }
            foreach (var chestDataReader in files.ChestDataReaders.Files)
            {
                chestDataReader.Value.Position = 0;
                savegame.Chests.Add(Chest.Load(chestReader, chestDataReader.Value));
            }
            foreach (var merchantDataReader in files.MerchantDataReaders.Files)
            {
                merchantDataReader.Value.Position = 0;
                savegame.Merchants.Add(Merchant.Load(merchantReader, merchantDataReader.Value));
            }
            // TODO automaps

            files.SaveDataReader.Position = 0;
            ReadSaveData(savegame, files.SaveDataReader);
        }

        public void Write(Savegame savegame, SavegameFiles files)
        {
            // TODO
            throw new NotImplementedException();
        }

        /// <summary>
        /// Loads a savegame from a legacy game data save slot.
        /// </summary>
        /// <param name="saveSlot">0 to 10 where 0 is the default save game.</param>
        public static Savegame Load(ISavegameSerializer savegameSerializer, IGameData gameData, int saveSlot)
        {
            var savegame = new Savegame();
            var savegameFiles = new SavegameFiles
            {
                SaveDataReader = gameData.Files[$"Save.{saveSlot:00}/Party_data.sav"].Files[1],
                PartyMemberDataReaders = gameData.Files[$"Save.{saveSlot:00}/Party_char.amb"],
                ChestDataReaders = gameData.Files[$"Save.{saveSlot:00}/Chest_data.amb"],
                MerchantDataReaders = gameData.Files[$"Save.{saveSlot:00}/Merchant_data.amb"],
                AutomapDataReaders = gameData.Files[$"Save.{saveSlot:00}/Automap.amb"]
            };

            savegameSerializer.Read(savegame, savegameFiles);

            return savegame;
        }

        public static Savegame LoadInitial(ISavegameSerializer savegameSerializer, IGameData gameData)
        {
            var savegame = new Savegame();
            var savegameFiles = new SavegameFiles
            {
                SaveDataReader = gameData.Files["Initial/Party_data.sav"].Files[1],
                PartyMemberDataReaders = gameData.Files["Initial/Party_char.amb"],
                ChestDataReaders = gameData.Files["Initial/Chest_data.amb"],
                MerchantDataReaders = gameData.Files["Initial/Merchant_data.amb"],
                AutomapDataReaders = gameData.Files["Initial/Automap.amb"]
            };

            savegameSerializer.Read(savegame, savegameFiles);

            return savegame;
        }
    }
}
