using System;
using UnityEngine;
using Common;

namespace InstaVacpack
{
	static class Utils
	{
		public static IItemContainer tryGetContainer(GameObject go, Identifiable.Id id = Identifiable.Id.NONE)
		{
			if (go.GetComponent<SiloCatcher>() is SiloCatcher siloCatcher)
				return tryGetContainer(siloCatcher, id);

			return null;
		}

		public static IItemContainer tryGetContainer(SiloCatcher siloCatcher, Identifiable.Id id = Identifiable.Id.NONE)
		{
			return siloCatcher?.type switch
			{
				SiloCatcher.Type.SILO_DEFAULT => new SiloContainer(siloCatcher, id),
				SiloCatcher.Type.SILO_OUTPUT_ONLY => new SiloOutputContainer(siloCatcher, id),
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
	}
}