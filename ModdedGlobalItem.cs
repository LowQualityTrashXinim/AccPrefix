using System;
using Terraria;
using System.Linq;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using System.Collections.Generic;
using System.IO;

namespace PrefixImproved
{
    internal class ModdedGlobalItem : GlobalItem
    {
        public override bool AppliesToEntity(Item entity, bool lateInstantiation)
        {
            return entity.IsAPrefixableAccessory();
        }
        public override bool InstancePerEntity => true;
        public List<int> PrefixList = new List<int>();
        public override void Load()
        {
            PrefixList = new();
        }
        public override void Unload()
        {
            PrefixList = null;
        }
        public override void SaveData(Item item, TagCompound tag)
        {
            tag["PrefixList"] = PrefixList;
        }
        public override void LoadData(Item item, TagCompound tag)
        {
            PrefixList = new();
            PrefixList = tag.Get<List<int>>("PrefixList");

            int count = PrefixList.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (i >= PrefixList.Count)
                {
                    continue;
                }
                if (PrefixModSystem.VanillaAccPrefixDict.ContainsKey(PrefixList[i]))
                {
                    continue;
                }
                if (PrefixLoader.GetPrefix(PrefixList[i]) == null)
                {
                    PrefixList.RemoveAt(i);
                }
            }
        }
        public override void NetSend(Item item, BinaryWriter writer)
        {
            writer.Write(PrefixList.Count);
            for (int i = 0; i < PrefixList.Count; i++)
            {
                writer.Write(PrefixList[i]);
            }
        }

        public override void NetReceive(Item item, BinaryReader reader)
        {
            PrefixList = new();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                PrefixList.Add(reader.ReadInt32());
            }
        }
        public override void PostReforge(Item item)
        {
            if (!item.accessory)
            {
                return;
            }
            PrefixList = new();
            int pre = item.prefix;
            int value = 0;
            if (PrefixModSystem.VanillaAccPrefixDict.ContainsKey(pre))
            {
                value = PrefixModSystem.VanillaAccPrefixDict[pre];
                PrefixList.Add(item.prefix);
            }
            else
            {
                ModPrefix modprefix = PrefixLoader.GetPrefix(pre);
                if (modprefix != null && PrefixModSystem.ModdedAccPrefixNameDict.ContainsKey(modprefix.Name))
                {
                    value = PrefixModSystem.ModdedAccPrefixNameDict[modprefix.Name];
                    PrefixList.Add(item.prefix);
                }
            }
            while (value < 4)
            {
                int valueRand = Main.rand.Next(1, 5 - value);

                int prefix = Main.rand.Next(PrefixModSystem.GetPrefixWithValue(valueRand));
                while (item.prefix == prefix || PrefixList.Contains(prefix))
                {
                    prefix = Main.rand.Next(PrefixModSystem.GetPrefixWithValue(valueRand));
                }

                PrefixList.Add(prefix);

                value += valueRand;
            }
        }
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (!item.accessory)
            {
                return;
            }
            if (PrefixList.Count <= 0)
            {
                return;
            }
            TooltipLine line = tooltips.FirstOrDefault(t => t.Name == "ItemName");
            TooltipLine prefixLine = tooltips.FirstOrDefault(t => t.IsModifier);
            string itemName = item.Name;
            string prefixFinalName = "";
            string prefixfinalEffect = "";
            for (int i = 0; i < PrefixList.Count; i++)
            {
                int prefix = PrefixList[i];

                string prefixtext = Lang.prefix[prefix].Value;
                prefixFinalName += $"{prefixtext} ";

                string prefixEffecttext = PrefixModSystem.prefixTooltip(item, prefix);
                if (i == PrefixList.Count - 1)
                {
                    prefixfinalEffect += $"{prefixEffecttext}";
                }
                else
                {
                    prefixfinalEffect += $"{prefixEffecttext} \n";
                }
            }
            line.Text = prefixFinalName + itemName;
            if (prefixLine != null)
                prefixLine.Text = prefixfinalEffect;
        }
        public override void UpdateEquip(Item item, Player player)
        {
            if (PrefixList.Count <= 0)
            {
                return;
            }
            foreach (var prefix in PrefixList)
            {
                if (item.CanApplyPrefix(prefix) && item.accessory)
                {
                    //we will simulate a item here
                    Item simulateitem = new Item(0, 1, prefix);
                    player.GrantPrefixBenefits(simulateitem);
                }
            }
        }
    }
    class PrefixModSystem : ModSystem
    {
        internal static PrefixModSystem instance;
        //param 0 : take method name
        //param 1 : take prefix name
        //param 2 : take prefix value
        public object Call(params object[] args)
        {
            try
            {
                // Where should other mods call? They could call at end of Load?
                string message = args[0] as string;
                if (message == "AddValueToModdedPrefix")
                {
                    string name = args[1] as string;
                    byte value = Convert.ToByte(args[2]);

                    AddValueToModdedPrefix(name, value);
                    return "Success";
                }
                else
                {
                    Mod.Logger.Error($"{Mod.Name} | Call Error: Unknown Message: {message}");
                }
            }
            catch (Exception e)
            {
                Mod.Logger.Error($"{Mod.Name} | Call Error: " + e.StackTrace + e.Message);
            }
            return "Failure";
        }
        public static string prefixTooltip(Item item, int prefix)
        {
            string tooltip = "";
            ModPrefix modprefix = PrefixLoader.GetPrefix(prefix);
            if (modprefix != null)
            {
                if (modprefix.GetTooltipLines(item).FirstOrDefault() != null)
                {
                    tooltip = modprefix.GetTooltipLines(item).FirstOrDefault().Text;
                }
            }
            else
            if (prefix == 62)
            {
                tooltip = "+1" + Lang.tip[25].Value;
            }
            else
            if (prefix == 63)
            {
                tooltip = "+2" + Lang.tip[25].Value;
            }
            else
            if (prefix == 64)
            {
                tooltip = "+3" + Lang.tip[25].Value;
            }
            else
            if (prefix == 65)
            {
                tooltip = "+4" + Lang.tip[25].Value;
            }
            else
            if (prefix == 66)
            {
                tooltip = "+20 " + Lang.tip[31].Value;
            }
            else
            if (prefix == 67)
            {
                tooltip = "+2" + Lang.tip[5].Value;
            }
            else
            if (prefix == 68)
            {
                tooltip = "+4" + Lang.tip[5].Value;
            }
            else
            if (prefix == 69)
            {
                tooltip = "+1" + Lang.tip[39].Value;
            }
            else
            if (prefix == 70)
            {
                tooltip = "+2" + Lang.tip[39].Value;
            }
            else
            if (prefix == 71)
            {
                tooltip = "+3" + Lang.tip[39].Value;
            }
            else
            if (prefix == 72)
            {
                tooltip = "+4" + Lang.tip[39].Value;
            }
            else
            if (prefix == 73)
            {
                tooltip = "+1" + Lang.tip[46].Value;
            }
            else
            if (prefix == 74)
            {
                tooltip = "+2" + Lang.tip[46].Value;
            }
            else
            if (prefix == 75)
            {
                tooltip = "+3" + Lang.tip[46].Value;
            }
            else
            if (prefix == 76)
            {
                tooltip = "+4" + Lang.tip[46].Value;
            }
            else
            if (prefix == 77)
            {
                tooltip = "+1" + Lang.tip[47].Value;
            }
            else
            if (prefix == 78)
            {
                tooltip = "+2" + Lang.tip[47].Value;
            }
            else
            if (prefix == 79)
            {
                tooltip = "+3" + Lang.tip[47].Value;
            }
            else
            if (prefix == 80)
            {
                tooltip = "+4" + Lang.tip[47].Value;
            }
            return tooltip;
        }
        private const byte MinValue = 0;
        private const byte MaxValue = 4;
        /// <summary>
        /// Keys : <see cref="ModType.Name"/><br/>
        /// Values : prefix value
        /// </summary>
        public static Dictionary<string, byte> ModdedAccPrefixNameDict = new Dictionary<string, byte>();
        private static Dictionary<string, int> SafeModdedAccPrefix = new Dictionary<string, int>();
        public static int SearchModdedAccPrefixByTheirName(string name)
        {
            if (SafeModdedAccPrefix.ContainsKey(name)) return SafeModdedAccPrefix[name];
            return 0;
        }
        /// <summary>
        /// Keys : <see cref="ModType.Name"/><br/>
        /// Values : prefix value
        /// </summary>
        public static Dictionary<int, byte> VanillaAccPrefixDict = new Dictionary<int, byte>();
        public static List<int> GetPrefixWithValue(int value)
        {
            List<int> prefixVanilla = VanillaAccPrefixDict.Keys.Where(v => VanillaAccPrefixDict[v] == value).ToList();
            List<int> prefixModded = ModdedAccPrefixNameDict.Keys.Where(v => ModdedAccPrefixNameDict[v] == value).Select(SearchModdedAccPrefixByTheirName).ToList();
            List<int> totalprefix = [.. prefixVanilla, .. prefixModded];
            return totalprefix;
        }
        public override void Load()
        {
            ModdedAccPrefixNameDict = new();
            SafeModdedAccPrefix = new();
            VanillaAccPrefixDict = new();
            instance = this;
            base.Load();
        }

        public override void Unload()
        {
            SafeModdedAccPrefix = null;
            ModdedAccPrefixNameDict = null;
            VanillaAccPrefixDict = null;
            instance = null;
        }
        /// <summary>
        /// Please add this at <see cref="PostSetupContent"/>
        /// </summary>
        /// <param name="prefix"></param>
        /// <param name="value"></param>
        public static void AddValueToModdedPrefix(string prefix, byte value)
        {
            value = Math.Clamp(value, MinValue, MaxValue);
            if (ModdedAccPrefixNameDict.ContainsKey(prefix))
            {
                ModdedAccPrefixNameDict[prefix] = value;
            }

        }
        public override void PostSetupContent()
        {
            ModdedAccPrefixNameDict = new();
            SafeModdedAccPrefix = new();
            VanillaAccPrefixDict = new();
            int[] vanillaprefix = Item.GetVanillaPrefixes(PrefixCategory.Accessory);
            for (int i = 0; i < vanillaprefix.Length; i++)
            {
                int prefix = vanillaprefix[i];
                switch (prefix)
                {
                    case PrefixID.Arcane:
                        VanillaAccPrefixDict.Add(prefix, 1);
                        break;
                    case PrefixID.Hard:
                        VanillaAccPrefixDict.Add(prefix, 1);
                        break;
                    case PrefixID.Guarding:
                        VanillaAccPrefixDict.Add(prefix, 2);
                        break;
                    case PrefixID.Armored:
                        VanillaAccPrefixDict.Add(prefix, 3);
                        break;
                    case PrefixID.Warding:
                        VanillaAccPrefixDict.Add(prefix, 4);
                        break;
                    case PrefixID.Precise:
                        VanillaAccPrefixDict.Add(prefix, 1);
                        break;
                    case PrefixID.Lucky:
                        VanillaAccPrefixDict.Add(prefix, 2);
                        break;
                    case PrefixID.Jagged:
                        VanillaAccPrefixDict.Add(prefix, 1);
                        break;
                    case PrefixID.Spiked:
                        VanillaAccPrefixDict.Add(prefix, 2);
                        break;
                    case PrefixID.Angry:
                        VanillaAccPrefixDict.Add(prefix, 3);
                        break;
                    case PrefixID.Menacing:
                        VanillaAccPrefixDict.Add(prefix, 4);
                        break;
                    case PrefixID.Brisk:
                        VanillaAccPrefixDict.Add(prefix, 1);
                        break;
                    case PrefixID.Fleeting:
                        VanillaAccPrefixDict.Add(prefix, 2);
                        break;
                    case PrefixID.Hasty2:
                        VanillaAccPrefixDict.Add(prefix, 3);
                        break;
                    case PrefixID.Quick2:
                        VanillaAccPrefixDict.Add(prefix, 4);
                        break;
                    case PrefixID.Wild:
                        VanillaAccPrefixDict.Add(prefix, 1);
                        break;
                    case PrefixID.Rash:
                        VanillaAccPrefixDict.Add(prefix, 2);
                        break;
                    case PrefixID.Intrepid:
                        VanillaAccPrefixDict.Add(prefix, 3);
                        break;
                    case PrefixID.Violent:
                        VanillaAccPrefixDict.Add(prefix, 4);
                        break;
                    default:
                        continue;
                }
            }
            for (int i = 0; i < PrefixLoader.PrefixCount; i++)
            {
                ModPrefix prefix = PrefixLoader.GetPrefix(i);
                if (prefix == null)
                {
                    continue;
                }
                //By default we made it hold value of 1
                ModdedAccPrefixNameDict.Add(prefix.Name, 1);
                SafeModdedAccPrefix.Add(prefix.Name, prefix.Type);
            }
        }
    }
}
