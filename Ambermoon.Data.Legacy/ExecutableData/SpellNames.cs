﻿using System.Collections.Generic;

namespace Ambermoon.Data.Legacy.ExecutableData
{
    /// <summary>
    /// After the <see cref="SpellTypeNames"/> there are the
    /// spell names like "Magic Projectile", etc.
    /// 
    /// Unused spells are represented by empty entries.
    /// Note that there are 7 spell types with 30 spells
    /// each. The 5th and 6th spell types are unused and
    /// contain 30 empty entries.
    /// </summary>
    public class SpellNames
    {
        readonly Dictionary<Spell, string> entries = new Dictionary<Spell, string>();
        readonly Dictionary<SpellType, List<string>> entriesPerType = new Dictionary<SpellType, List<string>>();
        public IReadOnlyDictionary<Spell, string> Entries => entries;
        public IReadOnlyDictionary<SpellType, List<string>> EntriesPerType => entriesPerType;

        /// <summary>
        /// The position of the data reader should be at
        /// the start of the spell names just behind the
        /// spell type names.
        /// 
        /// It will be behind the spell names after this.
        /// </summary>
        internal SpellNames(IDataReader dataReader)
        {
            entries.Add(Spell.None, "");
            int spellIndex = 1; // we skip Spell.None as it has no text entry

            foreach (var type in Enum.GetValues<SpellType>())
            {
                entriesPerType.Add(type, new List<string>(30));

                for (int i = 0; i < 30; ++i)
                {
                    var name = dataReader.ReadNullTerminatedString(AmigaExecutable.Encoding);
                    entries.Add((Spell)spellIndex++, name);
                    entriesPerType[type].Add(name);
                }
            }
        }
    }
}
