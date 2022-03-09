using System.Linq;

using HarmonyLib;
using UnityEngine;

using SRML;
using SRML.SR;
using SRML.SR.SaveSystem;
using SRML.SR.SaveSystem.Data;

namespace CustomGadgetSites
{
	static class SaveLoadUtils
	{
		const string dataPieceName = "gadget-sites";

		static GadgetSiteManager.GadgetSiteInfo dataToSite(DataPiece dataPiece)
		{
			return dataPiece is CompoundDataPiece data? new (data.GetValue<string>("id"), data.GetValue<Vector3>("position")): null;
		}

		static DataPiece siteToData(GadgetSiteManager.GadgetSiteInfo site)
		{
			CompoundDataPiece data = new (site.id);
			data.SetPiece("id", site.id);
			data.SetPiece("position", site.position);
			return data;
		}

		public static void init()
		{
			SaveRegistry.RegisterWorldDataPreLoadDelegate(data =>
				GadgetSiteManager.loadSitesInfo(data.GetCompoundPiece(dataPieceName).DataList.Select(dataToSite)));

			// we're using publicized SRML assembly (because of the patch below)
			static void onGameLoad(SceneContext _) => GadgetSiteManager.processSitesInfo();
			typeof(SRCallbacks).GetEvent("PreSaveGameLoad").GetAddMethod().Invoke(null, new[] { (SRCallbacks.OnSaveGameLoadedDelegate)onGameLoad });

			SaveRegistry.RegisterWorldDataSaveDelegate(data =>
				data.SetPiece(dataPieceName, GadgetSiteManager.sites.Select(siteToData).ToHashSet()));
		}

		// fix for SRML bug (first save for a new world doesn't contains extended data)
		[HarmonyPatch(typeof(ExtendedData), "Push")]
		static class ExtendedData_Push_Patch
		{
			static void Prefix()
			{
				var modInfo = SRModLoader.Mods[Main.id];

				if (!ExtendedData.worldSaveData.ContainsKey(modInfo))
					ExtendedData.worldSaveData[modInfo] = new CompoundDataPiece("root");
			}
		}
	}
}