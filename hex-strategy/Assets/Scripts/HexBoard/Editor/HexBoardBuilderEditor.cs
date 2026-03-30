#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HexStrategy.Board.Editor
{
    [CustomEditor(typeof(HexBoardBuilder))]
    public sealed class HexBoardBuilderEditor : UnityEditor.Editor
    {
        private const string CameraControllerTypeName = "HexStrategy.CameraControl.TopDownCameraController, Assembly-CSharp";
        private const string MasterSummoningControllerTypeName = "HexStrategy.Gameplay.MasterSummoningController, Assembly-CSharp";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space();

            HexBoardBuilder boardBuilder = (HexBoardBuilder)target;

            if (GUILayout.Button("Build Board"))
            {
                boardBuilder.BuildBoard();
                MarkSceneDirty(boardBuilder.gameObject.scene);
            }

            if (GUILayout.Button("Clear Board"))
            {
                boardBuilder.ClearBoard();
                MarkSceneDirty(boardBuilder.gameObject.scene);
            }

            if (GUILayout.Button("Align Camera Top Down"))
            {
                AlignTopDownCamera(boardBuilder);
                MarkSceneDirty(boardBuilder.gameObject.scene);
            }

            if (GUILayout.Button("Setup Summoning Gameplay"))
            {
                EnsureSummoningController(boardBuilder.gameObject);
                MarkSceneDirty(boardBuilder.gameObject.scene);
            }
        }

        [MenuItem("Tools/Hex Strategy/Create Starter Board")]
        private static void CreateStarterBoard()
        {
            HexBoardBuilder boardBuilder = FindAnyObjectByType<HexBoardBuilder>();
            bool createdBoard = false;

            if (boardBuilder == null)
            {
                GameObject boardObject = new GameObject("Starter Hex Board");
                Undo.RegisterCreatedObjectUndo(boardObject, "Create Starter Hex Board");
                boardBuilder = boardObject.AddComponent<HexBoardBuilder>();
                createdBoard = true;
            }

            boardBuilder.EnsureDefaultLayout();

            // Only build when the board is missing or currently empty.
            // This keeps "Create Starter Board" from wiping an existing scene layout.
            if (createdBoard || boardBuilder.GetTiles().Length == 0)
            {
                boardBuilder.BuildBoard();
            }

            EnsureSummoningController(boardBuilder.gameObject);
            AlignTopDownCamera(boardBuilder);

            Selection.activeGameObject = boardBuilder.gameObject;
            MarkSceneDirty(boardBuilder.gameObject.scene);
        }

        [MenuItem("Tools/Hex Strategy/Setup Summoning Gameplay")]
        private static void SetupSummoningGameplay()
        {
            HexBoardBuilder boardBuilder = FindAnyObjectByType<HexBoardBuilder>();
            if (boardBuilder == null)
            {
                return;
            }

            EnsureSummoningController(boardBuilder.gameObject);
            Selection.activeGameObject = boardBuilder.gameObject;
            MarkSceneDirty(boardBuilder.gameObject.scene);
        }

        [MenuItem("Tools/Hex Strategy/Align Main Camera Top Down")]
        private static void AlignExistingBoardCamera()
        {
            HexBoardBuilder boardBuilder = FindAnyObjectByType<HexBoardBuilder>();
            if (boardBuilder == null)
            {
                return;
            }

            AlignTopDownCamera(boardBuilder);
            MarkSceneDirty(boardBuilder.gameObject.scene);
        }

        private static void AlignTopDownCamera(HexBoardBuilder boardBuilder)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindAnyObjectByType<Camera>();
            }

            if (mainCamera == null)
            {
                return;
            }

            Bounds boardBounds = boardBuilder.GetBoardBounds();
            float orthographicSize = boardBuilder.GetRecommendedOrthographicSize(mainCamera.aspect);
            float cameraHeight = Mathf.Max(10f, orthographicSize * 2.5f);

            EnsureCameraController(mainCamera);
            mainCamera.transform.position = new Vector3(boardBounds.center.x, cameraHeight, boardBounds.center.z);
            mainCamera.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mainCamera.orthographic = true;
            mainCamera.orthographicSize = orthographicSize;
            mainCamera.nearClipPlane = 0.1f;
            mainCamera.farClipPlane = Mathf.Max(100f, cameraHeight + 50f);
        }

        private static void EnsureCameraController(Camera camera)
        {
            EnsureComponentByTypeName(camera.gameObject, "TopDownCameraController", CameraControllerTypeName);
        }

        private static void EnsureSummoningController(GameObject boardObject)
        {
            Component summoningController = EnsureComponentByTypeName(boardObject, "MasterSummoningController", MasterSummoningControllerTypeName);
            if (summoningController == null)
            {
                return;
            }

            SerializedObject serializedController = new SerializedObject(summoningController);
            SerializedProperty boardBuilderProperty = serializedController.FindProperty("boardBuilder");
            if (boardBuilderProperty == null)
            {
                return;
            }

            boardBuilderProperty.objectReferenceValue = boardObject.GetComponent<HexBoardBuilder>();
            serializedController.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Component EnsureComponentByTypeName(GameObject target, string shortTypeName, string qualifiedTypeName)
        {
            Component existingComponent = target.GetComponent(shortTypeName);
            if (existingComponent != null)
            {
                return existingComponent;
            }

            Type componentType = Type.GetType(qualifiedTypeName);
            if (componentType == null || !typeof(Component).IsAssignableFrom(componentType))
            {
                return null;
            }

            return Undo.AddComponent(target, componentType);
        }

        private static void MarkSceneDirty(UnityEngine.SceneManagement.Scene scene)
        {
            if (!scene.IsValid())
            {
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
#endif
