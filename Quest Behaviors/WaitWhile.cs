using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Helpers;
using System;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("WaitWhile")]
	internal class WaitWhileTag : ProfileBehavior
	{
		[XmlAttribute("Condition")]
		public string Condition { get; set; }

		public Func<bool> Conditional { get; set; }

		/// <summary> Gets a value indicating whether this object is done. </summary>
		/// <value> true if this object is done, false if not. </value>
		public override bool IsDone
		{
			get { return !GetConditionExec(); }
		}

		protected override Composite CreateBehavior()
		{
			return new PrioritySelector(
			new Sleep(500)
			);
		}

		public bool GetConditionExec()
		{
			try
			{
				if (Conditional == null)
					Conditional = ScriptManager.GetCondition(Condition);

				return Conditional();
			}
			catch (Exception ex)
			{
				Logging.WriteDiagnostic(ScriptManager.FormatSyntaxErrorException(ex));
				// Stop on syntax errors.
				TreeRoot.Stop(reason: "Error in condition.");
				throw;
			}
		}
	}
}