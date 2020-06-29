using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{



    // Start is called before the first frame update
    void Start()
    {
        

    }

    int speed = 10; //스피드 
    
    // Update is called once per frame
    void Update()
    {
       
            float xMove = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
            float yMove = Input.GetAxis("Vertical") * speed * Time.deltaTime; //y축으로 이동할양
            this.transform.Translate(new Vector3(xMove, yMove, 0));
  

    }
 }


