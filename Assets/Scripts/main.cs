using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class main : MonoBehaviour
{

    /*
        How this works:

        Firstly, the world is chopped up into chunks which are small cubes.
        Then the verticies of the static map get iterated over and each assigned to a different chunk.
        Then entities are spawned and assigned to the chunk they are in.

        Every Update game loops through every entity.
        Game checks the entity's chunk and iterates over all of the neighbouring chunks.
        When iterating over a chunk, it checks whether what the chunk contains (other entities, map verticies, or dynamic object verticies) are nearby and adds up their positions to variables.
        After it is done iterating over chunks it averages out each sum and normalizes it to create a movement vector.
        After the movement vector gets applied to the entity, the game checks whether entitiy's chunk has changed, if it did, then it removes it from the old chunk and moves it to the new one.
        Then it is ready to go to the next entity.

        The game could be more optimized using asynchronous functions, but I didn't know up until now that you can't use them in unity.
        You have to use the built in Jobs library which I tried but the way I've been writing the code up until now is incompatible with jobs.
    */

    System.Random rand;
    void Start(){

        xChunkAmount = Mathf.CeilToInt(xLength/lengthOfChunk)+1;
        yChunkAmount = Mathf.CeilToInt(yLength/lengthOfChunk)+1;
        zChunkAmount = Mathf.CeilToInt(zLength/lengthOfChunk)+1;

        initializeStaticMapVerticies(calculatePositionOfStaticVectorsOnMesh());
        
        rand = new System.Random();
        addEntities(500,0);
        addEntities(5,1);
    }

    public int greenGuys = 0;

    List<Entity> entityList = new List<Entity>();
    Entity mainEntity;    

    void Update(){
        for(int i = 0; i < entityList.Count; i++){
            mainEntity = entityList[i];
            goThroughChunkAndNeighbours();
            setNewMovement();
            checkEntitiesChunkAndAssignItThere();
        }
    }

    public struct Chunk
    {
        public Chunk(List<Entity> e, List<Vector3> v){
            entities = e;
            mapVerticies = v;
            rod = new List<Vector3>();
        }

        public List<Entity> entities;
        public List<Vector3> mapVerticies;
        public List<Vector3> rod;
    }

    public class Entity
    {
        public Entity(Transform TRANSFORM, Vector3 VELOCITY, int TYPE, int INDEXINLIST, single_entity SCRIPT){
            transform = TRANSFORM;
            velocity = VELOCITY;
            type = TYPE;
            indexInList = INDEXINLIST;
            script = SCRIPT;
            cooldown = 0f;
            speed = 1f;
        }

        public Transform transform;
        public Vector3 velocity;
        public int type;
        public single_entity script;
        public float cooldown;
        public float speed;

        //Current chunk information, so we know what chunk to remove it from.
        public int x,y,z,indexInChunk, indexInList;

    }

    public GameObject entityPrefab;

    public void addEntities(int amount, int type){
        Quaternion quaternion = new Quaternion();
        quaternion.eulerAngles = new Vector3(-90f,0,0);

        for(int i = 0; i < amount; i++){
            //Calculate random spawn in a circle from the center.
            Vector3 spawn = new Vector3(rand.Next(5, 20),rand.Next(0, 10),0);
            spawn = Quaternion.Euler(0, rand.Next(0,360), 0) * spawn;

            //Instantiate the object, create the entity, add it to the global entity list.
            GameObject entityObject = Instantiate(entityPrefab, spawn, quaternion);
            Entity tempEntity = new Entity(entityObject.transform, Vector3.Normalize(new Vector3(rand.Next(-3,3),rand.Next(-3,3),rand.Next(-3,3))), type, entityList.Count+1, entityObject.GetComponent<single_entity>());
            entityList.Add(tempEntity);

            //Set the main entity the rest of the code will be using.
            mainEntity = entityList[entityList.Count-1];

            //Assign it to a chunk it's in.
            firstChunkAssign();

            //Objects start out green (0), so if the object is red (1) then change his looks.
            if(type == 1){mainEntity.script.changeToChaser();}
            else{greenGuys++;}
        }
    }

    int neighbourAmount, tooCloseNeighbourAmount, closeWallAmount, farWallAmount, mainType;
    Vector3 sumOfNeighboursVelocities, sumOfNeighboursPositions, sumOfNeighboursDistances, sumOfFarWallPositions, sumOfCloseWallPositions, playerPosition, mainPosition, mainVelocity;

    void resetGlobalMovementVariables(){
        //Reset everything
        neighbourAmount = 0;
        tooCloseNeighbourAmount = 0;
        closeWallAmount = 0;
        farWallAmount = 0;
        sumOfNeighboursVelocities = new Vector3(0,0,0);
        sumOfNeighboursPositions = new Vector3(0,0,0);
        sumOfNeighboursDistances = new Vector3(0,0,0);
        sumOfFarWallPositions = new Vector3(0,0,0);
        sumOfCloseWallPositions = new Vector3(0,0,0);
        playerPosition = new Vector3(0,0,0);

        mainPosition = mainEntity.transform.position;
        mainVelocity = mainEntity.velocity;
        mainType = mainEntity.type;
    }

    Chunk chunk;
    List<List<List<Chunk>>> chunks = new List<List<List<Chunk>>>();

    void goThroughChunkAndNeighbours(){
        resetGlobalMovementVariables();
        //Start 1 off of the entities current chunk, so it can then move 2 chunks up, 2 forward, and 2 right creating a 3x3 cube of chunks.
        int neighbourX = mainEntity.x-1, neighbourY = mainEntity.y-1, neighbourZ = mainEntity.z-1;

        //Go through a 3x3 cube of chunks around the chunk the entity is in, and calculate movement based on entities inside those chunks.
        for(int x = neighbourX; x <= mainEntity.x+1; x++){
            if(x >= 0 && x < xChunkAmount){
                for(int y = neighbourY; y <= mainEntity.y+1; y++){
                    if(y >= 0 && y < yChunkAmount){
                        for(int z = neighbourZ; z <= mainEntity.z+1; z++){
                            if(z >= 0 && z < zChunkAmount){
                                chunk = chunks[x][y][z];
                                sumUpMovementInsideChunk();
                            }
                        }
                    }
                }
            }
        }
        sumUpPlayerAvoidanceMovement();
    }


    void sumUpFlockMovementFromEveryEntityWithinRadius(){

        //For every entity inside the currently iterated over chunk.
        for(int i = 0; i < chunk.entities.Count; i++){
            Vector3 neighbourPosition = chunk.entities[i].transform.position;
            Vector3 neighbourVelocity = chunk.entities[i].velocity;
            int neighbourType = chunk.entities[i].type;

            //Calculate the square distance instead of the actual distance so we don't waste time on calculating roots.
            Vector3 distanceBetweenMainAndNeighbour = mainEntity.transform.position - neighbourPosition;
            float neighbourDistanceSquared = Mathf.Pow(distanceBetweenMainAndNeighbour.x,2) + Mathf.Pow(distanceBetweenMainAndNeighbour.y,2) +Mathf.Pow(distanceBetweenMainAndNeighbour.z,2);
                
            //If the neighbour is close then calculate movement.
            if(neighbourDistanceSquared < 81f/16f && i != mainEntity.indexInList){

                //If the the neighbour is green then move towards him.
                if(neighbourType == 0){
                    //Cohesion
                    sumOfNeighboursPositions += neighbourPosition;
                    //Alignment
                    sumOfNeighboursVelocities += neighbourVelocity;
                    neighbourAmount++;
                    //Seperation
                    if(neighbourDistanceSquared < 25f/16f && mainType == 0){
                        sumOfNeighboursDistances += (neighbourPosition - mainEntity.transform.position);
                        tooCloseNeighbourAmount++;
                    }
                }
                //If entity is really close to his neighbour, and it's neighbour is red, and entity is green, and cooldown is not active, then change entity to red.
                else if(neighbourDistanceSquared < 9f/16f && neighbourType == 1 && mainType == 0 &&  chunk.entities[i].cooldown < Time.time){
                    //Add a 4 second cooldown so flocks don't explode when one red entity enters it.
                    chunk.entities[i].cooldown = mainEntity.cooldown = Time.time + 4f;    
                    //Change the entity to red.
                    greenGuys--;                    
                    mainType = 1;
                    mainEntity.script.changeToChaser(); 
                }
            }    
        }
    }

    void sumUpWallAvoidanceMovement(){
        //For every static verticy inside the currently iterated over chunk.
        for(int i = 0; i < chunk.mapVerticies.Count; i++){
            Vector3 wallPosition = chunk.mapVerticies[i];
            
            //Calculate the square distance instead of the actual distance so we don't waste time on calculating roots.
            Vector3 distanceBetweenMainAndWall = mainEntity.transform.position - wallPosition;
            float wallDistanceSquared = Mathf.Pow(distanceBetweenMainAndWall.x,2) + Mathf.Pow(distanceBetweenMainAndWall.y,2) +Mathf.Pow(distanceBetweenMainAndWall.y,2);

            //If entity is close to a wall
            if(wallDistanceSquared < 81f/16f){
                //If entity is reaalllyy close to a wall
                if(wallDistanceSquared < 6f/16f){
                    sumOfCloseWallPositions += wallPosition - mainEntity.transform.position;
                    closeWallAmount++;
                }
                else{
                    sumOfFarWallPositions += wallPosition - mainEntity.transform.position;
                    farWallAmount++;
                }
                //The difference between those two is that entities will avoid when they are close, but bounce off when they are really close.
            } 
        }
    }

    void sumUpDynamicAvoidanceMovement(){
        //For every verticy on the rotating rod.
        for(int i = 0; i < chunk.rod.Count; i++){
            Vector3 wallPosition = chunk.rod[i];

            //Calculate the square distance instead of the actual distance so we don't waste time on calculating roots.
            Vector3 distanceBetweenMainAndWall = mainEntity.transform.position - wallPosition;
            float wallDistanceSquared = Mathf.Pow(distanceBetweenMainAndWall.x,2) + Mathf.Pow(distanceBetweenMainAndWall.y,2) +Mathf.Pow(distanceBetweenMainAndWall.z,2);

            //If entity is close to a wall
            if(wallDistanceSquared < 81f/16f){
                //If entity is reaalllyy close to a wall
                if(wallDistanceSquared < 6f/16f){
                    sumOfCloseWallPositions -= distanceBetweenMainAndWall;
                    closeWallAmount++;
                }
                else{
                    sumOfFarWallPositions -= distanceBetweenMainAndWall;
                    farWallAmount++;
                }
            } 
        }
    }

    public GameObject player;
    public AudioSource audioSource;
    void sumUpPlayerAvoidanceMovement(){
        //Calculate the square distance instead of the actual distance so we don't waste time on calculating roots
        Vector3 distanceBetweenMainAndPlayer = mainEntity.transform.position - player.transform.position;
        float playerDistanceSquared = Mathf.Pow(distanceBetweenMainAndPlayer.x,2) + Mathf.Pow(distanceBetweenMainAndPlayer.y,2) +Mathf.Pow(distanceBetweenMainAndPlayer.z,2);

        if(playerDistanceSquared < 4f){
            //Play dodgeball sound when hitting red entity
            if(mainType == 1){audioSource.Play();}
            playerPosition = distanceBetweenMainAndPlayer;
        }
    }

    void sumUpMovementInsideChunk(){
        sumUpFlockMovementFromEveryEntityWithinRadius();
        sumUpWallAvoidanceMovement();
        sumUpDynamicAvoidanceMovement();
    }

    Vector3 finalVelocity, alignment, cohesion, seperation, farWallAvoidance, closeWallAvoidance, outOfBoundsAvoidance;
    void resetMovementVariables(){
        finalVelocity = new Vector3(0f,0f,0f);
        alignment = new Vector3(0,0,0);
        cohesion = new Vector3(0,0,0);
        seperation = new Vector3(0,0,0);
        farWallAvoidance = new Vector3(0,0,0);
        closeWallAvoidance = new Vector3(0,0,0);
        outOfBoundsAvoidance = new Vector3(0,0,0);
    }

    void calculateFlockMovement(){
        //Calculate the averages of cohesion, alignment and seperation and add them to create a final velocity which is then normalized.
        if(neighbourAmount > 0){
            alignment = sumOfNeighboursVelocities/neighbourAmount;
            alignment.Normalize();
            cohesion = (sumOfNeighboursPositions/neighbourAmount) - mainPosition;
            cohesion.Normalize();
        }
        else{
            //Make sure that final velocity isn't 0, so if the entity has no neighbours it will travel in a straight line, unless the object was stationary before.
            finalVelocity = mainVelocity;
        }
        if(tooCloseNeighbourAmount > 0){
            seperation += sumOfNeighboursDistances/tooCloseNeighbourAmount*-1;
        }
        seperation.Normalize();
    }

    float radius = 40f;
    void calculateStayInsideBoundsMovement(){
        Vector3 centerOffset = new Vector3(0,0,0) - mainEntity.transform.position;

        float percentageAwayFromCenter = centerOffset.magnitude / radius;
        if(percentageAwayFromCenter > 0.8f){
            //Multiply it by it's percantage so entities don't immediately start turning back.
            outOfBoundsAvoidance += centerOffset*percentageAwayFromCenter;
        }
        outOfBoundsAvoidance.Normalize();
        //Make the movement really small so entities don't bounce off the walls.
        outOfBoundsAvoidance *= 0.03f;   
    }

    void calculateWallAvoidanceMovement(){
        if(farWallAmount > 0){
            if(closeWallAmount > 0){
                closeWallAvoidance = sumOfCloseWallPositions / closeWallAmount * -1;
                closeWallAvoidance.Normalize();
            }
             farWallAvoidance = sumOfFarWallPositions / farWallAmount * -1;
             farWallAvoidance.Normalize();
             //Make the entity void walls very slightly so it doesn't look like it bounces off.
             farWallAvoidance *= 0.05f;
        }
    }

    void calculatePlayerAvoidance(){
        //Zero out the y coordinate so when red entities bounce off of player they won't go straight into the ground.
        playerPosition.y = 0;
        playerPosition.Normalize();
        playerPosition *= 2f;

        //If is red then make it 3 times more attracted to greens.
        if(mainType == 1){
            alignment *= 3f;
            cohesion *= 3f;
            //Also if touches player make red shoot out really far.
            mainEntity.speed += 7f*playerPosition.sqrMagnitude;
        }

        //Decrease the speed gradually after red entity bounces off really fast
        if(mainEntity.speed > 1f){
            mainEntity.speed *= .97f/(1f+Time.deltaTime);
        }
        else{
            mainEntity.speed = 1f;
        }
    }



    void setNewMovement(){
        resetMovementVariables();
        calculateFlockMovement();
        calculateStayInsideBoundsMovement();
        calculateWallAvoidanceMovement();
        calculatePlayerAvoidance();
        
        //Sum up all of the calculated values to create a final velocity vector.
        finalVelocity += alignment+cohesion+seperation+farWallAvoidance+closeWallAvoidance+outOfBoundsAvoidance+playerPosition;
        finalVelocity.Normalize();

        //Change the position of the object using the final velocity, then multiply it by 4 (if green) or 3 (if red) and also the speed so it can bounce off.
        mainEntity.transform.position = mainEntity.transform.position + finalVelocity*Time.deltaTime * (4-mainType) * mainEntity.speed;
        mainEntity.velocity = finalVelocity;
        mainEntity.type = mainType;
    }

    

    static float xLength = 130f, yLength = 65, zLength = 130, lengthOfChunk = 3f/4f;
    static Vector3 startingPoint = new Vector3(-65f,-5f,-65f);

    public void firstChunkAssign(){
        int x = Mathf.CeilToInt(Mathf.Abs(startingPoint.x - mainEntity.transform.position.x)/lengthOfChunk);
        int y = Mathf.CeilToInt(Mathf.Abs(startingPoint.y - mainEntity.transform.position.y)/lengthOfChunk);
        int z = Mathf.CeilToInt(Mathf.Abs(startingPoint.z - mainEntity.transform.position.z)/lengthOfChunk);
            
        //Add entity to current chunk.
        chunks[x][y][z].entities.Add(mainEntity);

        //Set new chunk information inside mainEntity. 
        mainEntity.x = x;
        mainEntity.y = y;
        mainEntity.z = z;
        mainEntity.indexInChunk = chunks[x][y][z].entities.Count-1;
    }

    /*
        I'm repeating a lot of lines of code here, but when i put them inside their own function it started lagging like crazy.
    */

    public void checkEntitiesChunkAndAssignItThere(){
        //Calculate entities chunk coordinates
        int x = Mathf.CeilToInt(Mathf.Abs(startingPoint.x - mainEntity.transform.position.x)/lengthOfChunk);
        int y = Mathf.CeilToInt(Mathf.Abs(startingPoint.y - mainEntity.transform.position.y)/lengthOfChunk);
        int z = Mathf.CeilToInt(Mathf.Abs(startingPoint.z - mainEntity.transform.position.z)/lengthOfChunk);

        //If the entity has changed it's chunk
        if(x != mainEntity.x || y != mainEntity.y || z != mainEntity.z){
            //And the new chunk is not out of bounds
            if(x >= 0 && x < xChunkAmount && z >= 0 && z < zChunkAmount && y >= 0 && y < yChunkAmount){
                
                //Remove entity from old chunk
                chunks[mainEntity.x][mainEntity.y][mainEntity.z].entities.RemoveAt(mainEntity.indexInChunk);

                //Change all of entities individual indexes inside the chunk
                for(int i = mainEntity.indexInChunk; i < chunks[mainEntity.x][mainEntity.y][mainEntity.z].entities.Count; i++){
                    chunks[mainEntity.x][mainEntity.y][mainEntity.z].entities[i].indexInChunk--;
                }
                //Add entity to current chunk.
                chunks[x][y][z].entities.Add(mainEntity);

                //Set new chunk information inside entity. 
                mainEntity.x = x; mainEntity.y = y; mainEntity.z = z;
                mainEntity.indexInChunk = chunks[x][y][z].entities.Count-1;
            }
        }
    }

    List<Chunk> previousChunks = new List<Chunk>();

    public void changeDynamicRodPoints(List<Vector3> rodPoints){

        //Delete all the rod verticies from the previous chunks.
        for(int i = 0; i < previousChunks.Count; i++){
            previousChunks[i].rod.Clear();
        }
        previousChunks.Clear();

        //For every verticy on the rod
        for(int i = 0; i < rodPoints.Count; i++){
            //Calculate verticies current chunk
            int x = Mathf.CeilToInt(Mathf.Abs(startingPoint.x - rodPoints[i].x)/lengthOfChunk);
            int y = Mathf.CeilToInt(Mathf.Abs(startingPoint.y - rodPoints[i].y)/lengthOfChunk);
            int z = Mathf.CeilToInt(Mathf.Abs(startingPoint.z - rodPoints[i].z)/lengthOfChunk);
            
            //Add the verticy to it's chunk
            chunks[x][y][z].rod.Add(rodPoints[i]);
            previousChunks.Add(chunks[x][y][z]);
        }
    }

    int xChunkAmount, yChunkAmount, zChunkAmount;
    void divideMapIntoChunks(){
        for(int x = 0; x < xChunkAmount; x++){
            chunks.Add(new List<List<Chunk>>());
            for(int y = 0; y < yChunkAmount; y++){
                chunks[x].Add(new List<Chunk>());
                for(int z = 0; z < zChunkAmount; z++){
                    chunks[x][y].Add(new Chunk(new List<Entity>(), new List<Vector3>()));
                }
            }
        }
    }

    void addStaticMapVerticiesToChunks(List<Vector3> mapPoints){
        for(int i = 0; i < mapPoints.Count; i++){
            //Calculate chunks coordinates.
            int x = Mathf.CeilToInt(Mathf.Abs(startingPoint.x - mapPoints[i].x)/lengthOfChunk);
            int y = Mathf.CeilToInt(Mathf.Abs(startingPoint.y - mapPoints[i].y)/lengthOfChunk);
            int z = Mathf.CeilToInt(Mathf.Abs(startingPoint.z - mapPoints[i].z)/lengthOfChunk);
            
            chunks[x][y][z].mapVerticies.Add(mapPoints[i]);
        }
    }


    void initializeStaticMapVerticies(List<Vector3> mapPoints){    
        divideMapIntoChunks();
        addStaticMapVerticiesToChunks(mapPoints);
    }

    public Mesh[] meshes;
    public GameObject mapObject;
    List<Vector3> calculatePositionOfStaticVectorsOnMesh(){
        List<Vector3> mapPoints = new List<Vector3>();
        Vector3 transformedVerticy;

        //For each mesh inside meshes array.
        for(int j = 0; j < meshes.Length; j++){
            Vector3[] vertices = meshes[j].vertices;
            //For each verticy inside the mesh.
            for(int i = 0; i < vertices.Length; i++){
                //Stretch them to fit actual size.
                vertices[i].x *= mapObject.transform.localScale.x;
                vertices[i].z *= mapObject.transform.localScale.z;
                vertices[i].y *= mapObject.transform.localScale.y;

                //Rotate them to fit maps rotation.
                transformedVerticy = mapObject.transform.rotation * vertices[i];
                transformedVerticy += mapObject.transform.localPosition;

                mapPoints.Add(transformedVerticy);
            }
        }   
        return mapPoints;
    }
}
