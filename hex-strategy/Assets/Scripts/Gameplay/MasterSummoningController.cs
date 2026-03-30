using HexStrategy.Board;
using UnityEngine;

namespace HexStrategy.Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HexBoardBuilder))]
    public sealed class MasterSummoningController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private HexBoardBuilder boardBuilder;

        [Header("Unit Colors")]
        [SerializeField] private Color masterColor = new Color(0.22f, 0.84f, 0.97f);
        [SerializeField] private Color monsterColor = new Color(0.93f, 0.35f, 0.33f);

        [Header("Audio")]
        [SerializeField] private AudioClip monsterSpawnSound;
        [SerializeField][Range(0f, 1f)] private float monsterSpawnVolume = 0.8f;

        private HexTile fortressTile;
        private HexTile castleTile;
        private HexTile strongholdTile;
        private GameObject masterObject;
        private AudioSource audioSource;
        private int castleMonsterCount;
        private int strongholdMonsterCount;

        private void Reset()
        {
            boardBuilder = GetComponent<HexBoardBuilder>();
        }

        private void Awake()
        {
            if (boardBuilder == null)
            {
                boardBuilder = GetComponent<HexBoardBuilder>();
            }

            EnsureAudioSource();
        }

        private void Start()
        {
            RefreshTiles();
        }

        public void RefreshTiles()
        {
            if (boardBuilder == null)
            {
                boardBuilder = GetComponent<HexBoardBuilder>();
            }

            if (boardBuilder == null)
            {
                return;
            }

            // Rebuild the starter layout if the board was cleared so the summon buttons still work.
            if (boardBuilder.GetTiles().Length == 0)
            {
                boardBuilder.BuildBoard();
            }

            fortressTile = boardBuilder.FindFirstTileByType(HexTileType.Fortress);
            castleTile = boardBuilder.FindFirstTileByType(HexTileType.Castle);
            strongholdTile = boardBuilder.FindFirstTileByType(HexTileType.Stronghold);

            PlaceMasterOnFortress();
        }

        public void SpawnMonsterOnCastle()
        {
            RefreshTiles();
            SpawnMonsterOnTile(castleTile, ref castleMonsterCount, "castle");
        }

        public void SpawnMonsterOnStronghold()
        {
            RefreshTiles();
            SpawnMonsterOnTile(strongholdTile, ref strongholdMonsterCount, "stronghold");
        }

        private void PlaceMasterOnFortress()
        {
            if (fortressTile == null)
            {
                Debug.LogWarning("No fortress tile was found, so the Master cannot be placed.");
                return;
            }

            if (masterObject == null)
            {
                masterObject = CreateMasterObject();
            }

            masterObject.transform.SetParent(fortressTile.GetUnitsRoot(), false);
            // Keep the Master on the fortress tile, but offset it away from the center keep so it is easy to see.
            masterObject.transform.localPosition = new Vector3(-0.58f, 0.34f, 0f);
            masterObject.transform.localRotation = Quaternion.identity;
        }

        private void SpawnMonsterOnTile(HexTile tile, ref int monsterCount, string tileName)
        {
            if (fortressTile == null)
            {
                Debug.LogWarning("The Master must be on a fortress tile before monsters can be summoned.");
                return;
            }

            if (tile == null)
            {
                Debug.LogWarning($"No {tileName} tile was found.");
                return;
            }

            if (monsterCount >= 1)
            {
                Debug.LogWarning($"The {tileName} tile already has its one allowed monster.");
                return;
            }

            // First monster stays centered. Later monsters spread outward using the tile helper.
            GameObject monsterObject = CreateMonsterObject(monsterCount + 1);
            monsterObject.transform.SetParent(tile.GetUnitsRoot(), false);
            monsterObject.transform.localPosition = tile.GetUnitLocalPosition(monsterCount);
            monsterObject.transform.localRotation = Quaternion.identity;
            monsterCount++;
            PlayMonsterSpawnSound();
        }

        private GameObject CreateMasterObject()
        {
            GameObject root = new GameObject("Master");
            GameObject pedestal = CreatePrimitiveVisual("Pedestal", PrimitiveType.Cylinder, root.transform, CreateMaterial(masterColor, "MasterMaterial"));
            pedestal.transform.localScale = new Vector3(0.42f, 0.18f, 0.42f);
            pedestal.transform.localPosition = Vector3.zero;

            GameObject crown = CreatePrimitiveVisual("Core", PrimitiveType.Sphere, root.transform, CreateMaterial(Color.white, "MasterCoreMaterial"));
            crown.transform.localScale = new Vector3(0.34f, 0.34f, 0.34f);
            crown.transform.localPosition = new Vector3(0f, 0.34f, 0f);

            return root;
        }

        private GameObject CreateMonsterObject(int monsterNumber)
        {
            GameObject root = new GameObject($"Monster_{monsterNumber}");

            GameObject body = CreatePrimitiveVisual("Body", PrimitiveType.Capsule, root.transform, CreateMaterial(monsterColor, "MonsterMaterial"));
            body.transform.localScale = new Vector3(0.34f, 0.28f, 0.34f);
            body.transform.localPosition = Vector3.zero;

            return root;
        }

        private static GameObject CreatePrimitiveVisual(string objectName, PrimitiveType primitiveType, Transform parent, Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = objectName;
            primitive.transform.SetParent(parent, false);

            Collider collider = primitive.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            MeshRenderer renderer = primitive.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            return primitive;
        }

        private void EnsureAudioSource()
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.playOnAwake = false;
            audioSource.loop = false;
            audioSource.spatialBlend = 0f;
        }

        private void PlayMonsterSpawnSound()
        {
            if (monsterSpawnSound == null)
            {
                return;
            }

            if (audioSource == null)
            {
                EnsureAudioSource();
            }

            audioSource.PlayOneShot(monsterSpawnSound, monsterSpawnVolume);
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
                name = materialName
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0.2f);
            }

            return material;
        }
    }
}
