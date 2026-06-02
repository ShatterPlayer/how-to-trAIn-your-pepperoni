using UnityEngine;

namespace PizzaGame.Infrastructure
{
    public class FirstPersonController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private CharacterController characterController;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        [Header("Look")]
        [SerializeField] private float lookSensitivity = 2f;
        [SerializeField] private float minPitch = -75f;
        [SerializeField] private float maxPitch = 75f;

        private float pitch;

        private void Awake()
        {
            if (cameraTransform == null && Camera.main != null)
            {
                cameraTransform = Camera.main.transform;
            }

            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }
        }

        private void OnEnable()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (cameraTransform == null)
            {
                return;
            }

            var mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
            var mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity;

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            transform.Rotate(Vector3.up * mouseX);
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            var move = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
            if (characterController != null)
            {
                characterController.SimpleMove(move);
            }
            else
            {
                transform.position += move * Time.deltaTime;
            }
        }
    }
}
