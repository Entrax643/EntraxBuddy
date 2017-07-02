using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.RemoteWindows;
using ff14bot.Settings;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;

#if RB_CN
    using ActionManager = ff14bot.Managers.Actionmanager;
#endif

namespace ff14bot.NeoProfiles
{
	[XmlElement("SellItem")]
	public class SellItem : ProfileBehavior
	{
		private bool _done;

		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }

		[DefaultValue(10)]
		[XmlAttribute("SellTimeout")]
		public int SellTimeout { get; set; }

		private static readonly Color MessageColor = Colors.DeepPink;

		public new static void Log(string text, params object[] args)
		{
			text = "[SellItem] " + string.Format(text, args);
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
			new ActionRunCoroutine(r => Sell()));
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> Sell()
		{
			if (GatheringManager.WindowOpen)
			{
				Log("Waiting for gathering window to close");
				Thread.Sleep(2000);
			}
			if (FishingManager.State != FishingState.None)
			{
				Log("Stop fishing");
				ActionManager.DoAction("Quit", Core.Me);
				await Coroutine.Wait(5000, () => FishingManager.State == FishingState.None);
			}
			IEnumerable<BagSlot> items;
			if (ItemIds != null)
			{
				items =
				InventoryManager.FilledSlots.
				Where(bs => Array.Exists(ItemIds, e => e == bs.RawItemId) && bs.IsSellable);
			}
			else
			{
				Log("You didn't specify anything to sell.");
				return _done = true;
			}
			var bagSlots = items as BagSlot[] ?? items.ToArray();
			var numItems = bagSlots.Count();
			if (numItems == 0)
			{
				Log("None of the items you requested can be sold.");
				return _done = true;
			}
			if (WorldManager.ZoneId != 129)
			{
				await TeleportTo(129, 8);
			}
			var destination = new Vector3(-129.1327f, 18.2f, 24.21809f);
			while (Core.Me.Distance(destination) > 1f)
			{
				var sprintDistance = Math.Min(20.0f, CharacterSettings.Instance.MountDistance);
				Navigator.MoveTo(destination);
				await Coroutine.Yield();
				if (Core.Me.Distance(destination) > sprintDistance && !Core.Me.IsMounted && !Core.Me.HasAura(50))
				{
					ActionManager.Sprint();
					await Coroutine.Sleep(500);
				}
			}
			GameObjectManager.GetObjectByNPCId(1001204).Interact();
			await Coroutine.Wait(5000, () => Shop.Open);
			if (Shop.Open)
			{
				var i = 1;
				foreach (var bagSlot in bagSlots)
				{
					string name = bagSlot.Name;
					Log("Attempting to sell item {0} (\"{1}\") of {2}.", i++, name, numItems);
					var result = await CommonTasks.SellItem(bagSlot);
					if (result != SellItemResult.Success)
					{
						Log("Unable to sell \"{0}\" due to {1}.", name, result);
						continue;
					}
					await Coroutine.Wait(SellTimeout * 1000, () => !bagSlot.IsFilled || !bagSlot.Name.Equals(name));
					if (bagSlot.IsFilled && bagSlot.Name.Equals(name))
					{
						Log("Timed out awaiting sale of \"{0}\" ({1} seconds).", name, SellTimeout);
					}
					else
					{
						Log("\"{0}\" sold.", name);
					}
					await Coroutine.Sleep(500);
				}
				Shop.Close();
				await Coroutine.Wait(2000, () => !Shop.Open);
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