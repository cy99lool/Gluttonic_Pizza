using UnityEngine;

public class Bow : MonoBehaviour
{
    public float rotationSpeed = 5f; // 回転の感度
    public float minZ = -45f; // 回転できる最小角度（左）
    public float maxZ = 45f;  // 回転できる最大角度（右）

    private bool isDragging = false;
    private Vector3 lastMousePosition;

    void Update()
    {
        // クリック開始でドラッグ開始
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }

        // 離したらドラッグ終了
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // ドラッグ中
        if (isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 delta = currentMousePosition - lastMousePosition;

            // Z軸で回転（マウスのX移動を使う）
            float rotateZ = delta.x * rotationSpeed * Time.deltaTime;
            transform.Rotate(0f, 0f, rotateZ, Space.Self); // 自分のZ軸で回転

            lastMousePosition = currentMousePosition;
        }
       

    }
}