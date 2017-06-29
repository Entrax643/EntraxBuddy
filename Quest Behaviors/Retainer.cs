using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("Retainer")]
	public class Retainer : ProfileBehavior
	{
		private bool _done;
		private static readonly Color MessageColor = Colors.DeepPink;

		public new static void Log(string text, params object[] args)
		{
			text = "[Retainer] " + string.Format(text, args);
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
			new ActionRunCoroutine(r => Retaining()));
		}

		protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> Retaining()
		{
			foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
			{
				if (unit.NpcId == 2000401 || unit.NpcId == 2000441)
				{
					unit.Interact();
					break;
				}
			}
			if (await Coroutine.Wait(5000, () => SelectString.IsOpen))
			{
				uint count = 0;
				int lineC = SelectString.LineCount;
				uint countLine = (uint)lineC;
				foreach (var retainer in SelectString.Lines())
				{
					if (retainer.EndsWith("]") || retainer.EndsWith(")"))
					{
						Log("Checking Retainer n° " + (count + 1));
						// If Venture Completed
						if (retainer.EndsWith("[探险归来]") || retainer.EndsWith("[Tâche terminée]") || retainer.EndsWith("(Venture complete)"))
						{
							Log("Venture Completed !");
							// Select the retainer
							SelectString.ClickSlot(count);
							if (await Coroutine.Wait(5000, () => Talk.DialogOpen))
							{
								// Skip Dialog
								Talk.Next();
								if (await Coroutine.Wait(5000, () => SelectString.IsOpen))
								{
									// Click on the completed venture
									SelectString.ClickSlot(5);
									if (await Coroutine.Wait(5000, () => RetainerTaskResult.IsOpen))
									{
										// Assign a new venture
										RetainerTaskResult.Reassign();
										if (await Coroutine.Wait(5000, () => RetainerTaskAsk.IsOpen))
										{
											// Confirm new venture
											RetainerTaskAsk.Confirm();
											if (await Coroutine.Wait(5000, () => Talk.DialogOpen))
											{
												// Skip Dialog
												Talk.Next();
												if (await Coroutine.Wait(5000, () => SelectString.IsOpen))
												{
													SelectString.ClickSlot((uint)SelectString.LineCount - 1);
													if (await Coroutine.Wait(5000, () => Talk.DialogOpen))
													{
														// Skip Dialog
														Talk.Next();
														await Coroutine.Sleep(3000);
														foreach (var unit in GameObjectManager.GameObjects.OrderBy(r => r.Distance()))
														{
															if (unit.NpcId == 2000401 || unit.NpcId == 2000441)
															{
																unit.Interact();
																break;
															}
														}
														if (await Coroutine.Wait(5000, () => SelectString.IsOpen))
														{
															count++;
														}
													}
												}
											}
										}
									}
								}
							}
						}
						else
						{
							Log("Venture not Completed !");
							count++;
						}
					}
					else
					{
						Log("No more Retainer to check");
						SelectString.ClickSlot(countLine - 1);
					}
				}
				return _done = true;
			}
			return _done = true;
		}
	}
}