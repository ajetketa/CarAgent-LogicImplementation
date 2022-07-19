using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointsProperty 
{
   //Controls whether this checkpoint has been visited before
   private bool visited = false;
   
   //Set checkpoint as visited or not visited
   public void setVisited(bool visitedCP){
       visited = visitedCP;
   }

   //get visited
   public bool getVisited(){
       return visited;
   }
}
