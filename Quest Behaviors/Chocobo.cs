using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.RemoteAgents;
using ff14bot.RemoteWindows;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;

#if RB_CN
    using ChocoboManager = ff14bot.Managers.Chocobo;
#endif

namespace ff14bot.NeoProfiles
{
	[XmlElement("Chocobot")]
	public class Chocobot : ProfileBehavior
	{
		private bool _done;
		private Dictionary<uint, string> _chocoboFood;

		[XmlAttribute("ChocoboFoodID")]
		public uint ChocoboFoodId { get; set; }

		[DefaultValue("Me")]
		[XmlAttribute("PlayerName")]
		public string PlayerName { get; set; }

		[DefaultValueAttribute("None")]
		[XmlAttribute("ThavnairianOnion")]
		public string ThavnairianOnion { get; set; }

		[DefaultValueAttribute(true)]
		[XmlAttribute("FetchAfter")]
		public bool FetchAfter { get; set; }

		[DefaultValueAttribute(true)]
		[XmlAttribute("CleanBefore")]
		public bool CleanBefore { get; set; }

		internal static Regex TimeRegex = new Regex(@"(?:.*?)(\d+).*", RegexOptions.Compiled);
		private static readonly Color MessageColor = Colors.DeepPink;

		public new static void Log(string text, params object[] args)
		{
			text = "[Chocobot] " + string.Format(text, args);
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
			new ActionRunCoroutine(r => ChocoTraining()));
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> ChocoTraining()
		{
			InitializeFoodName();
			if (CleanBefore)
			{
				for (var i = 1; i <= 3; i++)
				{
					if (ConditionParser.HasAtLeast(8168, 1))
					{
						foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
						{
							if (unit.NpcId == 131129)
							{
								unit.Interact();
								break;
							}
						}
						await Coroutine.Wait(5000, () => SelectString.IsOpen);
						if (SelectString.IsOpen)
						{
							SelectString.ClickSlot((uint)SelectString.LineCount - 3);
							await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
							Log("Cleaning Stable n°{0}", i);
							SelectYesno.ClickYes();
							await Coroutine.Sleep(2000);
						}
					}
					else
					{
						Log("No Magicked Stable Broom left");
						break;
					}
				}
			}
			if (!ChocoboManager.IsStabled)
			{
				foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
				{
					if (unit.NpcId == 131129)
					{
						unit.Interact();
						break;
					}
				}
				await Coroutine.Wait(5000, () => SelectString.IsOpen);
				if (SelectString.IsOpen)
				{
					SelectString.ClickSlot(1);
					await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
					Log("Chocobo Stabled");
					SelectYesno.ClickYes();
				}
				else
				{
					Log("Failed to open menu");
				}
			}
			await Coroutine.Sleep(3000);
			foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
			{
				if (unit.NpcId == 131129)
				{
					unit.Interact();
					break;
				}
			}
			await Coroutine.Wait(5000, () => SelectString.IsOpen);
			if (SelectString.IsOpen)
			{
				SelectString.ClickSlot(0);
				await Coroutine.Wait(5000, () => HousingChocoboList.IsOpen);
				await Coroutine.Sleep(1500);
				if (HousingChocoboList.IsOpen)
				{
					//Look for our chocobo
					var items = HousingChocoboList.Items;
					//512 possible chocobos, 14 items per page...
					for (uint stableSection = 0; stableSection < AgentHousingBuddyList.Instance.TotalPages; stableSection++)
					{
						if (stableSection != AgentHousingBuddyList.Instance.CurrentPage)
						{
							Log("Switching to page {0}", stableSection);
							HousingChocoboList.SelectSection(stableSection);
							await Coroutine.Sleep(5000);
							items = HousingChocoboList.Items;
						}
						for (uint i = 0; i < items.Length; i++)
						{
							if (string.IsNullOrEmpty(items[i].PlayerName))
								continue;
							if (i == 0)
							{
								if (items[i].ReadyAt < DateTime.Now)
								{
									Log("Selecting my Chocobo");
									HousingChocoboList.SelectMyChocobo();
									if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen) && string.Equals("None", ThavnairianOnion, StringComparison.OrdinalIgnoreCase))
									{
										Log("{0}, {1}'s chocobo is maxed out", items[i].ChocoboName, items[i].PlayerName);
										SelectYesno.ClickNo();
										await Coroutine.Sleep(1000);
										continue;
									}
									if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen) && !string.Equals("None", ThavnairianOnion, StringComparison.OrdinalIgnoreCase))
									{
										if (ConditionParser.HasAtLeast(8166, 1))
										{
											Log("{0}, {1}'s chocobo is maxed out, changing food to Thavnairian Onion", items[i].ChocoboName, items[i].PlayerName);
											SelectYesno.ClickNo();
											await Coroutine.Wait(1000, () => !SelectYesno.IsOpen);
											await Coroutine.Sleep(500);
											Log("Selecting {0}, {1}'s chocobo on page {2}", items[i].ChocoboName, items[i].PlayerName, stableSection);
											HousingChocoboList.SelectMyChocobo();
											await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
											SelectYesno.ClickYes();
											ChocoboFoodId = 8166;
										}
										else
										{
											Log("{0}, {1}'s chocobo is maxed out but you don't have any Thavnairian Onion", items[i].ChocoboName, items[i].PlayerName);
											SelectYesno.ClickNo();
											await Coroutine.Sleep(1000);
											continue;
										}
									}
									Log("Waiting for inventory menu to appear....");
									//Wait for the inventory window to open and be ready
									//Technically the inventory windows are always 'open' so we check if their callbackhandler has been set
									if (!await Coroutine.Wait(5000, () => AgentInventoryContext.Instance.CallbackHandlerSet))
									{
										Log("Inventory menu failed to appear, aborting current iteration.");
										continue;
									}
									Log("Feeding Chocobo : Food Name : {0}, Food ID : {1}", _chocoboFood[ChocoboFoodId], ChocoboFoodId);
									AgentHousingBuddyList.Instance.Feed(ChocoboFoodId);
									if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen))
									{
										SelectYesno.ClickYes();
										await Coroutine.Sleep(1000);
									}
									Log("Waiting for cutscene to start....");
									if (await Coroutine.Wait(5000, () => QuestLogManager.InCutscene))
									{
										Log("Waiting for cutscene to end....");
										await Coroutine.Wait(Timeout.Infinite, () => !QuestLogManager.InCutscene);
									}
									Log("Waiting for menu to reappear....");
									await Coroutine.Wait(Timeout.Infinite, () => HousingChocoboList.IsOpen);
									await Coroutine.Sleep(1000);
								}
								else
								{
									Log("{0}, {1}'s chocobo can't be fed yet ...", items[i].ChocoboName, items[i].PlayerName);
								}
							}
							else if (string.Equals(items[i].PlayerName, PlayerName, StringComparison.OrdinalIgnoreCase) || string.Equals("All", PlayerName, StringComparison.OrdinalIgnoreCase))
							{
								if (items[i].ReadyAt < DateTime.Now)
								{
									Log("Selecting {0}, {1}'s chocobo on page {2}", items[i].ChocoboName, items[i].PlayerName, stableSection);
									HousingChocoboList.SelectChocobo(i);
									//Chocobo is maxed out, don't interact with it again
									if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen) && string.Equals("None", ThavnairianOnion, StringComparison.OrdinalIgnoreCase))
									{
										Log("{0}, {1}'s chocobo is maxed out", items[i].ChocoboName, items[i].PlayerName);
										SelectYesno.ClickNo();
										await Coroutine.Sleep(1000);
										continue;
									}
									//Chocobo is maxed out, don't interact with it again
									if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen) && string.Equals("All", ThavnairianOnion, StringComparison.OrdinalIgnoreCase))
									{
										if (ConditionParser.HasAtLeast(8166, 1))
										{
											Log("{0}, {1}'s chocobo is maxed out, changing food to Thavnairian Onion", items[i].ChocoboName, items[i].PlayerName);
											SelectYesno.ClickNo();
											await Coroutine.Wait(1000, () => !SelectYesno.IsOpen);
											await Coroutine.Sleep(500);
											Log("Selecting {0}, {1}'s chocobo on page {2}", items[i].ChocoboName, items[i].PlayerName, stableSection);
											HousingChocoboList.SelectChocobo(i);
											await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
											SelectYesno.ClickYes();
											ChocoboFoodId = 8166;
										}
										else
										{
											Log("{0}, {1}'s chocobo is maxed out but you don't have any Thavnairian Onion", items[i].ChocoboName, items[i].PlayerName);
											SelectYesno.ClickNo();
											await Coroutine.Sleep(1000);
											continue;
										}
									}
									Log("Waiting for inventory menu to appear....");
									//Wait for the inventory window to open and be ready
									//Technically the inventory windows are always 'open' so we check if their callbackhandler has been set
									if (!await Coroutine.Wait(5000, () => AgentInventoryContext.Instance.CallbackHandlerSet))
									{
										Log("Inventory menu failed to appear, aborting current iteration.");
										continue;
									}
									Log("Feeding Chocobo : Food Name : {0}, Food ID : {1}", _chocoboFood[ChocoboFoodId], ChocoboFoodId);
									AgentHousingBuddyList.Instance.Feed(ChocoboFoodId);
									if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen))
									{
										SelectYesno.ClickYes();
										await Coroutine.Sleep(1000);
									}
									Log("Waiting for cutscene to start....");
									if (await Coroutine.Wait(5000, () => QuestLogManager.InCutscene))
									{
										Log("Waiting for cutscene to end....");
										await Coroutine.Wait(Timeout.Infinite, () => !QuestLogManager.InCutscene);
									}
									Log("Waiting for menu to reappear....");
									await Coroutine.Wait(Timeout.Infinite, () => HousingChocoboList.IsOpen);
									await Coroutine.Sleep(1000);
								}
								else
								{
									Log("{0}, {1}'s chocobo can't be fed yet ...", items[i].ChocoboName, items[i].PlayerName);
								}
							}
						}
					}
					await Coroutine.Sleep(500);
					HousingChocoboList.Close();
					await Coroutine.Wait(5000, () => !HousingChocoboList.IsOpen);
				}
				else if (HousingMyChocobo.IsOpen)
				{
					var matches = TimeRegex.Match(HousingMyChocobo.Lines[0]);
					if (!matches.Success)
					{
						//We are ready to train now
						HousingMyChocobo.SelectLine(0);
						//Chocobo is maxed out, don't interact with it again
						if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen) && string.Equals("None", ThavnairianOnion, StringComparison.OrdinalIgnoreCase))
						{
							Log("Your chocobo is maxed out");
							SelectYesno.ClickNo();
							await Coroutine.Sleep(1000);
						}
						//Chocobo is maxed out, don't interact with it again
						if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen) && (string.Equals("Me", ThavnairianOnion, StringComparison.OrdinalIgnoreCase) || string.Equals("All", ThavnairianOnion, StringComparison.OrdinalIgnoreCase)))
						{
							if (ConditionParser.HasAtLeast(8166, 1))
							{
								Log("Your chocobo is maxed out, changing food to Thavnairian Onion");
								SelectYesno.ClickNo();
								await Coroutine.Wait(1000, () => !SelectYesno.IsOpen);
								await Coroutine.Sleep(500);
								HousingMyChocobo.SelectLine(0);
								await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
								SelectYesno.ClickYes();
								ChocoboFoodId = 8166;
							}
							else
							{
								Log("Your chocobo is maxed out but you don't have any Thavnairian Onion");
								SelectYesno.ClickNo();
								await Coroutine.Sleep(1000);
							}
						}
						Log("Waiting for inventory menu to appear....");
						//Wait for the inventory window to open and be ready
						//Technically the inventory windows are always 'open' so we check if their callbackhandler has been set
						if (!await Coroutine.Wait(5000, () => AgentInventoryContext.Instance.CallbackHandlerSet))
						{
							Log("Inventory menu failed to appear, aborting current iteration.");
							return _done = true;
						}
						Log("Feeding Chocobo : Food Name : {0}, Food ID : {1}", _chocoboFood[ChocoboFoodId], ChocoboFoodId);
						AgentHousingBuddyList.Instance.Feed(ChocoboFoodId);
						if (await Coroutine.Wait(5000, () => SelectYesno.IsOpen))
						{
							SelectYesno.ClickYes();
							await Coroutine.Sleep(1000);
						}
						Log("Waiting for cutscene to start....");
						if (await Coroutine.Wait(5000, () => QuestLogManager.InCutscene))
						{
							Log("Waiting for cutscene to end....");
							await Coroutine.Wait(Timeout.Infinite, () => !QuestLogManager.InCutscene);
						}
						Log("Waiting for menu to reappear....");
						await Coroutine.Wait(Timeout.Infinite, () => HousingMyChocobo.IsOpen);
						await Coroutine.Sleep(1000);
					}
					else
					{
						Log("Your chocobo can't be fed yet ...");
					}
					await Coroutine.Sleep(500);
					HousingMyChocobo.Close();
					await Coroutine.Wait(5000, () => !HousingMyChocobo.IsOpen);
				}
				else
				{
					Log("Failed to open Chocobo list");
				}
				SelectString.ClickSlot((uint)SelectString.LineCount - 1);
				await Coroutine.Wait(5000, () => !SelectString.IsOpen);
			}
			else
			{
				Log("Failed to open menu");
			}
			await Coroutine.Sleep(3000);
			if (FetchAfter)
			{
				foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
				{
					if (unit.NpcId == 131129)
					{
						unit.Interact();
						break;
					}
				}
				await Coroutine.Wait(5000, () => SelectString.IsOpen);
				if (SelectString.IsOpen)
				{
					SelectString.ClickSlot(1);
					await Coroutine.Wait(5000, () => HousingMyChocobo.IsOpen);
					if (HousingMyChocobo.IsOpen)
					{
						HousingMyChocobo.SelectLine(3);
						await Coroutine.Wait(5000, () => SelectYesno.IsOpen);
						SelectYesno.ClickYes();
						Log("Chocobo Fetch");
					}
					else
					{
						Log("Failed to acces to my chocobo");
						SelectString.ClickSlot((uint)SelectString.LineCount - 1);
						await Coroutine.Wait(5000, () => !SelectString.IsOpen);
					}
				}
				else
				{
					Log("Failed to open menu");
				}
			}
			return _done = true;
		}

		private void InitializeFoodName()
		{
			_chocoboFood = new Dictionary<uint, string>
			{
				[7894] = "Curiel Root",
				[7895] = "Sylkis Bud",
				[7897] = "Mimett Gourd",
				[7898] = "Tantalplant",
				[7900] = "Pahsana Fruit",
				[8165] = "Krakka Root",
				[8166] = "Thavnairian Onion"
			};
		}
	}
}