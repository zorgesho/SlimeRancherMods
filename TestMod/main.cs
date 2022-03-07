using UnityEngine;

using SRML;
using SRML.Console;

using Common;

namespace TestMod
{
	class Main: Mod, IModEntryPoint
	{
		public virtual void PreLoad()
		{
			init();
			HarmonyPatcher.GetInstance().PatchAll();

			Console.RegisterCommand(new GiveAllGadgetsCommand());
			Console.RegisterCommand(new DumpRootObjectsCommand());
		}

		public virtual void Load() {}

		class KeyListener: MonoBehaviour
		{
			void Update()
			{
				if (Input.GetKeyDown(KeyCode.F1))
				{
					if (Screen.fullScreen)
						Screen.SetResolution(1280, 720, false);
					else
						Screen.SetResolution(2560, 1440, true);
				}
			}
		}

		public virtual void PostLoad()
		{
			Object.DontDestroyOnLoad(new GameObject("KeyListener", typeof(KeyListener)));
		}
	}
}