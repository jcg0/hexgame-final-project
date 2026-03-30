using System;
using System.Collections.Generic;
using UnityEngine;

namespace HexStrategy.Board
{
    public sealed class HexBoardBuilder : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [System.Serializable]
        public struct HexTileLayout
        {
            public HexTileType type;
            public Vector2Int axialCoordinate;
        }

        [Header("Board Shape")]
        [SerializeField] private bool buildOnStart = true;
        [SerializeField] private float outerRadius = 1f;
        [SerializeField] private float baseHeight = 0.3f;
        [SerializeField] private float spacingMultiplier = 1f;
        [SerializeField] private List<HexTileLayout> tiles = new();

        [Header("Optional Custom Prefabs")]
        [SerializeField] private GameObject plainsPrefab;
        [SerializeField] private GameObject castlePrefab;
        [SerializeField] private GameObject fortressPrefab;
        [SerializeField] private GameObject strongholdPrefab;

        [Header("Placeholder Palette")]
        [SerializeField] private Color plainsColor = new(0.42f, 0.67f, 0.34f);
        [SerializeField] private Color castleColor = new(0.82f, 0.73f, 0.52f);
        [SerializeField] private Color fortressColor = new(0.43f, 0.5f, 0.6f);
        [SerializeField] private Color strongholdColor = new(0.56f, 0.23f, 0.22f);
        [SerializeField] private Color structureColor = new(0.85f, 0.84f, 0.8f);

        [Header("Generated Objects")]
        [SerializeField] private Transform tileRoot;

        private void Reset()
        {
            tiles = CreateDefaultLayout();
        }

        private void Awake()
        {
            if (buildOnStart)
            {
                BuildBoard();
            }
        }

        [ContextMenu("Build Board")]
        public void BuildBoard()
        {
            EnsureDefaultLayout();
            EnsureTileRoot();
            ClearBoard();

            Mesh baseMesh = HexMeshUtility.CreateHexPrismMesh(outerRadius, baseHeight);
            Material structureMaterial = CreateMaterial(structureColor, "HexStructure");

            foreach (HexTileLayout tileLayout in tiles)
            {
                GameObject tileObject = new GameObject($"{tileLayout.type}_{tileLayout.axialCoordinate.x}_{tileLayout.axialCoordinate.y}");
                tileObject.transform.SetParent(tileRoot, false);
                tileObject.transform.localPosition = AxialToWorld(tileLayout.axialCoordinate);

                HexTile tile = tileObject.AddComponent<HexTile>();
                tile.Configure(
                    tileLayout.type,
                    tileLayout.axialCoordinate,
                    baseMesh,
                    baseHeight,
                    CreateMaterial(GetTileColor(tileLayout.type), tileLayout.type.ToString()),
                    structureMaterial,
                    GetPrefab(tileLayout.type));
            }
        }

        [ContextMenu("Clear Board")]
        public void ClearBoard()
        {
            if (tileRoot == null)
            {
                return;
            }

            for (int i = tileRoot.childCount - 1; i >= 0; i--)
            {
                GameObject child = tileRoot.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }
        }

        public Vector3 GetBoardCenter()
        {
            EnsureDefaultLayout();

            if (tiles.Count == 0)
            {
                return transform.position;
            }

            Vector3 total = Vector3.zero;
            foreach (HexTileLayout tile in tiles)
            {
                total += AxialToWorld(tile.axialCoordinate);
            }

            return transform.position + total / tiles.Count;
        }

        public Bounds GetBoardBounds()
        {
            EnsureDefaultLayout();

            if (tiles.Count == 0)
            {
                return new Bounds(transform.position, Vector3.zero);
            }

            Vector3 hexExtents = new Vector3(Mathf.Sqrt(3f) * outerRadius * 0.5f, baseHeight * 0.5f, outerRadius);
            Vector3 firstCenter = transform.TransformPoint(AxialToWorld(tiles[0].axialCoordinate));
            Bounds bounds = new Bounds(firstCenter, hexExtents * 2f);

            foreach (HexTileLayout tile in tiles)
            {
                Vector3 center = transform.TransformPoint(AxialToWorld(tile.axialCoordinate));
                bounds.Encapsulate(center + hexExtents);
                bounds.Encapsulate(center - hexExtents);
            }

            return bounds;
        }

        public float GetRecommendedOrthographicSize(float cameraAspect, float padding = 2f)
        {
            Bounds bounds = GetBoardBounds();

            if (cameraAspect <= 0f)
            {
                cameraAspect = 1f;
            }

            float requiredVerticalHalfSize = bounds.extents.z + padding;
            float requiredHorizontalHalfSize = bounds.extents.x + padding;
            return Mathf.Max(requiredVerticalHalfSize, requiredHorizontalHalfSize / cameraAspect);
        }

        public HexTile[] GetTiles()
        {
            if (tileRoot == null)
            {
                return Array.Empty<HexTile>();
            }

            return tileRoot.GetComponentsInChildren<HexTile>();
        }

        public HexTile FindFirstTileByType(HexTileType tileType)
        {
            foreach (HexTile tile in GetTiles())
            {
                if (tile.TileType == tileType)
                {
                    return tile;
                }
            }

            return null;
        }

        public List<HexTile> FindTilesByType(params HexTileType[] tileTypes)
        {
            List<HexTile> matchingTiles = new List<HexTile>();
            HexTile[] allTiles = GetTiles();

            foreach (HexTile tile in allTiles)
            {
                for (int i = 0; i < tileTypes.Length; i++)
                {
                    if (tile.TileType == tileTypes[i])
                    {
                        matchingTiles.Add(tile);
                        break;
                    }
                }
            }

            return matchingTiles;
        }

        public Vector3 AxialToWorld(Vector2Int axialCoordinate)
        {
            // The generated mesh is pointy-top, using pointy-top axial conversion here.
            // A spacing multiplier of 1.0 places neighboring tiles edge-to-edge with no gap.
            float x = outerRadius * Mathf.Sqrt(3f) * (axialCoordinate.x + axialCoordinate.y * 0.5f);
            float z = outerRadius * 1.5f * axialCoordinate.y;
            return new Vector3(x, 0f, z) * spacingMultiplier;
        }

        public void EnsureDefaultLayout()
        {
            if (tiles != null && tiles.Count > 0)
            {
                return;
            }

            tiles = CreateDefaultLayout();
        }

        private void EnsureTileRoot()
        {
            if (tileRoot != null)
            {
                return;
            }

            Transform existingRoot = transform.Find("Tiles");
            if (existingRoot != null)
            {
                tileRoot = existingRoot;
                return;
            }

            GameObject root = new GameObject("Tiles");
            root.transform.SetParent(transform, false);
            tileRoot = root.transform;
        }

        private List<HexTileLayout> CreateDefaultLayout()
        {
            return new List<HexTileLayout>
            {
                new HexTileLayout { type = HexTileType.Plains, axialCoordinate = new Vector2Int(0, 0) },
                new HexTileLayout { type = HexTileType.Castle, axialCoordinate = new Vector2Int(1, 0) },
                new HexTileLayout { type = HexTileType.Fortress, axialCoordinate = new Vector2Int(0, 1) },
                new HexTileLayout { type = HexTileType.Stronghold, axialCoordinate = new Vector2Int(1, 1) }
            };
        }

        private GameObject GetPrefab(HexTileType tileType)
        {
            return tileType switch
            {
                HexTileType.Plains => plainsPrefab,
                HexTileType.Castle => castlePrefab,
                HexTileType.Fortress => fortressPrefab,
                HexTileType.Stronghold => strongholdPrefab,
                _ => null
            };
        }

        private Color GetTileColor(HexTileType tileType)
        {
            return tileType switch
            {
                HexTileType.Plains => plainsColor,
                HexTileType.Castle => castleColor,
                HexTileType.Fortress => fortressColor,
                HexTileType.Stronghold => strongholdColor,
                _ => Color.white
            };
        }

        private static Material CreateMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader)
            {
                name = $"{materialName}_Material"
            };

            if (material.HasProperty(BaseColorId))
            {
                material.SetColor(BaseColorId, color);
            }

            if (material.HasProperty(ColorId))
            {
                material.SetColor(ColorId, color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.15f);
            }

            return material;
        }
    }
}
