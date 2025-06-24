using UnityEngine;

public class BowController : MonoBehaviour
{
    public Transform leftPoint;      // 弦の左端
    public Transform rightPoint;     // 弦の右端
    public Transform shootPoint;     // 矢の初期位置
    public GameObject arrowPrefab;
    public LineRenderer stringRenderer;
    public float maxPullDistance = 2f;
    public float maxForce = 30f;

    private GameObject currentArrow;
    private bool isAiming = false;
    private Vector3 startMousePos;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            startMousePos = GetMouseWorldPosition();
            currentArrow = Instantiate(arrowPrefab, shootPoint.position, shootPoint.rotation);
            currentArrow.GetComponent<Rigidbody>().isKinematic = true;
            isAiming = true;
        }

        if (Input.GetMouseButton(0) && isAiming)
        {
            Vector3 currentMousePos = GetMouseWorldPosition();
            float pullDistance = Mathf.Clamp(Vector3.Distance(startMousePos, currentMousePos), 0, maxPullDistance);
            Vector3 pullDirection = (currentMousePos - startMousePos).normalized;
            Vector3 arrowPos = shootPoint.position + pullDirection * pullDistance;

            // 矢を動かす
            currentArrow.transform.position = arrowPos;

            // 弦の中央点を矢の位置に
            UpdateStringRenderer(arrowPos);
        }

        if (Input.GetMouseButtonUp(0) && isAiming)
        {
            Vector3 currentMousePos = GetMouseWorldPosition();
            Vector3 direction = (startMousePos - currentMousePos).normalized;
            float pullDistance = Mathf.Clamp(Vector3.Distance(startMousePos, currentMousePos), 0, maxPullDistance);
            float force = (pullDistance / maxPullDistance) * maxForce;

            Rigidbody rb = currentArrow.GetComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.AddForce(direction * force, ForceMode.Impulse);

            isAiming = false;
            UpdateStringRenderer(shootPoint.position); // 弦を戻す
        }

        // 通常時は初期状態に
        if (!isAiming && stringRenderer != null)
        {
            UpdateStringRenderer(shootPoint.position);
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseScreenPos = Input.mousePosition;
        mouseScreenPos.z = 10f; // カメラからの距離に応じて調整
        return Camera.main.ScreenToWorldPoint(mouseScreenPos);
    }

    void UpdateStringRenderer(Vector3 centerPoint)
    {
        if (stringRenderer == null) return;
        stringRenderer.positionCount = 3;
        stringRenderer.SetPosition(0, leftPoint.position);
        stringRenderer.SetPosition(1, centerPoint);
        stringRenderer.SetPosition(2, rightPoint.position);
    }
}