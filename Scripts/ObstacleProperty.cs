using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleProperty
{

    //Stores whether the obstacle is visited or not
   private bool isVisited = false;

   //set isVisited
   public void setVisited(bool visited){
       isVisited = visited;
   }

   //get isVisited
   public bool getVisited(){
       return isVisited;
   }

}
