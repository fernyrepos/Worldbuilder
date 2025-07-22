using System.Collections.Generic;
using UnityEngine;
using Verse;
using RimWorld.Planet;
using System.Linq;
using System.Collections;

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
            foreach (object item in base.Regenerate())
            {
                yield return item;
            }

            if (Window_MapEditor.tilesToDraw == null || !Window_MapEditor.tilesToDraw.Any())
            {
                FinalizeMesh(MeshParts.All);
                yield break;
            }

            LayerSubMesh subMesh = GetSubMesh(Material);
            foreach (var tile in Window_MapEditor.tilesToDraw)
            {
                if (tile.Valid && tile.Layer == planetLayer)
                {
                    Find.WorldGrid.GetTileVertices(tile, verts);
                    int count = subMesh.verts.Count;
                    int i = 0;
                    for (int count2 = verts.Count; i < count2; i++)
                    {
                        subMesh.verts.Add(verts[i] + verts[i].normalized * 0.02f);
                        subMesh.uvs.Add((GenGeo.RegularPolygonVertexPosition(count2, i) + Vector2.one) / 2f);
                        if (i < count2 - 2)
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
