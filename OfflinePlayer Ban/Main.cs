using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Newtonsoft.Json;
using TShockAPI.DB;

namespace OfflinePlayer_Ban
{
	[ApiVersion(2, 1)]
	public class OfflinePlayer_Ban : TerrariaPlugin
	{
		#region Plugin Info
		public override string Name => "OfflinePlayer Ban";
		public override string Author => "hdseventh";
		public override string Description => "A plugin to ban Offline players.";
		public override Version Version { get { return new Version(1, 0, 1, 0); } }
		#endregion

		public OfflinePlayer_Ban(Main game) : base(game)
		{

		}

		#region Initialize/Dispose
		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
			}
			base.Dispose(disposing);
		}
		#endregion


		#region Hooks
		private void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("opb.ban", OfflineBan, "oban"));
			Commands.ChatCommands.Add(new Command("opb.ban", IPBan, "ipban"));
		}
		#endregion

		#region Commands
		private void OfflineBan(CommandArgs args)
		{

			AddBanResult banResult;
			string reason = "Banned.";
			string duration = null;
			var result = new System.Text.StringBuilder();
			DateTime expiration = DateTime.MaxValue;

			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage($"Invalid syntax: {TShock.Config.Settings.CommandSpecifier}oban <username> <d/m/h/s> <reason>");
				return;
			}

			var user = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
			if (user == null)
			{
				args.Player.SendErrorMessage("Unknown account.");
				return;
			}
			var iplist = JsonConvert.DeserializeObject<List<string>>(user.KnownIps);
			if (iplist.Count == 0 || string.IsNullOrWhiteSpace(user.UUID))
			{
				args.Player.SendErrorMessage("This is an empty account.");
				return;
			}

			if(args.Parameters.Count >= 2)
            {
				duration = args.Parameters[1];
				if (TShockAPI.TShock.Utils.TryParseTime(duration, out int seconds))
				{
					expiration = DateTime.UtcNow.AddSeconds(seconds);
				}
			}

			if(args.Parameters.Count > 2)
            {
				reason = args.Parameters[2];
            }

			string[] identifier = { $"acc:{user.Name}", $"uuid:{user.UUID}", $"name:{user.Name}"};
			for (int i = 0; i <= 2; i++)
			{
				banResult = TShockAPI.TShock.Bans.InsertBan(identifier[i], reason, args.Player.Name, DateTime.UtcNow, expiration);
				if (banResult.Ban != null)
				{
					result.Append($"Ban added. Ticket Number {banResult.Ban.TicketNumber} was created for identifier {identifier[i]}.\n");
				}
				else
				{
					result.Append($"Failed to add ban for identifier: {identifier[i]}\n");
					result.Append($"Reason: {banResult.Message}\n");
				}

			}

			foreach(var ip in iplist)
            {
				banResult = TShockAPI.TShock.Bans.InsertBan($"ip:{ip}", reason, args.Player.Name, DateTime.UtcNow, expiration);
				if (banResult.Ban != null)
				{
					result.Append($"Ban added. Ticket Number {banResult.Ban.TicketNumber} was created for identifier ip:{ip}.\n");
				}
				else
				{
					result.Append($"Failed to add ban for identifier: ip:{ip}\n");
					result.Append($"Reason: {banResult.Message}\n");
				}
			}
			args.Player.SendSuccessMessage(result.ToString());
			var players = TShockAPI.TSPlayer.FindByNameOrID(user.Name);
			if(players.Count > 0)
            {
				players[0].Disconnect($"You have been banned: {reason}");
			}
		}

		private void IPBan(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage($"Invalid syntax: {TShock.Config.Settings.CommandSpecifier}ipban <username> <d/m/h/s> <reason>");
				return;
			}

			var user = TShock.UserAccounts.GetUserAccountByName(args.Parameters[0]);
			if (user == null)
			{
				args.Player.SendErrorMessage("Unknown account.");
				return;
			}
			var iplist = JsonConvert.DeserializeObject<List<string>>(user.KnownIps);
			if (iplist.Count == 0 || string.IsNullOrWhiteSpace(user.UUID))
			{
				args.Player.SendErrorMessage("This is an empty account.");
				return;
			}

			AddBanResult banResult;
			string reason = "Banned.";
			string duration = null;
			var result = new System.Text.StringBuilder();
			DateTime expiration = DateTime.MaxValue;

			if (args.Parameters.Count >= 2)
			{
				duration = args.Parameters[1];
				if (TShockAPI.TShock.Utils.TryParseTime(duration, out int seconds))
				{
					expiration = DateTime.UtcNow.AddSeconds(seconds);
				}
			}

			if (args.Parameters.Count > 2)
			{
				reason = args.Parameters[2];
			}

			foreach (var ip in iplist)
			{
				banResult = TShockAPI.TShock.Bans.InsertBan($"ip:{ip}", reason, args.Player.Name, DateTime.UtcNow, expiration);
				if (banResult.Ban != null)
				{
					result.Append($"Ban added. Ticket Number {banResult.Ban.TicketNumber} was created for identifier ip:{ip}.\n");
				}
				else
				{
					result.Append($"Failed to add ban for identifier: ip:{ip}\n");
					result.Append($"Reason: {banResult.Message}\n");
				}
			}
			args.Player.SendSuccessMessage(result.ToString());
		}
		#endregion
	}
}
