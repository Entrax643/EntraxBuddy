using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
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
	[XmlElement("Convert")]
	public class ConverTag : ProfileBehavior
	{
		private bool _done = false;

		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }

		[DefaultValue(5000)]
		[XmlAttribute("MaxWait")]
		public int MaxWait { get; set; }

		[DefaultValue(true)]
		[XmlAttribute("NqOnly")]
		public bool NqOnly { get; set; }

		private static readonly Color MessageColor = Colors.DeepPink;

		public new static void Log(string text, params object[] args)
		{
			text = "[Convert] " + string.Format(text, args);
			Logging.Write(MessageColor, text);
		}

		protected override void OnStart()
		{
			_done = false;
		}

		public override bool IsDone
		{
			get
			{
				return _done;
			}
		}

		protected override void OnDone()
		{
		}

		protected override Composite CreateBehavior()
		{
			return
			new Decorator(
			ret => !_done,
			new ActionRunCoroutine(r => Convert()));
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> Convert()
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
			await CommonTasks.StopAndDismount();
			if (ItemIds != null && ItemIds.Length > 0)
			{
				foreach (var id in ItemIds)
				{
					await ConvertByItemId((uint)id, (ushort)MaxWait, NqOnly);
				}
			}
			return _done = true;
		}

		protected async Task<bool> ConvertAllItems(
		IEnumerable<BagSlot> bagSlots,
		ushort maxWait)
		{
			foreach (var bagSlot in bagSlots)
			{
				string name = bagSlot.Name;
				Log("Attempting to convert {0}.", name);
				if (bagSlot.Item != null && (bagSlot.Item.Unique || bagSlot.Item.Untradeable))
				{
					continue;
				}
				if (bagSlot != null)
				{
					var startingId = bagSlot.TrueItemId;
					//Check to make sure the bagslots contents doesn't change
					while (bagSlot.TrueItemId == startingId && bagSlot.Count > 0)
					{
						var result = await CommonTasks.ConvertToMateria(bagSlot, maxWait);
						if (!result.HasFlag(SpiritbondResult.Success))
						{
							Log("Unable to convert \"{0}\" due to {1}.", name, result);
							break;
						}
					}
				}
				Thread.Sleep(500);
			}
			return true;
		}

		protected async Task<bool> ConvertByItemId(
		uint itemId,
		ushort maxWait = 5000,
		bool nqOnly = true)
		{
			var slots = InventoryManager.EquippedItems;
			return
			await
			ConvertAllItems(
			slots.Where(i => i.RawItemId == itemId && (!nqOnly || i.TrueItemId == itemId) && i.SpiritBond == 100),
			maxWait);
		}
	}
}