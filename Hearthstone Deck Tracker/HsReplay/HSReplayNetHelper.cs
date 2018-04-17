﻿using System.Collections.Generic;
using System.Net;
using Hearthstone_Deck_Tracker.Controls.Error;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Utility.Analytics;
using Hearthstone_Deck_Tracker.Utility.Toasts;
using HearthSim.Core.EventManagers;
using HearthSim.Core.HSReplay;
using HearthSim.Core.Util.EventArgs;

namespace Hearthstone_Deck_Tracker.HsReplay
{
	internal static class HSReplayNetHelper
	{
		static HSReplayNetHelper()
		{
			//ConfigWrapper.CollectionSyncingChanged += () => SyncCollection().Forget();
			Core.HSReplay.OAuth.CollectionUpdated += HandleCollectionUpdated;
			Core.HSReplay.Events.CollectionUploadError += HandleCollectionUploadError;
			Core.HSReplay.Events.BlizzardAccountClaimed += args => Influx.OnBlizzardAccountClaimed(true);
			Core.HSReplay.Events.BlizzardAccountClaimError += HandleBlizzardAccountClaimError;
			Core.HSReplay.OAuth.AuthenticationError += HandleAuthenticationError;
			Core.HSReplay.OAuth.Authenticating += HandleAuthenticating;
			Core.HSReplay.OAuth.LoggedOut += Influx.OnOAuthLogout;
			Core.HSReplay.OAuth.Authenticated += () => Influx.OnOAuthLoginComplete(AuthenticationErrorType.None);
			Core.HSReplay.OAuth.AuthenticationBrowserError += HandleAuthenticationBrowserError;
			Core.HSReplay.LogUploader.UploadComplete += HandleUploadComplete;
			Core.HSReplay.LogUploader.UploadError += args =>
			{
				//todo? this might be handled elsewhere
			};
		}

		private static void HandleUploadComplete(UploadCompleteEventArgs args)
		{
			if(args.Status.Success)
			{
				//TODO: Update GameStats
			}
			else
			{
				//TODO: Store powerlog
				var status = (args.Status.Exception as WebException)?.Status ?? WebExceptionStatus.UnknownError;
				Influx.OnGameUploadFailed(status);
			}
		}

		private static void HandleAuthenticationBrowserError(string url)
		{
			ErrorManager.AddError("Could not open your browser.",
				"Please open the following url in your browser to continue:\n\n" + url, true);
		}

		private static void HandleAuthenticating(bool authenticating)
		{
			if(authenticating)
				Influx.OnOAuthLoginInitiated();
		}

		private static void HandleCollectionUpdated()
		{
			ToastManager.ShowCollectionUpdatedToast();
			Influx.OnCollectionSynced(true);
		}

		private static void HandleCollectionUploadError(CollectionUploadError args)
		{
			Influx.OnCollectionSynced(false);
			ErrorManager.AddError("HSReplay.net Error", "Could not upload your collection. Please try again later.");
		}

		private static void HandleAuthenticationError(AuthenticationErrorType args)
		{
			Influx.OnOAuthLoginComplete(args);
			if(args == AuthenticationErrorType.AccountData)
			{
				ErrorManager.AddError("HSReplay.net Error",
					"Could not load HSReplay.net account status. Please try again later.");
			}
			else
			{
				ErrorManager.AddError("HSReplay.net Error",
					"Could not authenticate with HSReplay.net. Please try running HDT as administrator "
					+ "(right-click the exe and select 'Run as administrator').\n"
					+ "If that does not help please try again later.", true);
			}
		}

		private static void HandleBlizzardAccountClaimError(BlizzardAccountClaimEventArgs args)
		{
			Influx.OnBlizzardAccountClaimed(false);
			if(args.Error == ClaimError.AlreadyClaimed)
			{
				ErrorManager.AddError("HSReplay.net Error",
					$"Your blizzard account ({args.BattleTag}, {args.Hi}-{args.Lo}) is already attached to another"
					+ " HSReplay.net Account. Please contact us at contact@hsreplay.net if this is not correct.");
			}
			else
			{
				ErrorManager.AddError("HSReplay.net Error",
					$"Could not attach your Blizzard account ({args.BattleTag}, {args.Hi}-{args.Lo}) to"
					+ $"your HSReplay.net Account ({Core.HSReplay.OAuth.AccountData?.Username})."
					+ " Please try again later or contact us at contact@hsreplay.net if this persists.");
			}
		}

		public static void OpenDecksUrlWithCollection(string campaign)
		{
			var query = new List<string>();
			if(Core.Hearthstone.Account.IsLoaded)
			{
				var region = Helper.GetRegion(Core.Hearthstone.Account.AccountHi);
				query.Add($"hearthstone_account={(int)region}-{Core.Hearthstone.Account.AccountLo}");
			}

			Helper.TryOpenUrl(Helper.BuildHsReplayNetUrl("decks", campaign, query, new[] { "maxDustCost=0" }));
		}
	}
}
