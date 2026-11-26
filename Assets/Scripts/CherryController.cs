using System.Collections;
using UnityEngine;

public class CherryController : MonoBehaviour
{
    [Header("Cherry Settings")]
    public GameObject cherryPrefab;
    public float spawnDelay = 5f;
    public float moveSpeed = 4f;
    public Vector2 levelCenter = Vector2.zero; // 맵 정중앙 월드 좌표
    public float spawnRadius = 15f;            // 카메라 및 맵 바깥쪽 반경

    private GameObject currentCherry;
    private Vector3 startPos;
    private Vector3 endPos;
    private float lerpProgress;
    private float lerpDuration;
    private bool isMoving = false;

    private void Start()
    {
        StartCoroutine(SpawnCherryRoutine());
    }

    private void Update()
    {
        if (!isMoving || currentCherry == null) return;

        lerpProgress += Time.deltaTime / lerpDuration;
        currentCherry.transform.position = Vector3.Lerp(startPos, endPos, lerpProgress);

        // 반대편 화면 밖 목표 지점에 도달하면 파괴 후 5초 타이머 재시작
        if (lerpProgress >= 1f)
        {
            Destroy(currentCherry);
            currentCherry = null;
            isMoving = false;
            StartCoroutine(SpawnCherryRoutine());
        }
    }

    private IEnumerator SpawnCherryRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);
        SpawnCherry();
    }

    private void SpawnCherry()
    {
        if (cherryPrefab == null) return;

        // 0 ~ 360도 무작위 각도에서 맵 바깥쪽 시작점 계산
        float randomAngle = Random.Range(0f, Mathf.PI * 2f);
        Vector2 offset = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * spawnRadius;

        startPos = new Vector3(levelCenter.x + offset.x, levelCenter.y + offset.y, 0f);
        // levelCenter를 정확히 관통하여 정반대편 화면 밖으로 향하는 도착점 계산
        endPos = new Vector3(levelCenter.x - offset.x, levelCenter.y - offset.y, 0f);

        currentCherry = Instantiate(cherryPrefab, startPos, Quaternion.identity);

        // 모든 스프라이트 위에 그려지도록 Sorting Order 최상단 설정
        SpriteRenderer sr = currentCherry.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sortingOrder = 100;
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        float totalDistance = Vector3.Distance(startPos, endPos);
        lerpDuration = totalDistance / moveSpeed;
        lerpProgress = 0f;
        isMoving = true;
    }

    // 70% 구간에서 PacStudent가 체리를 먹었을 때 외부에서 호출할 메서드
    public void OnCherryEaten()
    {
        if (currentCherry != null)
        {
            Destroy(currentCherry);
            currentCherry = null;
        }
        isMoving = false;
        StopAllCoroutines();
        StartCoroutine(SpawnCherryRoutine());
    }
}