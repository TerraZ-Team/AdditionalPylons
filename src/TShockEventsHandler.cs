using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TShockAPI;
using Terraria.GameContent.NetModules;
using TShockAPI.Net;
using Terraria.DataStructures;
using Terraria.GameContent.Tile_Entities;
using System;

namespace AdditionalPylons
{
    static class TShockEventsHandler
    {
        private static readonly HashSet<int> pylonItemIDList = new HashSet<int>() { 4875, 4876, 4916, 4917, 4918, 4919, 4920, 4921, 4951 };
        private static readonly HashSet<int> playersHoldingPylon = new HashSet<int>();

        public static void RegisterHandlers()
        {
            GetDataHandlers.PlayerUpdate.Register(OnPlayerUpdate);
            GetDataHandlers.PlaceTileEntity.Register(OnPlaceTileEntity, HandlerPriority.High);

            // STR Must be higher than TShock's to do some pre handling.
            GetDataHandlers.SendTileRect.Register(OnSendTileRect, HandlerPriority.High);
        }
        public static void UnrgisterHandlers()
        {
            GetDataHandlers.PlayerUpdate.UnRegister(OnPlayerUpdate);
            GetDataHandlers.PlaceTileEntity.UnRegister(OnPlaceTileEntity);
            GetDataHandlers.SendTileRect.UnRegister(OnSendTileRect);
        }
        private static void OnSendTileRect(object sender, GetDataHandlers.SendTileRectEventArgs args)
        {
            // Respect Highest priority plugin if they really needed it...
            if (args.Handled)
                return;

            // if player doesn't even have the permissions, no need to check data
            if (!args.Player.HasPermission(Permissions.infiniteplace))
                return;

            // Minimum sanity checks this STR is *probably* pylon
            if (args.Width != 3 || args.Length != 4)
                return;

            long savePosition = args.Data.Position;
            NetTile[,] tiles = new NetTile[args.Width, args.Length];

            for (int x = 0; x < args.Width; x++)
            {
                for (int y = 0; y < args.Length; y++)
                {
                    tiles[x, y] = new NetTile(args.Data);
                    if (tiles[x, y].Type != Terraria.ID.TileID.TeleportationPylon)
                    {
                        args.Data.Seek(savePosition, System.IO.SeekOrigin.Begin);
                        return;
                    }
                }
            }

            // Reset back the data
            args.Data.Seek(savePosition, System.IO.SeekOrigin.Begin);

            // Simply clear the Main system's pylon network to fool server >:DD
            // This works simply because the pylon system is refreshed anyways when it gets placed.
            // This section is required because TShock reimplmented STR with bouncer,
            // which then calls PlaceEntityNet which rejects the pylon because internally in Main.PylonSystem already contained a pylon of this type
            Main.PylonSystem._pylons.Clear();
        }

        private static void OnPlayerUpdate(object sender, GetDataHandlers.PlayerUpdateEventArgs args)
        {
            if (args.Handled)
                return;

            if (!args.Player.HasPermission(Permissions.infiniteplace))
                return;

            int holdingItem = args.Player.TPlayer.inventory[args.SelectedItem].netID;
            bool alreadyHoldingPylon = playersHoldingPylon.Contains(args.PlayerId);
            bool isHoldingPylon = pylonItemIDList.Contains(holdingItem);

            if (alreadyHoldingPylon)
            {
                if (!isHoldingPylon)
                {
                    // stopped holding pylon
                    playersHoldingPylon.Remove(args.PlayerId);

                    // Reload the Pylon system for player client
                    SendPlayerPylonSystem(args.PlayerId, true);
                }
            }
            else
            {
                if (isHoldingPylon)
                {
                    // Started holding pylon
                    playersHoldingPylon.Add(args.PlayerId);

                    // Clear Pylon System for player client
                    SendPlayerPylonSystem(args.PlayerId, false);
                }
            }
        }

        private static void OnPlaceTileEntity(object sender, GetDataHandlers.PlaceTileEntityEventArgs args)
        {
            if (args.Handled)
                return;

            if (args.Type != TETeleportationPylon._myEntityID)
                return;            

            // Send STR to update non-inf pylons players's first pylon placement
            if (!args.Player.HasPermission(Permissions.infiniteplace))
            {
                TSPlayer.All.SendTileRect((short)args.X, (short)args.Y, 3, 4);
                return;
            }

            TETeleportationPylon.Place(args.X, args.Y);

            // This is required to update the Server on the pylon list.
            // NOTE: Reset will broadcast changes to all players.
            Main.PylonSystem.Reset();

            // Send STR after manually doing TETeleportationPylon.Place() since other clients don't know about this pylon
            TSPlayer.All.SendTileRect((short)args.X, (short)args.Y, 3, 4);

            playersHoldingPylon.Remove(args.Player.Index);

            //e.Handled = true;
        }
        private static void SendPlayerPylonSystem(int playerId, bool addPylons)
        {
            foreach (Terraria.GameContent.TeleportPylonInfo pylon in Main.PylonSystem.Pylons)
            {
                Terraria.Net.NetManager.Instance.SendToClient(
                  NetTeleportPylonModule.SerializePylonWasAddedOrRemoved(pylon, addPylons ? NetTeleportPylonModule.SubPacketType.PylonWasAdded : NetTeleportPylonModule.SubPacketType.PylonWasRemoved),
                  playerId
                );
            }
        }

    }
}
