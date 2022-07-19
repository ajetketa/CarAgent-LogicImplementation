
using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;


public class CarAgent : Agent
    
{
   
    [Tooltip("Acceleration to apply when moving forwards")]
    public float forwardAcc = 10f;

    [Tooltip("Acceleration to apply when moving backwards")]
    public float backWardAcc =5f;
    
    [Tooltip("Acceleration to apply when breaking")]
    public float brakeAcc = 5f;


    [Tooltip("The deegre of strength on each rotation")]
    public float turnStrength=100f;

    [Tooltip("The platform where our mlagent is standing on")]
    public Transform platform;

    [Tooltip("CarObject MLAgent")]
    public Rigidbody carObject;
    
    [Tooltip("The minimum speed allowed for the agent!")]
    public float minStaticSpeed = 0;

    [Tooltip("The maximum speed allowed for the agent")]
    public float maxStaticSpeed = 0;

    [Tooltip("The active camera that will be tracking the agent's movements")]
    public Camera activeCamera;
   

    //The list of CheckPoints present on the Unity Scene
    GameObject [] CheckPoints;

    //Current speed of the agent
    float speed;
    
    //Record time from a specified event (important to keep the agent moving)
    private float time = 0;
    
    //The min speed allowed from agent during one itiration
    private float minSpeed;

    //The max speed allowed from agent during on itiration
    private float maxSpeed;

    //A list of Obstacles present on the Unity Scene
    private GameObject[] Obstacles;

    //A Dictionary that links a checkPoint with its properties
    private Dictionary<GameObject, CheckPointsProperty> checkPointProperty = new Dictionary<GameObject, CheckPointsProperty>();

    //A Dictionary that links an obstacle with its properties
    private Dictionary<GameObject, ObstacleProperty> obstacleProperty = new Dictionary<GameObject, ObstacleProperty>(); 
    
    //Stores the nearest checkpoint to the agent
    private GameObject nearestCheckPoint;

    //Stores the nearest obstacle to the agent
    private GameObject nearestObstacle;

    

    //As soon as the scene loads Awake is called automatically
    private void Awake() {

        //Get CheckPoints from the Unity Scene
        if(CheckPoints == null){
            CheckPoints = GameObject.FindGameObjectsWithTag("CheckPoint");
        }

        //Get Obstacles from the Unity Scene
        if(Obstacles == null){
            Obstacles = GameObject.FindGameObjectsWithTag("Obstacle");
            
        }
        
        //For each checkpoint create a CheckPointsProperty object and store them in the checkPointProperty dictionary 
        foreach(GameObject checkpoint in CheckPoints){
            checkPointProperty.Add(checkpoint, new CheckPointsProperty());
        }

        //For each Obstacle create an ObstacleProperty object and store them in the obstaclPropery dictionary
        foreach(GameObject obstacle in Obstacles){
            obstacleProperty.Add(obstacle, new ObstacleProperty());
        }
    }

    /*Initialize is run once, once the Awake function has ended*/
    public override void Initialize()
    {
        //Put the car agent on a specific position, rotation and give it a speed of 0*/
        transform.localPosition = new Vector3(551f, 2.27f, 158f);
        transform.localRotation = Quaternion.Euler(new Vector3(0,314f, 0));
        speed = 0f;

        /*If maxStaticSpeed has been changed, assign its value to maxSpeed,
        else assign a random value*/
        if(maxStaticSpeed != 0){
            maxSpeed = maxStaticSpeed;
        }else{
            maxSpeed = Random.Range(.8f, 1.4f);
        }

        /*If minStaticSpeed has been changed, assing its value to minSpeed,
        else assign a random value*/
        if(minStaticSpeed != 0){
            minSpeed = minStaticSpeed;
        }else{
            minSpeed = Random.Range(-.5f, -0.01f);
        }

        //Find and assign nearest non visitet checkpoint
        nearestNonVisitedCheckPoint();

        //Find and assign the nearest non Encountered obstacle
        nearestNonEncounteredObstacle();
    }

    //Reset the agent when the Episode begins
    public override void OnEpisodeBegin()
    {
        //Reset position,rotation and speed of agent
        transform.localPosition = new Vector3(551f, 2.27f, 158f);
        transform.localRotation = Quaternion.Euler(new Vector3(0,314f, 0));
        speed = 0f;

        //Reset the timer
        time = 0;
     
        //Reset Properties for each checkpoint
        foreach (GameObject checkpoint in CheckPoints){
          checkPointProperty[checkpoint].setVisited(false);
        }

        //Reset Properties for each obstacle
        foreach (GameObject obstacle in Obstacles){
            obstacleProperty[obstacle].setVisited(false);
        }

        //If maxStaticSpeed is changed, assign its value to maxSpeed, else assign random
        if(maxStaticSpeed != 0){
            maxSpeed = maxStaticSpeed;
        }else{
            maxSpeed = Random.Range(.8f, 1.4f);
        }

        //If minStaticSpeed is changed, assign its value to minSpeed, else assign random
        if(minStaticSpeed != 0){
            minSpeed = minStaticSpeed;
        }else{
            minSpeed = Random.Range(-.5f, -0.01f);
        }

        //Find and assign nearest non visited checkpoint
        nearestNonVisitedCheckPoint();
        
        //Remove all Obstacles
        ObstaclePositioning.removePositions();

        //Reassign new random values for the obstacles
        ObstaclePositioning.assignObstaclePosition(Obstacles);

        //Find and assign nearest non encountered obstacle
        nearestNonEncounteredObstacle();

    }


    //Collect vector observation from the environment
    public override void CollectObservations(VectorSensor sensor)
    {
        //Pass the position of our car (3 observations)
        sensor.AddObservation(transform.localPosition.normalized);

        //Pass the rotation of our car (4 observations)
        sensor.AddObservation(transform.localRotation.normalized);

        //Pass the current velocity of our car (3 observations)
        sensor.AddObservation(carObject.velocity.normalized);

        //Pass the difference of angles between object and nearest non visited checkpoint (1 observation)
        sensor.AddObservation(Vector3.Dot(transform.forward, nearestCheckPoint.transform.forward));

        //Pass the position of currently nearest non encountered obstacle (3 observations)
        sensor.AddObservation(nearestObstacle.transform.localPosition.normalized);

        //Pass the rotation of currently nearest non encountered obstacle (4 observations)
        sensor.AddObservation(nearestObstacle.transform.localRotation.normalized);
        
        //Total of 18 observations
    }

    /*
    When an action is recieved from either the player input or the neural network

    the actions.Continuous[i] represents:
    Index 0: moving  (+1 = forward, -1 = backward)
    Index 1: rotating (+1 = left, -1 = right)
    Index 2: brake (only on positive values)
    */
    public override void OnActionReceived(ActionBuffers actions)
    {   
        
        float speedInput = actions.ContinuousActions[0];
        float turnInput = actions.ContinuousActions[1];
        float brakeInput = actions.ContinuousActions[2];
        float acc = 0;

        //based on the speed input, decide whether the car needs to go forward or backwards
        if(speedInput > 0){
            acc = forwardAcc;
        }
        if(speedInput < 0){
           acc = backWardAcc;
        }
       
       //The agent can not turn unless it has velocity
        if(Vector3.Magnitude(carObject.velocity) == 0){
            turnInput = 0;
        }
        
        //hitting the breakes decreases the speed to zero
        if(brakeInput > 0){
            speed -= brakeInput * brakeAcc;

            if(speed < 0) speed = 0;
        }
        
        //find the new speed the agent has to have 
        speed += speedInput * acc * Time.deltaTime;
        
        //limit the speed between the values minspeed and maxspeed
        speed = Mathf.Clamp(speed, minSpeed, maxSpeed);
        
       //if the agent leaves the track, punish it and end the current episode 
       if(!isGrounded()){
           speed = 0;
           AddReward(-1f);
           EndEpisode();
       }
       
        //Fix agent's rotation in reference to the platform
        if(speed != 0) transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles + new Vector3(0f, turnInput * turnStrength * Time.deltaTime,0f));
        
        //assign agents' position based on its speed
        transform.localPosition += transform.forward * speed;
    }
    
    
    // when behaviour type is set to heuristic only on agent beheviour parameters,
    // this function will be called, the values will be returned to onActionReceived.
    public override void Heuristic(in ActionBuffers actionsOut){
        
        //Three parameters of movements
        float move = 0;
        float turn = 0;
        float brake = 0;
        
        //Assign moving to up and down keys
        if(Input.GetKey(KeyCode.UpArrow)) move = .5f;
        else if(Input.GetKey(KeyCode.DownArrow)) move =-.5f;

        //Assign rotation to left and right key
        if(Input.GetKey(KeyCode.LeftArrow)) turn = -.5f;
        else if(Input.GetKey(KeyCode.RightArrow)) turn = .5f;

        //Assign braking to space key
        if(Input.GetKey(KeyCode.Space)) brake = 1f;

        //Send the actions
        ActionSegment<float> actions = actionsOut.ContinuousActions;
        actions[0] = move;
        actions[1] = turn;
        actions[2] = brake;
    }
   
    // Called when the agent's collider enters a trigger collider
    private void OnTriggerEnter(Collider other){

        //If the agent hits a checkpoint
        if(other.tag=="CheckPoint")
        {
           //if the checkpoint has been hit on this itiration
           //punish it and end the episode 
           if(checkPointProperty[other.gameObject].getVisited())
           {
               AddReward(-0.4f);
               EndEpisode();
           }
           else
           {    
                //reset timer, give it a reward
                time = 0;
                AddReward(1);
                
                //Set checkpoint as visited
                checkPointProperty[other.gameObject].setVisited(true);

                //Find the nearest non visited checkpoint
                nearestNonVisitedCheckPoint();
           }
        }

        //If the agent hit an CheckPoint Enemy, punish it and end the episode
        if(other.tag == "CheckPointEnemy"){
            AddReward(-1);
            EndEpisode();
        }
        
        //If the agent fell off the track, punish it and end the episode
        if(other.tag=="TerrainObject"){
            AddReward(-1);
            EndEpisode();
        }
        
        //If the agent managed to dodge an obstacle
        if(other.tag == "ObstacleHandler"){
            
            //set obstacle as visited
            obstacleProperty[other.transform.parent.gameObject].setVisited(true);

            //give it a reward
            AddReward(1);

            //Find and assign nearest non encountered obstacle
            nearestNonEncounteredObstacle();
        }


    }

    //called when the agent enters a collision collider
    private void OnCollisionEnter(Collision other) {
        
        //if agent hit an obstacle, punish it and end the episode
        if(other.gameObject.tag == "Obstacle"){
            AddReward(-1);
            EndEpisode();
        }

    }

    //called each itiration the agent stays in a trigger collider
    private void OnTriggerStay(Collider other) {

        //increase the time for each itiration 
        time += Time.deltaTime;

        //give reward if it is staying in the Final Checkpoint
        if(other.tag == "FinalCheckpoint"){
            AddReward(.0001f);
        }

        //If 1 sec has passed and the agent is not staying in the final checkpoint
        //give it a punishment and end the episode
        if(time > 1){
            if(other.tag != "FinalCheckpoint") AddReward(-0.7f);
            EndEpisode();
        }
    }

  
    //returns true if the platform's and agent's y-axis form a deegre
    //of bigger than 85deg, false otherwise
    private bool isGrounded(){
    return Vector3.Dot(transform.up, platform.up) > 0.9;
    }

    //Checks for the nearestnonvisitedcheckpoint and assigns it to nearestCheckPoint
    private void nearestNonVisitedCheckPoint(){
        float minDistance = 10000000f;
        
        //loop through every checkpoint and find the nearest non visited checkpoint
        foreach(GameObject checkPoint in CheckPoints){
            if(!checkPointProperty[checkPoint].getVisited())
            {
               float distance = Mathf.Abs(Vector3.Distance(transform.position, checkPoint.transform.position));

               if(distance < minDistance){
                   minDistance = distance;
                   nearestCheckPoint = checkPoint;
               }
            }
        }

    }

    //Checks for the nearest non encountered obstacle and assigns it to nearestObstacle
    private void nearestNonEncounteredObstacle(){
        float minDistance = 100000000f;
        
        //loop through every obstacle and find the nearest non encountered obstacle
        foreach(GameObject obstacle in Obstacles){
            if(!obstacleProperty[obstacle].getVisited()){
                float distance = Mathf.Abs(Vector3.Distance(transform.position, obstacle.transform.position));

                if(distance < minDistance){
                    minDistance = distance;
                    nearestObstacle = obstacle;
                }
            }
        }
    }

}
