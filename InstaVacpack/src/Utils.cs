using System;

using HarmonyLib;
using UnityEngine;

using Common;

namespace InstaVacpack
{
	static class Utils
	{
		public static int frameVacModeChanged => FX.VacModeWatcher.lastFrameChanged;

		public static GameObject tryGetPointedObject(WeaponVacuum vacpack, float distance = 10f)
		{
			var tr = vacpack.vacOrigin.transform;
			Physics.Raycast(new Ray(tr.position, tr.up), out RaycastHit hit, distance, 1 << vp_Layer.Interactable, QueryTriggerInteraction.Collide);

			return hit.collider?.gameObject;
		}

		public static bool runMultiple(Func<bool> action, int count)
		{
			bool result = true;

			for (int i = 0; i < count; i++)
				result &= action();

			return result;
		}

		public static IItemContainer tryGetContainer(GameObject go, Identifiable.Id id = Identifiable.Id.NONE)
		{
			if (go.GetComponent<SiloCatcher>() is SiloCatcher siloCatcher)
				return tryGetContainer(siloCatcher, id);

			if (go.GetComponent<ScorePlort>() is ScorePlort scorePlort)
				return new MarketContainer(scorePlort, id);

			return null;
		}

		public static IItemContainer tryGetContainer(SiloCatcher siloCatcher, Identifiable.Id id = Identifiable.Id.NONE)
		{
			return siloCatcher?.type switch
			{
				SiloCatcher.Type.SILO_DEFAULT => new SiloContainer(siloCatcher, id),
				SiloCatcher.Type.SILO_OUTPUT_ONLY => new SiloOutputContainer(siloCatcher, id),
				SiloCatcher.Type.VIKTOR_STORAGE => new ViktorContainer(siloCatcher, id),
				SiloCatcher.Type.DECORIZER => new DecorizerContainer(siloCatcher, id),
				SiloCatcher.Type.REFINERY => new RefineryContainer(id),
				_ => null
			};
		}

		public static bool tryTransferMaxAmount(IItemContainer source, IItemContainer target)
		{
			if (source?.canRemove != true || target?.canAdd != true)
				return false;

			if (source.id != target.id)
			{
				$"tryTransferMaxAmount: source and target have different ids! (source id: {source.id}, target id: {target.id})".logError();
				return false;
			}
																														$"tryTransferMaxAmount: trying to transfer items from {source} to {target}".logDbg();
			int freeSpaceInTarget = Math.Max(0, target.maxCount - target.count);
			int toTransfer = Math.Min(source.count, freeSpaceInTarget);
																														$"tryTransferMaxAmount: free space in target is {freeSpaceInTarget}, trying to transfer {toTransfer} items".logDbg();
			return toTransfer > 0 && source.remove(toTransfer) && target.add(toTransfer); // can potentially fail in the middle
		}


		public static class FX
		{
			const int gapFrames = 10;

			// we need this and VacModeWatcher patch to avoid playing fx continuously while vacpack in the shoot/vac mode
			static int lastFramePlayedFX;

			static WeaponVacuum vacpack => SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();

			public static void playFX(bool success, GameObject go = null)
			{
				if (lastFramePlayedFX > VacModeWatcher.lastFrameChanged - gapFrames)
					return;

				if (!success)
					vacpack.CaptureFailedEffect();
				else if (go)
					playSuccessFX(go);
				else
					vacpack.CaptureEffect();

				lastFramePlayedFX = Time.frameCount;
			}

			static void playSuccessFX(GameObject go)
			{
				var tr = go.transform;

				if (go.GetComponent<SiloCatcher>() is SiloCatcher siloCatcher)
				{
					SRBehaviour.SpawnAndPlayFX(siloCatcher.storeFX, tr.position, tr.rotation);
					siloCatcher.audioSource.Play();
				}

				if (go.GetComponent<ScorePlort>() is ScorePlort scorePlort)
					SRBehaviour.SpawnAndPlayFX(scorePlort.ExplosionFX, tr.position, tr.rotation);
			}

			[HarmonyPatch(typeof(WeaponVacuum), "UpdateVacModeForInputs")]
			public static class VacModeWatcher
			{
				public static int lastFrameChanged { get; private set; }

				static WeaponVacuum.VacMode prevVacMode;

				static void Postfix(WeaponVacuum __instance)
				{
					if (__instance.vacMode == prevVacMode)
						return;

					prevVacMode = __instance.vacMode;
					lastFrameChanged = Time.frameCount;																	$"VacModeWatcher: vac mode {prevVacMode}, frame: {lastFrameChanged}".logDbg();
				}
			}
		}
	}
}