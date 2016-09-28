using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.RemoteWindows;
using ff14bot.Settings;
using TreeSharp;


namespace ff14bot.NeoProfiles
{
    [XmlElement("TurnInCouterfoil")]
    public class TurnInCouterfoil : ProfileBehavior
    {
        private bool _done;
        private bool _haveItem;

        private static readonly Color MessageColor = Colors.DeepPink;

        public new static void Log(string text, params object[] args)
        {
            text = "[TurnInCouterfoil] " + string.Format(text, args);
            Logging.Write(MessageColor, text);
        }
        protected override void OnStart()
        {
            _done = false;
        }

        public override bool IsDone => _done;

        protected override void OnDone()
        {
        }

        protected override Composite CreateBehavior()
        {
            return
                new Decorator(
                    ret => !_done,
                    new ActionRunCoroutine(r => TurnIn()));
        }

        protected override void OnResetCachedDone()
        {
            _done = false;
        }

        protected async Task<bool> TurnIn()
        {
            foreach (var slot in InventoryManager.FilledSlots)
            {
                // Adamantite
                // Chysahl Green
                // Thunderbolt Eel
                // Eventite Jade
                // Periwinkle
                // Tiny Axolotl
                if ((slot.RawItemId == 12538 && slot.Collectability >= 530) ||
                    (slot.RawItemId == 12900 && slot.Collectability >= 530) ||
                    (slot.RawItemId == 12828 && slot.Collectability >= 834) ||
                    (slot.RawItemId == 13760 && slot.Collectability >= 515) ||
                    (slot.RawItemId == 13762 && slot.Collectability >= 515) ||
                    (slot.RawItemId == 12774 && slot.Collectability >= 326))
                {
                    _haveItem = true;
                }

            }

            if (_haveItem)
            {
                Log("Start");
                if (GatheringManager.WindowOpen)
                {
                    Log("Waiting for gathering window to close");
                    Thread.Sleep(2000);
                }

                if (FishingManager.State != FishingState.None)
                {
                    Log("Stop fishing");
                    Actionmanager.DoAction("Quit", Core.Me);
                    await Coroutine.Wait(5000, () => FishingManager.State == FishingState.None);
                }

                if (WorldManager.ZoneId != 478)
                {
                    await TeleportTo(478, 75);
                }

                var destination = new Vector3(-18.48964f, 206.4994f, 53.98175f);

                if (Core.Me.Distance(destination) > CharacterSettings.Instance.MountDistance && !Core.Me.IsMounted)
                {
                    while (!Core.Me.IsMounted)
                    {
                        await CommonTasks.MountUp();
                    }
                }
                while (Core.Me.Distance(destination) > 1f)
                {
                    var sprintDistance = Math.Min(20.0f, CharacterSettings.Instance.MountDistance);

                    Navigator.MoveTo(destination);
                    await Coroutine.Yield();

                    if (Core.Me.Distance(destination) > sprintDistance && !Core.Me.IsMounted)
                    {
                        Actionmanager.Sprint();
                        await Coroutine.Sleep(500);
                    }
                }
                if (Core.Me.Distance(destination) <= 1f)
                {
                    await CommonTasks.StopAndDismount();
                }

                GameObjectManager.GetObjectByNPCId(1012229).Interact();
                await Coroutine.Wait(5000, () => SelectIconString.IsOpen);
                SelectIconString.ClickSlot(0);
                await Coroutine.Sleep(2000);

                foreach (var item in InventoryManager.FilledSlots)
                {
                    // Adamantite
                    if (item.RawItemId == 12538 && item.Collectability >= 530)
                    {
                        Log("Attempting to Turn In Adamantite Ore");
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(2, 0, 0, 1, 8);
                        await Coroutine.Sleep(1000);
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItemDialog").SendAction(1, 0, 0);
                        await Coroutine.Wait(1000, () => Request.IsOpen);
                        item.Handover();
                        await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);
                        Request.HandOver();
                        await Coroutine.Sleep(2000);
                    }
                    // Chysahl Green
                    if (item.RawItemId == 12900 && item.Collectability >= 530)
                    {
                        Log("Attempting to Turn In Chysahl Green");
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(2, 0, 0, 1, 9);
                        await Coroutine.Sleep(1000);
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItemDialog").SendAction(1, 0, 0);
                        await Coroutine.Wait(1000, () => Request.IsOpen);
                        item.Handover();
                        await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);
                        Request.HandOver();
                        await Coroutine.Sleep(2000);
                    }
                    // Thunderbolt Eel
                    if (item.RawItemId == 12828 && item.Collectability >= 834)
                    {
                        Log("Attempting to Turn In Thunderbolt Eel");
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(2, 0, 0, 1, 10);
                        await Coroutine.Sleep(1000);
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItemDialog").SendAction(1, 0, 0);
                        await Coroutine.Wait(1000, () => Request.IsOpen);
                        item.Handover();
                        await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);
                        Request.HandOver();
                        await Coroutine.Sleep(2000);
                    }
                    // Eventite Jade
                    if (item.RawItemId == 13760 && item.Collectability >= 515)
                    {
                        Log("Attempting to Turn In Eventite Jade");
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(2, 0, 0, 1, 19);
                        await Coroutine.Sleep(1000);
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItemDialog").SendAction(1, 0, 0);
                        await Coroutine.Wait(1000, () => Request.IsOpen);
                        item.Handover();
                        await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);
                        Request.HandOver();
                        await Coroutine.Sleep(2000);
                    }
                    // Periwinkle
                    if (item.RawItemId == 13762 && item.Collectability >= 515)
                    {
                        Log("Attempting to Turn In Periwinkle");
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(2, 0, 0, 1, 20);
                        await Coroutine.Sleep(1000);
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItemDialog").SendAction(1, 0, 0);
                        await Coroutine.Wait(1000, () => Request.IsOpen);
                        item.Handover();
                        await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);
                        Request.HandOver();
                        await Coroutine.Sleep(2000);
                    }
                    // Tiny Axolotl
                    if (item.RawItemId == 12774 && item.Collectability >= 326)
                    {
                        Log("Attempting to Turn In Tiny Axolotl");
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(2, 0, 0, 1, 21);
                        await Coroutine.Sleep(1000);
                        RaptureAtkUnitManager.GetWindowByName("ShopExchangeItemDialog").SendAction(1, 0, 0);
                        await Coroutine.Wait(1000, () => Request.IsOpen);
                        item.Handover();
                        await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);
                        Request.HandOver();
                        await Coroutine.Sleep(2000);
                    }
                }
                RaptureAtkUnitManager.GetWindowByName("ShopExchangeItem").SendAction(1, 3, uint.MaxValue);
                _haveItem = false;
                await Coroutine.Sleep(500);
                Log("Done");
            }
            else
            {
                Log("Nothing to Turn In");
            }
            return _done = true;
        }

        public static async Task<bool> TeleportTo(ushort zoneId, uint aetheryteId)
        {
            if (WorldManager.ZoneId == zoneId)
            {
                // continue we are in the zone.
                return false;
            }

            var ticks = 0;
            while (MovementManager.IsMoving && ticks++ < 5)
            {
                Navigator.Stop();
                await Coroutine.Sleep(240);
            }

            var casted = false;
            while (WorldManager.ZoneId != zoneId)
            {
                if (!Core.Player.IsCasting && casted)
                {
                    break;
                }

                if (!Core.Player.IsCasting && !CommonBehaviors.IsLoading)
                {
                    WorldManager.TeleportById(aetheryteId);
                    await Coroutine.Sleep(500);
                }

                casted = casted || Core.Player.IsCasting;
                await Coroutine.Yield();
            }

            await Coroutine.Wait(5000, () => CommonBehaviors.IsLoading);
            await Coroutine.Wait(10000, () => !CommonBehaviors.IsLoading);

            return true;
        }

    }
}
