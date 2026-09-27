using System;
using System.Collections.Generic;
using MysticEyeStudios.Extensions;
using MysticEyeStudios.Matrix.FieldPopulators;
using MysticEyeStudios.Meshing;
using MysticEyeStudios.Threading;
using UnityEngine;

namespace MysticEyeStudios.Matrix.Fields
{

    public class AsyncFieldInterpreter : IAsyncFieldInterpreter<Vector3, CubicObject?>
    {
        private readonly IFieldPopulator<Vector3, CubicObject?> fieldPopulator;
        private readonly TaskMaster taskMaster;

        public AsyncFieldInterpreter(IFieldPopulator<Vector3, CubicObject?> fieldPopulator, TaskMaster taskMaster)
        {
            this.fieldPopulator = fieldPopulator;
            this.taskMaster = taskMaster;
        }
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public virtual Promise<MeshBuilder, Exception> GenerateMesh(FieldRect<Vector3> renderRect, float gridSize)
        {
            var promise = new Threading.Promise<MeshBuilder, Exception>();
            var builder = new MeshBuilder();

            this.taskMaster.RunAnytime(() =>
            {
                int startX = Mathf.FloorToInt(renderRect.StartPosition.x);
                int startY = Mathf.FloorToInt(renderRect.StartPosition.y);
                int startZ = Mathf.FloorToInt(renderRect.StartPosition.z);

                int endX = Mathf.CeilToInt(renderRect.EndPosition.x);
                int endY = Mathf.CeilToInt(renderRect.EndPosition.y);
                int endZ = Mathf.CeilToInt(renderRect.EndPosition.z);

                // Normalize bounds to guarantee startX <= endX, etc.
                if (startX > endX) { int t = startX; startX = endX; endX = t; }
                if (startY > endY) { int t = startY; startY = endY; endY = t; }
                if (startZ > endZ) { int t = startZ; startZ = endZ; endZ = t; }

                // Base point for calculating voxel offsets
                Vector3 basePoint = new Vector3(startX, startY, startZ);

                // Iterate over every voxel in the defined region
                for (int x = startX; x < endX; x++)
                {
                    for (int y = startY; y < endY; y++)
                    {
                        for (int z = startZ; z < endZ; z++)
                        {
                            // Calculate world position of the voxel center
                            Vector3 voxelPos = new(x, y, z);



                            Polygonise(
                                                                                        voxelPos,
                                                                                        gridSize,
                                                                                        builder);



                            var voxelValue = this.fieldPopulator.Probe(voxelPos);
                            if (voxelValue != null)
                            {
                                if (
                                voxelValue.Value.RenderType == RenderType.Cubic)
                                {
                                    var faceMask = GenerateFaceMask(voxelPos);
                                    builder.DrawCube(new(x, y, z), gridSize, voxelValue.Value.VoxelColor, faceMask);
                                }


                            }
                        }
                    }
                }
                // Sync with the scene mesh

            })
            .OnError(_ => builder.Dispose())
            .Wrap(promise, builder);
            return promise;
        }


        public virtual VoxelFaceMask GenerateFaceMask(Vector3 position)
        {
            var cubicFaceMask = VoxelFaceMask.None;

            cubicFaceMask |= this.fieldPopulator.Probe(position + new Vector3(0, -1, 0)) == null ? VoxelFaceMask.Nadir : VoxelFaceMask.None;
            cubicFaceMask |= this.fieldPopulator.Probe(position + new Vector3(0, 1, 0)) == null ? VoxelFaceMask.Zenith : VoxelFaceMask.None;

            cubicFaceMask |= this.fieldPopulator.Probe(position + new Vector3(-1, 0, 0)) == null ? VoxelFaceMask.West : VoxelFaceMask.None;
            cubicFaceMask |= this.fieldPopulator.Probe(position + new Vector3(1, 0, 0)) == null ? VoxelFaceMask.East : VoxelFaceMask.None;
            cubicFaceMask |= this.fieldPopulator.Probe(position + new Vector3(0, 0, -1)) == null ? VoxelFaceMask.South : VoxelFaceMask.None;
            cubicFaceMask |= this.fieldPopulator.Probe(position + new Vector3(0, 0, 1)) == null ? VoxelFaceMask.North : VoxelFaceMask.None;


            return cubicFaceMask;
        }


        public virtual Promise<MeshBuilder, Exception> GenerateMeshAroundPoint(Vector3 center, float size, float gridSize)
        {
            var promise = new Threading.Promise<MeshBuilder, Exception>();
            var builder = new MeshBuilder();

            this.taskMaster.RunAnytime(() =>
            {


                int radius = Mathf.CeilToInt(
                                       size / gridSize);

                for (int x = -radius; x < radius; x++)
                {
                    for (int y = -radius; y < radius; y++)
                    {
                        for (int z = -radius; z < radius; z++)
                        {


                            Vector3 position =
                              center +
                              new Vector3(x, y, z) *
                              gridSize;

                            Polygonise(
                                                              position,
                                                              gridSize,
                                                              builder);


                            var voxelValue = this.fieldPopulator.Probe(position);
                            if (voxelValue != null)
                            {


                                if (
                                (voxelValue.Value.RenderType & RenderType.Cubic) != RenderType.None)
                                {
                                    var faceMask = GenerateFaceMask(position);


                                    builder.DrawCube(new(x, y, z), gridSize, voxelValue.Value.VoxelColor, faceMask);
                                }
                            }


                        }

                    }

                }
            })
            .OnError(_ => builder.Dispose())
            .Wrap(promise, builder);
            return promise;
        }
        private void Polygonise(
             Vector3 position,
             float gridSize,
             MeshBuilder meshBuilder)
        {
            Vector3[] cornerPositions = new Vector3[8];
            CubicObject?[] cornerObjects = new CubicObject?[8];

            /*
             * Sample all eight cube corners.
             */
            for (int i = 0; i < 8; i++)
            {
                Vector3 samplePosition =
                    position +
                    MarchingCubesHelper.CornerOffsets[i] *
                    gridSize;

                cornerPositions[i] = samplePosition;
                cornerObjects[i] =
                    this.fieldPopulator.Probe(samplePosition);
                if (cornerObjects[i] != null && ((cornerObjects[i].Value.RenderType & RenderType.MarchingCubes) != RenderType.None) == false) // im sorry about this , i dont feel like making this less complex right now
                    cornerObjects[i] = null;

            }

            /*
             * Build the marching-cubes case.
             */
            int cubeIndex = 0;

            for (int i = 0; i < 8; i++)
            {
                if (cornerObjects[i] != null)
                    cubeIndex |= 1 << i;
            }

            if (cubeIndex == 0 || cubeIndex == 255)
                return;

            int edgeMask =
                MarchingCubesHelper.Edges[cubeIndex];

            Vector3[] edgeVertices =
                new Vector3[12];

            Color[] edgeColors =
                new Color[12];

            bool[] edgeHasColor =
                new bool[12];

            /*
             * Generate the vertices along the intersected edges.
             *
             * We currently use the color from the nearest
             * cube corner to the generated midpoint.
             */
            for (int edge = 0; edge < 12; edge++)
            {
                if ((edgeMask & (1 << edge)) == 0)
                    continue;

                int a =
                    MarchingCubesHelper.EdgeCorners[edge, 0];

                int b =
                    MarchingCubesHelper.EdgeCorners[edge, 1];

                Vector3 aPosition =
                    cornerPositions[a];

                Vector3 bPosition =
                    cornerPositions[b];

                Vector3 edgePosition =
                    Vector3.Lerp(
                        aPosition,
                        bPosition,
                        0.5f);

                edgeVertices[edge] = edgePosition;

                /*
                 * Both endpoints should normally have a value
                 * when an edge is intersected. Handle missing
                 * values defensively anyway.
                 */
                if (cornerObjects[a].HasValue &&
                    cornerObjects[b].HasValue)
                {
                    float distanceA =
                        Vector3.SqrMagnitude(
                            edgePosition - aPosition);

                    float distanceB =
                        Vector3.SqrMagnitude(
                            edgePosition - bPosition);

                    edgeColors[edge] =
                        distanceA <= distanceB
                            ? cornerObjects[a].Value.VoxelColor
                            : cornerObjects[b].Value.VoxelColor;

                    edgeHasColor[edge] = true;
                }
                else if (cornerObjects[a].HasValue)
                {
                    edgeColors[edge] =
                        cornerObjects[a].Value.VoxelColor;

                    edgeHasColor[edge] = true;
                }
                else if (cornerObjects[b].HasValue)
                {
                    edgeColors[edge] =
                        cornerObjects[b].Value.VoxelColor;

                    edgeHasColor[edge] = true;
                }
            }

            /*
             * Build triangles.
             *
             * MeshBuilder currently accepts one color per triangle,
             * so choose the color belonging to the nearest generated
             * edge vertex.
             */
            for (int i = 0; i < 16; i += 3)
            {
                int edge0 =
                    MarchingCubesHelper.Triangles[cubeIndex, i];

                if (edge0 == -1)
                    break;

                int edge1 =
                    MarchingCubesHelper.Triangles[
                        cubeIndex,
                        i + 1];

                int edge2 =
                    MarchingCubesHelper.Triangles[
                        cubeIndex,
                        i + 2];

                Color triangleColor = Color.purple;

                /*
                 * Pick the color of the first edge that has
                 * a valid translated voxel color.
                 *
                 * Since the edge is exactly halfway between
                 * its two corners, this gives us the nearest
                 * corner's color without changing MeshBuilder.
                 */
                if (edgeHasColor[edge0])
                {
                    triangleColor =
                        edgeColors[edge0];
                }
                else if (edgeHasColor[edge1])
                {
                    triangleColor =
                        edgeColors[edge1];
                }
                else if (edgeHasColor[edge2])
                {
                    triangleColor =
                        edgeColors[edge2];
                }

                /*
                 * Preserve the winding that was already being
                 * used by the original interpreter.
                 */
                meshBuilder.AddTriangle(
                    edgeVertices[edge2],
                    edgeVertices[edge1],
                    edgeVertices[edge0],
                    triangleColor);
            }
        }


        public Promise<Transform, Exception> ApplyMesh(MeshData meshData, MeshBuilder builder, Transform transform)
        {
            var promise = new Threading.Promise<Transform, Exception>();

            this.taskMaster.RunOnMainThread(() =>
            {
                meshData.TransferMesh(builder);
                meshData.ApplyMesh(transform);
                builder.Dispose();
            })
            .OnError(_ => builder.Dispose())
            .Wrap(promise, transform);
            return promise;
        }
        public Promise<MeshBuilder[], Exception> GenerateMeshAroundPoints(
            Vector3[] points,
            float size,
            float gridSize)
        {
            var promise =
                new Promise<MeshBuilder[], Exception>();

            if (points == null || points.Length == 0)
            {
                promise.Success(
                    Array.Empty<MeshBuilder>());

                return promise;
            }

            MeshBuilder[] builders =
                new MeshBuilder[points.Length];

            int completed = 0;
            bool failed = false;

            object promiseLock =
                new object();

            for (int i = 0; i < points.Length; i++)
            {
                int index = i;

                GenerateMeshAroundPoint(
                    points[index],
                    size,
                    gridSize)

                    .OnError(exception =>
                    {
                        lock (promiseLock)
                        {
                            if (failed)
                                return;

                            failed = true;

                            promise.Error(
                                exception);
                        }
                    })

                    .OnSuccess(builder =>
                    {
                        lock (promiseLock)
                        {
                            if (failed)
                            {
                                builder.Dispose();
                                return;
                            }

                            builders[index] =
                                builder;

                            completed++;

                            if (completed != points.Length)
                                return;

                            promise.Success(
                                builders);
                        }
                    });
            }

            return promise;
        }
    }

}