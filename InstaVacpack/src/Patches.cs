using UnityEngine;

namespace InstaVacpack
{
	static class CommonPatches
	{
		public static void init()
		{
			Common.Vacpack.Patches.setModeHandlers(handleShootMode, handleVacMode);
			AnimHandler.init();
		}

		static class AnimHandler
		{
			static bool disableAnims = false;

			public static void init()
			{
				static bool _handleAnims(WeaponVacuum _) => !disableAnims || (disableAnims = false);
				Common.Vacpack.Patches.setAnimHandlers(_handleAnims);
			}

			public static void handleFxAndAnims(bool actionResult, WeaponVacuum vac, GameObject go = null)
			{
				Utils.playFX(actionResult, vac, go);
				disableAnims = !actionResult;
			}
		}


		static bool handleShootMode(WeaponVacuum vac)
		{
			if (vac.vacMode != WeaponVacuum.VacMode.SHOOT || !Input.GetKey(Config.instantModeKey))
				return true;

			bool result = false;
			var go = Common.Vacpack.Utils.tryGetPointedObject(vac);

			if (go)
			{
				var source = new PlayerAmmoContainer();
				var target = Utils.tryGetContainer(go, source.id);

				result = Utils.tryTransferMaxAmount(source, target);
			}

			AnimHandler.handleFxAndAnims(result, vac, go);
			return false;
		}


		static bool handleVacMode(WeaponVacuum vac)
		{
			if (!Input.GetKey(Config.instantModeKey))
				return true;

			// vacpack slots should be filled by separate actions
			bool currentFrameAction = Common.Vacpack.Utils.frameVacModeChanged == Time.frameCount;
			bool result = false;

			if (currentFrameAction && Common.Vacpack.Utils.tryGetPointedObject<SiloCatcher>(vac) is SiloCatcher silo)
			{
				var source = Utils.tryGetContainer(silo);
				var target = new PlayerAmmoContainer(source.id);

				result = Utils.tryTransferMaxAmount(source, target);
			}

			AnimHandler.handleFxAndAnims(result, vac);
			return false;
		}
	}
}