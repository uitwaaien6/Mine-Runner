﻿using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class TerrainDeformer : MonoBehaviour
{
    public int terrainDeformationTextureNum = 1;
    private Terrain terr;
    protected int hmWidth;
    protected int hmHeight;
    protected int alphaMapWidth;
    protected int alphaMapHeight;
    protected int numOfAlphaLayers;
    protected const float DEPTH_METER_CONVERT = 0.05f;
    protected const float TEXTURE_SIZE_MULTIPLIER = 1.25f;
    private float[,] heightMapBackup;
    private float[,,] alphaMapBackup;

    Touch touch;
    public Text text;
    AudioSource audioSource;
    RockMovement rockMovement;
    LevelManager levelManager;
    GameObject ground;
    [SerializeField] Transform mineCart;
    [SerializeField] ParticleSystem sandDeformParticles;
    [SerializeField] AudioClip sandDeformSFX;
    float minerQuantity = 100f;

    [System.Obsolete]
    void Start()
    {
        terr = this.GetComponent<Terrain>();
        hmWidth = terr.terrainData.heightmapResolution;
        hmHeight = terr.terrainData.heightmapResolution;
        alphaMapWidth = terr.terrainData.alphamapWidth;
        alphaMapHeight = terr.terrainData.alphamapHeight;
        numOfAlphaLayers = terr.terrainData.alphamapLayers;
        if (Debug.isDebugBuild)
        {
            heightMapBackup = terr.terrainData.GetHeights(0, 0, hmWidth, hmHeight);
            alphaMapBackup = terr.terrainData.GetAlphamaps(0, 0, alphaMapWidth, alphaMapHeight);
        }
        audioSource = GetComponent<AudioSource>();
        text.text = minerQuantity.ToString();
        ground = GameObject.Find("Ground");
        rockMovement = FindObjectOfType<RockMovement>();
        levelManager = FindObjectOfType<LevelManager>();
        ProcessCoroutines();
        if (SceneManager.GetActiveScene().name == "Menu")
        {
            minerQuantity = Mathf.Infinity;
            text.gameObject.SetActive(false);
        }
    }

    void OnApplicationQuit()
    {
        if (Debug.isDebugBuild)
        {
            terr.terrainData.SetHeights(0, 0, heightMapBackup);
            terr.terrainData.SetAlphamaps(0, 0, alphaMapBackup);
        }
    }

    public void SetTerrainHeightsBackToNormal()
    {
        terr.terrainData.SetHeights(0, 0, heightMapBackup);
        terr.terrainData.SetAlphamaps(0, 0, alphaMapBackup);
    }

    public float inds;
    public Transform go;
    private void Update()
    {
        DeformTerrainByInput();
        if (mineCart != null)
        {
            DeformTerrain(mineCart.position, inds + 10f);
        }
    }

    // todo replace the mouse position with touch position, deform terrain on mouse position for now
    private void DeformTerrainByInput()
    {
        if (Input.touchCount > 0 && minerQuantity > 0)
        {
            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(sandDeformSFX);
            }
            try { touch = Input.GetTouch(0); }
            catch { Debug.Log("Touch index you try to get is outside of the bounds of the array"); }
            Vector3 touchPos = touch.position;
            Vector3 mousePos = Input.mousePosition;
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(touchPos);
            if (Physics.Raycast(ray, out hit))
            {
                DeformTerrain(hit.point, inds);
                DecreaseMinerQuantity(1);

                if (hit.transform.gameObject.name == "Play Button")
                {
                    levelManager.LoadNextLevel();
                }
                if (hit.transform.gameObject.name == "Levels Button")
                {
                    SceneManager.LoadScene("levels");
                }
            }
        }
    }

    private void DecreaseMinerQuantity(float pointToDecrease)
    {
        minerQuantity -= pointToDecrease;
        int convertedMinerQuantity = (int)minerQuantity;
        text.text = Mathf.RoundToInt(convertedMinerQuantity).ToString();
    }

    public void IncreaseMinerQuantity(int pointToAdd)
    {
        int addedValue = (int)minerQuantity + pointToAdd;
        minerQuantity = Mathf.Lerp(minerQuantity, addedValue, Time.deltaTime);
        text.text = Mathf.RoundToInt(minerQuantity).ToString();
    }

    // instantiate sandDeform particles in every so if hit is equals to ground
    private IEnumerator ProcessSandParticles(float particleSpawnTime) //independent 0.2f
    {
        while (true)
        {
            if (Input.touchCount > 0)
            {
                try { touch = Input.GetTouch(0); }
                catch { Debug.Log("touch index is outside of the bounds"); }
                Vector3 touchPos = touch.position;
                //Vector3 mousePos = Input.mousePosition;
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(touchPos);
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider.gameObject == ground)
                    {
                        ParticleSystem sandDeformEffects = Instantiate(sandDeformParticles, hit.point + Vector3.right * 4f, Quaternion.identity);
                        Destroy(sandDeformEffects, particleSpawnTime);
                    }
                }
            }
            yield return new WaitForSeconds(particleSpawnTime);
        }
    }

    [System.Obsolete]
    private IEnumerator SandParticlesGarbageCollector(float waitSec) //independent // delete all the particel gameobject in every so for performance purposes
    {
        while (true)
        {
            GameObject[] garbageParticles = GameObject.FindGameObjectsWithTag("SandDeformParticle");
            foreach (GameObject garbageParticle in garbageParticles)
            {
                Destroy(garbageParticle); // garbageParticle.GetComponent<ParticleSystem>().duration, todo might add
            }
            yield return new WaitForSeconds(waitSec);
        }
    }

    [System.Obsolete]
    private void ProcessCoroutines()
    {
        StartCoroutine(ProcessSandParticles(0.2f));
        StartCoroutine(SandParticlesGarbageCollector(8f));
    }
    // ==============================================

    public void DestroyTerrain(Vector3 pos, float craterSizeInMeters)
    {
        DeformTerrain(pos, craterSizeInMeters);
        TextureDeformation(pos, craterSizeInMeters * 1.5f);
    }

    protected void DeformTerrain(Vector3 pos, float craterSizeInMeters)
    {
        //get the heights only once keep it and reuse, precalculate as much as possible
        Vector3 terrainPos = GetRelativeTerrainPositionFromPos(pos, terr, hmWidth, hmHeight); //terr.terrainData.heightmapResolution/terr.terrainData.heightmapWidth
        int heightMapCraterWidth = (int)(craterSizeInMeters * (hmWidth / terr.terrainData.size.x));
        int heightMapCraterLength = (int)(craterSizeInMeters * (hmHeight / terr.terrainData.size.z));
        int heightMapStartPosX = (int)(terrainPos.x - (heightMapCraterWidth / 2));
        int heightMapStartPosZ = (int)(terrainPos.z - (heightMapCraterLength / 2));

        float[,] heights = terr.terrainData.GetHeights(heightMapStartPosX, heightMapStartPosZ, heightMapCraterWidth, heightMapCraterLength);
        float circlePosX;
        float circlePosY;
        float distanceFromCenter;
        float depthMultiplier;

        float deformationDepth = (craterSizeInMeters / 3.0f) / terr.terrainData.size.y;

        // we set each sample of the terrain in the size to the desired height
        for (int i = 0; i < heightMapCraterLength; i++) //width
        {
            for (int j = 0; j < heightMapCraterWidth; j++) //height
            {
                circlePosX = (j - (heightMapCraterWidth / 2)) / (hmWidth / terr.terrainData.size.x);
                circlePosY = (i - (heightMapCraterLength / 2)) / (hmHeight / terr.terrainData.size.z);
                distanceFromCenter = Mathf.Abs(Mathf.Sqrt(circlePosX * circlePosX + circlePosY * circlePosY));
                //convert back to values without skew

                if (distanceFromCenter < (craterSizeInMeters / 4.0f))
                {
                    depthMultiplier = ((craterSizeInMeters / 4.0f - distanceFromCenter) / (craterSizeInMeters / 2.0f));

                    depthMultiplier += 0.1f;
                    depthMultiplier += Random.value * .1f;

                    depthMultiplier = Mathf.Clamp(depthMultiplier, 0, 1);
                    heights[i, j] = 0;//Mathf.Clamp(heights[i, j] - deformationDepth * depthMultiplier, 0, 1);
                }

            }
        }
        // set the new height
        try { terr.terrainData.SetHeights(heightMapStartPosX, heightMapStartPosZ, heights); }
        catch { Debug.Log("index is outside of the bounds"); }
    }

    protected void TextureDeformation(Vector3 pos, float craterSizeInMeters)
    {
        Vector3 alphaMapTerrainPos = GetRelativeTerrainPositionFromPos(pos, terr, alphaMapWidth, alphaMapHeight);
        int alphaMapCraterWidth = (int)(craterSizeInMeters * (alphaMapWidth / terr.terrainData.size.x));
        int alphaMapCraterLength = (int)(craterSizeInMeters * (alphaMapHeight / terr.terrainData.size.z));

        int alphaMapStartPosX = (int)(alphaMapTerrainPos.x - (alphaMapCraterWidth / 2));
        int alphaMapStartPosZ = (int)(alphaMapTerrainPos.z - (alphaMapCraterLength / 2));

        float[,,] alphas = terr.terrainData.GetAlphamaps(alphaMapStartPosX, alphaMapStartPosZ, alphaMapCraterWidth, alphaMapCraterLength);

        float circlePosX;
        float circlePosY;
        float distanceFromCenter;

        for (int i = 0; i < alphaMapCraterLength; i++) //width
        {
            for (int j = 0; j < alphaMapCraterWidth; j++) //height
            {
                circlePosX = (j - (alphaMapCraterWidth / 2)) / (alphaMapWidth / terr.terrainData.size.x);
                circlePosY = (i - (alphaMapCraterLength / 2)) / (alphaMapHeight / terr.terrainData.size.z);

                //convert back to values without skew
                distanceFromCenter = Mathf.Abs(Mathf.Sqrt(circlePosX * circlePosX + circlePosY * circlePosY));


                if (distanceFromCenter < (craterSizeInMeters / 2.0f))
                {
                    for (int layerCount = 0; layerCount < numOfAlphaLayers; layerCount++)
                    {
                        //could add blending here in the future
                        if (layerCount == terrainDeformationTextureNum)
                        {
                            alphas[i, j, layerCount] = 1;
                        }
                        else
                        {
                            alphas[i, j, layerCount] = 0;
                        }
                    }
                }
            }
        }

        terr.terrainData.SetAlphamaps(alphaMapStartPosX, alphaMapStartPosZ, alphas);
    }

    protected Vector3 GetNormalizedPositionRelativeToTerrain(Vector3 pos, Terrain terrain)
    {
        //code based on: http://answers.unity3d.com/questions/3633/modifying-terrain-height-under-a-gameobject-at-runtime
        // get the normalized position of this game object relative to the terrain
        Vector3 tempCoord = (pos - terrain.gameObject.transform.position);
        Vector3 coord;
        coord.x = tempCoord.x / terr.terrainData.size.x;
        coord.y = tempCoord.y / terr.terrainData.size.y;
        coord.z = tempCoord.z / terr.terrainData.size.z;

        return coord;
    }

    protected Vector3 GetRelativeTerrainPositionFromPos(Vector3 pos, Terrain terrain, int mapWidth, int mapHeight)
    {
        Vector3 coord = GetNormalizedPositionRelativeToTerrain(pos, terrain);
        // get the position of the terrain heightmap where this game object is
        return new Vector3((coord.x * mapWidth), 0, (coord.z * mapHeight));
    }
}
