using HarmonyLib;
using StardewModdingAPI;
using StardewValley;
using Microsoft.Xna.Framework;
using System;

namespace Tools.Movement
{
    public static class MovementPatcher
    {
        private static IMonitor? _monitor;
        private static readonly string PatchId = "AI_STAR_AGENT.MovementPatcher";

        public static void ApplyPatches(IMonitor monitor)
        {
            _monitor = monitor;
            var harmony = new Harmony(PatchId);

            try
            {
                // Patch Farmer.Halt
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.Halt)),
                    prefix: new HarmonyMethod(typeof(MovementPatcher), nameof(Prefix_Farmer_Halt))
                );

                // Patch Farmer.getMovementSpeed
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.getMovementSpeed)),
                    prefix: new HarmonyMethod(typeof(MovementPatcher), nameof(Prefix_Farmer_getMovementSpeed))
                );

                // Patch Farmer.MovePosition
                harmony.Patch(
                    original: AccessTools.Method(typeof(Farmer), nameof(Farmer.MovePosition), new[] { typeof(GameTime), typeof(xTile.Dimensions.Rectangle), typeof(GameLocation) }),
                    prefix: new HarmonyMethod(typeof(MovementPatcher), nameof(Prefix_Farmer_MovePosition))
                );

                // Patch Game1.UpdateControlInput
                harmony.Patch(
                    original: AccessTools.Method(typeof(Game1), "UpdateControlInput", new[] { typeof(GameTime) }),
                    postfix: new HarmonyMethod(typeof(MovementPatcher), nameof(Postfix_Game1_UpdateControlInput))
                );

                _monitor.Log($"[MovementPatcher] Applied Harmony patches successfully.", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _monitor.Log($"[MovementPatcher] Failed to apply patches: {ex}", LogLevel.Error);
            }
        }

        public static bool Prefix_Farmer_Halt()
        {
            // If movement controller is not active, let original Halt run
            if (MovementController.Instance == null || !MovementController.Instance.IsMoving)
                return true;

            // If we are auto-moving, prevent Halt unless specific conditions
            // Adapted from: return !ModEntry.isMovingAutomaticaly || ModEntry.isBeingAutoCommand;
            return MovementController.Instance.IsBeingAutoCommand;
        }

        public static bool Prefix_Farmer_MovePosition()
        {
            if (MovementController.Instance != null && 
                MovementController.Instance.IsMoving && 
                !MovementController.Instance.IsBeingControl && 
                Context.IsPlayerFree && 
                Game1.player.CanMove)
            {
                MovementController.Instance.MovePosition(Game1.currentGameTime, Game1.viewport, Game1.player.currentLocation);
                return false; // Skip original method
            }
            return true;
        }

        public static void Postfix_Game1_UpdateControlInput()
        {
            if (MovementController.Instance != null && 
                MovementController.Instance.IsMoving &&
                !MovementController.Instance.IsBeingControl && 
                Context.IsPlayerFree && 
                Game1.player.CanMove)
            {
                MovementController.Instance.IsBeingAutoCommand = true;
                MovementController.Instance.MoveVectorToCommand();
                
                // Handle running state
                // Simplified from reference logic
                if (MovementController.Instance.IsHoldingRunButton && !Game1.player.canOnlyWalk)
                {
                    Game1.player.setRunning(!Game1.options.autoRun, false);
                    Game1.player.setMoving(Game1.player.running ? (byte)16 : (byte)48);
                }
                else if (!MovementController.Instance.IsHoldingRunButton && !Game1.player.canOnlyWalk)
                {
                    Game1.player.setRunning(Game1.options.autoRun, false);
                    Game1.player.setMoving(Game1.player.running ? (byte)16 : (byte)48);
                }

                MovementController.Instance.IsBeingAutoCommand = false;
            }
            else if (MovementController.Instance != null)
            {
                MovementController.Instance.IsBeingAutoCommand = false;
            }
        }

        public static bool Prefix_Farmer_getMovementSpeed(ref float __result)
        {
            if (MovementController.Instance != null && 
                MovementController.Instance.IsMoving &&
                !MovementController.Instance.IsBeingControl && 
                Context.IsPlayerFree)
            {
                // Custom speed calculation logic to prevent diagonal slowdown
                if (Game1.player.UsingTool && Game1.player.canStrafeForToolUse())
                {
                    __result = 2f;
                    return false;
                }

                if (Game1.CurrentEvent == null || Game1.CurrentEvent.playerControlSequence)
                {
                    Game1.player.movementMultiplier = 0.066f;
                    float movementSpeed2 = 1f;
                    
                    // Complex logic simplified: Calculate speed without diagonal penalty
                    float baseSpeed = (float)Game1.player.speed;
                    float addedSpeed = Game1.eventUp ? 0f : (Game1.player.addedSpeed + Game1.player.temporarySpeedBuff);
                    
                    if (Game1.player.isRidingHorse())
                    {
                        // Horse logic
                        addedSpeed = Game1.eventUp ? 0f : (Game1.player.addedSpeed + 4.6f + (Game1.player.mount.ateCarrotToday ? 0.4f : 0f));
                        // Missing Book_Horse check but that's fine for now
                    }

                    movementSpeed2 = Math.Max(1f, (baseSpeed + addedSpeed) * Game1.player.movementMultiplier * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds);

                    if (Game1.CurrentEvent == null && Game1.player.hasBuff("19"))
                    {
                        movementSpeed2 = 0f;
                    }
                    __result = movementSpeed2;
                    return false;
                }
                
                // Fallback for other cases
                float movementSpeed = Math.Max(1f, (float)Game1.player.speed + (Game1.eventUp ? ((float)Math.Max(0, Game1.CurrentEvent.farmerAddedSpeed - 2)) : (Game1.player.addedSpeed + (Game1.player.isRidingHorse() ? 5f : Game1.player.temporarySpeedBuff))));
                // Skip diagonal check: if (Game1.player.movementDirections.Count > 1) ...
                __result = movementSpeed;
                return false;
            }
            return true;
        }
    }
}
