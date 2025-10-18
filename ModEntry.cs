using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Tools;
using StardewValley.Menus;
using Object = StardewValley.Object;

namespace SkipFishing
{
    /// <summary>Mod的入口点。</summary>
    public class ModEntry : Mod
    {
        /*********
        ** 字段
        *********/
        /// <summary>Mod的配置。</summary>
        private ModConfig Config = null!;

        /// <summary>当前是否正在自动钓鱼流程中。</summary>
        private bool isAutoFishing = false;

        /// <summary>上次检查自动收杆的时间。</summary>
        private float lastReelCheckTime = 0f;

        /// <summary>自动抛竿的延迟计时器。</summary>
        private float autoCastTimer = 0f;

        /// <summary>是否需要执行自动抛竿。</summary>
        private bool needsAutoCast = false;

        /*********
        ** 公共方法
        *********/
        /// <summary>Mod的入口点，在加载mod后首次调用。</summary>
        /// <param name="helper">提供简化操作mod代码的API。</param>
        public override void Entry(IModHelper helper)
        {
            // 读取配置
            this.Config = this.Helper.ReadConfig<ModConfig>();

            // 订阅事件
            helper.Events.GameLoop.GameLaunched += this.OnGameLaunched;
            helper.Events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            helper.Events.Input.ButtonPressed += this.OnButtonPressed;
            helper.Events.Display.MenuChanged += this.OnMenuChanged;
        }

        /*********
        ** 私有方法
        *********/
        /// <summary>在游戏启动后引发的事件。</summary>
        /// <param name="sender">事件发送者。</param>
        /// <param name="e">事件数据。</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            this.Monitor.Log("Skip Fishing Minigame mod 已加载！", LogLevel.Info);
            this.SetupConfigMenu();
        }

        /// <summary>设置Generic Mod Config Menu集成。</summary>
        private void SetupConfigMenu()
        {
            var configMenu = this.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
            {
                this.Monitor.Log("未找到Generic Mod Config Menu，配置菜单将不可用。请安装GMCM以使用配置界面。", LogLevel.Info);
                return;
            }

            // 注册mod配置
            configMenu.Register(
                mod: this.ModManifest,
                reset: () => this.Config = new ModConfig(),
                save: () => this.Helper.WriteConfig(this.Config)
            );

            // === 按键配置部分 ===
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "按键配置"
            );

            configMenu.AddKeybindList(
                mod: this.ModManifest,
                name: () => "切换Mod总开关",
                tooltip: () => "按下此键组合切换整个Mod的开关状态\n关闭后所有功能都将停用",
                getValue: () => this.Config.ToggleMod,
                setValue: value => this.Config.ToggleMod = value
            );

            // === 功能选择部分 ===
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "功能选择"
            );

            configMenu.AddParagraph(
                mod: this.ModManifest,
                text: () => "选择在Mod启用时要使用的功能"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "跳过钓鱼小游戏",
                tooltip: () => "勾选后将自动完成钓鱼小游戏\n需要Mod总开关启用才能生效",
                getValue: () => this.Config.EnableSkipFishingMinigame,
                setValue: value => this.Config.EnableSkipFishingMinigame = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "自动抛竿和收杆",
                tooltip: () => "勾选后将自动抛竿并在鱼咬钩时自动收杆\n需要Mod总开关启用才能生效",
                getValue: () => this.Config.EnableAutoCastAndReel,
                setValue: value => this.Config.EnableAutoCastAndReel = value
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "自动取出宝箱物品",
                tooltip: () => "勾选后将自动取出钓鱼宝箱中的所有物品\n需要Mod总开关启用才能生效",
                getValue: () => this.Config.EnableAutoLootTreasure,
                setValue: value => this.Config.EnableAutoLootTreasure = value
            );

            // === 品质配置部分 ===
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "鱼类品质配置"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "启用等级品质提升",
                tooltip: () => "启用后，鱼的品质将根据你的钓鱼等级提升",
                getValue: () => this.Config.EnableQualityByLevel,
                setValue: value => this.Config.EnableQualityByLevel = value
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "满级铱星概率",
                tooltip: () => "钓鱼等级达到10级时，钓到铱星品质鱼的概率（%）\n中间等级线性插值",
                getValue: () => this.Config.MaxLevelIridiumChance,
                setValue: value => this.Config.MaxLevelIridiumChance = value,
                min: 0,
                max: 100,
                interval: 5
            );

            // === 其他设置部分 ===
            configMenu.AddSectionTitle(
                mod: this.ModManifest,
                text: () => "其他设置"
            );

            configMenu.AddBoolOption(
                mod: this.ModManifest,
                name: () => "显示状态消息",
                tooltip: () => "启用后，切换功能时会在HUD显示消息提示",
                getValue: () => this.Config.ShowStatusMessages,
                setValue: value => this.Config.ShowStatusMessages = value
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "自动抛竿延迟",
                tooltip: () => "自动抛竿前的延迟时间（毫秒）",
                getValue: () => this.Config.AutoCastDelay,
                setValue: value => this.Config.AutoCastDelay = value,
                min: 100,
                max: 2000,
                interval: 100
            );

            configMenu.AddNumberOption(
                mod: this.ModManifest,
                name: () => "收杆检测间隔",
                tooltip: () => "自动收杆检测的间隔时间（毫秒）",
                getValue: () => this.Config.AutoReelCheckInterval,
                setValue: value => this.Config.AutoReelCheckInterval = value,
                min: 50,
                max: 500,
                interval: 50
            );
        }

        /// <summary>在每个游戏刻度更新时引发的事件。</summary>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // 检查Mod总开关
            if (!this.Config.ModEnabled)
                return;

            // 处理自动抛竿和收杆
            if (this.Config.EnableAutoCastAndReel)
            {
                this.HandleAutoCastAndReel();
            }
        }

        /// <summary>处理自动抛竿和收杆逻辑。</summary>
        private void HandleAutoCastAndReel()
        {
            var player = Game1.player;
            if (player?.CurrentTool is not FishingRod rod)
                return;

            // 检查玩家是否在可以钓鱼的位置
            if (!rod.isReeling && !rod.isFishing && !rod.hit && !rod.pullingOutOfWater && !rod.fishCaught)
            {
                // 需要自动抛竿
                if (!needsAutoCast && !isAutoFishing)
                {
                    needsAutoCast = true;
                    autoCastTimer = 0f;
                }

                // 处理抛竿延迟
                if (needsAutoCast)
                {
                    autoCastTimer += Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                    if (autoCastTimer >= this.Config.AutoCastDelay)
                    {
                        // 执行抛竿
                        if (this.CanCastFishingRod(rod))
                        {
                            this.AutoCast(rod);
                            needsAutoCast = false;
                            autoCastTimer = 0f;
                        }
                    }
                }
            }
            else
            {
                needsAutoCast = false;
                autoCastTimer = 0f;
            }

            // 检查是否需要收杆
            if (rod.isNibbling && !rod.hit && !rod.pullingOutOfWater && !rod.fishCaught && !isAutoFishing)
            {
                lastReelCheckTime += Game1.currentGameTime.ElapsedGameTime.Milliseconds;
                if (lastReelCheckTime >= this.Config.AutoReelCheckInterval)
                {
                    // 鱼咬钩了，自动收杆
                    this.AutoReel(rod);
                    lastReelCheckTime = 0f;
                }
            }
            else
            {
                lastReelCheckTime = 0f;
            }
        }

        /// <summary>检查是否可以抛竿。</summary>
        private bool CanCastFishingRod(FishingRod rod)
        {
            var player = Game1.player;
            
            // 检查基本条件
            if (player.UsingTool || player.isEating || Game1.activeClickableMenu != null)
                return false;

            // 检查是否在钓鱼
            if (rod.isFishing || rod.isReeling || rod.hit || rod.pullingOutOfWater || rod.fishCaught)
                return false;

            // 检查耐力
            if (player.Stamina <= 1)
                return false;

            return true;
        }

        /// <summary>执行自动抛竿。</summary>
        private void AutoCast(FishingRod rod)
        {
            try
            {
                var player = Game1.player;
                
                // 模拟按下使用工具键
                player.lastClick = player.GetToolLocation();
                player.BeginUsingTool();
                
                // 不在这里设置 isAutoFishing = true
                // 该标志只应在收杆后设置，以防止重复触发收杆
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"自动抛竿时发生错误: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>执行自动收杆。</summary>
        private void AutoReel(FishingRod rod)
        {
            try
            {
                var player = Game1.player;
                
                // 模拟拉竿动作
                rod.timeUntilFishingBite = -1f;
                rod.DoFunction(player.currentLocation, (int)player.lastClick.X, (int)player.lastClick.Y, 1, player);
                
                isAutoFishing = true;
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"自动收杆时发生错误: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>在菜单改变时引发的事件。</summary>
        private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
        {
            // 检查Mod总开关
            if (!this.Config.ModEnabled)
                return;

            // 处理钓鱼小游戏
            if (this.Config.EnableSkipFishingMinigame && e.NewMenu is BobberBar bobberBar)
            {
                this.SkipFishingMinigame(bobberBar);
            }

            // 处理宝箱自动拾取
            if (this.Config.EnableAutoLootTreasure && e.NewMenu is ItemGrabMenu itemGrabMenu)
            {
                this.AutoLootTreasureChest(itemGrabMenu);
            }

            // 钓鱼流程结束
            if (e.OldMenu is BobberBar || e.OldMenu is ItemGrabMenu)
            {
                isAutoFishing = false;
            }
        }

        /// <summary>跳过钓鱼小游戏。</summary>
        private void SkipFishingMinigame(BobberBar bobberBar)
        {
            try
            {
                var player = Game1.player;
                int fishingLevel = player.FishingLevel;

                // 计算鱼的品质
                int quality = this.CalculateFishQuality(fishingLevel);

                // 设置鱼被捕获
                this.Helper.Reflection.GetField<float>(bobberBar, "distanceFromCatching").SetValue(1f);
                this.Helper.Reflection.GetField<bool>(bobberBar, "treasure").SetValue(false);
                this.Helper.Reflection.GetField<bool>(bobberBar, "perfect").SetValue(quality >= 4);
                
                // 获取鱼的ID（兼容1.6的string类型和旧版本的int类型）
                string? fishId = null;
                try
                {
                    // 首先尝试作为string获取（1.6版本）
                    fishId = this.Helper.Reflection.GetField<string>(bobberBar, "whichFish").GetValue();
                }
                catch
                {
                    try
                    {
                        // 如果失败，尝试作为int获取（旧版本）
                        int whichFishInt = this.Helper.Reflection.GetField<int>(bobberBar, "whichFish").GetValue();
                        fishId = whichFishInt.ToString();
                    }
                    catch (Exception innerEx)
                    {
                        this.Monitor.Log($"无法获取鱼的ID: {innerEx.Message}", LogLevel.Error);
                        return;
                    }
                }

                if (!string.IsNullOrEmpty(fishId))
                {
                    // 获取玩家正在使用的鱼竿
                    if (player.CurrentTool is FishingRod rod)
                    {
                        // 创建鱼对象（星露谷1.6使用字符串ID）
                        Object fishObject = new Object(fishId, 1, false, -1, quality);
                        
                        // 直接添加到玩家背包
                        if (player.addItemToInventoryBool(fishObject))
                        {
                            // 关闭钓鱼小游戏
                            Game1.exitActiveMenu();
                            
                            // 增加钓鱼经验
                            int experience = quality switch
                            {
                                0 => 3,  // 普通品质
                                1 => 5,  // 银星品质
                                2 => 8,  // 金星品质
                                _ => 10  // 铱星品质
                            };
                            player.gainExperience(Farmer.fishingSkill, experience);
                            
                            // 标记钓鱼完成
                            rod.doneFishing(player);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"跳过钓鱼小游戏时发生错误: {ex.Message}", LogLevel.Error);
                this.Monitor.Log($"错误堆栈: {ex.StackTrace}", LogLevel.Error);
            }
        }

        /// <summary>计算鱼的品质。</summary>
        private int CalculateFishQuality(int fishingLevel)
        {
            if (!this.Config.EnableQualityByLevel)
                return 0; // 普通品质

            if (fishingLevel <= 0)
                return 0; // 普通品质

            // 钓鱼等级范围是1-10
            // 品质范围是0-4 (0=普通, 1=银星, 2=金星, 4=铱星)
            
            if (fishingLevel >= 10)
            {
                // 满级，按配置的概率获得铱星品质
                int chance = Game1.random.Next(100);
                if (chance < this.Config.MaxLevelIridiumChance)
                    return 4; // 铱星品质
                else
                    return 2; // 金星品质
            }

            // 线性计算中间等级的品质
            // 等级1-3: 普通品质(0)
            // 等级4-6: 银星品质(1) 
            // 等级7-9: 金星品质(2)，有一定概率获得铱星
            
            if (fishingLevel <= 3)
                return 0; // 普通品质
            else if (fishingLevel <= 6)
                return 1; // 银星品质
            else if (fishingLevel <= 9)
            {
                // 等级7-9时，基础金星品质，有概率升级到铱星
                // 线性插值：等级7=20%, 等级8=40%, 等级9=60%
                float iridiumChanceRatio = (fishingLevel - 6) / 4f; // (7-6)/4=0.25, (8-6)/4=0.5, (9-6)/4=0.75
                int iridiumChance = (int)(this.Config.MaxLevelIridiumChance * iridiumChanceRatio);
                
                if (Game1.random.Next(100) < iridiumChance)
                    return 4; // 铱星品质
                else
                    return 2; // 金星品质
            }

            return 0; // 默认普通品质
        }

        /// <summary>自动拾取宝箱物品。</summary>
        private void AutoLootTreasureChest(ItemGrabMenu menu)
        {
            try
            {
                // 检查是否是钓鱼宝箱
                if (menu.source != ItemGrabMenu.source_fishingChest)
                    return;

                var player = Game1.player;
                
                // 获取宝箱中的物品列表
                var itemsToGrab = menu.ItemsToGrabMenu?.actualInventory;
                if (itemsToGrab == null || itemsToGrab.Count == 0)
                    return;

                // 复制一份列表以避免在迭代时修改
                var items = itemsToGrab.ToList();
                
                // 将所有物品添加到玩家背包
                foreach (var item in items)
                {
                    if (item != null)
                    {
                        // 尝试添加到背包
                        if (player.addItemToInventoryBool(item))
                        {
                            // 从宝箱中移除
                            itemsToGrab.Remove(item);
                        }
                    }
                }

                // 如果所有物品都被拾取，关闭菜单
                if (itemsToGrab.Count == 0)
                {
                    Game1.exitActiveMenu();
                    Game1.playSound("coin");
                }
            }
            catch (Exception ex)
            {
                this.Monitor.Log($"自动拾取宝箱物品时发生错误: {ex.Message}", LogLevel.Error);
            }
        }

        /// <summary>在玩家按下按钮后引发的事件。</summary>
        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            // 切换Mod总开关
            if (this.Config.ToggleMod.JustPressed())
            {
                this.Config.ModEnabled = !this.Config.ModEnabled;
                this.Helper.WriteConfig(this.Config);
                
                if (this.Config.ShowStatusMessages)
                {
                    string status = this.Config.ModEnabled ? "已启用" : "已禁用";
                    
                    // 显示总开关状态
                    Game1.addHUDMessage(new HUDMessage($"SkipFishing Mod: {status}", 2));
                    
                    // 如果启用，显示当前选择的功能
                    if (this.Config.ModEnabled)
                    {
                        List<string> enabledFeatures = new List<string>();
                        if (this.Config.EnableSkipFishingMinigame) enabledFeatures.Add("跳过小游戏");
                        if (this.Config.EnableAutoCastAndReel) enabledFeatures.Add("自动抛收竿");
                        if (this.Config.EnableAutoLootTreasure) enabledFeatures.Add("自动取宝箱");
                        
                        if (enabledFeatures.Count > 0)
                        {
                            string features = string.Join(", ", enabledFeatures);
                            Game1.addHUDMessage(new HUDMessage($"已启用功能: {features}", 2));
                        }
                        else
                        {
                            Game1.addHUDMessage(new HUDMessage("提示: 请在配置菜单中勾选需要的功能", 2));
                        }
                    }
                }
                
                this.Monitor.Log($"SkipFishing Mod: {(this.Config.ModEnabled ? "启用" : "禁用")}", LogLevel.Info);
            }
        }
    }
}

