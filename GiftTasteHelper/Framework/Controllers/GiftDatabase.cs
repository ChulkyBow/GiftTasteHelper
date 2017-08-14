﻿using StardewModdingAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiftTasteHelper.Framework
{
    /// <summary>Database for storing NPC gift tastes.</summary>
    internal class GiftDatabase : IGiftDatabase
    {
        public event DataSourceChangedDelegate DatabaseChanged;

        public GiftDatabaseModel Database { get; protected set; }
        protected readonly IModHelper Helper;

        public GiftDatabase(IModHelper helper)
        {
            this.Helper = helper;
            this.Database = new GiftDatabaseModel();
        }

        public GiftDatabase(IModHelper helper, GiftDatabaseModel database)
        {
            this.Helper = helper;
            this.Database = database;
        }

        /// <summary>Returns if the database has an item for a particular NPC stored.</summary>
        public bool ContainsGift(string npcName, int itemId, GiftTaste taste)
        {
            if (taste == GiftTaste.MAX)
            {
                return false;
            }
            return Database.Entries[npcName].Contains(taste, itemId);
        }

        /// <summary>Adds an item for an npc to the database.</summary>
        public virtual bool AddGift(string npcName, int itemId, GiftTaste taste)
        {
            if (taste == GiftTaste.MAX)
            {
                return false;
            }

            bool check = true;
            if (!Database.Entries.ContainsKey(npcName))
            {
                Database.Entries.Add(npcName, new CharacterTasteModel());
                check = false;
            }

            if (!check || !ContainsGift(npcName, itemId, taste))
            {
                Utils.DebugLog($"Adding {itemId} to {npcName}'s {taste} tastes.");
                Database.Entries[npcName].Add(taste, new GiftModel() { ItemId = itemId });

                DatabaseChanged();
                return true;
            }
            return false;
        }

        /// <summary>Adds a range of items for an npc to the database.</summary>
        public virtual bool AddGifts(string npcName, GiftTaste taste, int[] itemIds)
        {
            if (taste == GiftTaste.MAX)
            {
                return false;
            }

            if (!Database.Entries.ContainsKey(npcName))
            {
                Database.Entries.Add(npcName, new CharacterTasteModel());
            }

            // Add only the gifts that are not already in the DB.
            var unique = itemIds.Where(id => !ContainsGift(npcName, id, taste)).Select(id => id);
            if (unique.Count() > 0)
            {
                Database.Entries[npcName].AddRange(taste, itemIds.Select(id => new GiftModel() { ItemId = id }));
                DatabaseChanged();
                return true;
            }
            return false;
        }

        /// <summary>Returns all the gifts of the given taste in the database for that npc.</summary>
        public int[] GetGiftsForTaste(string npcName, GiftTaste taste)
        {
            if (Database.Entries.ContainsKey(npcName))
            {
                var entryForTaste = Database.Entries[npcName][taste];
                if (entryForTaste != null)
                {
                    return entryForTaste.Select(model => model.ItemId).ToArray();
                }
            }
            return new int[] { };
        }
    }

    /// <summary>A gift database that is stored on disk.</summary>
    internal class StoredGiftDatabase : GiftDatabase
    {
        public static string DBRoot => "DB";
        public static string DBFileName => "GiftDatabase.json";

        private string DBPath;

        public StoredGiftDatabase(IModHelper helper, string path)
            : base(helper, helper.ReadJsonFile<GiftDatabaseModel>(path) ?? new GiftDatabaseModel())
        {
            Utils.DebugLog($"Setting DB path to {path}", LogLevel.Info);
            this.DBPath = path;
        }

        public override bool AddGift(string npcName, int itemId, GiftTaste taste)
        {
            if (base.AddGift(npcName, itemId, taste))
            {
                Write();
                return true;
            }
            return false;
        }

        public override bool AddGifts(string npcName, GiftTaste taste, int[] itemIds)
        {
            if (base.AddGifts(npcName, taste, itemIds))
            {
                Write();
                return true;
            }
            return false;
        }

        private void Write()
        {
            Utils.DebugLog($"Writing gift database to: {this.DBPath}");
            Helper.WriteJsonFile(DBPath, Database);
        }
    }
}
