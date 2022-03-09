using HarmonyLib;
using UnityEngine;

using Common;

#if DEBUG
using MonomiPark.SlimeRancher.DataModel;
#endif

namespace CustomGadgetSites
{
	[HarmonyPatch(typeof(TargetingUI), "Update")]
	static class TargetingUI_Update_Patch
	{
		// doesn't support changes on the fly
		static class Strings
		{
			static string _key(InControl.PlayerAction action) =>
				XlateKeyText.XlateKey(SRInput.GetButtonKey(action, SRInput.ButtonType.PRIMARY));

			public const string site = "Gadget site";
			public static readonly string createSite = $"Press {_key(SRInput.Actions.attack)} to create";
			public static readonly string moveRemoveSite = $"Press {_key(SRInput.Actions.attack)} to remove\nHold {_key(SRInput.Actions.vac)} to move";
		}

		const float raycastDistance = 10f;

		static WeaponVacuum vacpack;
		static GadgetSite movingSite;

		static bool getTargetPoint(out Vector3 position, out GadgetSite site)
		{
			var tr = vacpack.vacOrigin.transform;
			Ray ray = new (tr.position - tr.up * 2f, tr.up);

			bool point = Physics.Raycast(ray, out var hit, raycastDistance, 1);
			position = hit.point;

			Physics.Raycast(ray, out hit, raycastDistance, 1 << vp_Layer.RaycastOnly);
			site = hit.collider?.gameObject?.getParent()?.GetComponent<GadgetSite>();

			return point || site;
		}

		static void showInfo(TargetingUI ui, GadgetSite site)
		{
			void _showInfo(string title, string info = null)
			{
				ui.nameText.enabled = true;
				ui.nameText.text = title;

				info ??= Strings.moveRemoveSite;
#if DEBUG
				if (site)
					info = $"{info}\n<size=16>{site.gameObject.getFullName()}</size>";
#endif
				if (info != null)
				{
					ui.infoText.enabled = true;
					ui.infoText.text = info;
				}
			}

			if (site?.attached == true)
				return;

			if (site == null)
				_showInfo(Strings.site, Strings.createSite);
			else if (site.id.Contains(Main.id))
				_showInfo(Strings.site + " (custom)");
			else if (site.id.Contains("."))
				_showInfo(Strings.site + " (mc)");
			else
				_showInfo(Strings.site + " (vanilla)");
		}

		static UITemplates uiTemplates => SRSingleton<GameContext>.Instance.UITemplates;
		static void play(SECTR_AudioCue cue) => SECTR_AudioSystem.Play(cue, Vector3.zero, false);

		static void processLeftClick(Vector3 position, GadgetSite site)
		{
			if (!SRInput.Actions.attack.WasPressed || position == default)
				return;

			if (site)
			{
				if (GadgetSiteManager.removeSite(site))
					play(uiTemplates.removeGadgetCue);
			}
			else
			{
				if (GadgetSiteManager.createSite(position))
					play(uiTemplates.placeGadgetCue);
				else
					play(uiTemplates.errorCue);
			}
		}

		static void processRightClick(Vector3 position, GadgetSite site)
		{
			if (SRInput.Actions.vac.WasPressed && !site?.attached)
			{
				movingSite = site;
				play(uiTemplates.clickCue);
			}
			else if (!SRInput.Actions.vac.IsPressed)
			{
				movingSite = null;
			}

			if (movingSite && position != default)
				GadgetSiteManager.moveSite(movingSite, position);
		}

		static void Postfix(TargetingUI __instance)
		{
			if (!vacpack)
				vacpack = SRSingleton<SceneContext>.Instance.Player.GetComponentInChildren<WeaponVacuum>();

			if (vacpack.vacMode != WeaponVacuum.VacMode.GADGET)
				return;

			if (!getTargetPoint(out var position, out var targetSite))
				return;

			if (Config.showSiteInfo)
				showInfo(__instance, targetSite);

			processLeftClick(position, targetSite);
			processRightClick(position, targetSite);
		}
	}

#if DEBUG
	static class DebugPatches
	{
		static readonly bool includeVanillaSites = false;

		static bool shouldLogSite(string siteId) => includeVanillaSites || siteId.Contains(".");

		[HarmonyPatch(typeof(GameModel), "RegisterGadgetSite")]
		static class GameModel_RegisterGadgetSite_Patch
		{
			static void Postfix(string siteId, GameObject gameObject)
			{
				if (shouldLogSite(siteId))
					$"Gadget site registered: {siteId} ({gameObject.name})".logDbg();
			}
		}

		[HarmonyPatch(typeof(GameModel), "UnregisterGadgetSite")]
		static class GameModel_UnregisterGadgetSite_Patch
		{
			static void Postfix(string siteId)
			{
				if (shouldLogSite(siteId))
					$"Gadget site unregistered: {siteId}".logDbg();
			}
		}
	}
#endif // DEBUG
}