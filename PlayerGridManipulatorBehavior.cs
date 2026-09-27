using System;
using MechaVox.Behaviors.DI;
using MechaVox.Behaviors.Matrix;
using MechaVox.Behaviors.UI;
using MechaVox.DependencyInjection;
using MechaVox.Entities;
using MechaVox.Extensions;
using MechaVox.Matrix;
using MechaVox.Registries;
using MechaVox.UI;
using MysticEye.Inputs;
using NUnit.Framework.Constraints;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
namespace MechaVox.Behaviors.Players
{
    [DefaultExecutionOrder(2)]
    public class PlayerGridManipulatorBehavior : ScopedBehavior
    {
        [AutoWire]
        private VoxelRegistry voxelRegistry;

        [AutoWire]
        private InputManager inputManager;
        [AutoWire]
        private Scope scope;
        [AutoWire]
        private Player player;
        [AttachBehavior]
        private PlayerBehavior playerBehavior;

        private VoxelSelection selection;
        private VoxelClipboard currentClipboard;
        private VoxelClipboardManipulator voxelClipboardManipulator;
        private VoxelProjector voxelProjector;



        #region Reticles
        private Transform selectionRet;



        #endregion


        private void SpawnReticles()
        {
            this.selectionRet = Transform.Instantiate<Transform>(Resources.Load<Transform>("Prefabs/SelectionRet"));
        }
        public override void AfterAttachStart()
        {
            base.AfterAttachStart();
            this.selection = new(this.voxelRegistry);

            this.currentClipboard = new VoxelClipboard(voxelRegistry, 1, 1, 1, new ChunkVoxel[1, 1, 1] { { { new(voxelRegistry.Stone, voxelRegistry) } } });
            if (Cursor.lockState != CursorLockMode.Locked)
                Cursor.lockState = CursorLockMode.Locked;
            SpawnReticles();
        }
        public Camera Camera;
        [AutoWire]
        private UIManager uiManager;
        public override void AfterAttachUpdate()
        {

            if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.Tab))
            {
                if (uiManager.HasUIElement("BlockSelector"))
                {
                    uiManager.UnloadUIElement("BlockSelector");
                }
                else
                {
                    uiManager.LoadUIElement<BlockSelectorMenuBehavior>("BlockSelector", this.scope);

                }
            }
            //reset reticle positions

            if (!this.playerBehavior.PlayerReticlesBehavior.IsHittingChunk)
            {

            }
            else
            {
                tickControls();

            }
        }


        private void tickControls()
        {


            if (this.voxelProjector == null && this.voxelClipboardManipulator == null)
            {

                if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.X) && this.selection.EndPosition != null)
                {
                    this.selection.SetBlocks
                    (this.playerBehavior.PlayerReticlesBehavior.Chunk.World, new(this.voxelRegistry.None, this.voxelRegistry));


                }

                if (inputManager.GetKey(this.playerBehavior,KeyCode.LeftControl))
                {

                    if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.RightBracket) && this.selection.EndPosition != null)
                        this.selection.StartPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.StartPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), playerBehavior.PlayerReticlesBehavior.Chunk.World.Configuration);
                    if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.LeftBracket) && this.selection.EndPosition != null)
                        this.selection.StartPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.StartPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), playerBehavior.PlayerReticlesBehavior.Chunk.World.Configuration);
                    if ((inputManager.GetKeyDown(this.playerBehavior,KeyCode.RightBracket) || inputManager.GetKeyDown(this.playerBehavior,KeyCode.LeftBracket)) && this.selection.EndPosition != null)
                    {
                        //this.selection.EndPosition = this.selection.CurrentPosition;
                        RecalculateSelectionVisualization();

                    }
                }
                else
                {


                    if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.RightBracket) && this.selection.EndPosition != null)
                        this.selection.CurrentPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.CurrentPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), playerBehavior.PlayerReticlesBehavior.Chunk.World.Configuration);
                    if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.LeftBracket) && this.selection.EndPosition != null)
                        this.selection.CurrentPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.CurrentPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), playerBehavior.PlayerReticlesBehavior.Chunk.World.Configuration);
                    if ((inputManager.GetKeyDown(this.playerBehavior,KeyCode.RightBracket) || inputManager.GetKeyDown(this.playerBehavior,KeyCode.LeftBracket)) && this.selection.EndPosition != null)
                    {
                        this.selection.EndPosition = this.selection.CurrentPosition;
                        RecalculateSelectionVisualization();

                    }
                }



                selectionControls();

                if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.Z) && this.selection.EndPosition != null)///
                {
                    this.currentClipboard = this.selection.GetClipboard(playerBehavior.PlayerReticlesBehavior.Chunk.World);

                    this.voxelProjector = new VoxelProjector(this.currentClipboard);
                    //this.voxelProjector.Project(this.playerBehavior.PlayerReticlesBehavior.NormalReticle);
                    Helper.CompressVoxels(this.voxelProjector.Project(this.playerBehavior.PlayerReticlesBehavior.NormalReticle), this.currentClipboard.Length, this.currentClipboard.Height, this.currentClipboard.Width);

                }

                if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.C) && this.selection.EndPosition != null)///
                {
                    this.currentClipboard = this.selection.GetClipboard(playerBehavior.PlayerReticlesBehavior.Chunk.World);

                    this.voxelClipboardManipulator = new VoxelClipboardManipulator(this.currentClipboard, this.playerBehavior.PlayerReticlesBehavior.NormalReticle.transform, this.voxelRegistry);
                }


            }


            if (this.voxelProjector != null)///////
            {
                if ((inputManager.GetKeyDown(this.playerBehavior,KeyCode.Backspace)))
                {
                    this.voxelProjector.Dispose();
                    this.voxelProjector = null;
                }


                if (inputManager.GetMouseButtonDown(this.playerBehavior,1))
                {
                    this.playerBehavior.PlayerReticlesBehavior.Chunk.SetVoxel(this.playerBehavior.PlayerReticlesBehavior.NormalPosition, this.playerBehavior.PlayerReticlesBehavior.Chunk.Registry.SubVoxel.CreateSubVoxel(this.voxelProjector.GetClipboard()));
                }



            }
            if (this.voxelClipboardManipulator != null)
            {


                if ((inputManager.GetKeyDown(this.playerBehavior,KeyCode.Backspace)))
                {
                    this.voxelClipboardManipulator.Dispose();
                    this.voxelClipboardManipulator = null;
                }
                else
                {
                    this.handleMatrixManipulatorControls();
                }
            }








        }


        /// <summary>
        /// Snaps a float value to the nearest integer if within a threshold.
        /// </summary>
        private float SnapToNearestIntWithinThreshold(float value, float threshold)
        {
            float nearestInt = Mathf.Round(value);
            if (Mathf.Abs(value - nearestInt) <= threshold)
            {
                return nearestInt;
            }
            return value;
        }

        /// <summary>
        /// Snaps an angle (in degrees) to the nearest multiple of `step` if within `threshold` degrees.
        /// Angles are normalized between 0-360.
        /// </summary>
        private float SnapAngleToNearestMultiple(float angle, float step, float threshold)
        {
            angle = NormalizeAngle(angle);
            float nearestMultiple = Mathf.Round(angle / step) * step;
            if (Mathf.Abs(angle - nearestMultiple) <= threshold)
            {
                return nearestMultiple;
            }
            return angle;
        }

        /// <summary>
        /// Normalize angle to [0, 360)
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }




        private void handleMatrixManipulatorControls()
        {
            if (this.voxelClipboardManipulator == null) return;

            bool manipulated = false;

            Vector2 mouseDelta = new Vector2(inputManager.GetAxis(this.playerBehavior,"Mouse X"), inputManager.GetAxis(this.playerBehavior,"Mouse Y"));
            float scrollDelta = Input.mouseScrollDelta.y;

            float rotationSpeed = 360f;
            float scaleSpeed = .25f;
            float positionSpeed = 0.1f;
            float offsetSpeed = 0.5f;


            if (inputManager.GetKey(this.playerBehavior,KeyCode.LeftControl) && mouseDelta.magnitude != 0)
            {

                // if (Clipboard.IsQuantized)
                // {
                //     Clipboard.ResetQuantization();
                // }

                // Rotation manipulation
                Vector3 euler = voxelClipboardManipulator.Rotation.eulerAngles;
                euler.x += -mouseDelta.y * rotationSpeed * Time.deltaTime;
                euler.y += -mouseDelta.x * rotationSpeed * Time.deltaTime;
                euler.z += scrollDelta * rotationSpeed;

                // Snap rotation to nearest multiple of 45 within 3 degrees
                euler.x = SnapAngleToNearestMultiple(euler.x, 45f, 3f);
                euler.y = SnapAngleToNearestMultiple(euler.y, 45f, 3f);
                euler.z = SnapAngleToNearestMultiple(euler.z, 45f, 3f);

                Quaternion newRotation = Quaternion.Euler(euler);

                if (newRotation != voxelClipboardManipulator.Rotation)
                {
                    voxelClipboardManipulator.Rotation = newRotation; // uses your property, which recalculates matrix
                    manipulated = true;
                }


            }

            if (inputManager.GetKey(this.playerBehavior,KeyCode.LeftAlt) && mouseDelta.magnitude != 0)
            {

                Vector3 offset = new Vector3(mouseDelta.x, mouseDelta.x, mouseDelta.x) * scaleSpeed;
                if (inputManager.GetKey(this.playerBehavior,KeyCode.LeftShift))
                {
                    offset = new Vector3(mouseDelta.x, mouseDelta.y, scrollDelta) * scaleSpeed;
                }
                Vector3 newScale = voxelClipboardManipulator.Scale + offset;

                // Snap scale to integers within 0.3
                newScale.x = SnapToNearestIntWithinThreshold(newScale.x, 0.1f);
                newScale.y = SnapToNearestIntWithinThreshold(newScale.y, 0.1f);
                newScale.z = SnapToNearestIntWithinThreshold(newScale.z, 0.1f);

                voxelClipboardManipulator.Scale = newScale;
                manipulated = true;


            }
            if (inputManager.GetKey(this.playerBehavior,KeyCode.Q))
            {
                this.voxelClipboardManipulator = this.voxelClipboardManipulator.Quantize();

            }

            if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.P))
            {

                this.voxelClipboardManipulator.SaveToDisk("clipboard.bin");
            }

            if (inputManager.GetKeyDown(this.playerBehavior,KeyCode.V))
            {
                this.voxelClipboardManipulator.Paste(this.playerBehavior.PlayerReticlesBehavior.NormalPosition, playerBehavior.PlayerReticlesBehavior.Chunk.World);


                this.voxelClipboardManipulator.Dispose();
                this.voxelClipboardManipulator = null;
            }
        }

        private void selectionControls()
        {
            if (inputManager.GetMouseButtonDown(this.playerBehavior,0))
            {
                this.selection = new(voxelRegistry)
                {
                    StartPosition = this.playerBehavior.PlayerReticlesBehavior.CurrentPosition,
                    CurrentPosition = this.playerBehavior.PlayerReticlesBehavior.CurrentPosition
                };
                RecalculateSelectionVisualization();

            }

            if (inputManager.GetMouseButton(this.playerBehavior,0))
            {
                this.selection.CurrentPosition = this.playerBehavior.PlayerReticlesBehavior.CurrentPosition;
                RecalculateSelectionVisualization();
            }

            if (inputManager.GetMouseButtonUp(this.playerBehavior,0))
            {
                this.selection.EndPosition = this.playerBehavior.PlayerReticlesBehavior.CurrentPosition;
                this.selection.CurrentPosition = this.playerBehavior.PlayerReticlesBehavior.CurrentPosition;
                RecalculateSelectionVisualization();

            }

        }

        private void RecalculateSelectionVisualization()
        {
            this.selectionRet.transform.position = this.selection.Min + (this.selection.CenterOffsett) / 2.0f;
            this.selectionRet.transform.localScale = this.selection.ClipboardSize;
        }

        public void SetClipboard(ChunkVoxel chunkVoxel)
        {
            if (this.voxelClipboardManipulator != null)
                this.voxelClipboardManipulator.Dispose();
            if (this.voxelProjector != null)
            {
                this.voxelProjector.Dispose();
            }
            var cv = new ChunkVoxel[1, 1, 1];
            cv[0, 0, 0] = chunkVoxel;

            this.currentClipboard = new(voxelRegistry, 1, 1, 1, cv);
            this.voxelClipboardManipulator = new VoxelClipboardManipulator(this.currentClipboard, this.playerBehavior.PlayerReticlesBehavior.NormalReticle.transform, this.voxelRegistry);

        }
        public VoxelClipboard GetCurrentClipboard() => this.currentClipboard;
        public VoxelMatrixCoordinate GetSubRetPosition()
        {
            return playerBehavior.PlayerReticlesBehavior.CurrentPosition;

        }

        public VoxelSelection GetSelection() => this.selection;

    }

}

/*



            if (inputManager.GetMouseButtonDown(0))
            {
                this.selection = new()
                {
                    StartPosition = this.subRetPosition,
                    CurrentPosition = this.subRetPosition
                };
                this.pointRet.transform.position = subRetPosition;
            }




            if (inputManager.GetMouseButtonDown(1))
            {

                if (this.currentClipboard.Size == 1)
                {
                    this.Entity.GetWorld().SetVoxel(this.playerBehavior.PlayerReticlesBehavior.NormalReticlePosition, this.currentClipboard.Voxels[0, 0, 0].Clone());
                    this.selection.SetBlocks(chunk.Entity.World, this.currentClipboard.Voxels[0, 0, 0].Clone());
                }
            }



            if (inputManager.GetMouseButton(0))
            {
                this.selection.CurrentPosition = this.subRetPosition;
                RecalculateSelectionVisualization();

            }


            if (inputManager.GetMouseButtonUp(0))
            {
                this.selection.EndPosition = this.subRetPosition;
                this.selection.CurrentPosition = this.subRetPosition;

                // this.selectionRet.transform.position = subRetPosition;



            }

            if (inputManager.GetKey(KeyCode.LeftControl))
            {

                if (inputManager.GetKeyDown(KeyCode.RightBracket) && this.selection.EndPosition != null)
                    this.selection.StartPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.StartPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), chunk.Entity.World.Settings);
                if (inputManager.GetKeyDown(KeyCode.LeftBracket) && this.selection.EndPosition != null)
                    this.selection.StartPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.StartPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), chunk.Entity.World.Settings);
                if ((inputManager.GetKeyDown(KeyCode.RightBracket) || inputManager.GetKeyDown(KeyCode.LeftBracket)) && this.selection.EndPosition != null)
                {
                    //this.selection.EndPosition = this.selection.CurrentPosition;
                    RecalculateSelectionVisualization();

                }
            }
            else
            {


                if (inputManager.GetKeyDown(KeyCode.RightBracket) && this.selection.EndPosition != null)
                    this.selection.CurrentPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.CurrentPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), chunk.Entity.World.Settings);
                if (inputManager.GetKeyDown(KeyCode.LeftBracket) && this.selection.EndPosition != null)
                    this.selection.CurrentPosition = VoxelMatrixCoordinate.ShiftGrid(this.selection.CurrentPosition.WorldPosition, this.Camera.transform.forward.ToInt3(FloatRoundMethod.Midpoint), chunk.Entity.World.Settings);
                if ((inputManager.GetKeyDown(KeyCode.RightBracket) || inputManager.GetKeyDown(KeyCode.LeftBracket)) && this.selection.EndPosition != null)
                {
                    this.selection.EndPosition = this.selection.CurrentPosition;
                    RecalculateSelectionVisualization();

                }
            }


            if (inputManager.GetKeyDown(KeyCode.V) && this.currentClipboard != null && this.currentClipboard.Size > 0)
            {
                if (this.currentClipboard.Size == 1)
                {
                    this.selection.SetBlocks(chunk.Entity.World, this.currentClipboard.Voxels[0, 0, 0].Clone());
                }
                else
                {
                    this.currentClipboard.Paste(this.playerBehavior.PlayerReticlesBehavior.NormalReticlePosition, chunk.Entity.World);
                }

                this.currentClipboard = null;
                if (this.voxelProjector != null)
                {
                    this.voxelProjector.Dispose();
                    this.voxelProjector = null;

                }

            }



            if (inputManager.GetKeyDown(KeyCode.P) && this.currentClipboard != null && this.currentClipboard.Size > 0)
            {

                this.currentClipboard.SaveToDisk("clipboard.bin");



            }

            if (inputManager.GetKeyDown(KeyCode.X) && this.selection.EndPosition != null)
            {
                this.selection.SetBlocks(chunk.Entity.World, new(this.hitChunk.Registry.Air));


            }


            if (inputManager.GetKeyDown(KeyCode.C) && this.selection.EndPosition != null)
            {
                this.currentClipboard = this.selection.GetClipboard(chunk.Entity.World);
                this.voxelProjector = new(this.currentClipboard);
                this.voxelProjector.Project(this.playerBehavior.PlayerReticlesBehavior.NormalReticle);


            }



            if (inputManager.GetKeyDown(KeyCode.Z) && this.selection.EndPosition != null)///
            {
                this.currentClipboard = this.selection.GetClipboard(chunk.Entity.World);
                this.voxelProjector = new(this.currentClipboard);
                this.voxelProjector.Project(this.playerBehavior.PlayerReticlesBehavior.NormalReticle, true);


            }

            // if (inputManager.GetMouseButton(0)) // left
            // {
            //     this.entity.GetWorld().SetBlock(this.subRetPosition,new(this.hitChunk.Registry.Air));
            // }


            // if (inputManager.GetMouseButton(1)) // right
            // {
            //     this.entity.GetWorld().SetBlock(this.playerBehavior.PlayerReticlesBehavior.NormalReticlePosition,new(this.hitChunk.Registry.Grass));
            // }

*/

