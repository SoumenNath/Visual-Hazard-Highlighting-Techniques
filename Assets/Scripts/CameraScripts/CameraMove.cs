using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    public float speed = 3f;
    public float lookSensitivity = 2f;

    private float _rotX = 0f;

    void Update()
    {
        // WASD to move
        Vector2 move = Vector2.zero;
        if (Keyboard.current.wKey.isPressed) move.y += 1;
        if (Keyboard.current.sKey.isPressed) move.y -= 1;
        if (Keyboard.current.aKey.isPressed) move.x -= 1;
        if (Keyboard.current.dKey.isPressed) move.x += 1;

        transform.Translate(new Vector3(move.x, 0, move.y) * speed * Time.deltaTime);

        // Hold right-click and drag to look around
        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _rotX -= mouseDelta.y * lookSensitivity * Time.deltaTime * 10f;
            _rotX = Mathf.Clamp(_rotX, -90f, 90f);
            transform.localEulerAngles = new Vector3(_rotX, 
                transform.localEulerAngles.y + mouseDelta.x * lookSensitivity * Time.deltaTime * 10f, 
                0);
        }
    }
}