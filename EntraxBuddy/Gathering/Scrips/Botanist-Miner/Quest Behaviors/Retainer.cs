using System.Threading.Tasks;
using System.Windows.Media;
using Buddy.Coroutines;
using Clio.XmlEngine;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
    [XmlElement("Retainer")]
    public class Retainer : ProfileBehavior
    {
		private bool _done = false;

		private static readonly Color MessageColor = Color.FromRgb(151, 59, 216);

        new public static void Log(string text, params object[] args)
        {
            text = "[Retainer] " + string.Format(text, args);
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
					new ActionRunCoroutine(r => Retaining()));
         }

        protected override void OnResetCachedDone()
		{
			_done = false;
		}
		
        protected async Task<bool> Retaining()
        {
            GameObjectManager.GetObjectByNPCId(2000401).Interact();
            if (await Coroutine.Wait(5000, () => SelectString.IsOpen))
            {
                uint count = 0;
                int lineC = SelectString.LineCount;
                uint countLine = (uint)lineC;
                foreach (var retainer in SelectString.Lines())
                {
                    if (retainer.ToString().EndsWith("]") || retainer.ToString().EndsWith(")"))
                    {
                        Log("Checking Retainer n° " + (count + 1));
                        if (retainer.ToString().EndsWith("[Tâche terminée]") || retainer.ToString().EndsWith("(Venture complete)"))
                        {
                            Log("Venture Completed !");
                            SelectString.ClickSlot(count);
                            if (await Coroutine.Wait(5000, () => Talk.DialogOpen))
                            {
                                Talk.Next();
                                if (await Coroutine.Wait(5000, () => SelectString.IsOpen))
                                {
                                    SelectString.ClickSlot(5);
                                    if (await Coroutine.Wait(5000, () => RetainerTaskResult.IsOpen))
                                    {
                                        RetainerTaskResult.Reassign();
                                        if (await Coroutine.Wait(5000, () => RetainerTaskAsk.IsOpen))
                                        {
                                            RetainerTaskAsk.Confirm();
                                            if (await Coroutine.Wait(5000, () => Talk.DialogOpen))
                                            {
                                                Talk.Next();
                                                if (await Coroutine.Wait(54000, () => SelectString.IsOpen))
                                                {
                                                    SelectString.ClickSlot(9);
                                                    if (await Coroutine.Wait(5000, () => Talk.DialogOpen))
                                                    {
                                                        Talk.Next();
                                                        await Coroutine.Sleep(3000);
                                                        GameObjectManager.GetObjectByNPCId(2000401).Interact();
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
