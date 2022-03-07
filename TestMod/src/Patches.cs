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
		static class PlayerState_Update_Patch
		{
			const int money = 1000000;

			static bool Prepare() => Config.Dbg.playerCheats;

			static void Postfix(PlayerState __instance)
			{
				if (__instance.model.currency < money)
					__instance.AddCurrency(money - __instance.model.currency);

				__instance.SetEnergy(__instance.GetMaxEnergy());
			}
		}

		static class JetpackPatches
		{
			[HarmonyPatch(typeof(EnergyJetpack), "Awake")]
			static class EnergyJetpack_Awake_Patch
			{
				static bool Prepare() => Config.Dbg.jetpackCheats;

				static void Postfix(EnergyJetpack __instance) =>
					__instance.jetpackVelThreshold = 10f;
			}

			[HarmonyPatch(typeof(EnergyJetpack), "OnStart_Jump")]
			static class EnergyJetpack_OnStartJump_Patch
			{
				static bool Prepare() => Config.Dbg.jetpackCheats;

				static void Postfix(EnergyJetpack __instance) =>
					__instance.canKickInJetpackTime = 0f;
			}

			[HarmonyPatch(typeof(EnergyJetpack), "OnStart_Jetpack")]
			static class EnergyJetpack_OnStartJetpack_Patch
			{
				static bool Prepare() => Config.Dbg.jetpackCheats;

				static void Postfix(EnergyJetpack __instance) =>
					__instance.hoverY += 1e5f;
			}
		}
	}
}