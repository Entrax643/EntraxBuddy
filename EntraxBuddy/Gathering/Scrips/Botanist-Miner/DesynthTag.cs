using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;

using Buddy.Coroutines;

using Clio.Utilities;
using Clio.XmlEngine;

using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using ff14bot.Enums;
using ff14bot.Helpers;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("Desynth")]
	public class DesynthTag : ProfileBehavior
	{
		private bool             _done = false;
		private InventoryBagId[] _bagIds = null;
		
		[XmlAttribute("ItemIds")]
		public int[] ItemIds { get; set; }
		
		[XmlAttribute("BagIds")]
		public string BagIds { get; set; }
		
  		[DefaultValue(6000)]
		[XmlAttribute("DesynthDelay")]
		public int DesynthDelay { get; set; }
		
  		[DefaultValue(10)]
		[XmlAttribute("DesynthTimeout")]
		public int DesynthTimeout { get; set; }
		
		private static readonly Color MessageColor = Color.FromRgb(64, 224, 208);
		
        new public static void Log(string text, params object[] args)
        {
            text = "[Desynth] " + string.Format(text, args);
            Logging.Write(MessageColor, text);
        }
		
		public void GetBagIds()
		{
			if (BagIds != null)
			{
				string[] bagIds = BagIds.Split(',');
				
				if (bagIds.Count() > 0)
				{
					List<InventoryBagId> bagIdList = new List<InventoryBagId>();
					
					foreach (string bagId in bagIds)
					{
						try
						{
							bagIdList.Add(
								(InventoryBagId)Enum.Parse(typeof(InventoryBagId),bagId));
						}
						catch (ArgumentException)
						{
							Log("{0} is not a member of the InventoryBagId enumeration.",bagId);
						}
					}
					
					_bagIds = bagIdList.ToArray();
				}
			}
		}
		
		protected override void OnStart()
		{
			_done = false;
			
			GetBagIds();
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
			
			Log("Your {0}'s desynthesis level is: {1}.",Core.Player.CurrentJob,Core.Player.GetDesynthesisLevel(Core.Player.CurrentJob));
			
			IEnumerable<BagSlot> desynthables = null;
			
			if (_bagIds != null && ItemIds != null)
			{
				desynthables =
					InventoryManager.FilledSlots.
					Where(
						bs => Array.Exists(_bagIds,e=>e==bs.BagId) && Array.Exists(ItemIds,e=>e==bs.RawItemId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else if (_bagIds != null)
			{
				desynthables =
					InventoryManager.FilledSlots.
					Where(
						bs => Array.Exists(_bagIds,e=>e==bs.BagId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else if (ItemIds != null)
			{
				desynthables =
					InventoryManager.FilledSlots.
					Where(
						bs => Array.Exists(ItemIds,e=>e==bs.RawItemId) && bs.IsDesynthesizable && bs.CanDesynthesize);
			}
			else
			{
				Log("You didn't specify anything to desynthesize.");
				
				return _done = true;
			}
						
			var numItems = desynthables.Count();
						
			if ( numItems == 0)
			{
				Log("None of the items you requested can be desynthesized.");
				
				return _done = true;
			}
			else
			{
				Log("You have {0} items to desynthesize.",numItems);
			}
			
			var i = 1;
						
			foreach (var bagSlot in desynthables)
			{
				string name = bagSlot.EnglishName;
				
				Log("Attempting to desynthesize item {0} (\"{1}\") of {2} - success chance is {3}%.",i++,name,numItems,await CommonTasks.GetDesynthesisChance(bagSlot));
								
				var result = await CommonTasks.Desynthesize(bagSlot,DesynthDelay);
				
				if (result != DesynthesisResult.Success)
				{
					Log("Unable to desynthesize \"{0}\" due to {1}.", name,result);
					
					continue;
				}
				
				await Coroutine.Wait(DesynthTimeout*1000, () => (!bagSlot.IsFilled || !bagSlot.EnglishName.Equals(name)));
				
				if (bagSlot.IsFilled && bagSlot.EnglishName.Equals(name))
				{
					Log("Timed out awaiting desynthesis of \"{0}\" ({1} seconds).",name,DesynthTimeout);
				}
				else
				{
					Log("Desynthed \"{0}\": your {1}'s desynthesis level is now {2}.",name,Core.Player.CurrentJob,Core.Player.GetDesynthesisLevel(Core.Player.CurrentJob));
				}
			}
			
			return _done = true;
		}
	}
}
