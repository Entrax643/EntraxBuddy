using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("UseAuraItem")]
	public class UseAuraItem : ProfileBehavior
	{
		[XmlAttribute("ItemId")]
		public uint ItemId { get; set; }

		[XmlAttribute("AuraId")]
		public uint AuraId { get; set; }

		[XmlAttribute("MinDuration")]
		[DefaultValue(5)]
		public int MinDuration { get; set; }

		[XmlAttribute("HqOnly")]
		public bool HqOnly { get; set; }

		[XmlAttribute("NqOnly")]
		public bool NqOnly { get; set; }

		protected bool _IsDone;

		#region Overrides of ProfileBehavior

		public override bool IsDone
		{
			get
			{
				return _IsDone;
			}
		}

		#endregion Overrides of ProfileBehavior

		private BagSlot itemslot;
		private Item itemData;

		protected override void OnStart()
		{
			itemData = DataManager.GetItem(ItemId);
			if (itemData == null)
			{
				TreeRoot.Stop("Couldn't locate item with id of " + ItemId);
				return;
			}

			if (HqOnly && NqOnly)
			{
				TreeRoot.Stop("Both HqOnly and NqOnly cannot be true");
				return;
			}

			var validItems = InventoryManager.FilledSlots.Where(r => r.RawItemId == ItemId).ToArray();

			if (validItems.Length == 0)
			{
				TreeRoot.Stop(string.Format("We don't have any {0} {1} in our inventory.", itemData.CurrentLocaleName, ItemId));
				return;
			}

			if (HqOnly)
			{
				var items = validItems.Where(r => r.IsHighQuality).ToArray();
				if (items.Any())
				{
					itemslot = items.FirstOrDefault();
				}
				else
				{
					TreeRoot.Stop("HqOnly and we don't have any Hq in the inventory with id " + ItemId);
					return;
				}
			}
			else if (NqOnly)
			{
				var items = validItems.Where(r => !r.IsHighQuality).ToArray();
				if (items.Any())
				{
					itemslot = items.FirstOrDefault();
				}
				else
				{
					TreeRoot.Stop("NqOnly and we don't have any Nq in the inventory with id " + ItemId);
					return;
				}
			}
			else
			{
				itemslot = validItems.OrderBy(r => r.IsHighQuality).FirstOrDefault();
			}
		}

		protected override void OnResetCachedDone()
		{
			_IsDone = false;
		}

		public override string StatusText
		{
			get
			{
				if (itemData != null)
				{
					return "Using " + itemData.CurrentLocaleName;
				}
				return "";
			}
		}

		private async Task<bool> UseItem()
		{
			bool shouldUse = false;
			bool alreadyPresent = false;
			if (Core.Player.HasAura(AuraId))
			{
				var auraInfo = Core.Player.GetAuraById(AuraId);
				if (auraInfo.TimespanLeft.TotalMinutes < MinDuration)
				{
					shouldUse = true;
					alreadyPresent = true;
				}
			}
			else
			{
				shouldUse = true;
			}

			if (shouldUse)
			{
				if (CraftingLog.IsOpen || CraftingManager.IsCrafting)
				{
					await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => CraftingLog.IsOpen);
					await Coroutine.Sleep(1000);
					CraftingLog.Close();
					await Coroutine.Yield();
					await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => !CraftingLog.IsOpen);
					await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => !CraftingManager.AnimationLocked);
				}

				Log("Waiting until the item is usable.");
				await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => itemslot.CanUse(null));

				Log("Using {0}", itemData.CurrentLocaleName);
				itemslot.UseItem();
				await Coroutine.Sleep(3000);

				if (!alreadyPresent)
				{
					Log("Waiting for the aura to appear");
					await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => Core.Player.HasAura(AuraId));
				}
				else
				{
					Log("Waiting until the duration is refreshed");
					await Coroutine.Wait(Timeout.InfiniteTimeSpan, () => Core.Player.GetAuraById(AuraId).TimespanLeft.TotalMinutes > MinDuration);
				}
			}

			_IsDone = true;

			return true;
		}

		private bool dialogwasopen;

		protected override Composite CreateBehavior()
		{
			return new ActionRunCoroutine(ctx => UseItem());
		}
	}
}