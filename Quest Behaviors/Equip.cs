using Buddy.Coroutines;
using Clio.XmlEngine;
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
	[XmlElement("Equip")]
	public class Equip : ProfileBehavior
	{
		private bool _done = false;

		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }

		[DefaultValue(true)]
		[XmlAttribute("NqOnly")]
		public bool NqOnly { get; set; }

		[DefaultValue(10000)]
		[XmlAttribute("MaxWait")]
		public int MaxWait { get; set; }

		private static readonly Color MessageColor = Colors.DeepPink;

		public new static void Log(string text, params object[] args)
		{
			text = "[Equip] " + string.Format(text, args);
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
			new ActionRunCoroutine(r => Move()));
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> Move()
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
			if (ItemIds != null && ItemIds.Length > 0)
			{
				foreach (var id in ItemIds)
				{
					await MoveByItemId((uint)id, NqOnly);
				}
			}
			return _done = true;
		}

		protected async Task<bool> MoveAllItems(IEnumerable<BagSlot> bagSlots)
		{
			Thread.Sleep(500);
			foreach (var bagSlot in bagSlots)
			{
				string name = bagSlot.Name;
				if (bagSlot != null)
				{
					var startingId = bagSlot.TrueItemId;
					Dictionary<int, BagSlot> equippedSlot;
					int i = 0;
					var itemCateg = bagSlot.Item.EquipmentCatagory.ToString();
					BagSlot equipSlot;
					equippedSlot = new Dictionary<int, BagSlot>();
					foreach (BagSlot slot in InventoryManager.EquippedItems)
					{
						equippedSlot[i] = slot;
						i++;
					}
					if (itemCateg.Contains("Primary") || itemCateg.Contains("Arm"))
					{
						equipSlot = equippedSlot[0];
					}
					else if (itemCateg.Contains("Secondary") || itemCateg.Contains("Shield"))
					{
						equipSlot = equippedSlot[1];
					}
					else if (itemCateg.Contains("Soul"))
					{
						equipSlot = equippedSlot[13];
					}
					else if (itemCateg.Contains("Ring") && equippedSlot[11].TrueItemId == startingId)
					{
						equipSlot = equippedSlot[12];
					}
					else
					{
						switch (itemCateg)
						{
							case "Head":
								equipSlot = equippedSlot[2];
								break;

							case "Body":
								equipSlot = equippedSlot[3];
								break;

							case "Hands":
								equipSlot = equippedSlot[4];
								break;

							case "Waist":
								equipSlot = equippedSlot[5];
								break;

							case "Legs":
								equipSlot = equippedSlot[6];
								break;

							case "Feet":
								equipSlot = equippedSlot[7];
								break;

							case "Earrings":
								equipSlot = equippedSlot[8];
								break;

							case "Necklace":
								equipSlot = equippedSlot[9];
								break;

							case "Bracelets":
								equipSlot = equippedSlot[10];
								break;

							case "Ring":
								equipSlot = equippedSlot[11];
								break;

							default:
								equipSlot = null;
								break;
						}
					}
					if (equipSlot == null)
					{
						Log("You can not equip {0}.", name);
					}
					else
					{
						while (equipSlot.TrueItemId != startingId)
						{
							Log("Attempting to equip {0}.", name);
							bagSlot.Move(equipSlot);
							if (await Coroutine.Wait(MaxWait, () => equipSlot.TrueItemId == startingId))
							{
								Log("{0} equipped successfully.", name);
							}
							else
							{
								Log("Failed to equip {0}.", name);
							}
						}
					}
				}
				Thread.Sleep(1500);
			}
			return true;
		}

		protected async Task<bool> MoveByItemId(
		uint itemId,
		bool nqOnly = true)
		{
			var slots = InventoryManager.FilledInventoryAndArmory;
			return
			await
			MoveAllItems(
			slots.Where(i => i.RawItemId == itemId && (!nqOnly || i.TrueItemId == itemId)));
		}
	}
}