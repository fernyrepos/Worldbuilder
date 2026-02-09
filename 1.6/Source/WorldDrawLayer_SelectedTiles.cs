using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using System.Collections;
using System.Linq;

namespace Worldbuilder
{
    public class WorldDrawLayer_SelectedTiles : WorldDrawLayer
    {
        private List<Vector3> verts = new List<Vector3>();

        public override bool ShouldRegenerate
        {
            get
            {
                return false;
            }
        }

        protected Material Material => WorldMaterials.SelectedTile;

        public override IEnumerable Regenerate()
        {
            this.dirty = false;
            ClearSubMeshes(MeshParts.All);

            var tilesSnapshot = Window_MapEditor.tilesToDraw.ToList();

            if (!tilesSnapshot.Any())
            {
                FinalizeMesh(MeshParts.All);
                yield break;
            }

            var subMesh = GetSubMesh(Material);
            foreach (var tile in tilesSnapshot)
            {
                if (tile.Valid && tile.Layer == planetLayer)
                {
                    if (subMesh.verts.Count > 39000) subMesh = GetSubMesh(Material);

                    Find.WorldGrid.GetTileVertices(tile, verts);
                    int count = subMesh.verts.Count;
                    for (int i = 0; i < verts.Count; i++)
                    {
                        subMesh.verts.Add(verts[i] + verts[i].normalized * 0.02f);
                        subMesh.uvs.Add((GenGeo.RegularPolygonVertexPosition(verts.Count, i) + Vector2.one) / 2f);
                        if (i < verts.Count - 2)
                        {
                            subMesh.tris.Add(count + i + 2);
                            subMesh.tris.Add(count + i + 1);
                            subMesh.tris.Add(count);
                        }
                    }
                }
            }
            FinalizeMesh(MeshParts.All);
        }
    }
}
