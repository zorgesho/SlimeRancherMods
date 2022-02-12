using System;
using UnityEngine;
using Common;

namespace InstaVacpack
{
	static class Utils
	{
		public static void playFX(bool success, WeaponVacuum vac, GameObject go = null)
		{
			FX.play(success, vac, go);
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
			if (go.TryGetComponent<SiloCatcher>(out var siloCatcher))
				return tryGetContainer(siloCatcher, id);

			if (go.TryGetComponent<ScorePlort>(out var scorePlort))
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


		static class FX
		{
			const int gapFrames = 30;

			// we need this and VacModeWatcher patch to avoid playing fx continuously while vacpack in the shoot/vac mode
			static int lastFramePlayedFX;

			public static void play(bool success, WeaponVacuum vac, GameObject go = null)
			{
				if (lastFramePlayedFX > Common.Vacpack.Utils.frameVacModeChanged - gapFrames)
					return;

				if (!success)
					vac.CaptureFailedEffect();
				else if (go)
					playSuccessFX(go);
				else
					vac.CaptureEffect();

				lastFramePlayedFX = Time.frameCount;
			}

			static void playSuccessFX(GameObject go)
			{
				var tr = go.transform;

				if (go.TryGetComponent<SiloCatcher>(out var siloCatcher))
				{
					SRBehaviour.SpawnAndPlayFX(siloCatcher.storeFX, tr.position, tr.rotation);
					siloCatcher.audioSource.Play();
				}

				if (go.TryGetComponent<ScorePlort>(out var scorePlort))
					SRBehaviour.SpawnAndPlayFX(scorePlort.ExplosionFX, tr.position, tr.rotation);
			}
		}
	}
}