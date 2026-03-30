using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HexStrategy.CameraControl
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class TopDownCameraController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float panSpeed = 8f;

        [Header("Mouse Edge Scrolling")]
        [SerializeField] private bool edgeScrollEnabled = true;
        [SerializeField] private float edgeScrollThreshold = 24f;

        [Header("Keyboard Scrolling")]
        [SerializeField] private bool keyboardScrollEnabled = true;

        private void Update()
        {
            Vector2 input = Vector2.zero;

            if (keyboardScrollEnabled)
            {
                input += ReadKeyboardInput();
            }

            if (edgeScrollEnabled)
            {
                input += ReadEdgeScrollInput();
            }

            // Clamp the combined mouse + keyboard input so diagonal movement is not faster.
            input = Vector2.ClampMagnitude(input, 1f);

            if (input == Vector2.zero)
            {
                return;
            }

            Vector3 movement = new Vector3(input.x, 0f, input.y) * (panSpeed * Time.deltaTime);
            transform.position += movement;
        }

        private static Vector2 ReadKeyboardInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.leftArrowKey.isPressed)
            {
                horizontal -= 1f;
            }

            if (keyboard.rightArrowKey.isPressed)
            {
                horizontal += 1f;
            }

            if (keyboard.downArrowKey.isPressed)
            {
                vertical -= 1f;
            }

            if (keyboard.upArrowKey.isPressed)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        private Vector2 ReadEdgeScrollInput()
        {
            if (!Application.isFocused)
            {
                return Vector2.zero;
            }

            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return Vector2.zero;
            }

            Vector2 pointerPosition = mouse.position.ReadValue();
            if (!IsPointerWithinGameWindow(pointerPosition))
            {
                return Vector2.zero;
            }

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return Vector2.zero;
            }

            // Compare the pointer against a small border around the game window to trigger panning.
            float horizontal = 0f;
            float vertical = 0f;

            if (pointerPosition.x <= edgeScrollThreshold)
            {
                horizontal -= 1f;
            }
            else if (pointerPosition.x >= Screen.width - edgeScrollThreshold)
            {
                horizontal += 1f;
            }

            if (pointerPosition.y <= edgeScrollThreshold)
            {
                vertical -= 1f;
            }
            else if (pointerPosition.y >= Screen.height - edgeScrollThreshold)
            {
                vertical += 1f;
            }

            return new Vector2(horizontal, vertical);
        }

        private static bool IsPointerWithinGameWindow(Vector2 pointerPosition)
        {
            return pointerPosition.x >= 0f &&
                   pointerPosition.y >= 0f &&
                   pointerPosition.x <= Screen.width &&
                   pointerPosition.y <= Screen.height;
        }
    }
}
