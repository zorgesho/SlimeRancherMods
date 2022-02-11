using System;
using System.Linq;
using System.Reflection.Emit;
using System.Collections.Generic;

using HarmonyLib;

using Common.Harmony;

namespace Common.Vacpack
{
	static class Patches
	{
		public delegate bool HandleVac(WeaponVacuum vac);

		static HandleVac handleShootMode;
		static HandleVac handleVacMode;
		static Action<bool> handleExit;

		static HandleVac handleAnims;
		static Func<bool> overrideAnims;

		public static void setModeHandlers(HandleVac handleShootMode, HandleVac handleVacMode, Action<bool> handleExit = null)
		{
			Patches.handleShootMode = handleShootMode;
			Patches.handleVacMode = handleVacMode;

			static void _default(bool _) {} // needed to be static
			Patches.handleExit = handleExit ?? _default;
		}

		public static void setAnimHandlers(HandleVac handleAnims, Func<bool> overrideAnims = null)
		{
			Patches.handleAnims = handleAnims;

			static bool _default() => false; // needed to be static
			Patches.overrideAnims = overrideAnims ?? _default;
		}

		// we handling shoot/vac modes here and disabling vacpack in case current action is restricted (one of the handlers returned 'false')
		[HarmonyPatch(typeof(WeaponVacuum), "Update")]
		static class WeaponVacuum_Update_Patch
		{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins, ILGenerator ilg)
			{
				var list = cins.ToList();
				var actionEnabled = ilg.DeclareLocal(typeof(bool));
				var label = ilg.DefineLabel();

				var call_Expel = new CIHelper.MemberMatch(nameof(WeaponVacuum.Expel));
				var call_SetActive = new CIHelper.MemberMatch("SetActive");

				// inserting call to 'handleShootMode' _before_ line with 'nextShot' check (so we can call it on each frame)
				list.ciInsert(ci => ci.isLDC(1f), +2, 1,
					OpCodes.Ldarg_0,
					CIHelper.emitCall(handleShootMode),
					OpCodes.Stloc, actionEnabled);

				// don't call 'Expel' if current action is disabled
				list.ciInsert(call_Expel, -2, 1, OpCodes.Ldloc, actionEnabled, OpCodes.Brfalse, label);
				list.ciInsert(call_Expel, CIHelper.emitLabel(label));

				// inserting call to 'handleVacMode' after 'vacMode == VacMode.VAC'
				list.ciInsert(ci => ci.isOp(OpCodes.Ldc_I4_2), +2, 1,
					OpCodes.Ldarg_0,
					CIHelper.emitCall(handleVacMode),
					OpCodes.Stloc, actionEnabled);

				int[] ii = list.ciFindIndexes(
					ci => ci.isOp(OpCodes.Ldc_I4_2), // vacMode == VacMode.VAC
					call_SetActive, // this.vacAudioHandler.SetActive(true);
					call_SetActive, // this.vacFX.SetActive(this.held == null);
					new CIHelper.MemberMatch("set_enabled")); // this.siloActivator.enabled = (this.held == null);

				if (ii == null)
					return cins;

				// disabling various stuff in 'VacMode.VAC' block if current action is disabled
				for (int i = 3; i > 0; i--)
				{
					var lb = ilg.DefineLabel();

					list.ciInsert(ii[i],
						OpCodes.Ldloc, actionEnabled,
						OpCodes.Brtrue, lb,
						OpCodes.Pop,
						i == 3? OpCodes.Ldc_I4_0: CIHelper.emitCall(overrideAnims),
						CIHelper.emitLabel(lb));
				}

				// inserting call to 'handleExit' right before 'UpdateVacAnimators'
				list.ciInsert(new CIHelper.MemberMatch(nameof(WeaponVacuum.UpdateVacAnimators)), 0, 1,
					OpCodes.Ldloc, actionEnabled,
					CIHelper.emitCall(handleExit));

				return list;
			}
		}

		// correctly disabling animations for one frame if animations handler 'handleAnims' returned false
		[HarmonyPatch(typeof(WeaponVacuum), "UpdateVacAnimators")]
		static class WeaponVacuum_UpdateVacAnimators_Patch
		{
			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins, ILGenerator ilg)
			{
				var label = ilg.DefineLabel();

				// insert right after assigning all the flags
				return cins.ciInsert(ci => ci.isOp(OpCodes.Stloc_2),
					OpCodes.Ldarg_0,
					CIHelper.emitCall(handleAnims),
					OpCodes.Brtrue, label,
					OpCodes.Ldloc_0, OpCodes.Stloc_1,	// flag2 = flag;
					OpCodes.Ldc_I4_0, OpCodes.Stloc_2,	// flag3 = false;
					CIHelper.emitLabel(label));
			}
		}
	}
}