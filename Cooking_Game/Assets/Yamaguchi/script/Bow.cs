using UnityEngine;

public class Bow : MonoBehaviour
{
    public float rotationSpeed = 5f; // 回転の感度
    public float minX = -5f; // 回転できる最小角度（左）
    public float maxX = 5f;  // 回転できる最大角度（右）

    private bool isDragging = false;
    private Vector3 lastMousePosition;

    void Update()
    {
        float move = Input.GetAxis("Horizontal") * rotationSpeed * Time.deltaTime;

        // 現在の位置に移動量を加算して新しい位置を計算
        float newPosition = transform.position.x + move;

        // 移動範囲を超えないように制限
        if (newPosition > maxX)
        {
            newPosition = maxX;
        }
        else if (newPosition < minX)
        {
            newPosition = minX;
        }

        // オブジェクトの位置を更新
        transform.position = new Vector3(newPosition, transform.position.y, transform.position.z);

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