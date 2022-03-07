using System;
using System.Linq;

using UnityEngine.SceneManagement;

using SRML.Console;

using Common;

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


	class DumpRootObjectsCommand: ConsoleCommand
	{
		public override string ID => "dumprootobjects";
		public override string Usage => ID;
		public override string Description => ID;

		public override bool Execute(string[] args)
		{
			string name = args.Length > 0? args[0].ToLower(): null;

			Enumerable.Range(0, SceneManager.sceneCount).
				Select(i => SceneManager.GetSceneAt(i)).
				Where(s => s.isLoaded).
				SelectMany(s => s.GetRootGameObjects()).
				Where(go => name == null || go.name.ToLower().Contains(name)).
				forEach(go => go.dump());

			return true;
		}
	}
}