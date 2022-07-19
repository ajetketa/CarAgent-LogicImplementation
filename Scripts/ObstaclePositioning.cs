using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstaclePositioning
{
  
  //57 positions to fill
  //This variable shows whether a position has been taken
  private static bool[] positionsPlaced = new bool[57];

  //reset positionsPlaced
  public static void removePositions(){
      for(int i = 0; i<57; i++){
          positionsPlaced[i] = false;
      }
  }

  //Assign positions and rotations for every obstacle
  public static void assignObstaclePosition(GameObject[] obstacles){
      
      //Foreach Obstacle
      foreach(GameObject obstacle in obstacles ){

        //decide whether the obstacle will be put on the left or right
        //give an initial value for its position and rotation values
        int direction = Random.Range(0, 2);
        int index = Random.Range(0, 57);

        //If a specific position has been taken, try the others until a free
        //position
        while(positionsPlaced[index]){
            index = Random.Range(0, 57);
        }
        
        //Based on the direction and index, give position and rotation 
        if(direction == 0)
        {
            obstacle.transform.localPosition = StaticPositions.obstaclePositionRight[index];
            obstacle.transform.localRotation = Quaternion.Euler(StaticPositions.obstacleRotationRight[index]);
        }
        else
        {
            obstacle.transform.localPosition = StaticPositions.obstaclePositionLeft[index];
            obstacle.transform.localRotation = Quaternion.Euler(StaticPositions.obstacleRotationLeft[index]);
        }

        //Scale each obstacle
        obstacle.transform.localScale = new Vector3(0.8153f, 0.8153f, 0.8153f);

        //Get obstacleHandler
        Transform obstacleHandler = findChildWithTag(obstacle);

        //based on the direction, give obstacleHandler a suitable position
        if(direction == 0)
        {
            obstacleHandler.position = StaticPositions.obstaclePositionRight[index] + 3.5f*obstacleHandler.forward;
        }
        else
        {
            obstacleHandler.position = StaticPositions.obstaclePositionLeft[index] - 3.5f * obstacleHandler.forward;
        }

        //Set that index as taken
        positionsPlaced[index] = true;
      }

  }

  //find and return ObstacleHandler
  private static Transform findChildWithTag(GameObject parent){
    string tag = "ObstacleHandler";
    Transform childWithTag = null;

    //for every child that the obstacle has, check whether its tag is ObstacleHandler
    //if it is assign childWithTag
    foreach(Transform child in parent.transform){
        if(child.tag == tag){
            childWithTag = child;
            break;
        }
    }
    
    //return ObstacleHandler
    return childWithTag;
  }
}
