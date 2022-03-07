using System;
using SRML.Console;

namespace TestMod
{
	class GiveAllGadgetsCommand: ConsoleCommand
	{
		public override string ID => "giveallgadgets";
		public override string Usage => ID;
		public override string Description => ID;

		void addGadget(Gadget.Id id, int count = 1)
		{
			var gadgetDirector = SRSingleton<SceneContext>.Instance.GadgetDirector;

			for (int i = 0; i < count; i++)
				gadgetDirector.AddGadget(id);
		}

		public override bool Execute(string[] _)
		{
			foreach (Gadget.Id id in Enum.GetValues(typeof(Gadget.Id)))
				addGadget(id, 2); // giving 2 items so warp gadgets can be used

			return true;
		}
	}
}