using UnityEngine;

public class UISlide : MonoBehaviour
{
    public float speed = 10f;
    private Vector3 targetPos;
    private bool sliding = false;

    public void SetTarget(Vector3 pos)
    {
        targetPos = pos;
        sliding = true;
    }

    private void Update()
    {
        if (!sliding) return;

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * speed);

        if (Vector3.Distance(transform.localPosition, targetPos) < 0.1f)
        {
            transform.localPosition = targetPos;
            sliding = false;
        }
    }
}