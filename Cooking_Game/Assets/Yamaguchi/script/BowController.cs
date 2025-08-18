using UnityEngine;

public class BowController : MonoBehaviour
{
    public float rotationSpeed = 5f; 
    public float minX = -5f; // X移動範囲（左）
    public float maxX = 5f;  // X移動範囲（右）

    public float minRotationZ = -30f; // Z回転の最小角
    public float maxRotationZ = 30f;  // Z回転の最大角

   private bool isDragging = false;
    private Vector3 lastMousePosition;

    void Update()
    {
        // ←→キーによる移動
        float move = Input.GetAxis("Horizontal") * 5f * Time.deltaTime;
        float newPositionX = Mathf.Clamp(transform.position.x + move, minX, maxX);
        transform.position = new Vector3(newPositionX, transform.position.y, transform.position.z);

        // クリック開始でドラッグON
        if (Input.GetMouseButtonDown(0))
        {
            isDragging = true;
        }

        // 離したらドラッグ終了
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // ★マウスドラッグ中はカーソル方向を向く
        if (isDragging)
        {
            Vector3 currentMousePosition = Input.mousePosition;
            Vector3 delta = currentMousePosition - lastMousePosition;

            float rotateZ = delta.x * rotationSpeed * Time.deltaTime;

            // 現在のZ角度を-180〜+180に変換
            float currentZ = transform.localEulerAngles.z;
            if (currentZ > 180f) currentZ -= 360f;

            // 新しい角度に回転量を加える
            float newZ = Mathf.Clamp(currentZ + rotateZ, minRotationZ, maxRotationZ);

            // 回転を適用（XとYは変えずにZだけ）
            transform.localEulerAngles = new Vector3(0f, 0f, newZ);

            lastMousePosition = currentMousePosition;
        }
    }
}