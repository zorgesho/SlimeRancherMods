using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common.Harmony;

#if DEBUG
using Common;
using Common.UnityDebug;
#endif

namespace InstaVacpack
{
	// when we trying to pull out an item from a storage
	[HarmonyPatch(typeof(SiloCatcher), "OnTriggerStay")]
	static class SiloCatcher_OnTriggerStay_Patch
	{
		static bool processInstantMode(SiloCatcher siloCatcher)
		{
			if (!Input.GetKey(Config.instantModeKey))
				return false;

			var source = Utils.tryGetContainer(siloCatcher);
			var target = new PlayerAmmoContainer(source.id);

			bool result = Utils.tryTransferMaxAmount(source, target);
			Utils.FX.playFX(result);

			return true;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins, ILGenerator ilg)
		{
			var list = cins.ToList();
			int i = list.ciFindIndexForLast(ci => ci.isLDC(45f), ci => ci.isOp(OpCodes.Ret));

			if (i == -1)
				return cins;

			// insert 'processInstantMode' right before call to 'Remove'
			// if instant mode is enabled we will ignore the rest of the method (no matter the result of the transfer)
			var label = ilg.DefineLabel();

			return list.ciInsert(i + 1,
				OpCodes.Ldarg_0,
				CIHelper.emitCall<Func<SiloCatcher, bool>>(processInstantMode),
				OpCodes.Brfalse, label,
				OpCodes.Ret,
				new CodeInstruction(OpCodes.Nop) { labels = { label } });
		}
	}

	// when we trying to shoot an item to a storage
	[HarmonyPatch(typeof(WeaponVacuum), "Update")]
	static class WeaponVacuum_Update_Patch
	{
		static bool processInstantMode(WeaponVacuum vacpack)
		{
			if (!Input.GetKey(Config.instantModeKey))
				return false;

			if (tryGetPointedObject(vacpack) is not GameObject go)
				return false;

			var source = new PlayerAmmoContainer();
			var target = Utils.tryGetContainer(go, source.id);

			bool result = Utils.tryTransferMaxAmount(source, target);
			Utils.FX.playFX(result, go);

			return true;
		}

		static GameObject tryGetPointedObject(WeaponVacuum vacpack, float distance = Mathf.Infinity)
		{
			var tr = vacpack.vacOrigin.transform;
			Physics.Raycast(new Ray(tr.position, tr.up), out RaycastHit hit, distance, 1 << vp_Layer.Interactable, QueryTriggerInteraction.Collide);

			return hit.collider?.gameObject;
		}

		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
		{
			var list = cins.ToList();
			int i = list.ciFindIndexForLast(ci => ci.isOp(OpCodes.Ldc_I4_1));

			if (i == -1)
				return cins;

			// insert 'processInstantMode' right before call to 'Expel'
			return list.ciInsert(i + 2,
				OpCodes.Ldarg_0,
				CIHelper.emitCall<Func<WeaponVacuum, bool>>(processInstantMode),
				OpCodes.Brtrue, list[i + 1].operand);
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