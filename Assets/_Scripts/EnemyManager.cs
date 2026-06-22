using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager main;
    public Transform spawnpoint;
    public Transform[] checkpoints;

    [SerializeField] private GameObject simon; 
    [SerializeField] private GameObject ultrasimon;
    [SerializeField] private GameObject simonking;

    [SerializeField] private int wave = 1;
    [SerializeField] private int enemyCount = 6;
    [SerializeField] private float enemyCountRate = 0.2f;
    [SerializeField] private float spawnDelayMax = 1f;
    [SerializeField] private float spawnDelayMix = 0.75f;

    [SerializeField] private float simonRate = 0.5f;
    [SerializeField] private float ultrasimonRate = 0.4f;
    [SerializeField] private float simonkingRate = 0.1f;

    private bool wavedone = false;
    private List<GameObject> waveset = new List<GameObject>();
    private int enemyLeft;
    private int simonCount;
    private int ultrasimonCount;
    private int simonkingCount;
    void Awake()
    {
        main = this;
    }
    
    void Start()
    {
        SetWave();
    }

    void Update()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (Input.GetKeyDown(KeyCode.Return) && wavedone && enemies.Length == 0)
        {
            wave++;
            wavedone = false;
            enemyCount += Mathf.RoundToInt(enemyCount + enemyCountRate);
            SetWave();
        }

        if (Input.GetKeyDown(KeyCode.D) && wavedone)
        {
            for (int i = 0; i < enemies.Length; i++)
            {
                Destroy(enemies[i]);
            }
        }

    }

    private void SetWave()
    {
        simonCount = Mathf.RoundToInt(enemyCount * simonRate + simonkingRate);
        ultrasimonCount = Mathf.RoundToInt(enemyCount * ultrasimonRate);
        simonkingCount = 0;

        if (wave % 5 == 0)
        {
            simonCount = Mathf.RoundToInt(enemyCount * simonRate);
            ultrasimonCount = Mathf.RoundToInt(enemyCount * ultrasimonRate);
        }

        enemyLeft = simonCount + ultrasimonCount + simonkingCount;
        enemyCount = enemyLeft;

        waveset = new List<GameObject>();

        for (int i = 0; i < simonCount; i++)
        {
            waveset.Add(simon);
        }
        for (int i = 0; i < ultrasimonCount; i++)
        {
            waveset.Add(ultrasimon);
        }
        for (int i = 0; i < simonkingCount; i++)
        {
            waveset.Add(simonking);
        }

        waveset = Shuffle(waveset);

        StartCoroutine(spawn());
    }
    
    public List<GameObject> Shuffle(List<GameObject> waveSet)
    {
        List<GameObject> temp = new List<GameObject>();
        List<GameObject> result = new List<GameObject>();
        temp.AddRange(waveSet);

        for(int i = 0;i < waveSet.Count; i++)
        {
            int index = Random.Range(0, temp.Count - 1);
            result.Add(temp[index]);
            temp.RemoveAt(index);
        }

        return result;
    }
    IEnumerator spawn()
    {
        for (int i = 0; i < waveset.Count; i++)
        {
            if (waveset[i] != null)
            {
                GameObject enemyInstance = Instantiate(waveset[i], spawnpoint.position, Quaternion.identity);
                enemyInstance.SetActive(true);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }
}
