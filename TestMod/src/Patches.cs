using HarmonyLib;
using UnityEngine;

using Common;
using Common.UnityDebug;

namespace TestMod
{
	static class DebugPatches
	{
		[HarmonyPatch(typeof(WeaponVacuum), "Start")]
		static class VacpackAxisPatch
		{
			static bool Prepare() => Config.Dbg.showColliders;

			static void Prefix(WeaponVacuum __instance) =>
				__instance.vacOrigin.ensureComponent<DrawAxis>().scale = Vector3.one * 10f;
		}

		[HarmonyPatch(typeof(SiloStorage), "OnAdded")]
		static class SiloStorageColliderPatch
		{
			static bool Prepare() => Config.Dbg.showColliders;

			static void Prefix(SiloStorage __instance) =>
				__instance.gameObject.ensureComponent<DrawColliders>();
		}

		[HarmonyPatch(typeof(PlayerState), "Update")]
		static class MoneyCheatPatch
		{
			const int moneyStep = 10000;
			const int moneyMax = 1000000;

			static bool Prepare() => Config.Dbg.playerCheats;

			static void Postfix(PlayerState __instance)
			{
				if (__instance.model.currency < moneyMax)
					__instance.AddCurrency(moneyStep);

				__instance.SetEnergy(__instance.GetMaxEnergy());
			}
		}
	}
}