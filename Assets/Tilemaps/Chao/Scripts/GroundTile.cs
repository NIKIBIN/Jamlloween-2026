using System;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Jamlloween.Tilemaps
{
    public enum GroundKind { Grama, Midway, Serragem, Floresta }

    /// <summary>
    /// A 4 x 4 m floor tile that spawns a PSXCarnivalKit ground prefab. Grass picks its own edge or corner piece
    /// from its neighbours, so painting midway or forest next to grass draws the transitions by itself
    /// (midway wins over forest). Meant for a Grid with cell size 4 and swizzle XZY.
    /// </summary>
    [CreateAssetMenu(menuName = "Jamlloween/Ground Tile", fileName = "GroundTile")]
    public class GroundTile : TileBase
    {
        // N, E, S, W, then the diagonals NE, SE, SW, NW: index * 90 is the piece's yaw in the kit.
        static readonly Vector3Int[] Sides = { new(0, 1, 0), new(1, 0, 0), new(0, -1, 0), new(-1, 0, 0) };
        static readonly Vector3Int[] Diagonals = { new(1, 1, 0), new(1, -1, 0), new(-1, -1, 0), new(-1, 1, 0) };

        [SerializeField] GroundKind kind;
        [SerializeField] GameObject prefab;
        [SerializeField] Sprite preview;

        [Header("Grama: transições")]
        [SerializeField] GameObject midwayEdge;
        [SerializeField] GameObject midwayCornerIn;
        [SerializeField] GameObject midwayCornerOut;
        [SerializeField] GameObject forestEdge;
        [SerializeField] GameObject forestCornerIn;
        [SerializeField] GameObject forestCornerOut;

        public GroundKind Kind => kind;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            if (prefab == null) throw new InvalidOperationException($"{name}: the ground prefab is not set.");
            Pick(position, tilemap, out GameObject piece, out _);
            tileData.sprite = preview;
            tileData.color = Color.white;
            tileData.gameObject = piece;
            tileData.flags = TileFlags.LockTransform | TileFlags.LockColor;
            tileData.colliderType = Tile.ColliderType.None;
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            for (int x = -1; x <= 1; x++)
                for (int y = -1; y <= 1; y++)
                    tilemap.RefreshTile(position + new Vector3Int(x, y, 0));
        }

        /// <summary>Places the spawned piece on the cell, flat on the ground, turned to face its neighbour.</summary>
        public override bool StartUp(Vector3Int position, ITilemap tilemap, GameObject go)
        {
            if (go == null) return true;
            Pick(position, tilemap, out _, out float yaw);
            var map = tilemap.GetComponent<Tilemap>();
            go.transform.localPosition = map.GetCellCenterLocal(position);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one;
            return true;
        }

        /// <summary>The prefab and yaw this cell spawns, given its neighbours.</summary>
        public void Pick(Vector3Int position, ITilemap tilemap, out GameObject piece, out float yaw)
        {
            piece = prefab;
            yaw = 0f;
            if (kind != GroundKind.Grama) return;
            if (TryTransition(position, tilemap, GroundKind.Midway, midwayEdge, midwayCornerIn, midwayCornerOut, ref piece, ref yaw)) return;
            TryTransition(position, tilemap, GroundKind.Floresta, forestEdge, forestCornerIn, forestCornerOut, ref piece, ref yaw);
        }

        static bool TryTransition(Vector3Int position, ITilemap tilemap, GroundKind other,
            GameObject edge, GameObject cornerIn, GameObject cornerOut, ref GameObject piece, ref float yaw)
        {
            if (edge == null || cornerIn == null || cornerOut == null)
                throw new InvalidOperationException("Grass tile: a transition prefab is not set.");

            bool[] side = new bool[4];
            for (int i = 0; i < 4; i++) side[i] = Is(tilemap, position + Sides[i], other);

            // Two sides that meet (N+E, E+S, S+W, W+N): outer corner.
            for (int i = 0; i < 4; i++)
                if (side[i] && side[(i + 1) % 4]) { piece = cornerOut; yaw = i * 90f; return true; }
            for (int i = 0; i < 4; i++)
                if (side[i]) { piece = edge; yaw = i * 90f; return true; }
            for (int i = 0; i < 4; i++)
                if (Is(tilemap, position + Diagonals[i], other)) { piece = cornerIn; yaw = i * 90f; return true; }
            return false;
        }

        static bool Is(ITilemap tilemap, Vector3Int cell, GroundKind kind) =>
            tilemap.GetTile(cell) is GroundTile tile && tile.kind == kind;
    }
}
