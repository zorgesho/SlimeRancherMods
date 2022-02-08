using System;
using System.Reflection.Emit;
using System.Collections;
using System.Collections.Generic;

using HarmonyLib;
using UnityEngine;

using Common;
using Common.Harmony;

namespace SmartVacpack
{
	// Counters for currently flying items that either expelled from the vacpack or pulled out from the storage.
	// Used for correcting maximum amounts of items while processing shoot/vac mode.
	static class FlyingItems
	{
		public static int vacItemsCount => VacItem.counter;
		public static int expItemsCount => ExpItem.counter;

		#region counters
		abstract class Counter<T>: MonoBehaviour where T: class
		{
			public static int counter { get; private set; } = 0;

			void Awake()
			{
				counter++;
				$"{GetType().Name}: added (count: {counter})".logDbg();
			}

			void OnDestroy()
			{
				counter--;
				$"{GetType().Name}: removed (count: {counter})".logDbg();
			}
		}

		// Item that expelled from the vacpack.
		// Will be ignored after small amount of time or after first collision with anything.
		class ExpItem: Counter<ExpItem>
		{
			void OnCollisionEnter()
			{
				Destroy(this);
			}

			IEnumerator Start()
			{
				yield return new WaitForSeconds(.5f);
				Destroy(this);
			}
		}

		// Item that pulled out from the storage.
		// Will be ignored if dropped (also, depending on the config, will be returned to the original storage).
		class VacItem: Counter<VacItem>
		{
			public SiloCatcher catcher;

			IEnumerator Start()
			{
				var vacuumable = GetComponent<Vacuumable>();
				yield return new WaitWhile(() => vacuumable.isCaptive());
				yield return new WaitForSeconds(.3f);

				if (Config.returnDroppedToSilo)
					catcher?.OnTriggerEnter(GetComponentInChildren<Collider>());
				else
					Destroy(this);
			}
		}
		#endregion

		#region patches
		[HarmonyPatch(typeof(SiloCatcher), "OnTriggerStay")]
		static class SiloCatcher_OnTriggerStay_Patch
		{
			static void processGO(Vacuumable item, SiloCatcher siloCatcher)
			{
				item.gameObject.AddComponent<VacItem>().catcher = siloCatcher;
			}

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
			{
				// add right before call to 'ForceJoint'
				return cins.ciInsert(new CIHelper.MemberMatch(nameof(WeaponVacuum.ForceJoint)), 0, 1,
					OpCodes.Dup,
					OpCodes.Ldarg_0,
					CIHelper.emitCall<Action<Vacuumable, SiloCatcher>>(processGO));
			}
		}

		[HarmonyPatch(typeof(WeaponVacuum), "Expel", typeof(GameObject), typeof(bool))]
		static class WeaponVacuum_Expel_Patch
		{
			static void processGO(GameObject go)
			{
				go.AddComponent<ExpItem>();
			}

			static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> cins)
			{
				// add right before exit
				return cins.ciInsert(ci => ci.isOp(OpCodes.Ret), 0, 1,
					OpCodes.Ldloc_S, 6,
					CIHelper.emitCall<Action<GameObject>>(processGO));
			}
		}
		#endregion
	}
}