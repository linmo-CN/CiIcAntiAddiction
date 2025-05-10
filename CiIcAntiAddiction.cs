using System;
using System.Collections.Generic;
using System.ComponentModel;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Interfaces;
using Exiled.Events.EventArgs.Player;
using UnityEngine;
using MEC;

namespace CiIcAntiAddiction
{
    public class Config : IConfig
    {
        [Description("是否启动插件喵")]
        public bool IsEnabled { get; set; } = true;

        [Description("是否启用调试模式喵")]
        public bool Debug { get; set; } = false;

        [Description("是否启用插件启动点阵画喵")]
        public bool photo { get; set; } = true;

        [Description("最大持有混沌卡时间(秒)喵")]
        public int MaxHoldTime { get; set; } = 30;

        [Description("警告时间(秒)喵")]
        public int WarningTime { get; set; } = 10;

        [Description("初始警告消息喵")]
        public string WarningMessage { get; set; } = "警告: 你在地表区域拿着混沌卡! 你只有30秒时间离开地表或放下卡片!";

        [Description("时间警告消息喵")]
        public string TimeWarningMessage { get; set; } = "警告: 你还有{0}秒时间放下混沌卡或离开地表!";

        [Description("惩罚消息喵")]
        public string PunishmentMessage { get; set; } = "你在地表区域拿着混沌卡太久了!卡片已被移除!";

        [Description("是否启用惩罚喵")]
        public bool EnablePunishment { get; set; } = true;

        [Description("惩罚伤害值喵")]
        public int PunishmentDamage { get; set; } = 25;
    }

    public class CiIcAntiAddictionPlugin : Plugin<Config>
    {
        public override string Author => "Linmo-CN";
        public override string Name => "CI-IC防沉迷插件";
        public override string Prefix => "CIICAA";
        public override Version Version => new Version(1, 1, 0);
        public override Version RequiredExiledVersion => new Version(9, 5, 2);

        private Dictionary<Player, int> chaosCardHolders = new Dictionary<Player, int>();
        private Dictionary<Player, CoroutineHandle> coroutines = new Dictionary<Player, CoroutineHandle>();

        public override void OnEnabled()
        {
            Exiled.Events.Handlers.Player.PickingUpItem += OnPickingUpItem;
            Exiled.Events.Handlers.Player.DroppingItem += OnDroppingItem;
            Exiled.Events.Handlers.Player.Destroying += OnPlayerLeave;
            Exiled.Events.Handlers.Player.ChangingItem += OnChangingItem;
            Exiled.Events.Handlers.Player.EnteringEnvironmentalHazard += OnEnteringEnvironmentalHazard;
            base.OnEnabled();

            if (Config.IsEnabled)
            {
                Log.Info("插件已启用，防沉迷功能已启动喵~");
                if (Config.photo)
                {
                    Log.Info("插件启动点阵画已启用喵~");
                    Log.Info("\r\n%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%%##%%#*######*%@@@@%*%++#@%++*%#%#**##@@@@@@@@@@@@@@@@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@@@@@@@@@@@@@@#*+*%%#**+*##%#**#@@@@@*@@##++%@%*=++*%%%###%@@@@@@@@@@@@@@@@@@@@@@@@@@\r\n%@@@@@@@@%@@@@@@@@@@@@@@@@%*++#%%*=+**#%@%###%@@@@@*#@@@#@*+*%@%+===+%@@@%##%@@@@@@@@@@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@@@@@@@@@@*+*%%%#==#@%%@@##@%%%@@@@##@@@@@*@##**@@####**%@@@@#+%@@@@@@@@@@@@@@@@@@@@@\r\n%@@%@@@@@@@@@@@%@@%@@@@#*#@%%%**%%%@@@%#@@@%%@@@@#%@@@@@@%*%*#*+%@%##@%**@@@@%**%@@@@@@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@@@%@@#+#@@%@%*#@#%@@@%#@@@@#@@@@#%@@@%@@@@####%*+%@%%*%@%*#%%@@**#@@@@@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@@@@%+#@@@#%%*%%#@@@@@%@@@@%%@@@@@@@@@%@@@@%###%#*+@@%%*%%%##%#@@%%*@@@@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@%@#+%@@@#%%*@%#@@@@@@@@@@#%@@@@@@@@@@%@@@@@%+##@#**@@%%*@%%%%%#@@@%+%@@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@@##@@@%#@@*@@%@@@@@@@@@@@*@@@@@@@@@@%%@@@@@%+=#@@**@@@%%#@@%#@%#@@@%-%@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@@*#@@@##@@%@@@@@@@@@@@@@@@#%@@@@@@@@%%%@@@@@@#=##@#+@@@%%*%@@%#@##@#@++@@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@@*%@@%*%@@%@@@@@@@@@@@@@@@@##@@@@@@@@%%%@@@@@@#*##@@+#@@@%%*@@@@#%+@%%#+*@@@@@@@@@@@@@\r\n%@@@@@@@@@@@@@*%@@@*%@@@@@@@@@@@@@@@@@@@@**%@@@@@@@%#%@@@@@@%**#@@**@@@@%*%%@@@%++@##+=*%%@@@@@@@@@@\r\n%@@@@@@@@@@@@##@@@*@@@@#%@@@@@@@@@@@@@@@@*+#@@@@@@%%#@@@@@@@@*##@@%*@@@@%*=@**%%*:*++-*%%%*%@@@@@@@@\r\n%@@@@@@@@@@@##@@@##@@@@#@@@@@@@%=@@@@@@@%*%+%@@@@@%%%@@@@@@@@##%@@%*@@@@@%==-#@@%*%@@@@@%%**@@@@@@@@\r\n%@@@@@@@@@@%*@%@@+@@@@%#@@@@@@@**@@@@%@@*#@#*@@@@@##@@@@@@@@@*#@@@%#@@@@@%*=+@@@@@@@@@@@@@@#+@@@@@@@\r\n%@@@@@@@@@@*%*@@@#@@@@%%@@@@%=*#%@@@@#@@+%@@@*@@@%#%@@@@@@@@%##@@@%%@@@@@#+#=%@@@@@@@@@@@@@@++@@@@@@\r\n%@@@@@@@@@##=#@@@@@@@@*#%%@#==**%@@@%*@@+@@@@%#@@**@%=%@@@@@#%%@@@%@@@@@@#+*+@@@@@@@@@%@@@@*@=@@@@@@\r\n%@@@@@@@@@%#+@@#@@@@@@#%@@%=@+%##@@@#-@%*@@@@@@@#.#@#:#@@@@@%%@@@@@@@@@@@*=*=@@@@@@@@@#@@@@@@*%@@@@@\r\n%@@@@@@@@%*%*@%#@@@@@@=*@%+%@=%%+%@@*-@%*@@@@@%%==#%==+@@@@@%%@@@@@@@@@@@*-+++@@@@@@@@@@@@@@#*@@@@@@\r\n%@@@@@@@@#*%*@*@@@@@@#+*@-#@@*%@*#*@#*#%*@@@@*+=%%#%=@+###%@%%%@@@@@@@@@@+==#*-+%@@@@@@@@@%#*@@@@@@@\r\n%@@@@@@@@#*%#@*@@@@%==-:=.-=*#*#@#+#*###%@@@%+*@@%#%+@%+@%%#***@@%@@@@@@@=+=%@#--:=**#**+=-%@@@@@@@@\r\n%@@@@@@@@%#%*#*%@@@+--**::: :::=%@+%*+@#+@@**=#%@@#*#@@++@@#*=%@%%@@%%@@@=+ :@@@% *%*==+%@#*@@@@@@@@\r\n%@@@@@@@@@%@=-=#@@@-=-@%:=-.-+-:%@@@@%%@+#****+==+=-*%@@+#@#++@@%@%@%@@@@:-  +*#=:@@@*#+%@@%#@@@@@@@\r\n%@@@@@@@@%@%: .+@#=.=#@@-#@+::=@%@@@@@@@@**@#-::.   .:-=#=%%++@%@%%%%@@@#+=#=    *@@@*##=#@@#*@@@@@@\r\n%@@@@@@@@@%#*  =%-- :@@@%####*@@@@@@@@@@@@@@%%@@*-=:..::.::+##%%@%%#@@%@+-+%#-:+-#@@@##@*:@@@**@@@@@\r\n%@@@@@@@@##@@::=* :  %@@@@@@@@@@@@@@@@@@@@@@@@@@=.%#=--+-=*:.-*%@%#%@@*%:*%%@@#%-@@@@%#@%=:@@#=%@@@@\r\n%@@@@@@@####*+#:-:   +@@@@@@@@@@@@@@@@@@@@@@@@@@*+%%%#%=-%@==%*@%#%@@*#*=%@@%##%-@#@@@%@%* =@@==@@@@\r\n%@@@@@@##*+#+%**:=.  -@@@@@@@@@%@@@@@@@@@@@@@@@@@@%%@%%%@@**@@@@##@@@+%*##*#%@@*+@#@@@%@@#::*@%-*@@@\r\n%@@@@@###=%*@#%#**.. :@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%#=*@@@@##@@@++#@@+#*@%*=#%%@@%%@@%=+.@@*=@@@\r\n%@@@@#*#+%*@@#%#*#-:  *@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%#*#+*@@@@#*@@@%*+%@%+#*%++=%#@@@%@@@@=%-=@*+%@@\r\n%@@@#*%##**@@%@%**=-.::#@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@##@@@@#*@@@@===%@%@#***##%@@@@%@@@@=%*:@%+*%@\r\n%@@%=#@*#=#%%%*##*==*###%##@@@@@@@@@@@@@@@@@@@@@@@@@@@*=@@@@##@@@%*++%###**#%@%#%@@@%@@@@%*@*=+@++*@\r\n%%@=-@@*.=+#@@##@@%%%@@#%@##@@@@@%@%%%@@@@@@@@@@@@@@@++*@@@#%@@@#*##%*++*%@@@@*%@@@@%@@@@##@**-@**=@\r\n%@@--%#%+*%#+*@%+*%@%=%@#*%*#@@@@@@@%#@@@@@@@@@@@@@@%+%*+@@@@@+==##+*#%@@@@@@**@@@@@@@@@**%@**:@#%-@\r\n%#*%%@@@@@@%%**##=#%@#=@@++*=%@@@@@@@@@@@@@@@@@@@@@@#%*=+@@@%+=*:-+#@@@@@@@@*+@@@@@@@@@#-#@@**:%#**@\r\n*%@@@@@@@@@%@%%###@###*+##**++*#@@@@@%@@@%@@@@@@@%*+--=+*+@%#%@@=#%@@@@@@@%*=@@@@@@@@@@.+#@%#*=%*=#@\r\n@@%%*+=-===+##%%#####**+*##%%@%++*##**%%%#%*#=-.  :=**%**-#+%@@@+##%%@@@@#-+@@@@@@@@@@:.#%@%%+*%**#@\r\n@@@+  :=#%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#=:#@@@@@@@@#*%##@#    .:--.+@@@@@@@@@%: *%@@%#++###@#\r\n@%#*%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%#@@@@@@@%@@@@@@%*@-    . .*@@@@%@@@@*. =#@@@#+%%++%#@\r\n*@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@%#=.--::@@@@@@@@=:...  :%@@@%*%@@%=..-%@@%#+@%%*:%#@\r\n@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@*     %@@@@@@@#.    =@@%*=*%%#=  .-=*%%##@@*#%.+%#\r\n@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#.   =@@@@@@@@+   +*==++#%+:... ::  .:-*#*#@@:*##\r\n@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@*.   %@@@@@@@=  -::+##*-  .:----.       :=##-@*#\r\n@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@-  .%@@@@@@@..=#%%+-.  ....-::---:.     .:=%@@=\r\n@@+:-%@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@#   %%@@@%#*#%@%=.           .:::-::.    .=@@@=\r\n@= .=@@@@@@@@@@@@@@@@@%##@@@@@@@@@@@@@@@@@@@@@@@@@@@@@. %@@@%*=*#**=.  ..:-:..      ...:::...  :*@@+\r\n#=+#%@@%%#%%%@@@@@@@@+. .-%@@@@@@@@@@@@@@@@@@@@@@@@@@@:+@@%**=#*-:    ...%@@%%:.         .:...::-#@-\r\n%%@%@@@@@@@%%%%%@%%@@:.:..%@@@@@@@@@@@@@@@@@@@@@@@@@@%#@@###+*:    . ..::##+=#=--:         ::  .::-*\r\n@@@%++++###@@@@@@@%@@+=#+=#%%@@@@@@@@@@@@@@@@@@@@@@@@@@@%#%-=:   :.:.----###+%=----.         :.  .-%\r\n@@@#.   :+#@@@@@@@@@@#*%%%%%#%@@@@@@@@@@@@@@@@@@@@@@@@*##%-:     .-==+=--%%#*%=.:---.         .:  :%");
                }

                if (Config.Debug)
                {
                    Log.Info("调试模式已启用喵~");
                }
            }
        }

        public override void OnDisabled()
        {
            Exiled.Events.Handlers.Player.PickingUpItem -= OnPickingUpItem;
            Exiled.Events.Handlers.Player.DroppingItem -= OnDroppingItem;
            Exiled.Events.Handlers.Player.Destroying -= OnPlayerLeave;
            Exiled.Events.Handlers.Player.ChangingItem -= OnChangingItem;
            Exiled.Events.Handlers.Player.EnteringEnvironmentalHazard -= OnEnteringEnvironmentalHazard;

            foreach (var coroutine in coroutines.Values)
                Timing.KillCoroutines(coroutine);

            coroutines.Clear();
            chaosCardHolders.Clear();

            base.OnDisabled();

            if (Config.Debug)
                Log.Info("插件已禁用喵~");
        }

        private void OnPickingUpItem(PickingUpItemEventArgs ev)
        {
            if (ev.Pickup.Type == ItemType.KeycardChaosInsurgency)
            {
                if (Config.Debug) Log.Debug($"玩家 {ev.Player.Nickname} 拾取了混沌卡");
                CheckSurfaceAndStartTimer(ev.Player);
            }
        }

        private void OnDroppingItem(DroppingItemEventArgs ev)
        {
            if (ev.Item.Type == ItemType.KeycardChaosInsurgency)
            {
                if (Config.Debug) Log.Debug($"玩家 {ev.Player.Nickname} 丢弃了混沌卡");
                StopTimer(ev.Player);
            }
        }

        private void OnChangingItem(ChangingItemEventArgs ev)
        {
            if (ev.Item?.Type == ItemType.KeycardChaosInsurgency)
            {
                if (Config.Debug) Log.Debug($"玩家 {ev.Player.Nickname} 切换到了混沌卡");
                CheckSurfaceAndStartTimer(ev.Player);
            }
            else if (ev.Player.CurrentItem?.Type == ItemType.KeycardChaosInsurgency)
            {
                if (Config.Debug) Log.Debug($"玩家 {ev.Player.Nickname} 切换走了混沌卡");
                StopTimer(ev.Player);
            }
        }

        private void OnPlayerLeave(DestroyingEventArgs ev)
        {
            if (Config.Debug) Log.Debug($"玩家 {ev.Player.Nickname} 离开游戏");
            StopTimer(ev.Player);
        }

        private void OnEnteringEnvironmentalHazard(EnteringEnvironmentalHazardEventArgs ev)
        {
            if (ev.Player.Zone == ZoneType.Surface &&
                ev.Player.CurrentItem?.Type == ItemType.KeycardChaosInsurgency)
            {
                if (Config.Debug) Log.Debug($"玩家 {ev.Player.Nickname} 进入地表区域并持有混沌卡");
                StartTimer(ev.Player);
            }
        }

        private void CheckSurfaceAndStartTimer(Player player)
        {
            if (player.Zone == ZoneType.Surface)
            {
                StartTimer(player);
            }
        }

        private void StartTimer(Player player)
        {
            if (coroutines.ContainsKey(player))
                return;

            chaosCardHolders[player] = (int)Time.time;
            coroutines[player] = Timing.RunCoroutine(CheckPlayerTime(player));
            player.ShowHint(Config.WarningMessage, 5);

            if (Config.Debug) Log.Debug($"开始监控玩家 {player.Nickname} 的混沌卡持有时间");
        }

        private void StopTimer(Player player)
        {
            if (coroutines.TryGetValue(player, out var coroutine))
            {
                Timing.KillCoroutines(coroutine);
                coroutines.Remove(player);
                chaosCardHolders.Remove(player);

                if (Config.Debug) Log.Debug($"停止监控玩家 {player.Nickname} 的混沌卡持有时间");
            }
        }

        private IEnumerator<float> CheckPlayerTime(Player player)
        {
            while (true)
            {
                if (!player.IsAlive ||
                    player.CurrentItem?.Type != ItemType.KeycardChaosInsurgency ||
                    player.Zone != ZoneType.Surface)
                {
                    StopTimer(player);
                    yield break;
                }

                int timeHolding = (int)Time.time - chaosCardHolders[player];

                if (timeHolding >= Config.MaxHoldTime)
                {
                    ExecutePunishment(player);
                    yield break;
                }
                else if (timeHolding >= Config.WarningTime)
                {
                    player.ShowHint(
                        string.Format(Config.TimeWarningMessage,
                        Config.MaxHoldTime - timeHolding),
                        1);
                }
                yield return Timing.WaitForSeconds(1);
            }
        }

        private void ExecutePunishment(Player player)
        {
            if (player.CurrentItem?.Type == ItemType.KeycardChaosInsurgency)
            {
                player.RemoveItem(player.CurrentItem);
            }

            player.ShowHint(Config.PunishmentMessage, 5);

            if (Config.EnablePunishment && player.IsAlive)
            {
                player.Hurt(Config.PunishmentDamage, DamageType.Decontamination);
            }

            StopTimer(player);

            if (Config.Debug) Log.Debug($"已对玩家 {player.Nickname} 执行惩罚");
        }
    }
}
