using StardewValley;
using StardewModdingAPI;
using Microsoft.Xna.Framework;
using Core.Models;
using System;
using StardewValley.Objects;

namespace Tools.Movement
{
    public class MovementController
    {
        public static MovementController? Instance { get; private set; }

        public bool IsMoving { get; private set; }
        public bool IsArrived { get; private set; }
        public Vector2 Destination { get; private set; }
        
        // Properties for Harmony patches
        public bool IsBeingAutoCommand { get; set; } = false;
        public bool IsBeingControl { get; set; } = false;
        public bool IsHoldingMove { get; set; } = false;
        public bool IsHoldingRunButton { get; set; } = false;

        private readonly PathFindingHelper _pathFinder;
        private readonly ActionHandler _actionHandler;
        private readonly IMonitor _monitor;
        private readonly MovementConfig _config;

        private int _holdTickCount;
        private int _tickCount = 15;
        
        // Cached vector for movement logic
        private Vector2 _vectorAutoMove;

        public MovementController(IMonitor monitor, MovementConfig config)
        {
            Instance = this;
            _monitor = monitor;
            _config = config;
            _pathFinder = new PathFindingHelper(monitor, config);
            _actionHandler = new ActionHandler(monitor);
        }

        public void StartMoveTo(int tileX, int tileY)
        {
            _pathFinder.loadMap();
            Destination = new Vector2(tileX * 64f + 32f, tileY * 64f + 32f); // Center of tile
            _pathFinder.changeDes(Destination);
            IsMoving = true;
            IsArrived = false;
            _actionHandler.cancelAction();
            _holdTickCount = 0;
            _monitor.Log($"[Movement] Started moving to ({tileX}, {tileY})", LogLevel.Info);
        }

        public void StartMoveToNPC(string npcName)
        {
            var npc = Game1.getCharacterFromName(npcName);
            if (npc == null)
            {
                _monitor.Log($"[Movement] NPC {npcName} not found", LogLevel.Warn);
                return;
            }

            _pathFinder.loadMap();
            // Target is the NPC's tile
            Destination = npc.getStandingPosition();
            _pathFinder.changeDes(Destination);
            
            // Set up action handler to interact
            _actionHandler.targetNPC = npc;
            _actionHandler.updateTarget(Destination);
            
            IsMoving = true;
            IsArrived = false;
            _holdTickCount = 0;
            _monitor.Log($"[Movement] Started moving to NPC {npcName} at {Destination}", LogLevel.Info);
        }

        public void Update()
        {
            // NPC tracking logic: if targeting NPC, update destination if they moved
            if (IsMoving && _actionHandler.targetNPC != null && _actionHandler.targetNPC.currentLocation == Game1.player.currentLocation)
            {
                var npcPos = _actionHandler.targetNPC.getStandingPosition();
                if (TileUtil.toTile(_pathFinder.getCurrentDestinationTile()) != TileUtil.toTile(npcPos))
                {
                    Destination = npcPos;
                    _pathFinder.changeDes(Destination);
                    _actionHandler.updateTarget(Destination);
                }
            }

            if (!IsMoving) return;

            // Note: The actual movement logic is now driven by the Harmony patch on Game1.UpdateControlInput
            // which calls MoveVectorToCommand. This Update() method is mainly for high-level state management.

            if (_actionHandler.tryDoAction())
            {
                Stop();
                IsArrived = true; // Considered arrived if action performed
                return;
            }
            
            // If pathfinder says we are done and we are colliding with destination (or very close)
            if (_pathFinder.nextPath() is null)
            {
                 // Check if we arrived at destination tile
                 if (Vector2.Distance(Game1.player.GetBoundingBox().Center.ToVector2(), Destination) < 64f)
                 {
                     // Close enough?
                     Stop();
                     IsArrived = true;
                     _monitor.Log("[Movement] Arrived at destination (Path ended)", LogLevel.Info);
                 }
            }
        }

        // Logic from ModEntry.MoveVectorToCommand
        public void MoveVectorToCommand()
        {
            if (!IsMoving)
                return;

            // We following the path finding result
            _vectorAutoMove = _pathFinder.moveDirection();

            if (_vectorAutoMove == Vector2.Zero)
            {
                IsMoving = false;
                // If stopped, maybe we arrived?
                IsArrived = true;
                return;
            }

            // Handling unreachable destination but colliding with it
            // Some time, the destination is unreachable, but we will goes until
            // colision with the grab tiles, then try to facing toward it
            // before stop and perform action if needed
            if (_pathFinder.nextPath() is null && Game1.player.isColliding(Game1.player.currentLocation, TileUtil.toTile(_pathFinder.originalDestination)))
            {
                // _monitor.Log("Colliding to grabTile", LogLevel.Trace);
                IsMoving = false;
                IsArrived = true; // Treated as arrived so action handler can try to interact

                var facingVector = _pathFinder.moveDirection();
                if (facingVector.X > facingVector.Y)
                {
                    if (facingVector.X < 0)
                        Game1.player.SetMovingLeft(true);
                    else
                        Game1.player.SetMovingRight(true);
                }
                else
                {
                    if (facingVector.Y < 0)
                        Game1.player.SetMovingUp(true);
                    else
                        Game1.player.SetMovingDown(true);
                }
                return;
            }

            if (_vectorAutoMove.Length() > 1f)
                _vectorAutoMove.Normalize();
            
            if (_vectorAutoMove.X > 0)
                Game1.player.SetMovingRight(true);
            else
                Game1.player.SetMovingLeft(true);

            if (_vectorAutoMove.Y > 0)
                Game1.player.SetMovingDown(true);
            else
                Game1.player.SetMovingUp(true);
        }

        // Logic from ModEntry.MovePosition
        public void MovePosition(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation)
        {
            if (Game1.player.IsSitting()) { return; }

            if (Game1.CurrentEvent == null || Game1.CurrentEvent.playerControlSequence)
            {
                if (Game1.shouldTimePass() && Game1.player.temporarilyInvincible)
                {
                    if (Game1.player.temporaryInvincibilityTimer < 0)
                    {
                        Game1.player.currentTemporaryInvincibilityDuration = 1200;
                    }
                    Game1.player.temporaryInvincibilityTimer += time.ElapsedGameTime.Milliseconds;
                    if (Game1.player.temporaryInvincibilityTimer > Game1.player.currentTemporaryInvincibilityDuration)
                    {
                        Game1.player.temporarilyInvincible = false;
                        Game1.player.temporaryInvincibilityTimer = 0;
                    }
                }
            }
            else if (Game1.player.temporarilyInvincible)
            {
                Game1.player.temporarilyInvincible = false;
                Game1.player.temporaryInvincibilityTimer = 0;
            }

            if (Game1.activeClickableMenu != null)
            {
                if (Game1.CurrentEvent == null) return;
                if (Game1.CurrentEvent.playerControlSequence) return;
            }

            if (Game1.player.isRafting)
            {
                Game1.player.moveRaft(currentLocation, time);
                return;
            }

            // Velocity handling
            if (Game1.player.xVelocity != 0f || Game1.player.yVelocity != 0f)
            {
                if (double.IsNaN((double)Game1.player.xVelocity) || double.IsNaN((double)Game1.player.yVelocity))
                {
                    Game1.player.xVelocity = 0f;
                    Game1.player.yVelocity = 0f;
                }

                Rectangle bounds = Game1.player.GetBoundingBox();
                Rectangle value = new Rectangle(bounds.X + (int)Math.Floor(Game1.player.xVelocity), bounds.Y - (int)Math.Floor(Game1.player.yVelocity), bounds.Width, bounds.Height);
                Rectangle nextPositionCeil = new Rectangle(bounds.X + (int)Math.Ceiling(Game1.player.xVelocity), bounds.Y - (int)Math.Ceiling(Game1.player.yVelocity), bounds.Width, bounds.Height);
                Rectangle nextPosition = Rectangle.Union(value, nextPositionCeil);

                if (!currentLocation.isCollidingPosition(nextPosition, viewport, true, -1, false, Game1.player))
                {
                    Game1.player.position.X += Game1.player.xVelocity;
                    Game1.player.position.Y -= Game1.player.yVelocity;
                    Game1.player.xVelocity -= Game1.player.xVelocity / 16f;
                    Game1.player.yVelocity -= Game1.player.yVelocity / 16f;
                    if (Math.Abs(Game1.player.xVelocity) <= 0.05f) Game1.player.xVelocity = 0f;
                    if (Math.Abs(Game1.player.yVelocity) <= 0.05f) Game1.player.yVelocity = 0f;
                }
                else
                {
                    Game1.player.xVelocity -= Game1.player.xVelocity / 16f;
                    Game1.player.yVelocity -= Game1.player.yVelocity / 16f;
                    if (Math.Abs(Game1.player.xVelocity) <= 0.05f) Game1.player.xVelocity = 0f;
                    if (Math.Abs(Game1.player.yVelocity) <= 0.05f) Game1.player.yVelocity = 0f;
                }
            }

            if (Game1.player.CanMove || Game1.eventUp || Game1.player.controller != null || Game1.player.canStrafeForToolUse())
            {
                Game1.player.TemporaryPassableTiles.ClearNonIntersecting(Game1.player.GetBoundingBox());
                float movementSpeed = Game1.player.getMovementSpeed();
                Game1.player.temporarySpeedBuff = 0f;

                if (Game1.player.movementDirections.Contains(0))
                    TryMoveDrection(time, viewport, currentLocation, FaceDirection.UP);

                if (Game1.player.movementDirections.Contains(2))
                    TryMoveDrection(time, viewport, currentLocation, FaceDirection.DOWN);

                if (Game1.player.movementDirections.Contains(1))
                    TryMoveDrection(time, viewport, currentLocation, FaceDirection.RIGHT);

                if (Game1.player.movementDirections.Contains(3))
                    TryMoveDrection(time, viewport, currentLocation, FaceDirection.LEFT);

                // Diagonal stop check
                if (Game1.player.movementDirections.Count == 2)
                {
                    if (Math.Abs(_vectorAutoMove.Y / _vectorAutoMove.X).CompareTo(0.45f) < 0)
                    {
                        Game1.player.SetMovingDown(false);
                        Game1.player.SetMovingUp(false);
                    }
                    else if (Math.Abs(_vectorAutoMove.Y) > Math.Sin(Math.PI / 3))
                    {
                        Game1.player.SetMovingRight(false);
                        Game1.player.SetMovingLeft(false);
                    }
                }
                return;
            }

            if (Game1.player.movementDirections.Count > 0 && !Game1.player.UsingTool)
            {
                Game1.player.FarmerSprite.intervalModifier = 1f - (Game1.player.running ? 0.0255f : 0.025f) * (Math.Max(1f, ((float)Game1.player.speed + (Game1.eventUp ? 0f : ((float)(int)Game1.player.addedSpeed + (Game1.player.isRidingHorse() ? 4.6f : 0f)))) * Game1.player.movementMultiplier * (float)Game1.currentGameTime.ElapsedGameTime.Milliseconds) * 1.25f);
            }
            else
            {
                Game1.player.FarmerSprite.intervalModifier = 1f;
            }

            if (currentLocation != null && currentLocation.isFarmerCollidingWithAnyCharacter())
            {
                Game1.player.TemporaryPassableTiles.Add(new Rectangle(Game1.player.TilePoint.X * 64, Game1.player.TilePoint.Y * 64, 64, 64));
            }
        }

        private void TryMoveDrection(GameTime time, xTile.Dimensions.Rectangle viewport, GameLocation currentLocation, FaceDirection faceDirection)
        {
            Warp warp = Game1.currentLocation.isCollidingWithWarp(Game1.player.nextPosition(((int)faceDirection)), Game1.player);
            if (warp != null && Game1.player.IsLocalPlayer)
            {
                Game1.player.warpFarmer(warp);
                return;
            }
            float movementSpeed = Game1.player.getMovementSpeed();
            if (Game1.player.movementDirections.Contains((int)faceDirection))
            {
                Rectangle nextPos = Game1.player.nextPosition((int)faceDirection);

                if (!currentLocation.isCollidingPosition(nextPos, viewport, true, 0, false, Game1.player))
                {
                    if (faceDirection == FaceDirection.UP || faceDirection == FaceDirection.DOWN)
                        Game1.player.position.Y += movementSpeed * _vectorAutoMove.Y;
                    else
                        Game1.player.position.X += movementSpeed * _vectorAutoMove.X;

                    Game1.player.behaviorOnMovement((int)faceDirection);
                }
                else
                {
                    nextPos = Game1.player.nextPositionHalf((int)faceDirection);

                    if (!currentLocation.isCollidingPosition(nextPos, viewport, true, 0, false, Game1.player))
                    {

                        if (faceDirection == FaceDirection.UP || faceDirection == FaceDirection.DOWN)
                            Game1.player.position.Y += movementSpeed * _vectorAutoMove.Y / 2f;
                        else
                            Game1.player.position.X += movementSpeed * _vectorAutoMove.X / 2f;

                        Game1.player.behaviorOnMovement((int)faceDirection);
                    }
                    else if (Game1.player.movementDirections.Count == 1)
                    {
                        Rectangle tmp = Game1.player.nextPosition((int)faceDirection);
                        tmp.Width /= 4;
                        bool leftCorner = currentLocation.isCollidingPosition(tmp, viewport, true, 0, false, Game1.player);
                        tmp.X += tmp.Width * 3;
                        bool rightCorner = currentLocation.isCollidingPosition(tmp, viewport, true, 0, false, Game1.player);
                        if (leftCorner && !rightCorner && !currentLocation.isCollidingPosition(Game1.player.nextPosition((int)TileUtil.LeftDirection(faceDirection)), viewport, true, 0, false, Game1.player))
                        {
                            if (faceDirection == FaceDirection.UP || faceDirection == FaceDirection.DOWN)
                                Game1.player.position.X += (float)Game1.player.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
                            else
                                Game1.player.position.Y += (float)Game1.player.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
                        }
                        else if (rightCorner && !leftCorner && !currentLocation.isCollidingPosition(Game1.player.nextPosition((int)TileUtil.RightDirection(faceDirection)), viewport, true, 0, false, Game1.player))
                        {
                            if (faceDirection == FaceDirection.UP || faceDirection == FaceDirection.DOWN)
                                Game1.player.position.X -= (float)Game1.player.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
                            else
                                Game1.player.position.Y -= (float)Game1.player.speed * ((float)time.ElapsedGameTime.Milliseconds / 64f);
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            IsMoving = false;
            IsBeingAutoCommand = false;
            Game1.player.Halt();
        }

        public Vector2 GetMoveDirection()
        {
            if (!IsMoving) return Vector2.Zero;
            return _pathFinder.moveDirection();
        }
    }
}
