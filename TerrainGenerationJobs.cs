using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Jobs;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using System;
using Unity.Burst;
public class TerrainGenerationJobs : MonoBehaviour
{

    //planet radius
    public float radius = 5000;
    public Transform player;
    public int collisionMeshMinDetalLevel;

    //cube mesh and collison meshes are seperate reason performance
    Mesh[] cubeMesh;
    Mesh[] collisionMesh;



    List<GameObject> meshObjects;

    
    public string texturePath;
    //each face of cube 
    public Plane[] cube;

  
    [SerializeField]
    MeshFilter[] meshFilters;

    [SerializeField]
    MeshCollider[] meshColliders;

    //earth material
    public Material Material;

    public LOD lod;

    [Range(2, 16)]
    public int res = 2;
    public float[] range;
    [Range(0, 9)]
    public int maxDetail = 8;
    public int2 heightmapDimensions;


   //player pos update later
    Vector3 pos;
   
    //when a face finished calculating data finished count +=1 resets to zero before mesh change started
    int finishedCount = 6;
    //int setupFinishedCount = 6;

    //0 setup 1 render
    int state = 0;

    //must be divisibe by res*res
    public int verticeFixedSize = 500000; //preallocate 
    int triangleFixedSize;

    public int verticeFixedSizeCol = 40000;
    int triangleFixedSizeCol;

    //rate of height map displacement
    public float heightMapPower=200;


    //native arrays for each face data
    private NativeArray<float3> verticeListFixed0;
    private NativeArray<int> triangleListFixed0;
    private NativeArray<float3> normalListFixed0;
    private NativeArray<float2> uvCoordinates0;

    private NativeArray<float3> verticeListFixed1;
    private NativeArray<int> triangleListFixed1;
    private NativeArray<float3> normalListFixed1;
    private NativeArray<float2> uvCoordinates1;

    private NativeArray<float3> verticeListFixed2;
    private NativeArray<int> triangleListFixed2;
    private NativeArray<float3> normalListFixed2;
    private NativeArray<float2> uvCoordinates2;


    private NativeArray<float3> verticeListFixed3;
    private NativeArray<int> triangleListFixed3;
    private NativeArray<float3> normalListFixed3;
    private NativeArray<float2> uvCoordinates3;


    private NativeArray<float3> verticeListFixed4;
    private NativeArray<int> triangleListFixed4;
    private NativeArray<float3> normalListFixed4;
    private NativeArray<float2> uvCoordinates4;

    private NativeArray<float3> verticeListFixed5;
    private NativeArray<int> triangleListFixed5;
    private NativeArray<float3> normalListFixed5;
    private NativeArray<float2> uvCoordinates5;

    //collision mesh data

    private NativeArray<float3> verticeListFixedCol0;
    private NativeArray<int> triangleListFixedCol0;
    private NativeArray<float3> normalListFixedCol0;

    private NativeArray<float3> verticeListFixedCol1;
    private NativeArray<int> triangleListFixedCol1;
    private NativeArray<float3> normalListFixedCol1;

    private NativeArray<float3> verticeListFixedCol2;
    private NativeArray<int> triangleListFixedCol2;
    private NativeArray<float3> normalListFixedCol2;

    private NativeArray<float3> verticeListFixedCol3;
    private NativeArray<int> triangleListFixedCol3;
    private NativeArray<float3> normalListFixedCol3;

    private NativeArray<float3> verticeListFixedCol4;
    private NativeArray<int> triangleListFixedCol4;
    private NativeArray<float3> normalListFixedCol4;

    private NativeArray<float3> verticeListFixedCol5;
    private NativeArray<int> triangleListFixedCol5;
    private NativeArray<float3> normalListFixedCol5;

    private NativeArray<float> planetTextureData;
    //job using this loop over all leaf nodes and calculate data
    private NativeArray<QuadTreeNodeJob> leafnodes;


    //use prodecural data
    //public PlanetHeightSettings planetHeightSettings;
    

    //triangle index precalculated
    private int[] tmpTriangleBordered;
    private int[] tmpTriangle;

    // Start is called before the first frame update

    private bool start = false;
    private bool updateCollision;

    //used in updateMesh function
    private int state_counter=0;
    //leaf node count preallocate
    public int leafNodeMax = 1000000;
    private void Awake()
    {
        lod = new LOD();
        //get heightmap color array 

      
       
        Texture2D planetMap= Resources.Load(texturePath, typeof(Texture2D)) as Texture2D;
        Color[] tmp = planetMap.GetPixels(0, 0, planetMap.width, planetMap.height);
        planetTextureData = new NativeArray<float>(tmp.Length, Allocator.Persistent);
        int len = tmp.Length;
        for (int i = 0; i < len; i++)
        {
            planetTextureData[i] = tmp[i].r; //use red channel reduce size of array
        }
        heightmapDimensions.x = planetMap.width;
        heightmapDimensions.y = planetMap.height;
        Resources.UnloadAsset(planetMap);
        //triangle count is determined by vertice count calculate triangle count
        triangleFixedSize = (verticeFixedSize / (res * res) )* ((res - 1) * (res - 1) * 6);
        triangleFixedSizeCol = (verticeFixedSizeCol / (res * res)) * ((res - 1) * (res - 1) * 6);
        //init native arrays fixed size 
        //add variable for sizes


        
        verticeListFixed0 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        triangleListFixed0 = new NativeArray<int>(triangleFixedSize, Allocator.Persistent);
        normalListFixed0 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        uvCoordinates0 = new NativeArray<float2>(verticeFixedSize, Allocator.Persistent);


        verticeListFixed1 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        triangleListFixed1 = new NativeArray<int>(triangleFixedSize, Allocator.Persistent);
        normalListFixed1 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        uvCoordinates1 = new NativeArray<float2>(verticeFixedSize, Allocator.Persistent);


        verticeListFixed2 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        triangleListFixed2 = new NativeArray<int>(triangleFixedSize, Allocator.Persistent);
        normalListFixed2 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        uvCoordinates2 = new NativeArray<float2>(verticeFixedSize, Allocator.Persistent);


        verticeListFixed3 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        triangleListFixed3 = new NativeArray<int>(triangleFixedSize, Allocator.Persistent);
        normalListFixed3 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        uvCoordinates3 = new NativeArray<float2>(verticeFixedSize, Allocator.Persistent);

        verticeListFixed4 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        triangleListFixed4 = new NativeArray<int>(triangleFixedSize, Allocator.Persistent);
        normalListFixed4 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        uvCoordinates4 = new NativeArray<float2>(verticeFixedSize, Allocator.Persistent);

        verticeListFixed5 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        triangleListFixed5 = new NativeArray<int>(triangleFixedSize, Allocator.Persistent);
        normalListFixed5 = new NativeArray<float3>(verticeFixedSize, Allocator.Persistent);
        uvCoordinates5 = new NativeArray<float2>(verticeFixedSize, Allocator.Persistent);


        verticeListFixedCol0 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
        triangleListFixedCol0 = new NativeArray<int>(triangleFixedSizeCol, Allocator.Persistent);
        normalListFixedCol0 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);

        verticeListFixedCol1 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
        triangleListFixedCol1 = new NativeArray<int>(triangleFixedSizeCol, Allocator.Persistent);
        normalListFixedCol1 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);

        verticeListFixedCol2 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
        triangleListFixedCol2 = new NativeArray<int>(triangleFixedSizeCol, Allocator.Persistent);
        normalListFixedCol2 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);

        verticeListFixedCol3 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
        triangleListFixedCol3 = new NativeArray<int>(triangleFixedSizeCol, Allocator.Persistent);
        normalListFixedCol3 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);

        verticeListFixedCol4 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
        triangleListFixedCol4 = new NativeArray<int>(triangleFixedSizeCol, Allocator.Persistent);
        normalListFixedCol4 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);

        verticeListFixedCol5 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
        triangleListFixedCol5 = new NativeArray<int>(triangleFixedSizeCol, Allocator.Persistent);
        normalListFixedCol5 = new NativeArray<float3>(verticeFixedSizeCol, Allocator.Persistent);
       
    }
    void Start()
    {
       
        cubeMesh = null;
        cube = null;
        meshFilters = null;
        initMesh();

        //set mesh for render
        cube[0].setNativeArrays(verticeListFixed0, normalListFixed0, triangleListFixed0,uvCoordinates0);
        cube[1].setNativeArrays(verticeListFixed1, normalListFixed1, triangleListFixed1,uvCoordinates1);
        cube[2].setNativeArrays(verticeListFixed2, normalListFixed2, triangleListFixed2,uvCoordinates2);
        cube[3].setNativeArrays(verticeListFixed3, normalListFixed3, triangleListFixed3,uvCoordinates3);
        cube[4].setNativeArrays(verticeListFixed4, normalListFixed4, triangleListFixed4,uvCoordinates4);
        cube[5].setNativeArrays(verticeListFixed5, normalListFixed5, triangleListFixed5,uvCoordinates5);

        //set mesh for collisions
        cube[0].setNativeArraysCol(verticeListFixedCol0, normalListFixedCol0, triangleListFixedCol0);
        cube[1].setNativeArraysCol(verticeListFixedCol1, normalListFixedCol1, triangleListFixedCol1);
        cube[2].setNativeArraysCol(verticeListFixedCol2, normalListFixedCol2, triangleListFixedCol2);
        cube[3].setNativeArraysCol(verticeListFixedCol3, normalListFixedCol3, triangleListFixedCol3);
        cube[4].setNativeArraysCol(verticeListFixedCol4, normalListFixedCol4, triangleListFixedCol4);
        cube[5].setNativeArraysCol(verticeListFixedCol5, normalListFixedCol5, triangleListFixedCol5);

        
        pos = new Vector3(player.position.x, player.position.y, player.position.z);
        start = true;

        updateCollision = false;


        
        for (int i = 0; i < 6; i++)
        {
            cube[i].updateQuadtree();
            // cube[i].quadtree.leafNodes = cube[i].quadtree.root.getLeafNode();
            cube[i].quadtree.getLeafNodes();
        }
        
        for (int i = 0; i < 6; i++)
        {

            cube[i].quadtree.updateEdgeNeighbors();
        }

        //calculate triangle order once on start
        calculateTriangle();
       

       
        for (int i = 0; i < 6; i++)
        {
            cube[i].quadtree.convertJobs();

        }
        
        
        


        //claculate mesh on start
        for (int i = 0; i < 6; i++)
        {
            JobCalculateTerrain(i,false);
            JobCalculateTerrain(i, true);
            cube[i].updateMesh();

        }
        finishedCount = 0;
    }

    private void OnDestroy()
    {
        //free memory on destroy to avoid memory leaks
        verticeListFixed0.Dispose();
        triangleListFixed0.Dispose();
        normalListFixed0.Dispose();
        uvCoordinates0.Dispose();


        verticeListFixed1.Dispose();
        triangleListFixed1.Dispose();
        normalListFixed1.Dispose();
        uvCoordinates1.Dispose();


        verticeListFixed2.Dispose();
        triangleListFixed2.Dispose();
        normalListFixed2.Dispose();
        uvCoordinates2.Dispose();


        verticeListFixed3.Dispose();
        triangleListFixed3.Dispose();
        normalListFixed3.Dispose();
        uvCoordinates3.Dispose();


        verticeListFixed4.Dispose();
        triangleListFixed4.Dispose();
        normalListFixed4.Dispose();
        uvCoordinates4.Dispose();


        verticeListFixed5.Dispose();
        triangleListFixed5.Dispose();
        normalListFixed5.Dispose();
        uvCoordinates5.Dispose();


        verticeListFixedCol0.Dispose();
        triangleListFixedCol0.Dispose();
        normalListFixedCol0.Dispose();
      

        verticeListFixedCol1.Dispose();
        triangleListFixedCol1.Dispose();
        normalListFixedCol1.Dispose();

        verticeListFixedCol2.Dispose();
        triangleListFixedCol2.Dispose();
        normalListFixedCol2.Dispose();

        verticeListFixedCol3.Dispose();
        triangleListFixedCol3.Dispose();
        normalListFixedCol3.Dispose();

        verticeListFixedCol4.Dispose();
        triangleListFixedCol4.Dispose();
        normalListFixedCol4.Dispose();

        verticeListFixedCol5.Dispose();
        triangleListFixedCol5.Dispose();
        normalListFixedCol5.Dispose();

        planetTextureData.Dispose();
    }
    private void OnValidate()
    {
        /*
        if (start)
        {
           
            for(int i = 0; i < 6; i++)
            {
                JobCalculateTerrain(i,false);
            }
        }
        */
    }
    
    public float updateInterval = 50;
    private void Update()
    {
        //timer -= Time.deltaTime;
        if(Vector3.Distance(gameObject.transform.position, player.position) > 2*radius && finishedCount==6)
        {
            return;
        }
        //Debug.Log(Vector3.Distance(pos, player.position));
        //Vector3.Distance(pos, player.position) > 50 
        if (Vector3.Distance(pos, player.position) > updateInterval && finishedCount == 6 )
        {
            
            LOD.cameraPos = player;
            pos = new Vector3(player.position.x, player.position.y, player.position.z);

            //setupUpdateCubePartition();
            finishedCount = 0; 
            state = 0;
            state_counter = 0;
            updateCollision = false;
            //drawUpdateCube();
        }
        if (finishedCount < 6)
        {
            updateMesh();
            /*
            print(cube[0].quadtree.verticeCount);
            print(cube[1].quadtree.verticeCount);
            print(cube[2].quadtree.verticeCount);
            print(cube[3].quadtree.verticeCount);
            print(cube[4].quadtree.verticeCount);
            print(cube[5].quadtree.verticeCount);
            */
        }
        /*
        if (timer <= 0)
        {
            timer = maxTimer;
        }
        */
      


    }

    //regenerate mesh
    //distribute work across the frames to reduce workload of a frame
    //every state is divided to six 
    //one face of a cube per frame

    public void updateMesh()
    {
        if (state == 0)
        {
            cube[state_counter].updateQuadtree();
            //
            state_counter += 1;
            
            if (state_counter >= 6)
            {
                state = 1;
                state_counter = 0;
            }
               
            
        }else if (state == 1)
        {
           
            cube[state_counter].quadtree.getLeafNodes();
            state_counter += 1;
            //cube[i].quadtree.findNeighbours();
            if (state_counter >= 6)
            {
                state = 2;
                state_counter = 0;
            }
            
        }
        else if (state == 2)
        {
            cube[state_counter].quadtree.findNeighbours();
            //
            state_counter += 1;

            if (state_counter >= 6)
            {
                state = 3;
                state_counter = 0;
            }
        }
        else if (state == 3)
        {
            
            
            cube[state_counter].quadtree.updateEdgeNeighbors();
            state_counter += 1;
            if (state_counter >= 6)
            {
                state = 4;
                state_counter = 0;
            }
                  
            

        }else if (state == 4)
        {
            
            cube[state_counter].quadtree.convertJobs();
            state_counter += 1;
            if (state_counter >= 6)
            {
                state = 5;
                state_counter = 0;
            }
              
           
        }
        else
        {

            if (updateCollision == false)
            {
                JobCalculateTerrain(finishedCount, false);
                cube[finishedCount].updateMesh();
                updateCollision = true;
            }
            else
            {
                JobCalculateTerrain(finishedCount, true);
                updateCollision = false;
                cube[finishedCount].updateCollisionMesh();
                finishedCount += 1;
                

            }
        }
    }
   
    //parallel compute leaf nodes cube[cubeindex] if compute  for meshes collision=true
    //compute the given face of the cube mesh 
    private void JobCalculateTerrain(int cubeIndex,bool collision)
    {
          /*
          calculateTriangle();
          cube[cubeIndex].updateQuadtree();
          cube[cubeIndex].quadtree.setupMeshDataPartition();
          */

          //use tempjob for single frame task 
          //no need to initialize memory
          NativeArray<int> borderedSizeIndex = new NativeArray<int>((res + 2) * (res + 2), Allocator.TempJob, NativeArrayOptions.UninitializedMemory);
          
          //since every leaf node has same amount of triangles in same order precalculate triangle order 

          int meshIndex = 0;
          int borderIndex = -1;
          int c = 0;
          for (int i = 0; i < res + 2; i++)
          {
              for (int j = 0; j < res + 2; j++)
              {
                  if (i == 0 || i == res + 1 || j == 0 || j == res + 1)
                  {
                      borderedSizeIndex[c] = borderIndex;
                      borderIndex -= 1;
                  }
                  else
                  {
                      borderedSizeIndex[c] = meshIndex;
                      meshIndex += 1;
                  }

                  //Debug.Log(borderedSizeIndex[c]);
                  c += 1;
              }

          }

        float2 uvMap; //position on 4x4 grid  

     
        NativeArray<float3> verticeList;
        NativeArray<int> triangleList;
        NativeArray<float3> normalList;
        NativeArray<float2> uvList;

        // up
        if (cubeIndex == 0)
        {
            uvList = uvCoordinates0;
            uvMap = new float2(1, 1);
            if (collision)
            {
                verticeList = verticeListFixedCol0;
                triangleList = triangleListFixedCol0;
                normalList = normalListFixedCol0;
            }
            else
            {
                verticeList = verticeListFixed0;
                triangleList = triangleListFixed0;
                normalList = normalListFixed0;
            }
           
        }
        else if (cubeIndex == 1)
        {
            uvMap = new float2(2, 3);
            uvList = uvCoordinates1;
            if (collision)
            {
                verticeList = verticeListFixedCol1;
                triangleList = triangleListFixedCol1;
                normalList = normalListFixedCol1;
            }
            else
            {
                verticeList = verticeListFixed1;
                triangleList = triangleListFixed1;
                normalList = normalListFixed1;
            }
        }
        else if (cubeIndex == 2)
        {
            uvMap = new float2(2, 0);
            uvList = uvCoordinates2;
            if (collision)
            {
                verticeList = verticeListFixedCol2;
                triangleList = triangleListFixedCol2;
                normalList = normalListFixedCol2;
            }
            else
            {
                verticeList = verticeListFixed2;
                triangleList = triangleListFixed2;
                normalList = normalListFixed2;
            }
        }
        else if (cubeIndex == 3)
        {
            uvMap = new float2(2, 1);
            uvList = uvCoordinates3;
            if (collision)
            {
                verticeList = verticeListFixedCol3;
                triangleList = triangleListFixedCol3;
                normalList = normalListFixedCol3;
            }
            else
            {
                verticeList = verticeListFixed3;
                triangleList = triangleListFixed3;
                normalList = normalListFixed3;
            }
        }
        else if (cubeIndex == 4)
        {
            uvMap = new float2(2, 2);
            uvList = uvCoordinates4;
            if (collision)
            {
                verticeList = verticeListFixedCol4;
                triangleList = triangleListFixedCol4;
                normalList = normalListFixedCol4;
            }
            else
            {
                verticeList = verticeListFixed4;
                triangleList = triangleListFixed4;
                normalList = normalListFixed4;
            }
        }
        else
        {
            uvMap = new float2(3, 1);
            uvList = uvCoordinates5;
            if (collision)
            {
                verticeList = verticeListFixedCol5;
                triangleList = triangleListFixedCol5;
                normalList = normalListFixedCol5;
            }
            else
            {
                verticeList = verticeListFixed5;
                triangleList = triangleListFixed5;
                normalList = normalListFixed5;
            }
        }

        int listCount;
        //list of leaf nodes converted for to be compatible with jobs format
        QuadTreeNodeJob[] list;
        if (collision)
        {
            list = cube[cubeIndex].quadtree.leafnodeJobsCollision;
            listCount = cube[cubeIndex].quadtree.collisionLeafNodeCount;
        }
        else
        {
            list = cube[cubeIndex].quadtree.leafnodeJobs;
            listCount = cube[cubeIndex].quadtree.leafNodeCount;
        }
        //convert to jobs format
        leafnodes = new NativeArray<QuadTreeNodeJob>(list, Allocator.TempJob);
        NativeArray<int> tmpTriangleN = new NativeArray<int>(tmpTriangle, Allocator.TempJob);
        NativeArray<int> tmpTriangleBorderedN = new NativeArray<int>(tmpTriangleBordered, Allocator.TempJob);
        
        //create job 
        CalculatePositionJob job = new CalculatePositionJob
        {
            nodesJob = leafnodes,
            verticesJob = verticeList,
            trianglesJob = triangleList,
            normalsJob = normalList,
            uvJob = uvList,
            radiusJob = radius,
            tmpTriangleJob = tmpTriangleN,
            tmpTriangleJobBordered = tmpTriangleBorderedN,
            borderedSizeIndex = borderedSizeIndex,
          
            isCollision = collision,
            uvMap = uvMap,
            heightmapDimensions =heightmapDimensions,
            heightMap=planetTextureData,
            resJob = res,
            heightMapPower =heightMapPower


        };
        //TO DO update 12 to thread count
        //execute job
        JobHandle jobHandle = job.Schedule(listCount, listCount/ 12);

        //wait for complete all threads
        jobHandle.Complete();
       

        //release memory 
        tmpTriangleN.Dispose();
        tmpTriangleBorderedN.Dispose();
        borderedSizeIndex.Dispose();
     
     
        leafnodes.Dispose();
        


       
    }
    //pre calculate triangle index
    private void calculateTriangle()
    {
        //calculate bordered triangle 
        //bordered  has extra vertices at borders for normal calculations
        int count = 0;
        int tris = 0;
        tmpTriangleBordered = new int[(res + 1) * (res + 1) * 6];
        for (int i = 0; i < res + 2; i++)
        {
            for (int j = 0; j < res + 2; j++)
            {


                if (i != res + 1 && j != res + 1)
                {
                   
                    tmpTriangleBordered[tris] = count; //0
                    tmpTriangleBordered[tris + 1] = count + 1; //1
                    tmpTriangleBordered[tris + 2] = count + res + 2; //2


                    tmpTriangleBordered[tris + 3] = count + 1; //1
                    tmpTriangleBordered[tris + 4] = count + res + 2 + 1; //2
                    tmpTriangleBordered[tris + 5] = count + res + 2;//3
                    tris += 6;

                }



                count += 1;


            }
        }


        //calculate triangle
        tmpTriangle = new int[(res - 1) * (res - 1) * 6];
        count = 0;
        tris = 0;
        for (int i = 0; i < res; i++)
        {
            for (int j = 0; j < res; j++)
            {



                if (i != res - 1 && j != res - 1)
                {

                    {
                        tmpTriangle[tris] = count; //0
                        tmpTriangle[tris + 1] = count + 1; //1
                        tmpTriangle[tris + 2] = count + res; //2


                        tmpTriangle[tris + 3] = count + 1; //1
                        tmpTriangle[tris + 4] = count + res + 1; //2
                        tmpTriangle[tris + 5] = count + res;//3
                        tris += 6;
                    }
                }

                count += 1;
            }



        }
    }

    void updateRange()
    {
        if (range == null)
        {

            return;
        }
        for (int i = 0; i < range.Length; i++)
        {
            lod.detailLevelDist[i] = range[i];
        }
    }


    void initMesh()
    {


        lod.MaxDetail = maxDetail;
        LOD.cameraPos = player;
       
        updateRange();

        if (cubeMesh == null)
        {
            cubeMesh = new Mesh[6];
            collisionMesh = new Mesh[6];
            for (int i = 0; i < 6; i++)
            {
                cubeMesh[i] = new Mesh();
                collisionMesh[i] = new Mesh();
            }
        }
        if (cube == null)
        {
            createCube();
        }


        if (meshFilters == null || meshFilters.Length == 0)
        {
            meshFilters = new MeshFilter[6];
            meshColliders = new MeshCollider[6];
            createCube();
            for (int i = 0; i < 6; i++)
            {
                GameObject meshObj = new GameObject("mesh" + i);

                meshObj.transform.parent = transform;
                meshObj.transform.localPosition = Vector3.zero;
                meshObj.transform.localRotation = quaternion.identity;
                meshObj.AddComponent<MeshRenderer>();
                
                meshFilters[i] = meshObj.AddComponent<MeshFilter>();
                meshColliders[i] = meshObj.AddComponent<MeshCollider>();
                meshFilters[i].sharedMesh = cube[i].mesh;
                meshColliders[i].sharedMesh = cube[i].collisionMesh;
                meshColliders[i].convex =false;
                meshObj.GetComponent<MeshRenderer>().material = Material;
                cube[i].meshCollider = meshColliders[i];
                Rigidbody rb;
                rb=meshObj.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.isKinematic = true;
            };
        }




    }


    void createCube()
    {
        cube = new Plane[6];
        cube[0] = new Plane(cubeMesh[0], collisionMesh[0], Vector3.up      , radius, res, this);
        cube[1] = new Plane(cubeMesh[1], collisionMesh[1], Vector3.left    , radius, res, this);
        cube[2] = new Plane(cubeMesh[2], collisionMesh[2], Vector3.forward , radius, res, this);
        cube[3] = new Plane(cubeMesh[3], collisionMesh[3], Vector3.right   , radius, res, this);
        cube[4] = new Plane(cubeMesh[4], collisionMesh[4], Vector3.back    , radius, res, this);
        cube[5] = new Plane(cubeMesh[5], collisionMesh[5], Vector3.down    , radius, res, this);

      
        cube[0].setNeighbors(cube[3], cube[1], cube[4], cube[2], DIRECTION.WEST, DIRECTION.WEST, DIRECTION.SOUTH, DIRECTION.NORTH);
        cube[1].setNeighbors(cube[4], cube[2], cube[5], cube[0], DIRECTION.EAST, DIRECTION.EAST, DIRECTION.NORTH, DIRECTION.SOUTH);
        cube[2].setNeighbors(cube[0], cube[5], cube[1], cube[3], DIRECTION.WEST, DIRECTION.WEST, DIRECTION.SOUTH, DIRECTION.NORTH);
        cube[3].setNeighbors(cube[2], cube[4], cube[5], cube[0], DIRECTION.WEST, DIRECTION.WEST, DIRECTION.SOUTH, DIRECTION.NORTH);
        cube[4].setNeighbors(cube[5], cube[0], cube[1], cube[3], DIRECTION.EAST, DIRECTION.EAST, DIRECTION.NORTH, DIRECTION.SOUTH);
        cube[5].setNeighbors(cube[1], cube[3], cube[4], cube[2], DIRECTION.EAST, DIRECTION.EAST, DIRECTION.NORTH, DIRECTION.SOUTH);
    }
   




    //node in quadtree 
    public class QuadTreeNode
    {
        //0 top left
        //1 top right
        //2 bottom right
        //3 bottom left
        
        //center of node
        public Vector3 center;
        public QuadTreeNode parent;
        public QuadTreeNode root;
        
        public QuadTreeNode[] children;
        public float radius;
        public int detailLevel;


        public Vector3 localUp;
        public Vector3 axisA;
        public Vector3 axisB;
        public Vector3[] vertices;
        public Vector3[] normals;
        public int[] triangles;
        public Vector3[] verticesBorder;
        public Vector3[] normalsBorder;
        public int[] trianglesBorder;
        public float planetRadius;
        public byte corner;
        public uint hash = 0;
        public bool[] neighbours;
        public LOD lod;
        Transform planetTransform;

        public bool[] edgeNeighbours;
        public int[] edgeDirections;

        public QuadTreeNode(Vector3 center, QuadTreeNode root, QuadTreeNode parent, float radius, int detailLevel, Vector3 localUp, Vector3 axisA, Vector3 axisB, byte corner, uint hash,Transform planetTransform,float planetRadius,LOD lod)
        {
            this.center = center;
            this.parent = parent;
            this.root = root;
            this.radius = radius;
            this.detailLevel = detailLevel;
            this.localUp = localUp;
            this.axisA = axisA;
            this.axisB = axisB;
            this.children = new QuadTreeNode[0];
            this.corner = corner;
            this.hash = hash;
            this.planetTransform = planetTransform;
            this.planetRadius = planetRadius;
            neighbours = new bool[4] { false, false, false, false };
            edgeNeighbours = new bool[4] { false, false, false, false };
            edgeDirections = new int[2] { -1, -1 };
            this.lod = lod;
        }




        public void CreateChildren()
        {
            if (detailLevel >= lod.MaxDetail)
            {
                return;
            }
            if (Vector3.Distance(planetTransform.TransformPoint(center.normalized * planetRadius) , LOD.cameraPos.position) > lod.detailLevelDist[detailLevel])
            {
                return;
            }
            this.children = new QuadTreeNode[4];
            this.children[0] = new QuadTreeNode(center + (axisA * radius) / 2 - (axisB * radius) / 2, root, this, radius / 2, detailLevel + 1, this.localUp, this.axisA, this.axisB, 0, hash * 4,this.planetTransform,planetRadius,lod);
            this.children[1] = new QuadTreeNode(center + (axisA * radius) / 2 + (axisB * radius) / 2, root, this, radius / 2, detailLevel + 1, this.localUp, this.axisA, this.axisB, 1, hash * 4 + 1,this.planetTransform, planetRadius,lod);
            this.children[2] = new QuadTreeNode(center - (axisA * radius) / 2 + (axisB * radius) / 2, root, this, radius / 2, detailLevel + 1, this.localUp, this.axisA, this.axisB, 2, hash * 4 + 2,this.planetTransform, planetRadius,lod);
            this.children[3] = new QuadTreeNode(center - (axisA * radius) / 2 - (axisB * radius) / 2, root, this, radius / 2, detailLevel + 1, this.localUp, this.axisA, this.axisB, 3, hash * 4 + 3,this.planetTransform, planetRadius,lod);

            for (int i = 0; i < 4; i++)
            {
                this.children[i].CreateChildren();
            }
        }


        //update node 
       
        public void UpdateChildren()
        {
            if (detailLevel >= lod.MaxDetail)
            {
                return;
            }

            //local to world space conversion
            if (Vector3.Distance(planetTransform.TransformPoint(center.normalized * planetRadius), LOD.cameraPos.position) > lod.detailLevelDist[detailLevel])
            {
                children = new QuadTreeNode[0];
                return;
            }
            else
            {
                if (children.Length > 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        children[i].UpdateChildren();
                    }
                }
                else
                {
                    vertices = null;
                    triangles = null;
                    normals = null;
                    CreateChildren();
                }


            }



        }


        /*
        public QuadTreeNode[] getLeafNode()
        {
            List<QuadTreeNode> leafNodes = new List<QuadTreeNode>();
            if (children.Length > 0)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    leafNodes.AddRange(children[i].getLeafNode());
                }
            }
            else
            {
                this.quadtreeEdgeFind();
                leafNodes.Add(this);
            }
            return leafNodes.ToArray();
        }
        */
        //if a neighbor is bigger than node that neighbor is true
        //directions specified in direction class
        public void findNeighbours()
        {
            for (int i = 0; i < 4; i++)
            {
                neighbours[i] = false;
            }
            QuadTreeNode n1;
            QuadTreeNode n2;
            int dir1;
            int dir2;

            //top left
            if (corner == 0)
            {
                dir1 = DIRECTION.NORTH;
                dir2 = DIRECTION.WEST;
            }

            //top right
            else if (corner == 1)
            {
                dir1 = DIRECTION.NORTH;
                dir2 = DIRECTION.EAST;
            }

            //bottom left
            else if (corner == 2)
            {
                dir1 = DIRECTION.EAST;
                dir2 = DIRECTION.SOUTH;
            }

            //bottom right
            else
            {
                dir1 = DIRECTION.WEST;
                dir2 = DIRECTION.SOUTH;
            }

            n1 = findNeighboursBiggerEqual(dir1);
            n2 = findNeighboursBiggerEqual(dir2);
            n1 = FindLeafNeighBour(n1, dir1);
            n2 = FindLeafNeighBour(n2, dir2);
            if (n1 != null)
            {
                if (n1.children.Length == 0 && n1.detailLevel < this.detailLevel)
                {
                    neighbours[dir1] = true;
                }

            }
            if (n2 != null)
            {
                if (n2.children.Length == 0 && n2.detailLevel < this.detailLevel)
                {
                    neighbours[dir2] = true;
                }

            }


        }

        //this finds common parent and get child node of that parent in given direction
        public QuadTreeNode findNeighboursBiggerEqual(int direction)
        {
            if (parent == null)
            {
                return null;
            }
            if (corner == 0)
            {
                if (direction == DIRECTION.SOUTH)
                {
                    return parent.children[3];
                }
                else if (direction == DIRECTION.EAST)
                {
                    return parent.children[1];
                }
            }
            else if (corner == 1)
            {



                if (direction == DIRECTION.SOUTH)
                {
                    return parent.children[2];
                }
                else if (direction == DIRECTION.WEST)
                {
                    return parent.children[0];
                }


            }
            else if (corner == 2)
            {


                if (direction == DIRECTION.WEST)
                {
                    return parent.children[3];
                }
                else if (direction == DIRECTION.NORTH)
                {
                    return parent.children[1];
                }

            }
            else if (corner == 3)
            {


                if (direction == DIRECTION.EAST)
                {
                    return parent.children[2];
                }
                else if (direction == DIRECTION.NORTH)
                {
                    return parent.children[0];
                }

            }

            return parent.findNeighboursBiggerEqual(direction);


        }
        //1-aa-bb  format encode path 
        //convert hash value to array 1-01-01-10-11 -> 1123  left most bit is start bit given in root node
        public int[] decryptHash()
        {
            int[] loc = new int[9] { -1, -1, -1, -1, -1, -1, -1, -1 ,-1};

            uint tmphash = hash;

            int index = detailLevel - 1;

            while (tmphash > 1)
            {
                int num = (int)(tmphash & 3); //get last two bit this gives position on path
                loc[index] = num;
                index -= 1;
                tmphash = tmphash >> 2; 

            }

            return loc;
        }

        //when neighbor parent is found search tree to find neighbor node 
        //follow path
        public QuadTreeNode FindLeafNeighBour(QuadTreeNode parent, int direction)
        {
            if (parent == null)
            {
                return null;
            }

            bool axisX = false;
            if (direction == DIRECTION.WEST || direction == DIRECTION.EAST)
            {
                axisX = true;
            }
            int[] path = decryptHash();
           

            int index = parent.detailLevel;

                           
            QuadTreeNode t = parent;
            while (t.children.Length != 0 && path[index] != -1)
            {
                if (axisX == false)
                {
                    t = t.children[DIRECTION.MIRROR_AXIS_X[path[index]]]; //neighbor node mirrors path so use mirrored  direction  horizontal mirror
                }
                else
                {
                    t = t.children[DIRECTION.MIRROR_AXIS_Y[path[index]]]; //vertical mirror
                }
                index += 1;
            }
            if (t.children.Length == 0)
            {
                return t;
            }
            return null;

        }


        //find nodes that has at least one edge at edge of cube face
        public bool quadtreeEdgeFind()
        {

            int[] array = decryptHash();
            bool isEdge1 = true;
            bool isEdge2 = true;
            edgeDirections[0] = -1;
            edgeDirections[1] = -1;
            
            //0 1
            //3 2
           
            //0 north  west     1 north east     3 south west
            //1 north  east     0 north west     2 south east
            //2 south  east     1 north east     3 south west
            //3 south  west     0 north west     2 south east



            //for 0
            //   ^ ^
            // <-0 1  only west and north directions leads to an edge so 1 can lead noth edge or 3 can lead to west edge
            // <-3 2  
            int otherPossibleCorner1;
            int otherPossibleCorner2;
            int non; //imposible corner

            if (corner == 0)
            {
                otherPossibleCorner1 = 1;
                otherPossibleCorner2 = 3;
                non = 2;

            }
            else if (corner == 1)
            {
                otherPossibleCorner1 = 0;
                otherPossibleCorner2 = 2;
                non = 3;
            }
            else if (corner == 2)
            {
                otherPossibleCorner1 = 1;
                otherPossibleCorner2 = 3;
                non = 0;
            }
            else
            {
                otherPossibleCorner1 = 0;
                otherPossibleCorner2 = 2;
                non = 1;
            }

            int i = 0;
            bool search = true;

            //change this if more depth than 8 
            while (i < 8 && search == true)
            {
                if (array[i] == -1)
                {

                    search = false;
                }
                else if (array[i] == non)
                {
                    isEdge1 = false;
                    isEdge2 = false;
                    search = false;
                }
                else if (array[i] == otherPossibleCorner1)
                {
                    isEdge2 = false;
                }
                else if (array[i] == otherPossibleCorner2)
                {
                    isEdge1 = false;
                }
                i += 1;
            }

            if (isEdge1)
            {
                if (corner == 0)
                {
                    edgeDirections[0] = DIRECTION.NORTH;
                }
                else if (corner == 1)
                {
                    edgeDirections[0] = DIRECTION.NORTH;
                }
                else if (corner == 2)
                {
                    edgeDirections[0] = DIRECTION.EAST;

                }
                else
                {
                    edgeDirections[0] = DIRECTION.WEST;
                }

            }

            if (isEdge2)
            {
                if (corner == 0)
                {
                    edgeDirections[1] = DIRECTION.WEST;
                }
                else if (corner == 1)
                {
                    edgeDirections[1] = DIRECTION.EAST;
                }
                else if (corner == 2)
                {
                    edgeDirections[1] = DIRECTION.SOUTH;

                }
                else
                {
                    edgeDirections[1] = DIRECTION.SOUTH;
                }

            }

            return isEdge1 | isEdge2;

        }
    }




    

    //tree structure used for lod mesh
    public class Quadtree
    {
        public LOD lod;
        public int maxDetailLevel;
        public float radius;
        public Vector3 center;
        public Vector3 localUp;
        public Vector3 AxisA;
        public Vector3 AxisB;


        
        public Vector3[] normals;
       
        public float planetRadius;
       
        public int res;

        public QuadTreeNode[] leafNodes;

        //render mesh vertice and triangle count
        public int verticeCount;
        public int triangleCount;

        //collison mesh vertice and triangle count
        public int verticeCountCollision;
        public int triangleCountCollision;

        //root node 
        public QuadTreeNode root;

        //nodes converted for parallel programming
        public QuadTreeNodeJob[] leafnodeJobs;
        public QuadTreeNodeJob[] leafnodeJobsCollision;

        public int leafNodeCount;
        public int collisionLeafNodeCount;
        public Transform planetCenter;

        public Plane plane;
        public int collisionDetailLevel;
        public Quadtree(int maxDetailLevel, float radius, Vector3 center, Vector3 localUp, Vector3 AxisA, Vector3 AxisB, int res, TerrainGenerationJobs t,Plane p)
        {
            
            this.maxDetailLevel = maxDetailLevel;
            this.radius = radius;
            this.center = center;
            this.localUp = localUp;
            this.AxisA = AxisA;
            this.AxisB = AxisB;
            this.res = res;
            planetCenter = t.transform;
            this.plane = p;
            this.planetRadius = p.planetRadius;
            this.lod = p.Lod;
            this.collisionDetailLevel = p.collisionDetailLevel;
            leafNodeCount = 0;
            collisionLeafNodeCount = 0;

            //preallocate max size 
            this.leafNodes = new QuadTreeNode[100000];
            this.leafnodeJobs = new QuadTreeNodeJob[100000];
            this.leafnodeJobsCollision = new QuadTreeNodeJob[100000];
        }
        //createTree

        //get connecting edge detailLevel  used for removing seams
        public int getNeighborQuadTreeDetailLevel(QuadTreeNode node, int direction)
        {
            // int maxDetail = 10000000;
            Plane neighborPlane = plane.neighbours[direction];
            int connectDirection = plane.neighborConnectDirection[direction];
            int[] nodePath = node.decryptHash();
            int len = nodePath.Length;
            for (int i = 0; i < node.detailLevel; i++)
            {
                nodePath[i] = DIRECTION.DIRECTÝON_MAP[direction, connectDirection, nodePath[i]];
            }

            //search through neighbor quadtree;
            Quadtree neighborTree = neighborPlane.quadtree;
            QuadTreeNode searchNode = neighborTree.root;
            int index = 0;
            while (searchNode.children.Length != 0 && index < node.detailLevel)
            {
                searchNode = searchNode.children[nodePath[index]];
                index += 1;

            }
            return searchNode.detailLevel;
        }

        //toggle edge neighbor bool if true remove seam in the job task
        public void updateEdgeNeighbors()
        {
            int len = leafNodeCount;
            int depth1;
            int depth2; 
            for (int i = 0; i < len; i++)
            {

                leafNodes[i].edgeNeighbours[0] = false;
                leafNodes[i].edgeNeighbours[1] = false;
                leafNodes[i].edgeNeighbours[2] = false;
                leafNodes[i].edgeNeighbours[3] = false;
                if (leafNodes[i].edgeDirections[0] != -1)
                    {
                        


                        depth1 = getNeighborQuadTreeDetailLevel(leafNodes[i], leafNodes[i].edgeDirections[0]);
                        if (depth1 < leafNodes[i].detailLevel)
                        {
                        //
                            leafNodes[i].edgeNeighbours[leafNodes[i].edgeDirections[0]] = true;
                        }
                    }
                    if (leafNodes[i].edgeDirections[1] != -1)
                    {
                        depth2 = getNeighborQuadTreeDetailLevel(leafNodes[i], leafNodes[i].edgeDirections[1]);
                        if (depth2 < leafNodes[i].detailLevel)
                        {
                            //
                            leafNodes[i].edgeNeighbours[leafNodes[i].edgeDirections[1]] = true;
                        }
                }
                
            }
        }
        public void generateTree()
        {
            root = new QuadTreeNode(center, root, null, radius, 0, localUp, AxisA, AxisB, 0, 1,planetCenter,planetRadius,lod);
            root.CreateChildren();
        }
        public void findNeighbours()
        {
            int len = leafNodeCount;
            //leafnodeJobs = new QuadTreeNodeJob[len];
            for (int i = 0; i < len; i++)
            {
                leafNodes[i].findNeighbours();
            }
                
        }
        //convert node format to job compatible format
        public void convertJobs()
        {
            int len = leafNodeCount;
            //leafnodeJobs = new QuadTreeNodeJob[len];
            for(int i = 0; i < len; i++)
            {
                
                leafnodeJobs[i].center = leafNodes[i].center;
                leafnodeJobs[i].axisA = leafNodes[i].axisA;
                leafnodeJobs[i].axisB = leafNodes[i].axisB;
                leafnodeJobs[i].detailLevel = leafNodes[i].detailLevel;
                leafnodeJobs[i].localUp = leafNodes[i].localUp;
                leafnodeJobs[i].radius = leafNodes[i].radius;
         
                leafnodeJobs[i].neighbours0 = leafNodes[i].neighbours[0];
                leafnodeJobs[i].neighbours1= leafNodes[i].neighbours[1];
                leafnodeJobs[i].neighbours2= leafNodes[i].neighbours[2];
                leafnodeJobs[i].neighbours3 = leafNodes[i].neighbours[3];

                leafnodeJobs[i].edgeDirection1 = leafNodes[i].edgeDirections[0];
                leafnodeJobs[i].edgeDirection2 = leafNodes[i].edgeDirections[1];

                leafnodeJobs[i].edgeNeighbors0 = leafNodes[i].edgeNeighbours[0];
                leafnodeJobs[i].edgeNeighbors1 = leafNodes[i].edgeNeighbours[1];
                leafnodeJobs[i].edgeNeighbors2 = leafNodes[i].edgeNeighbours[2];
                leafnodeJobs[i].edgeNeighbors3 = leafNodes[i].edgeNeighbours[3];


                leafnodeJobs[i].verticeIndexStart = i * (res * res);
                leafnodeJobs[i].triangleIndexStart = i * ((res -1)* (res - 1) *6);
            }
            verticeCount = len * (res * res);
            triangleCount= len * ((res - 1) * (res - 1) * 6);
            getCollisionLeafTree();
        }
        public void convertJobsV2()
        {
            int len = leafNodeCount;
            //leafnodeJobs = new QuadTreeNodeJob[len];
            for (int i = 0; i < len; i++)
            {
                leafNodes[i].findNeighbours();
                leafnodeJobs[i].center = leafNodes[i].center;
                leafnodeJobs[i].axisA = leafNodes[i].axisA;
                leafnodeJobs[i].axisB = leafNodes[i].axisB;
                leafnodeJobs[i].detailLevel = leafNodes[i].detailLevel;
                leafnodeJobs[i].localUp = leafNodes[i].localUp;
                leafnodeJobs[i].radius = leafNodes[i].radius;

                leafnodeJobs[i].neighbours0 = leafNodes[i].neighbours[0];
                leafnodeJobs[i].neighbours1 = leafNodes[i].neighbours[1];
                leafnodeJobs[i].neighbours2 = leafNodes[i].neighbours[2];
                leafnodeJobs[i].neighbours3 = leafNodes[i].neighbours[3];

                leafnodeJobs[i].edgeDirection1 = leafNodes[i].edgeDirections[0];
                leafnodeJobs[i].edgeDirection2 = leafNodes[i].edgeDirections[1];

                leafnodeJobs[i].edgeNeighbors0 = leafNodes[i].edgeNeighbours[0];
                leafnodeJobs[i].edgeNeighbors1 = leafNodes[i].edgeNeighbours[1];
                leafnodeJobs[i].edgeNeighbors2 = leafNodes[i].edgeNeighbours[2];
                leafnodeJobs[i].edgeNeighbors3 = leafNodes[i].edgeNeighbours[3];


                leafnodeJobs[i].verticeIndexStart = i * (res * res);
                leafnodeJobs[i].triangleIndexStart = i * ((res - 1) * (res - 1) * 6);
            }
            verticeCount = len * (res * res);
            triangleCount = len * ((res - 1) * (res - 1) * 6);
            getCollisionLeafTree();
        }
        public void updateTree()
        {
            root.UpdateChildren();
        }

        //get leaf nodes for collision mesh 
        public void getCollisionLeafTree()
        {
            //List<QuadTreeNodeJob> collisionMeshNode=new List<QuadTreeNodeJob>();
            collisionLeafNodeCount = 0;
            int len = leafNodeCount;
            int c = 0;
            for(int i = 0; i< len; i++)
            {
                if (leafNodes[i].detailLevel >= collisionDetailLevel)
                {
                    //QuadTreeNodeJob t = new QuadTreeNodeJob();
                    leafnodeJobsCollision[c].center = leafNodes[i].center;
                    leafnodeJobsCollision[c].axisA = leafNodes[i].axisA;
                    leafnodeJobsCollision[c].axisB = leafNodes[i].axisB;
                    leafnodeJobsCollision[c].detailLevel = leafNodes[i].detailLevel;
                    leafnodeJobsCollision[c].localUp = leafNodes[i].localUp;
                    leafnodeJobsCollision[c].radius = leafNodes[i].radius;

                    leafnodeJobsCollision[c].neighbours0 = leafNodes[i].neighbours[0];
                    leafnodeJobsCollision[c].neighbours1 = leafNodes[i].neighbours[1];
                    leafnodeJobsCollision[c].neighbours2 = leafNodes[i].neighbours[2];
                    leafnodeJobsCollision[c].neighbours3 = leafNodes[i].neighbours[3];
                    //need vertice start index for update array
                    leafnodeJobsCollision[c].verticeIndexStart = c * (res * res);
                    leafnodeJobsCollision[c].triangleIndexStart = c * ((res - 1) * (res - 1) * 6);
                    //collisionMeshNode.Add(t);
                    
                    c += 1;
                }

                verticeCountCollision = c * (res * res);
                triangleCountCollision = c * ((res - 1) * (res - 1) * 6);
            }
            collisionLeafNodeCount = c;
            return;
        }

        
        public void setupMeshDataPartition()
        {

            //leafNodes = root.getLeafNode();
            getLeafNodes();
            convertJobs();
         
            
        }

        public void getLeafNodes()
        {
            leafNodeCount = 0;
            DFS(root);
        }

        public void DFS(QuadTreeNode node)
        {

            if (node.children.Length > 0)
            {
                for (int i = 0; i < node.children.Length; i++)
                {
                    DFS(node.children[i]);
                }
            }
            else
            {
                node.quadtreeEdgeFind();
                leafNodes[leafNodeCount] = node;
                leafNodeCount += 1;
            }
            
        }

    
    }

    //a face of cube 
    public class Plane
    {
        
        public Quadtree quadtree;
        public Mesh mesh;
        public Mesh collisionMesh;
        public MeshCollider meshCollider;
        //local up cube normal 
        public Vector3 localUp;
        //cube axis perpendicular to local up
        public Vector3 AxisA;
        public Vector3 AxisB;
        //planet radius
        public float rad;
        public int collisionDetailLevel;
        public int res;
        public List<Vector3> borderVertices;
        public List<int> borderTriangles;
        public TerrainGenerationJobs terrainGenerator;
        public bool complete = false;
        public LOD Lod;
        

        public NativeArray<float3> verticeListFixed;
        public NativeArray<float3> normalListFixed;
        public NativeArray<int> triangleListFixed;
        public NativeArray<float2> uvListFixed;

        public NativeArray<float3> verticeListFixedCol;
        public NativeArray<float3> normalListFixedCol;
        public NativeArray<int> triangleListFixedCol;


        public Vector2[] uvs;
        public Color[] vertexcolors;
        public List<float3> verticesExtra;
        public List<float3> normalsExtra;

        public Plane[] neighbours;
        public int[] neighborConnectDirection;
        public float planetRadius;
        public Plane(Mesh mesh,Mesh collisionMesh, Vector3 localUp, float rad, int res, TerrainGenerationJobs t)
        {
            this.mesh = mesh;
            this.collisionMesh = collisionMesh;
            
            this.localUp = localUp;
            this.rad = rad;
            AxisA = new Vector3(localUp.y, localUp.z, localUp.x);
            AxisB = Vector3.Cross(localUp, AxisA);

            neighbours = new Plane[4];
            neighborConnectDirection = new int[4];

            this.planetRadius = t.radius;
            terrainGenerator = t;
            this.res = res;
            this.Lod = t.lod;
            this.collisionDetailLevel = t.collisionMeshMinDetalLevel;
            generateQuadTree();

            uvs = new Vector2[500000];
        }
        public void setNeighbors(Plane north, Plane south, Plane east, Plane west, int connectNorth, int connectSouth, int connectEast, int connectWest)
        {
            neighbours[DIRECTION.NORTH] = north;
            neighbours[DIRECTION.SOUTH] = south;
            neighbours[DIRECTION.EAST] = east;
            neighbours[DIRECTION.WEST] = west;
            neighborConnectDirection[DIRECTION.NORTH] = connectNorth;
            neighborConnectDirection[DIRECTION.SOUTH] = connectSouth;
            neighborConnectDirection[DIRECTION.EAST] = connectEast;
            neighborConnectDirection[DIRECTION.WEST] = connectWest;
        }


        public void setNativeArrays(NativeArray<float3> verticeListFixed, NativeArray<float3> normalListFixed, NativeArray<int> triangleListFixed,NativeArray<float2> uv)
        {
            this.verticeListFixed = verticeListFixed;
            this.normalListFixed = normalListFixed;
            this.triangleListFixed = triangleListFixed;
            this.uvListFixed = uv;
        }
        public void setNativeArraysCol(NativeArray<float3> verticeListFixed, NativeArray<float3> normalListFixed, NativeArray<int> triangleListFixed)
        {
            this.verticeListFixedCol = verticeListFixed;
            this.normalListFixedCol = normalListFixed;
            this.triangleListFixedCol = triangleListFixed;
        }
        
        public void generateQuadTree()
        {
            quadtree = new Quadtree(8, rad, localUp * rad, localUp, AxisA, AxisB, res, terrainGenerator,this);
            quadtree.generateTree();
        }
        public void updateQuadtree()
        {
            quadtree.updateTree();
            
        }
        

        public void updateMesh()
        {
            mesh.Clear();
            //setUVdataEq();
            //if mesh has more than 65000 vertice should use 32 bit int index
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verticeListFixed, 0, quadtree.verticeCount);
            mesh.SetIndices(triangleListFixed, 0, quadtree.triangleCount, MeshTopology.Triangles, 0, false, 0);
            mesh.SetNormals(normalListFixed, 0, quadtree.verticeCount);
            mesh.SetUVs(0, uvListFixed, 0, quadtree.verticeCount);
            //mesh.SetUVs(0, uvs, 0, quadtree.verticeCount);
        }


        //not used 
        void setUVdataEq()
        {


            verticesExtra = new List<float3>();
            normalsExtra = new List<float3>();

            int triangleCount = quadtree.triangleCount / 3;
            int normalTriangleIndex;
            int vertexIndexA;
            int vertexIndexB;
            int vertexIndexC;
            float3 pointA;
            float3 pointB;
            float3 pointC;



            for (int i = 0; i < quadtree.verticeCount; i++)
            {
                uvs[i] = getUVEq(normalListFixed[i]);

                //vertexcolors[i] = terrainGenerator.planetTextureData[(int)(tv * terrainGenerator.planetMap.width + tu)];
            }
            
            int extraVerticeCount = 0;
            float3 triangleNormal;
            for (int i = 0; i < triangleCount; i++)
            {
                normalTriangleIndex = i * 3;
                vertexIndexA = triangleListFixed[normalTriangleIndex];
                vertexIndexB = triangleListFixed[normalTriangleIndex + 1];
                vertexIndexC = triangleListFixed[normalTriangleIndex + 2];

                pointA = verticeListFixed[vertexIndexA];
                pointB = verticeListFixed[vertexIndexB];
                pointC = verticeListFixed[vertexIndexC];
                // float3 avg = (pointA + pointB + pointC) / 3;
                float2 uvA = getUVEq(normalListFixed[vertexIndexA]);
                float2 uvB = getUVEq(normalListFixed[vertexIndexB]);
                 float2 uvC = getUVEq(normalListFixed[vertexIndexC]);
                float3 nA = normalListFixed[vertexIndexA];
                float3 nB = normalListFixed[vertexIndexB];
                float3 nC = normalListFixed[vertexIndexC];
                float threshold = 0.5f;

                if (nA.z <= 0.001f && nA.z >= -0.001f && nA.x > 0.01f && (uvB.x>threshold || uvC.x>threshold))
                {

                    verticesExtra.Add(verticeListFixed[vertexIndexA]);
                    normalsExtra.Add(normalListFixed[vertexIndexA]);
                    triangleListFixed[normalTriangleIndex] = quadtree.verticeCount + extraVerticeCount;
                    uvs[quadtree.verticeCount + extraVerticeCount].x = 1.0f;
                    uvs[quadtree.verticeCount + extraVerticeCount].y = uvA.y;
                    extraVerticeCount += 1;
                }
                
                if (nB.z <= 0.001f && nB.z >= -0.001f && nB.x >= 0.01f && (uvC.x > threshold || uvA.x > threshold))
                {

                    verticesExtra.Add(verticeListFixed[vertexIndexB]);
                    normalsExtra.Add(normalListFixed[vertexIndexB]);
                    triangleListFixed[normalTriangleIndex+1] = quadtree.verticeCount + extraVerticeCount;
                    uvs[quadtree.verticeCount + extraVerticeCount].x = 1.0f;
                    uvs[quadtree.verticeCount + extraVerticeCount].y = uvB.y;
                    extraVerticeCount += 1;
                }
                
                if (nC.z <= 0.001f && nC.z >= -0.001f && nC.x >= 0.01f && (uvA.x > threshold || uvB.x > threshold))
                {

                    verticesExtra.Add(verticeListFixed[vertexIndexC]);
                    normalsExtra.Add(normalListFixed[vertexIndexC]);
                    triangleListFixed[normalTriangleIndex+2] = quadtree.verticeCount + extraVerticeCount;
                    uvs[quadtree.verticeCount + extraVerticeCount].x = 1.0f;
                    uvs[quadtree.verticeCount + extraVerticeCount].y = uvC.y;
                    extraVerticeCount += 1;
                }

                //pole


            }
            
            for(int i = 0; i < extraVerticeCount; i++)
            {
                verticeListFixed[quadtree.verticeCount] = verticesExtra[i];
                normalListFixed[quadtree.verticeCount] = normalsExtra[i];
                quadtree.verticeCount += 1;
            }
            /*
            for (int i = 0; i < quadtree.verticeCount; i++)
            {
                //atan(modelPositionVarying.y, modelPositionVarying.x) / (2 * M_PI) + .5, acos(modelPositionVarying.z / sqrt(length(modelPositionVarying.xyz))
                float3 n = math.normalize(verticeListFixed[i]);

                float u = math.atan2(n.z, n.x) / (math.PI * 2.0f); //asin(Nx) / PI + 0.5
                
                float v = math.acos(-n.y) / math.PI;
                uvs[i].x = u;
                uvs[i].y = v;
                if(uvs[i].x<0.001f && uvs[i].x >- 0.001f)
                {
                    verticesExtra.Add(verticeListFixed[i]);
                }
               
                //vertexcolors[i] = terrainGenerator.planetTextureData[(int)(tv * terrainGenerator.planetMap.width + tu)];
            }
            */
            //mesh.SetColors(vertexcolors);
          
        }

        float2 getUVEq(float3 n)
        {

            float u = math.atan2(n.z, n.x) / (math.PI * 2.0f); //asin(Nx) / PI + 0.5
             float v = math.acos(-n.y) / math.PI;
            //float u = math.asin(n.x)/math.PI +0.5f;
            //float v = math.asin(n.y) / math.PI + 0.5f;
            
            u = u % 1;
            if (u < 0 )
            {
                u += 1;
               
            }
            v = v % 1;

            if (v < 0)
            {
                v += 1;
            }

            if (u > 0.995f || u < 0.005f)
            {
                if(n.z<0.001f && n.z>=-0.001f)
                u = 0;
            }

            if (n.z < 0.01f && n.z >= -0.01f && n.x<0.01f && n.x >= -0.01f)
            {
                u = 0.0f;
            }
               
            return new float2(u, v); 
        }
        public void updateCollisionMesh()
        {
            collisionMesh.Clear();
            if (quadtree.verticeCountCollision == 0)
            {
                return;
            }
            collisionMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            collisionMesh.SetVertices(verticeListFixedCol, 0, quadtree.verticeCountCollision);
            collisionMesh.SetIndices(triangleListFixedCol, 0, quadtree.triangleCountCollision, MeshTopology.Triangles, 0, false, 0);
            collisionMesh.SetNormals(normalListFixedCol, 0, quadtree.verticeCountCollision);
            
            meshCollider.sharedMesh = collisionMesh;
        }
       
       
    }



    //used for calculating neighbor nodes in quadtree
    //directions quadtree
    //0 1
    //3 2

    //every node has a value 0 1 2 3 corresponding place in quadtree

 
    public static class DIRECTION
    {
        public static int EAST = 0;
        public static int NORTH = 1;
        public static int WEST = 2;
        public static int SOUTH = 3;
        public static int[] MIRROR_AXIS_X = new int[4] { 3, 2, 1, 0 };
        public static int[] MIRROR_AXIS_Y = new int[4] { 1, 0, 3, 2 };
        // E 12
        // N 01
        // W 03
        // S 23

        //connection map DIRECTION_MAP[START_DIR][TARGET_DIR][NW,NE,SE,SW]
        public static int[,,] DIRECTÝON_MAP = new int[4, 4, 4] {
            { { -1, 2, 1, -1 }, { -1, 1, 0, -1 }, { -1,  0,  3, -1 }, { -1, 3,2, -1 } },//E
            { { 2, 1, -1, -1 }, { 1, 0 , -1,-1 }, {  0,  3, -1, -1 }, {  3, 2,-1, -1 } },  //N
            { { 1, -1, -1, 2 }, { 0, -1, -1, 1 }, { 3, -1, -1, 0 }, { 2, -1, -1, 3} }, //W
            { { -1,-1 , 2, 1 }, { -1,-1,  1, 0}, { -1 ,-1 , 0,3 }, { -1,-1,  3, 2 } } //S
        };
    }


    //parallel computing data 
    [BurstCompile]
    public struct CalculatePositionJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<QuadTreeNodeJob> nodesJob;
      
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> verticesJob;
        [NativeDisableParallelForRestriction]
        public NativeArray<float3> normalsJob;
        [NativeDisableParallelForRestriction]
        public NativeArray<int> trianglesJob;
        [NativeDisableParallelForRestriction]
        public NativeArray<float2> uvJob;


        [ReadOnly] public NativeArray<int> tmpTriangleJob;
        [ReadOnly] public NativeArray<int> tmpTriangleJobBordered;
        [ReadOnly] public NativeArray<int> borderedSizeIndex;
        [ReadOnly] public int resJob;
        [ReadOnly] public float radiusJob;
        [ReadOnly] public float baseFrequency;
        [ReadOnly] public float2 uvMap; //uv index 0 1 row 0 column 1
        [ReadOnly] public bool isCollision;
        [ReadOnly] public NativeArray<float> heightMap;
        [ReadOnly] public int2 heightmapDimensions;
        [ReadOnly] public float heightMapPower;
        public void Execute(int index)
        {
            NativeArray<float3> tmpVertices = new NativeArray<float3>(resJob * resJob, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> tmpNormals = new NativeArray<float3>(resJob * resJob, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<float2> tmpUV = new NativeArray<float2>(resJob * resJob, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            convertToVertices(nodesJob[index],tmpVertices,tmpNormals,tmpUV);
            

            for (int j = 0; j < tmpVertices.Length; j++)
            {
                verticesJob[j + nodesJob[index].verticeIndexStart] = tmpVertices[j];
            }
            



            for (int j = 0; j < tmpTriangleJob.Length; j++)
            {
                trianglesJob[j + nodesJob[index].triangleIndexStart] = tmpTriangleJob[j] + nodesJob[index].verticeIndexStart;
            }


            for (int j = 0; j < tmpNormals.Length; j++)
            {
                normalsJob[j + nodesJob[index].verticeIndexStart] = tmpNormals[j];

            }

            if (isCollision == false)
            {
                for (int j = 0; j < tmpUV.Length; j++)
                {
                    uvJob[j + nodesJob[index].verticeIndexStart] = tmpUV[j];

                }
            }
            
            tmpVertices.Dispose();
            tmpNormals.Dispose();
            tmpUV.Dispose();
        }
        public void convertToVertices(QuadTreeNodeJob node,NativeArray<float3>   verticeArray, NativeArray<float3> normalArray, NativeArray<float2> uvArray)
        {
            
            NativeArray<float3> borderedVerticeArray = new NativeArray<float3>((resJob + 2) * (resJob + 2), Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<float3> normalsBorder = new NativeArray<float3>((resJob + 2) * (resJob + 2), Allocator.Temp, NativeArrayOptions.ClearMemory);
            NativeArray<float2> uvBorder = new NativeArray<float2>((resJob + 2) * (resJob + 2), Allocator.Temp, NativeArrayOptions.UninitializedMemory);



            int count = 0;



            float3 pointOnCube;
            float3 pointOnSphere;

            float multiplier = 1;
            /*
            if (node.edgeDirection1 != -1 || node.edgeDirection2 != -1)
            {
                multiplier = 1.2f;
            }
            */
            /*
            if (node.edgeNeighbors0 || node.edgeNeighbors1|| node.edgeNeighbors2 || node.edgeNeighbors3)
            {
                multiplier = 1.2f;
            }
            */
            for (int i = 0; i < resJob + 2; i++)
            {
                for (int j = 0; j < resJob + 2; j++)
                {

                    float2 percent = new float2(j - 1, i - 1) / (resJob - 1);
                    pointOnCube = node.center + ((percent.x - 0.5f) * 2 * node.axisA+ (percent.y - 0.5f) * 2 * node.axisB) * node.radius;

                    //float3 x= pointOnCube;
                    
                      //calculate uvs 
                        float2 Coordinate2D = new float2();  
                        if (node.localUp.x != 0)
                        {
                           
                                Coordinate2D.x = pointOnCube.z;
                                Coordinate2D.y = pointOnCube.y;
                           

                        }
                        else if (node.localUp.y != 0)
                        {
                            if (node.localUp.y > 0)
                            {
                                Coordinate2D.x = pointOnCube.z;
                                Coordinate2D.y = pointOnCube.x;
                            }
                            else
                            {
                                Coordinate2D.x = pointOnCube.z;
                                Coordinate2D.y = pointOnCube.x;
                            }
                        }
                        else
                        {
                            if (node.localUp.z > 0)
                            {
                                Coordinate2D.x = pointOnCube.x;
                                Coordinate2D.y = pointOnCube.y;
                            }
                            else
                            {
                                Coordinate2D.x = pointOnCube.x;
                                Coordinate2D.y = pointOnCube.y;
                            }
                        }

                        Coordinate2D.x += radiusJob;
                        Coordinate2D.y += radiusJob;
                        Coordinate2D  /= (radiusJob * 2);
                    if (node.localUp.x != 0)
                    {
                        
                        if (node.localUp.x > 0)
                        {
                            Coordinate2D.x = 1 - Coordinate2D.x;
                            Coordinate2D.y = 1 - Coordinate2D.y;

                        }
                        else
                        {
                           
                            Coordinate2D.y = 1 - Coordinate2D.y;
                        }
                        
                    }
                    else if (node.localUp.y != 0)
                    {
                        if (node.localUp.y > 0)
                        {
                            Coordinate2D.x = 1 - Coordinate2D.x;

                        }
                        else
                        {
                            Coordinate2D.x = 1 - Coordinate2D.x;
                            Coordinate2D.y = 1 - Coordinate2D.y;
                        }
                       
                    }
                    else
                    {
                        if (node.localUp.z < 0)
                        {
                            Coordinate2D.x = 1 - Coordinate2D.x;
                            Coordinate2D.y = 1 - Coordinate2D.y;
                        }
                        else
                        {
                          
                            Coordinate2D.y = 1 - Coordinate2D.y;
                        }
                       
                        
                    }
                    Coordinate2D.x = (Coordinate2D.x / 4) + uvMap.y*0.25f;
                        Coordinate2D.y = (Coordinate2D.y / 4) + uvMap.x*0.25f;
                        
                        uvBorder[count] = Coordinate2D;

                   
                    pointOnSphere = math.normalize(pointOnCube);


                    borderedVerticeArray[count] = pointOnSphere * (radiusJob+heightMapPower*calculateHeightMap(uvBorder[count]));//pointOnSphere*radius;
                    //borderedVerticeArray[count] = terrainGenerator.CalculatePointOnPlanet(pointOnSphere, radius);
                    //normalsBorder[count] = pointOnSphere;


                    count += 1;


                }
            }


            calculateNormals(borderedVerticeArray, tmpTriangleJobBordered, normalsBorder);

            count = 0;
            //stitching
            
            for (int i = 0; i < resJob + 2; i++)
            {
                for (int j = 0; j < resJob + 2; j++)
                {


                    //n 
                    if (node.neighbours1)
                    {
                        if (j == resJob && i % 2 == 0 && i > 0 && i < resJob + 1)
                        {
                            borderedVerticeArray[count] = (borderedVerticeArray[count - (resJob + 2)] + borderedVerticeArray[count + (resJob + 2)]) / 2; //= (borderedVerticeArray[count - (res+1)] + borderedVerticeArray[count +(res+1])/2;
                        }
                    }

                    //s
                    if (node.neighbours3)
                    {
                        if (j == 1 && i % 2 == 0 && i > 0 && i < resJob + 1)
                        {
                            borderedVerticeArray[count] = (borderedVerticeArray[count - (resJob + 2)] + borderedVerticeArray[count + (resJob + 2)]) / 2; //= (borderedVerticeArray[count - (res+1)] + borderedVerticeArray[count +(res+1])/2;
                        }
                    }
                    //e
                    if (node.neighbours0)
                    {
                        if (i == resJob && j % 2 == 0)
                        {

                            borderedVerticeArray[count] = (borderedVerticeArray[count - 1] + borderedVerticeArray[count + 1]) / 2;
                        }
                    }
                    //w
                    if (node.neighbours2)
                    {
                        if (i == 1 && j % 2 == 0)
                        {
                            borderedVerticeArray[count] = (borderedVerticeArray[count - 1] + borderedVerticeArray[count + 1]) / 2;
                        }
                    }
                    count += 1;
                }


            }
           
            count = 0;
            //edge Stitching
            if (node.edgeNeighbors0 || node.edgeNeighbors1 || node.edgeNeighbors2 || node.edgeNeighbors3)
            {
                for (int i = 0; i < resJob + 2; i++)
                {
                    for (int j = 0; j < resJob + 2; j++)
                    {


                        //north
                        if (node.edgeNeighbors1)
                        {
                            if (j == resJob && i % 2 == 0 && i > 0 && i < resJob + 1)
                            {
                                borderedVerticeArray[count] = (borderedVerticeArray[count - (resJob + 2)] + borderedVerticeArray[count + (resJob + 2)]) / 2;
                                //borderedVerticeArray[count] *= 1.1f;
                            }
                        }

                        //south
                        if (node.edgeNeighbors3)
                        {
                            if (j == 1 && i % 2 == 0 && i > 0 && i < resJob + 1)
                            {
                                borderedVerticeArray[count] = (borderedVerticeArray[count - (resJob + 2)] + borderedVerticeArray[count + (resJob + 2)]) / 2;
                                //borderedVerticeArray[count] *= 1.1f;
                                //= (borderedVerticeArray[count - (res+1)] + borderedVerticeArray[count +(res+1])/2;
                            }
                        }

                        //east
                        if (node.edgeNeighbors0)
                        {
                            if (i == resJob && j % 2 == 0)
                            {

                                borderedVerticeArray[count] = (borderedVerticeArray[count - 1] + borderedVerticeArray[count + 1]) / 2;
                                //borderedVerticeArray[count] *= 1.1f;
                            }
                        }

                        //west
                        if (node.edgeNeighbors2)
                        {
                            if (i == 1 && j % 2 == 0)
                            {
                               borderedVerticeArray[count] = (borderedVerticeArray[count - 1] + borderedVerticeArray[count + 1]) / 2;
                                //borderedVerticeArray[count] *= 1.1f;
                            }
                        }
                        count += 1;
                    }


                }

            }



           

            int c = 0;
            for (int i = 0; i < borderedVerticeArray.Length; i++)
            {
                if (borderedSizeIndex[i] >= 0)
                {
                    verticeArray[borderedSizeIndex[i]] = (borderedVerticeArray[i]);
                    normalArray[borderedSizeIndex[i]] = normalsBorder[i];
                    uvArray[borderedSizeIndex[i]] = uvBorder[i];
                    c += 1;
                }
            }

            borderedVerticeArray.Dispose();
            normalsBorder.Dispose();
            uvBorder.Dispose();
        }
        float calculateHeightMap(float2 uv)
        {
            return bilinearFiltering(math.clamp(uv.x, 0, 1), math.clamp(uv.y, 0, 1));
            /*
            uv.x = math.clamp(uv.x, 0, 1);
            uv.y = math.clamp(uv.y, 0, 1);
            
            int index = (int)((uv.y * heightmapDimensions.x) * (heightmapDimensions.x) + (uv.x * heightmapDimensions.y));
            index = math.clamp(index, 0, heightmapDimensions.x * heightmapDimensions.y - 1);
            return heightMap[index].r ;
            */
        }
        //sample texture
        float bilinearFiltering(float xf,float yf)
        {
            int w = heightmapDimensions.x-1;
            int h = heightmapDimensions.y-1;
            int x1 =(int) math.floor(xf * w);
            int y1 =(int) math.floor(yf * h);
            int x2 =(int)math.clamp(x1 + 1, 0, w);
            int y2 =(int)math.clamp(y1 + 1, 0, h);

            float xp = xf * w - x1;
            float yp = yf * h - y1;

            //colors
            float p11 = getPixel(x1, y1);
            float p21 = getPixel(x2, y1);
            float p12 = getPixel(x1, y2);
            float p22 = getPixel(x2, y2);

            float px1 = math.lerp( p11, p21,xp);
            float px2 = math.lerp( p12, p22,xp);

            return math.lerp( px1, px2,yp);
        }
        float getPixel(int x,int y)
        {
            return heightMap[(x + heightmapDimensions.x * y)];

        }

        //procedural 
        /*
        float heightCalc(float3 pointUnitSphere)
        {
            float noiseFinal = 0.0f;
            int len = noiseSettings.Length;
            float amplitude;
            float frequency;
            float mask;
            
                if (noiseSettings[0].ridge == false)
                {
                    noiseFinal += SimpleNoise(pointUnitSphere, 0,1);
                }
                else
                {
                    noiseFinal += RidgeNoise(pointUnitSphere, 0,1);
                }

            mask = noiseFinal;

            for (int i = 1; i < len; i++)
            {
                if (noiseSettings[i].ridge == false)
                {
                    noiseFinal += SimpleNoise(pointUnitSphere, i,mask);
                }
                else
                {
                    noiseFinal += RidgeNoise(pointUnitSphere, i,mask);
                }
              
            }

            return noiseFinal;
        }
        
        float SimpleNoise(float3 pointUnitSphere, int index,float mask)
        {
            if (noiseSettings[index].useMask && mask == 0)
            {
                return 0;
            }
            float tmpNoise = 0;
            float amplitude = 1;
            float frequency = noiseSettings[index].baseRoughness;
      
            float v;
            for (int j = 0; j < noiseSettings[index].layer; j++)
            {
                v =noise.snoise(pointUnitSphere * frequency + noiseSettings[index].offset);
              
                tmpNoise += v * amplitude;
                amplitude *= noiseSettings[index].persistance;
                frequency *= noiseSettings[index].roughness;
            }

            tmpNoise = math.max(0, tmpNoise - noiseSettings[index].minVal);
            if (noiseSettings[index].useMask)
            {
                tmpNoise *= mask;
            }
            return tmpNoise * noiseSettings[index].power;
        }
        

        

        float RidgeNoise(float3 pointUnitSphere, int index,float mask)
        {
            if (noiseSettings[index].useMask && mask == 0)
            {
                return 0;
            }
            float tmpNoise = 0;
            float amplitude = 1;
           
            float frequency = noiseSettings[index].baseRoughness;
            float weight = noiseSettings[index].ridgeWeight;
            float v;
            for (int j = 0; j < noiseSettings[index].layer; j++)
            {
                v =1-math.abs( noise.snoise(pointUnitSphere * frequency + noiseSettings[index].offset));
                v *= v;
                v *= weight;
                tmpNoise += v * amplitude;
                amplitude *= noiseSettings[index].persistance;
                frequency *= noiseSettings[index].roughness;
            }
           
            tmpNoise = math.max(0, tmpNoise - noiseSettings[index].minVal);
            if (noiseSettings[index].useMask)
            {
                tmpNoise *= mask;
            }
            return tmpNoise* noiseSettings[index].power;
        }
        */
        void calculateNormals(NativeArray<float3> vertices, NativeArray<int> triangles, NativeArray<float3> normals)
        {


            int triangleCount = triangles.Length / 3;
            int normalTriangleIndex;
            int vertexIndexA;
            int vertexIndexB;
            int vertexIndexC;
            float3 pointA;
            float3 pointB;
            float3 pointC;

            float3 sideAB;
            float3 sideAC;
           
            float3 triangleNormal;
            for (int i = 0; i < triangleCount; i++)
            {
                normalTriangleIndex = i * 3;
                vertexIndexA = triangles[normalTriangleIndex];
                vertexIndexB = triangles[normalTriangleIndex + 1];
                vertexIndexC = triangles[normalTriangleIndex + 2];
                

                pointA = vertices[vertexIndexA];
                pointB = vertices[vertexIndexB];
                pointC = vertices[vertexIndexC];

                sideAB =pointB - pointA;
                sideAC = pointC - pointA;
               
                triangleNormal = math.cross(sideAB, sideAC);
                triangleNormal = math.normalize(triangleNormal);
                
                
                normals[vertexIndexA] += triangleNormal;
                normals[vertexIndexB] += triangleNormal;
                normals[vertexIndexC] += triangleNormal;



            }
            int len = normals.Length;
            for (int i = 0; i < len; i++)
            {
               
                normals[i] = math.normalize(normals[i]);
                
            }
        }
    }

    //parallel computing cant use classes struct is used
    public struct QuadTreeNodeJob
    {
        public float3 center;
        public float radius;
        public bool neighbours0;
        public bool neighbours1;
        public bool neighbours2;
        public bool neighbours3;
        public int detailLevel;
        public float3 localUp;
        public float3 axisA;
        public float3 axisB;
        public int verticeIndexStart;
        public int triangleIndexStart;
        public int edgeDirection1;
        public int edgeDirection2;
        public bool edgeNeighbors0;
        public bool edgeNeighbors1;
        public bool edgeNeighbors2;
        public bool edgeNeighbors3;
    }


    [System.Serializable]
    public class LOD
    {
        public static Transform cameraPos;
        public int MaxDetail = 9;
        public Dictionary<int, float> detailLevelDist = new Dictionary<int, float>()
        {
            {0,Mathf.Infinity },
            {1,60f },
            {2,25f },
            {3,10f },
            {4,4f },
            {5,1.5f },
            {6,0.7f },
            {7,0.3f },
            {8,0.1f }
        };

    }
    /*
    private void _OnDrawGizmos()
    {
        return;
        int d = 0;
       
        if (cube[d] == null)
        {
            return;
        }
        if (start == false)
        {
            return;
        }
        for(int k = 0; k < 6; k++)
        {
            for (int i = 0; i < cube[k].quadtree.verticeCount; i++)
            {
                float3 n = cube[k].normalListFixed[i];
                if (n.z <= 0.001f && n.z >= -0.001f && n.x >= -0.01f && n.x <= 0.01f )
                {
                    Debug.Log("hehe " + cube[k].uvs[i].x.ToString());
                    Gizmos.DrawSphere(cube[k].verticeListFixed[i], 50);
                }
                //Gizmos.DrawSphere(cube[0].verticeListFixed[i]+ cube[0].normalListFixed[i]*20, 10f);
                //Gizmos.DrawLine(cube[d].verticeListFixed[i], cube[d].verticeListFixed[i] + cube[d].normalListFixed[i] * 150f);
                //Gizmos.DrawSphere(cube[d].verticeListFixed[i], 100 +100* cube[k].uvs[i].x);
                
            }
        }
           
        
       
    }
    */
}

