using System.Linq;

using UnityEngine;

using SRML.SR;
using SRML.SR.SaveSystem;
using SRML.SR.SaveSystem.Data;

namespace CustomGadgetSites
{
	static class SaveLoadUtils
	{
		const string dataPieceName = "gadget-sites";

		static GadgetSiteManager.CustomGadgetSite dataToSite(DataPiece dataPiece)
		{
			return dataPiece is CompoundDataPiece data? new (data.GetValue<string>("id"), data.GetValue<Vector3>("position")): null;
		}

		static DataPiece siteToData(GadgetSiteManager.CustomGadgetSite site)
		{
			CompoundDataPiece data = new (site.id);
			data.SetPiece("id", site.id);
			data.SetPiece("position", site.position);
			return data;
		}

		public static void init()
		{
			SaveRegistry.RegisterWorldDataPreLoadDelegate(data =>
				GadgetSiteManager.loadSites(data.GetCompoundPiece(dataPieceName).DataList.Select(dataToSite)));

			SRCallbacks.PreSaveGameLoad += _ => GadgetSiteManager.createLoadedSites();

			SaveRegistry.RegisterWorldDataSaveDelegate(data =>
				data.SetPiece(dataPieceName, GadgetSiteManager.sites.Select(siteToData).ToHashSet()));
		}
	}
}