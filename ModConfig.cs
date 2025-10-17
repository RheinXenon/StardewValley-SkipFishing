using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace SkipFishing
{
    /// <summary>Mod的配置类。</summary>
    public class ModConfig
    {
        /*********
        ** 功能开关
        *********/
        /// <summary>是否启用跳过钓鱼小游戏功能。</summary>
        public bool SkipFishingMinigame { get; set; } = true;

        /// <summary>是否启用自动抛竿和收杆功能。</summary>
        public bool AutoCastAndReel { get; set; } = false;

        /// <summary>是否自动取出宝箱物品。</summary>
        public bool AutoLootTreasure { get; set; } = false;

        /*********
        ** 按键配置
        *********/
        /// <summary>切换跳过钓鱼小游戏功能的按键。</summary>
        public KeybindList ToggleSkipMinigame { get; set; } = KeybindList.Parse("F5");

        /// <summary>切换自动抛竿和收杆功能的按键。</summary>
        public KeybindList ToggleAutoCastReel { get; set; } = KeybindList.Parse("F6");

        /// <summary>切换自动取出宝箱物品功能的按键。</summary>
        public KeybindList ToggleAutoLootTreasure { get; set; } = KeybindList.Parse("F7");

        /*********
        ** 品质配置
        *********/
        /// <summary>是否启用基于钓鱼等级的品质提升。</summary>
        public bool EnableQualityByLevel { get; set; } = true;

        /// <summary>满级（等级10）时钓到最高品质（铱星品质）的概率（百分比）。</summary>
        public int MaxLevelIridiumChance { get; set; } = 80;

        /// <summary>是否在HUD显示功能状态通知。</summary>
        public bool ShowStatusMessages { get; set; } = true;

        /// <summary>自动抛竿的延迟时间（毫秒）。</summary>
        public int AutoCastDelay { get; set; } = 500;

        /// <summary>自动收杆检测的间隔时间（毫秒）。</summary>
        public int AutoReelCheckInterval { get; set; } = 100;
    }
}

