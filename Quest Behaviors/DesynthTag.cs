using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("Desynth")]
	public class DesynthTag : ProfileBehavior
	{
		private bool _done = false;

		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }

		[DefaultValue(6000)]
		[XmlAttribute("DesynthDelay")]
		public int DesynthDelay { get; set; }

		[DefaultValue(10)]
		[XmlAttribute("DesynthTimeout")]
		public int DesynthTimeout { get; set; }

		private static readonly Color MessageColor = Colors.DeepPink;

		public new static void Log(string text, params object[] args)
		{
			text = "[Desynth] " + string.Format(text, args);
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
			new ActionRunCoroutine(r => Desynth()));
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> Desynth()
		{
			if (!Core.Player.DesynthesisUnlocked)
			{
				Log("You have not unlocked the desynthesis ability.");
				return _done = true;
			}
			IEnumerable<BagSlot> desynthables = null;
			if (ItemIds != null)
			{
				desynthables =
				InventoryManager.FilledSlots.
				Where(bs => Array.Exists(ItemIds, e => e == bs.RawItemId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else
			{
				Log("You didn't specify anything to desynthesize.");
				return _done = true;
			}
			var numItems = desynthables.Count();
			if (numItems == 0)
			{
				Log("None of the items you requested can be desynthesized.");
				return _done = true;
			}
			var i = 1;
			foreach (var bagSlot in desynthables)
			{
				string name = bagSlot.Name;
				Log("Attempting to desynthesize item {0} (\"{1}\") of {2}.", i++, name, numItems);
				var result = await CommonTasks.Desynthesize(bagSlot, DesynthDelay);
				if (result != DesynthesisResult.Success)
				{
					Log("Unable to desynthesize \"{0}\" due to {1}.", name, result);
					continue;
				}
				await Coroutine.Wait(DesynthTimeout * 1000, () => !bagSlot.IsFilled || !bagSlot.Name.Equals(name));
				if (bagSlot.IsFilled && bagSlot.EnglishName.Equals(name))
				{
					Log("Timed out awaiting desynthesis of \"{0}\" ({1} seconds).", name, DesynthTimeout);
				}
				else
				{
					Log("Desynthed \"{0}\".", name);
				}
			}
			return _done = true;
		}
	}
}