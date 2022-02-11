using HarmonyLib;
using UnityEngine;

#if DEBUG
using Common;
using Common.UnityDebug;
#endif

namespace InstaVacpack
{
	static class CommonPatches
	{
		public static void init()
		{
			Common.Vacpack.Patches.setModeHandlers(handleShootMode, handleVacMode);
			Common.Vacpack.Patches.setAnimHandlers(AnimHandler.handleAnims);
		}

		static class AnimHandler
		{
			static bool disableAnims = false;
			public static void disableForFrame() => disableAnims = true;

			public static bool handleAnims(WeaponVacuum _) => !disableAnims || (disableAnims = false);
		}

		static bool handleShootMode(WeaponVacuum vac)
		{
			if (vac.vacMode != WeaponVacuum.VacMode.SHOOT || !Input.GetKey(Config.instantModeKey))
				return true;

			bool result = false;

			if (Utils.tryGetPointedObject(vac) is GameObject go)
			{
				var source = new PlayerAmmoContainer();
				var target = Utils.tryGetContainer(go, source.id);

				result = Utils.tryTransferMaxAmount(source, target);
				Utils.FX.playFX(result, go);
			}

			if (!result)
			{
				Utils.FX.playFX(false);
				AnimHandler.disableForFrame();
			}

			return false;
		}

		static bool handleVacMode(WeaponVacuum vac)
		{
			if (!Input.GetKey(Config.instantModeKey))
				return true;

			bool result = false;

			if (Utils.tryGetPointedObject(vac)?.GetComponent<SiloCatcher>() is SiloCatcher silo)
			{
				var source = Utils.tryGetContainer(silo);
				var target = new PlayerAmmoContainer(source.id);

				result = Utils.tryTransferMaxAmount(source, target);
				Utils.FX.playFX(result);
			}

			if (!result)
			{
				Utils.FX.playFX(false);
				AnimHandler.disableForFrame();
			}

			return false;
		}
	}

#if DEBUG
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
#endif // DEBUG
}