﻿using Ambermoon.Data.GameDataRepository.Util;
using Ambermoon.Data.Legacy.Serialization;
using Ambermoon.Data.Serialization;
using System;
using System.ComponentModel.DataAnnotations;
using static Ambermoon.Data.Monster;

namespace Ambermoon.Data.GameDataRepository.Data
{
    public enum MonsterAnimation
    {
        /// <summary>
        /// Played when the monster is standing (and moving).
        /// </summary>
        Stand,
        /// <summary>
        /// Played when the monster attacks with a close-ranged weapon.
        /// </summary>
        ShortRangeAttack,
        /// <summary>
        /// Played when the monster attacks with a long-ranged weapon.
        /// </summary>
        LongRangeAttack,
        /// <summary>
        /// Played when the monster casts a spell.
        /// </summary>
        SpellCast,
        /// <summary>
        /// Played when the monster receives damage.
        /// </summary>
        ReceiveDamage,
        /// <summary>
        /// Played just before the monster dies.
        /// </summary>
        Die,
        /// <summary>
        /// Played when the monster appears (like Nera or Ancient Guard transformations).
        /// </summary>
        Appear,
        /// <summary>
        /// Not used in Ambermoon. And technically can't be used by the original code. So never use it!
        /// </summary>
        Invalid
    }

    public enum MonsterAnimationProgression
    {
        Cycle,
        Wave
    }

    public class MonsterData : BattleCharacterData, IIndexedData
    {
        private uint _morale = 0;
        private uint _defeatExperience = 0;
        private uint _combatGraphicIndex = 0;
        private uint _originalFrameWidth = 0;
        private uint _originalFrameHeight = 0;
        private uint _displayFrameWidth = 0;
        private uint _displayFrameHeight = 0;
        private readonly Animation[] _animations = new Animation[8];
        private readonly MonsterAnimationProgression[] _animationProgressions = new MonsterAnimationProgression[8];

        #region Serialization
        public static IIndexedData Deserialize(IDataReader dataReader, uint index, bool advanced)
        {
            var monsterData = (MonsterData)Deserialize(dataReader, advanced);
            monsterData.Index = index;
            return monsterData;
        }

        public static IData Deserialize(IDataReader dataReader, bool advanced)
        {
            if (dataReader.ReadByte() != (byte)CharacterType.Monster)
                throw new InvalidDataException("The given data is no valid monster data.");

            void SkipBytes(int amount) => dataReader.Position += amount;

            var monsterData = new MonsterData();

            monsterData.Gender = (Gender)dataReader.ReadByte();
            monsterData.Race = (Race)dataReader.ReadByte();
            monsterData.Class = (Class)dataReader.ReadByte();
            monsterData.SpellMastery = (SpellTypeMastery)dataReader.ReadByte();
            monsterData.Level = dataReader.ReadByte();
            SkipBytes(6);
            monsterData.CombatGraphicIndex = dataReader.ReadByte();
            SkipBytes(2);
            monsterData.Morale = dataReader.ReadByte();
            monsterData.SpellTypeImmunity = (SpellTypeImmunity)dataReader.ReadByte();
            monsterData.AttacksPerRound = dataReader.ReadByte();
            monsterData.BattleFlags = (BattleFlags)dataReader.ReadByte();
            monsterData.Element = (CharacterElement)dataReader.ReadByte();
            SkipBytes(4);
            monsterData.Gold = dataReader.ReadWord();
            monsterData.Food = dataReader.ReadWord();
            SkipBytes(2);
            monsterData.Conditions = (Condition)dataReader.ReadWord();
            monsterData.DefeatExperience = dataReader.ReadWord();
            SkipBytes(8);
            for (int i = 0; i < 8; i++)
            {
                var attribute = monsterData.Attributes[(Attribute)i];
                attribute.CurrentValue = dataReader.ReadWord();
                attribute.MaxValue = dataReader.ReadWord();
                attribute.BonusValue = dataReader.ReadSignedWord();
                attribute.StoredValue = dataReader.ReadWord();
            }
            monsterData.Age = dataReader.ReadWord();
            monsterData.MaxAge = dataReader.ReadWord();
            SkipBytes(4);
            if (advanced)
            {
                monsterData.BonusSpellDamage = dataReader.ReadWord();
                monsterData.BonusMaxSpellDamage = dataReader.ReadWord();
                monsterData.BonusSpellDamageReduction = dataReader.ReadSignedWord();
                monsterData.BonusSpellDamagePercentage = dataReader.ReadSignedWord();
            }
            else
            {
                SkipBytes(8);
            }
            for (int i = 0; i < 10; i++)
            {
                var skill = monsterData.Skills[(Skill)i];
                skill.CurrentValue = dataReader.ReadWord();
                skill.MaxValue = dataReader.ReadWord();
                skill.BonusValue = dataReader.ReadSignedWord();
                skill.StoredValue = dataReader.ReadWord();
            }
            var hitPoints = monsterData.HitPoints;
            hitPoints.CurrentValue = dataReader.ReadWord();
            hitPoints.MaxValue = dataReader.ReadWord();
            hitPoints.BonusValue = dataReader.ReadSignedWord();
            var spellPoints = monsterData.SpellPoints;
            spellPoints.CurrentValue = dataReader.ReadWord();
            spellPoints.MaxValue = dataReader.ReadWord();
            spellPoints.BonusValue = dataReader.ReadSignedWord();
            monsterData.BaseDefense = dataReader.ReadWord();
            int bonusDefense = dataReader.ReadSignedWord(); // TODO: check against equipment
            monsterData.BaseAttackDamage = dataReader.ReadWord();
            int bonusAttackDamage = dataReader.ReadSignedWord(); // TODO: check against equipment
            monsterData.MagicAttackLevel = dataReader.ReadSignedWord();
            monsterData.MagicDefenseLevel = dataReader.ReadSignedWord();
            SkipBytes(16);
            monsterData.LearnedSpellsHealing = dataReader.ReadDword();
            monsterData.LearnedSpellsAlchemistic = dataReader.ReadDword();
            monsterData.LearnedSpellsMystic = dataReader.ReadDword();
            monsterData.LearnedSpellsDestruction = dataReader.ReadDword();
            monsterData.LearnedSpellsType5 = dataReader.ReadDword();
            monsterData.LearnedSpellsType6 = dataReader.ReadDword();
            monsterData.LearnedSpellsFunctional = dataReader.ReadDword();
            SkipBytes(4);
            monsterData.Name = dataReader.ReadString(16).TrimEnd('\0', ' ');

            #region Equipment and Items
            monsterData._equipment = DataCollection<ItemSlotData>.Deserialize(dataReader, EquipmentSlotCount, advanced);
            monsterData._items = DataCollection<ItemSlotData>.Deserialize(dataReader, InventorySlotCount, advanced);

            // TODO
            /*uint calculatedBonusDefense = Util.Util.CalculateItemPropertySum(monsterData._equipment, index => ItemManager.GetItem(index), item => item.Defense);
            uint calculatedBonusAttackDamage = Util.Util.CalculateItemPropertySum(monsterData._equipment, index => ItemManager.GetItem(index), item => item.Damage);
            if (bonusDefense != calculatedBonusDefense)
                throw new InvalidDataException("Invalid monster data. Wrong stored bonus defense.");
            if (bonusAttackDamage != calculatedBonusAttackDamage)
                throw new InvalidDataException("Invalid monster data. Wrong stored bonus attack damage.");*/
            #endregion

            #region Monster Display Data
            var allFrameIndices = dataReader.ReadBytes(256); // 8 * 32
            var frameCounts = dataReader.ReadBytes(8);
            for (int i = 0; i < 8; i++)
            {
                var animation = monsterData._animations[i] = new Animation();
                int count = animation.UsedAmount = frameCounts[i];
                animation.FrameIndices = count == 0
                    ? Array.Empty<byte>()
                    : allFrameIndices.Skip(i * 32).Take(count).ToArray();
            }
            SkipBytes(16); // Atari palette
            monsterData.CustomPalette = dataReader.ReadBytes(32);
            byte animationProgressions = dataReader.ReadByte();
            for (int i = 0; i < 8; i++)
            {
                monsterData._animationProgressions[i] = (animationProgressions & (1 << i)) == 0
                    ? MonsterAnimationProgression.Cycle
                    : MonsterAnimationProgression.Wave;
            }
            SkipBytes(1); // padding byte
            monsterData.OriginalFrameWidth = dataReader.ReadWord();
            monsterData.OriginalFrameHeight = dataReader.ReadWord();
            monsterData.DisplayFrameWidth = dataReader.ReadWord();
            monsterData.DisplayFrameHeight = dataReader.ReadWord();
            #endregion

            return monsterData;
        }

        public void Serialize(IDataWriter dataWriter, bool advanced)
        {
            void WriteFillBytes(int count)
            {
                for (int i = 0; i < count; i++)
                    dataWriter.Write(0);
            }

            dataWriter.Write((byte)Type);
            dataWriter.Write((byte)Gender);
            dataWriter.Write((byte)Race);
            dataWriter.Write((byte)Class);
            dataWriter.Write((byte)SpellMastery);
            dataWriter.Write((byte)Level);
            dataWriter.Write((byte)Class);
            WriteFillBytes(6);
            dataWriter.Write((byte)CombatGraphicIndex);
            WriteFillBytes(2);
            dataWriter.Write((byte)Math.Min(Morale, 100));
            dataWriter.Write((byte)SpellTypeImmunity);
            dataWriter.Write((byte)AttacksPerRound);
            var battleFlags = (byte)BattleFlags;
            if (!advanced)
                battleFlags &= 0xf;
            dataWriter.Write(battleFlags);
            dataWriter.Write((byte)Element);
            WriteFillBytes(4);
            dataWriter.Write((ushort)Gold);
            dataWriter.Write((ushort)Food);
            WriteFillBytes(2);
            dataWriter.Write((ushort)Conditions);
            dataWriter.Write((ushort)DefeatExperience);
            WriteFillBytes(8);
            for (int i = 0; i < 8; i++)
            {
                var attribute = Attributes[(Attribute)i];
                dataWriter.Write((ushort)attribute.CurrentValue);
                dataWriter.Write((ushort)attribute.MaxValue);
                dataWriter.WriteSignedWord(attribute.BonusValue);
                dataWriter.Write((ushort)attribute.StoredValue);
            }
            dataWriter.Write((ushort)Age);
            dataWriter.Write((ushort)MaxAge);
            WriteFillBytes(4);
            if (advanced)
            {
                dataWriter.Write((ushort)BonusSpellDamage);
                dataWriter.Write((ushort)BonusMaxSpellDamage);
                dataWriter.WriteSignedWord(BonusSpellDamageReduction);
                dataWriter.WriteSignedWord(BonusSpellDamagePercentage);
            }
            else
            {
                WriteFillBytes(8);
            }
            for (int i = 0; i < 10; i++)
            {
                var skill = Skills[(Skill)i];
                dataWriter.Write((ushort)skill.CurrentValue);
                dataWriter.Write((ushort)skill.MaxValue);
                dataWriter.WriteSignedWord(skill.BonusValue);
                dataWriter.Write((ushort)skill.StoredValue);
            }
            var hitPoints = HitPoints;
            dataWriter.Write((ushort)hitPoints.CurrentValue);
            dataWriter.Write((ushort)hitPoints.MaxValue);
            dataWriter.WriteSignedWord(hitPoints.BonusValue);
            var spellPoints = SpellPoints;
            dataWriter.Write((ushort)spellPoints.CurrentValue);
            dataWriter.Write((ushort)spellPoints.MaxValue);
            dataWriter.WriteSignedWord(spellPoints.BonusValue);
            dataWriter.Write((ushort)BaseDefense);
            // TODO: calc from equip
            int bonusDefense = BonusDefense;
            dataWriter.WriteSignedWord(unchecked((ushort)(short)bonusDefense));
            dataWriter.Write((ushort)BaseAttackDamage);
            // TODO: calc from equip
            int bonusAttackDamage = BonusAttackDamage;
            dataWriter.WriteSignedWord(bonusAttackDamage);
            dataWriter.WriteSignedWord(MagicAttackLevel);
            dataWriter.WriteSignedWord(MagicDefenseLevel);
            WriteFillBytes(16);
            dataWriter.Write((ushort)LearnedSpellsHealing);
            dataWriter.Write((ushort)LearnedSpellsAlchemistic);
            dataWriter.Write((ushort)LearnedSpellsMystic);
            dataWriter.Write((ushort)LearnedSpellsDestruction);
            dataWriter.Write((ushort)LearnedSpellsType5);
            dataWriter.Write((ushort)LearnedSpellsType6);
            dataWriter.Write((ushort)LearnedSpellsFunctional);
            WriteFillBytes(4);
            var name = Name;
            if (name.Length > 15)
                name = name[0..15];
            dataWriter.WriteWithoutLength(name.PadRight(16, '\0'));

            #region Equipment and Items
            _equipment.Serialize(dataWriter, advanced);
            _items.Serialize(dataWriter, advanced);
            #endregion

            #region Monster Display Data
            for (int i = 0; i < 8; i++)
            {
                int n;

                for (n = 0; n < _animations[i].UsedAmount && n < 32; n++)
                    dataWriter.Write(_animations[i].FrameIndices[n]);

                for (; n < 32; n++)
                    dataWriter.Write(0);
            }
            for (int i = 0; i < 8; i++)
            {
                dataWriter.Write((byte)Math.Min(_animations[i].UsedAmount, 32));
            }
            foreach (var c in Enumerable.Range(0, 16)) // Atari palette
                dataWriter.Write((byte)c); // just 0x0 to 0xf
            foreach (var c in CustomPalette)
                dataWriter.Write(c);
            byte animationProgressions = 0;
            for (int i = 0; i < 8; i++)
            {
                if (_animationProgressions[i] == MonsterAnimationProgression.Wave)
                    animationProgressions |= (byte)(1 << i);
            }
            dataWriter.Write(animationProgressions);
            WriteFillBytes(1); // padding byte
            dataWriter.Write((ushort)OriginalFrameWidth);
            dataWriter.Write((ushort)OriginalFrameHeight);
            dataWriter.Write((ushort)DisplayFrameWidth);
            dataWriter.Write((ushort)DisplayFrameHeight);
            #endregion
        }
        #endregion

        #region Helper Functions
        public void SetCombatGraphic([Range(0, byte.MaxValue)] uint index,
            [Range(0, ushort.MaxValue)] uint originalFrameWidth, [Range(0, ushort.MaxValue)] uint originalFrameHeight,
            [Range(0, ushort.MaxValue)] uint displayFrameWidth, [Range(0, ushort.MaxValue)] uint displayFrameHeight)
        {
            CombatGraphicIndex = index;
            OriginalFrameWidth = originalFrameWidth;
            OriginalFrameHeight = originalFrameHeight;
            DisplayFrameWidth = displayFrameWidth;
            DisplayFrameHeight = displayFrameHeight;
        }

        public uint[] GetAnimationFrames(MonsterAnimation monsterAnimation)
        {
            var animation = _animations[(int)monsterAnimation];
            return animation.FrameIndices.Take(Math.Min(32, animation.UsedAmount)).Select(f => (uint)f).ToArray();
        }

        public void SetAnimationFrames(MonsterAnimation monsterAnimation, uint[] frameIndices)
        {
            if (frameIndices.Length > 32)
                throw new ArgumentOutOfRangeException(nameof(frameIndices), "Only 32 frames are possible.");

            var animation = _animations[(int)monsterAnimation];            
            animation.FrameIndices = frameIndices.Select(frameIndex =>
            {
                if (frameIndex > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(frameIndices), $"A frame index must not be larger than {byte.MaxValue}.");
                return (byte)frameIndex;
            }).ToArray();
            animation.UsedAmount = frameIndices.Length;
        }
        #endregion

        #region Properties
        public override CharacterType Type => CharacterType.Monster;
        [Range(0, byte.MaxValue)]
        public uint CombatGraphicIndex
        {
            get => _combatGraphicIndex;
            private set
            {
                if (value > byte.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(CombatGraphicIndex), $"Combat graphic index is limited to the range 0 to {byte.MaxValue}.");

                _combatGraphicIndex = value;
            }
        }
        [Range(0, 100)]
        public uint Morale
        {
            get => _morale;
            set
            {
                if (value > 100)
                    throw new ArgumentOutOfRangeException(nameof(Morale), "Morale is limited to the range 0 to 100.");

                _morale = value;
            }
        }
        [Range(0, ushort.MaxValue)]
        public uint DefeatExperience
        {
            get => _defeatExperience;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(DefeatExperience), $"Defeat experience is limited to the range 0 to {ushort.MaxValue}.");

                _defeatExperience = value;
            }
        }
        public byte[] CustomPalette { get; set; } = new byte[32];
        [Range(0, ushort.MaxValue)]
        public uint OriginalFrameWidth
        {
            get => _originalFrameWidth;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(OriginalFrameWidth), $"Original frame width is limited to the range 0 to {ushort.MaxValue}.");

                _originalFrameWidth = value;
            }
        }
        [Range(0, ushort.MaxValue)]
        public uint OriginalFrameHeight
        {
            get => _originalFrameHeight;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(OriginalFrameHeight), $"Original frame height is limited to the range 0 to {ushort.MaxValue}.");

                _originalFrameHeight = value;
            }
        }
        [Range(0, ushort.MaxValue)]
        public uint DisplayFrameWidth
        {
            get => _displayFrameWidth;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(DisplayFrameWidth), $"Display frame width is limited to the range 0 to {ushort.MaxValue}.");

                _displayFrameWidth = value;
            }
        }
        [Range(0, ushort.MaxValue)]
        public uint DisplayFrameHeight
        {
            get => _displayFrameHeight;
            set
            {
                if (value > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(DisplayFrameHeight), $"Display frame height is limited to the range 0 to {ushort.MaxValue}.");

                _displayFrameHeight = value;
            }
        }
        #endregion
    }
}
