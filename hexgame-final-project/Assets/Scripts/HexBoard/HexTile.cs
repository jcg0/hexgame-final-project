using UnityEngine;

namespace HexStrategy.Board
{
    public sealed class HexTile : MonoBehaviour
    {
        private const string VisualRootName = "VisualRoot";
        private const string BaseName = "Base";
        private const string StructureRootName = "StructureRoot";
        private const string UnitsRootName = "UnitsRoot";

        [SerializeField] private HexTileType tileType;
        [SerializeField] private Vector2Int axialCoordinate;

        private Transform visualRoot;
        private Transform unitsRoot;
        private float topSurfaceY;

        public HexTileType TileType => tileType;
        public Vector2Int AxialCoordinate => axialCoordinate;

        public void Configure(
            HexTileType newTileType,
            Vector2Int newAxialCoordinate,
            Mesh baseMesh,
            float baseHeight,
            Material tileMaterial,
            Material structureMaterial,
            GameObject visualPrefab)
        {
            tileType = newTileType;
            axialCoordinate = newAxialCoordinate;
            topSurfaceY = baseHeight * 0.5f;

            EnsureVisualRoot();
            ClearChildren(visualRoot);

            // Prefabs take priority so the same board logic works with temporary geometry now and final art later.
            if (visualPrefab != null)
            {
                GameObject prefabInstance = Instantiate(visualPrefab, visualRoot);
                prefabInstance.name = visualPrefab.name;
                prefabInstance.transform.localPosition = Vector3.zero;
                prefabInstance.transform.localRotation = Quaternion.identity;
                prefabInstance.transform.localScale = Vector3.one;
                return;
            }

            BuildPlaceholder(baseMesh, baseHeight, tileMaterial, structureMaterial);
        }

        public Transform GetUnitsRoot()
        {
            if (unitsRoot != null)
            {
                return unitsRoot;
            }

            Transform existingRoot = transform.Find(UnitsRootName);
            if (existingRoot != null)
            {
                unitsRoot = existingRoot;
                return unitsRoot;
            }

            GameObject rootObject = new GameObject(UnitsRootName);
            rootObject.transform.SetParent(transform, false);
            unitsRoot = rootObject.transform;
            return unitsRoot;
        }

        public Vector3 GetUnitLocalPosition(int slotIndex)
        {
            if (slotIndex <= 0)
            {
                // The stronghold has a tall center keep, so place its unit slightly off-center to keep it visible.
                if (tileType == HexTileType.Stronghold)
                {
                    return new Vector3(0.34f, topSurfaceY + 0.3f, -0.2f);
                }

                return new Vector3(0f, topSurfaceY + 0.28f, 0f);
            }

            // Additional units spread in rings around the tile so they stay readable as more monsters are summoned.
            int ringIndex = slotIndex - 1;
            int sideIndex = ringIndex % 6;
            int ring = 1 + ringIndex / 6;
            float angle = Mathf.Deg2Rad * (-90f + sideIndex * 60f);
            float radius = 0.24f + 0.14f * ring;

            return new Vector3(
                Mathf.Cos(angle) * radius,
                topSurfaceY + 0.18f,
                Mathf.Sin(angle) * radius);
        }

        private void BuildPlaceholder(Mesh baseMesh, float baseHeight, Material tileMaterial, Material structureMaterial)
        {
            // The base hex carries the collider; the structure mesh on top is just a quick visual stand-in for the tile type.
            GameObject baseObject = new GameObject(BaseName);
            baseObject.transform.SetParent(visualRoot, false);

            MeshFilter meshFilter = baseObject.AddComponent<MeshFilter>();
            meshFilter.sharedMesh = baseMesh;

            MeshRenderer meshRenderer = baseObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = tileMaterial;

            MeshCollider meshCollider = baseObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = baseMesh;

            GameObject structureRoot = new GameObject(StructureRootName);
            structureRoot.transform.SetParent(visualRoot, false);

            switch (tileType)
            {
                case HexTileType.Plains:
                    BuildPlains(structureRoot.transform, baseHeight, structureMaterial);
                    break;
                case HexTileType.Castle:
                    BuildCastle(structureRoot.transform, baseHeight, structureMaterial);
                    break;
                case HexTileType.Fortress:
                    BuildFortress(structureRoot.transform, baseHeight, structureMaterial);
                    break;
                case HexTileType.Stronghold:
                    BuildStronghold(structureRoot.transform, baseHeight, structureMaterial);
                    break;
            }
        }

        private static void BuildPlains(Transform structureRoot, float baseHeight, Material structureMaterial)
        {
            GameObject ridge = CreateCube("Ridge", structureRoot, structureMaterial);
            ridge.transform.localPosition = new Vector3(0f, baseHeight * 0.65f, 0f);
            ridge.transform.localScale = new Vector3(0.85f, 0.08f, 0.3f);
        }

        private static void BuildCastle(Transform structureRoot, float baseHeight, Material structureMaterial)
        {
            GameObject keep = CreateCube("Keep", structureRoot, structureMaterial);
            keep.transform.localPosition = new Vector3(0f, baseHeight * 0.5f + 0.22f, 0f);
            keep.transform.localScale = new Vector3(0.55f, 0.45f, 0.55f);
        }

        private static void BuildFortress(Transform structureRoot, float baseHeight, Material structureMaterial)
        {
            GameObject keep = CreateCube("Keep", structureRoot, structureMaterial);
            keep.transform.localPosition = new Vector3(0f, baseHeight * 0.5f + 0.18f, 0f);
            keep.transform.localScale = new Vector3(0.45f, 0.35f, 0.45f);

            Vector3[] towerPositions =
            {
                new Vector3(0.42f, baseHeight * 0.5f + 0.15f, 0.42f),
                new Vector3(-0.42f, baseHeight * 0.5f + 0.15f, 0.42f),
                new Vector3(0.42f, baseHeight * 0.5f + 0.15f, -0.42f),
                new Vector3(-0.42f, baseHeight * 0.5f + 0.15f, -0.42f)
            };

            for (int i = 0; i < towerPositions.Length; i++)
            {
                GameObject tower = CreateCube($"Tower_{i}", structureRoot, structureMaterial);
                tower.transform.localPosition = towerPositions[i];
                tower.transform.localScale = new Vector3(0.22f, 0.3f, 0.22f);
            }
        }

        private static void BuildStronghold(Transform structureRoot, float baseHeight, Material structureMaterial)
        {
            GameObject keep = CreateCube("GreatKeep", structureRoot, structureMaterial);
            keep.transform.localPosition = new Vector3(0f, baseHeight * 0.5f + 0.35f, 0f);
            keep.transform.localScale = new Vector3(0.5f, 0.7f, 0.5f);

            // Place six posts around the keep to suggest a fortified ring around the central structure.
            for (int i = 0; i < 6; i++)
            {
                float angle = Mathf.Deg2Rad * (30f + i * 60f);
                Vector3 position = new Vector3(Mathf.Cos(angle) * 0.5f, baseHeight * 0.5f + 0.12f, Mathf.Sin(angle) * 0.5f);
                GameObject wallPost = CreateCube($"WallPost_{i}", structureRoot, structureMaterial);
                wallPost.transform.localPosition = position;
                wallPost.transform.localScale = new Vector3(0.16f, 0.24f, 0.16f);
            }
        }

        private static GameObject CreateCube(string objectName, Transform parent, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = objectName;
            cube.transform.SetParent(parent, false);

            Collider collider = cube.GetComponent<Collider>();
            if (collider != null)
            {
                if (Application.isPlaying)
                {
                    Object.Destroy(collider);
                }
                else
                {
                    Object.DestroyImmediate(collider);
                }
            }

            MeshRenderer renderer = cube.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return cube;
        }

        private void EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                return;
            }

            Transform existingRoot = transform.Find(VisualRootName);
            if (existingRoot != null)
            {
                visualRoot = existingRoot;
                return;
            }

            GameObject rootObject = new GameObject(VisualRootName);
            rootObject.transform.SetParent(transform, false);
            visualRoot = rootObject.transform;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                GameObject child = root.GetChild(i).gameObject;

                if (Application.isPlaying)
                {
                    Object.Destroy(child);
                }
                else
                {
                    Object.DestroyImmediate(child);
                }
            }
        }
    }
}
